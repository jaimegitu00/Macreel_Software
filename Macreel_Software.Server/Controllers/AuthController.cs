using Azure.Core;
using Macreel_Software.DAL;
using Macreel_Software.DAL.Auth;
using Macreel_Software.Models;
using Macreel_Software.Services.MailSender;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Macreel_Software.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthServices _authServices;
        private readonly JwtTokenProvider _jwtProvider;
        //private readonly IMemoryCache _cache;
        //private readonly OTPVerificationService _otpService;
        private readonly MailSender _mailservice;
        private readonly PasswordEncrypt _pass;

        public AuthController(IAuthServices authServices,JwtTokenProvider jwtProvider, PasswordEncrypt pass, MailSender sender)
        {
            _authServices = authServices;
            _jwtProvider = jwtProvider;
            //_otpService = otpService;
            _mailservice =sender;
            _pass=pass;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { Status = 400, Message = "Username and password are required." });
            }

            var user = await _authServices.ValidateUserAsync(model.UserName, model.Password);

            if (user == null)
                return Unauthorized(new { Status = 401, Message = "Invalid username or password." });

            var accessToken = _jwtProvider.CreateToken(user);
            var refreshToken = _jwtProvider.GenerateRefreshToken();
            var refreshExpire = DateTime.UtcNow.AddDays(2);

            await _authServices.SaveRefreshTokenAsync(user.UserId, refreshToken, refreshExpire);

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(2)
            });
            var response = new LoginResponse
            {
                UserId = user.UserId,
                Role = user.Role,
                UserName=user.Username,
                Name=user.Name,
                RefreshToken = refreshToken,
                AccessToken = accessToken,
                RefreshTokenExpire = refreshExpire
            };

            return Ok(new
            {
                Status = 200,
                Message = "Login successful",
                Data = response
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.RefreshToken))
                return Unauthorized("Refresh token missing");

            var tokenData = await _authServices.GetRefreshTokenAsync(model.RefreshToken);

            if (tokenData == null || tokenData.Expiry < DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token");

            var user = await _authServices.GetUserByIdAsync(tokenData.UserId);

            if (user == null)
                return Unauthorized();

            var newAccessToken = _jwtProvider.CreateToken(user);

            var newRefreshToken = _jwtProvider.GenerateRefreshToken();
            var refreshExpire = DateTime.UtcNow.AddDays(2);

            await _authServices.SaveRefreshTokenAsync(
                user.UserId,
                newRefreshToken,
                refreshExpire);
            var response = new LoginResponse
            {
                UserId = user.UserId,
                Role = user.Role,
                UserName = user.Username,
                Name = user.Name,
                RefreshToken = newRefreshToken,
                AccessToken = newAccessToken,
                RefreshTokenExpire = refreshExpire
            };
            return Ok(new
            {
                Status = 200,
                Message = "Token refreshed",
                Data =response
            });
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
                await _authServices.RevokeRefreshTokenAsync(refreshToken);

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            return Ok();
        }
        [HttpGet("me")]
        public IActionResult Me()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                userId,
                role
            });
        }


        #region forget password

        //private string GetFlowId()
        //{
        //    return Request.Headers["X-Flow-Id"].FirstOrDefault();
        //}


        //[HttpPost("get-otp")]
        //public async Task<IActionResult> GetOtp([FromBody] ForgetPasswordRequest data)
        //{
        //    try
        //    {
        //        if (data == null || string.IsNullOrWhiteSpace(data.Email))
        //            return BadRequest(new { status = false, message = "Email is required" });

        //        var user = await _authServices.CheckUserExistOrNot(data.Email);
        //        if (user == null)
        //            return NotFound(new { status = false, message = "User not found" });

        //        var (flowId, otp) = await _otpService.GenerateOtpAsync(data.Email);

        //        var mailStatus = await _mailservice.SendMailAsync(new MailRequest
        //        {
        //            ToEmail = data.Email,
        //            Subject = "OTP for Password Reset",
        //            BodyType = MailBodyType.ForgotPassword,
        //            otp = otp
        //        });

        //        if (mailStatus == null)
        //            return StatusCode(500, new { status = false, message = "Failed to send OTP email" });

        //        return Ok(new { status = true, message = "OTP sent successfully", flowId });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = "Something went wrong", error = ex.Message });
        //    }
        //}

        //[HttpPost("verify-otp")]
        //public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest data)
        //{
        //    try
        //    {
        //        if (data == null || string.IsNullOrWhiteSpace(data.Otp))
        //            return BadRequest(new { status = false, message = "OTP is required" });

        //        var flowId = GetFlowId();
        //        if (string.IsNullOrEmpty(flowId))
        //            return BadRequest(new { status = false, message = "FlowId missing" });

        //        bool isValid = await _otpService.VerifyOtpAsync(flowId, data.Otp);
        //        if (!isValid)
        //            return BadRequest(new { status = false, message = "Incorrect or expired OTP" });

        //        return Ok(new { status = true, message = "OTP verified successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = "Something went wrong", error = ex.Message });
        //    }
        //}

        //[HttpPost("reset-password")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest data)
        //{
        //    try
        //    {
        //        if (data == null || string.IsNullOrWhiteSpace(data.NewPassword))
        //            return BadRequest(new { status = false, message = "New password is required" });

        //        var flowId = GetFlowId();
        //        if (string.IsNullOrEmpty(flowId))
        //            return BadRequest(new { status = false, message = "FlowId missing" });

        //        bool isVerified = await _otpService.IsFlowVerifiedAsync(flowId);
        //        if (!isVerified)
        //            return BadRequest(new { status = false, message = "OTP not verified" });

        //        string email = await _otpService.GetEmailByFlowIdAsync(flowId);
        //        if (string.IsNullOrEmpty(email))
        //            return BadRequest(new { status = false, message = "Session expired" });

        //        int? userId = await _authServices.GetUserIdByEmailId(email);
        //        if (userId == null)
        //            return BadRequest(new { status = false, message = "User not found" });

        //        var encryptedPassword = _pass.EncryptPassword(data.NewPassword);
        //        var result = await _authServices.UpdatePassword(encryptedPassword, userId);
        //        if (!result)
        //            return BadRequest(new { status = false, message = "Password update failed" });

        //        await _otpService.ClearFlowAsync(flowId);

        //        return Ok(new { status = true, message = "Password updated successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = "Something went wrong", error = ex.Message });
        //    }
        //}
    }

    #endregion
}


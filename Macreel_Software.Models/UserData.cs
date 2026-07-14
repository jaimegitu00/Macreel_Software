namespace Macreel_Software.Models
{
    public class UserData
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }

    }
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpire { get; set; }
        public string Role { get; set; }
    }
    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string? IpAddress { get; set; } 
        public string? WifiName { get; set; } 
    
    }
    public class LoginRequestDto
    {
        public LoginRequest Login { get; set; }
        public string DeviceId { get; set; }
    }
    public class MailSettings
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
    public class CommonMessage
    {
        public bool Status { get; set; }
        public string Message { get; set; }
    }

    public class ForgetPasswordRequest
    {
        public string Email { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Otp { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; }
    }
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

}

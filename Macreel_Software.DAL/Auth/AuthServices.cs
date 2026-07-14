using System.Data;
using System.Security.Cryptography.Pkcs;
using Macreel_Software.Models;
using Macreel_Software.Services.MailSender;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using static Macreel_Software.DAL.Auth.main;

namespace Macreel_Software.DAL.Auth
{
    public class AuthServices : IAuthServices
    {
        private readonly IConfiguration _config;
        private readonly PasswordEncrypt _pass;

        public AuthServices(IConfiguration config, PasswordEncrypt pass)
        {
            _config = config;
            _pass = pass;
        }

        public async Task<UserData?> ValidateUserAsync(string userName, string enteredPassword)
        {
            UserData? user = null;

            try
            {
                using SqlConnection con =
                    new SqlConnection(_config.GetConnectionString("DefaultConnection"));

                using SqlCommand cmd = new SqlCommand("sp_Login", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserName", userName);
                cmd.Parameters.AddWithValue("@Action", "LOGIN");

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();

                if (await dr.ReadAsync())
                {
                    string encryptedDbPassword = dr["Password"].ToString()!;
                    string decryptedDbPassword = _pass.DecryptPassword(encryptedDbPassword);

                 
                    if (decryptedDbPassword == enteredPassword)
                    {
                        user = new UserData
                        {
                            UserId = dr["UserId"] != DBNull.Value? Convert.ToInt32(dr["UserId"]):0,
                            Username = dr["UserName"].ToString()!,
                            Role = dr["roleName"].ToString()!.ToLower(),
                            Name = dr["Name"].ToString()!.ToLower(),
                            
                        };
                    }
                }
            }
            catch
            {
                return null;
            }

            return user;
        }


        public async Task<bool> SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiry)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                using SqlCommand cmd = new SqlCommand("sp_Login", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@RefreshToken", refreshToken);
                cmd.Parameters.AddWithValue("@ExpireDate", expiry);
                cmd.Parameters.AddWithValue("@Action", "UPDATE_REFRESH");

                await con.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected > 0; 
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<RefreshTokenData?> GetRefreshTokenAsync(string refreshToken)
        {
            RefreshTokenData? tokenData = null;

            try
            {
                using SqlConnection con =
                    new SqlConnection(_config.GetConnectionString("DefaultConnection"));

                using SqlCommand cmd = new SqlCommand("sp_Login", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@RefreshToken", refreshToken);
                cmd.Parameters.AddWithValue("@Action", "GET_REFRESH");

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();

                if (await dr.ReadAsync())
                {
                    tokenData = new RefreshTokenData
                    {
                        UserId = Convert.ToInt32(dr["UserId"]),
                        RefreshToken = dr["RefreshToken"].ToString()!,
                        Expiry = Convert.ToDateTime(dr["RefreshTokenExpire"])
                    };
                }
            }
            catch
            {
                return null;
            }

            return tokenData;
        }
        public async Task<UserData?> GetUserByIdAsync(int userId)
        {
            UserData? user = null;

            try
            {
                using SqlConnection con =
                    new SqlConnection(_config.GetConnectionString("DefaultConnection"));

                using SqlCommand cmd = new SqlCommand("sp_Login", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Action", "GET_USER_BY_ID");

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();

                if (await dr.ReadAsync())
                {
                    user = new UserData
                    {
                        UserId = dr["UserId"] != DBNull.Value ? Convert.ToInt32(dr["UserId"]) : 0,
                        Username = dr["UserName"].ToString()!,
                        Role = dr["roleName"].ToString()!.ToLower(),
                        Name = dr["Name"].ToString()!.ToLower(),

                    };
                }
            }
            catch
            {
                return null;
            }

            return user;
        }
        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                using SqlConnection con =
                    new SqlConnection(_config.GetConnectionString("DefaultConnection"));

                using SqlCommand cmd = new SqlCommand("sp_Login", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@RefreshToken", refreshToken);
                cmd.Parameters.AddWithValue("@Action", "REVOKE_REFRESH");

                await con.OpenAsync();
                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0;
            }
            catch
            {
                return false;
            }
        }
        public async Task<int?> CheckUserExistOrNot(string email)
        {
            int? user = null;

            using SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            try
            {
                using SqlCommand cmd = new SqlCommand("sp_Login", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@emailId", email);
                cmd.Parameters.AddWithValue("@action", "CheckUserByEmail"); 

                if (con.State == ConnectionState.Closed)
                    await con.OpenAsync();

                using SqlDataReader sdr = await cmd.ExecuteReaderAsync();
                if (await sdr.ReadAsync())
                {
                    user = sdr["UserExists"] != DBNull.Value? Convert.ToInt32(sdr["UserExists"]): null;
                }
            }
            catch (Exception)
            {
                throw; 
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    await con.CloseAsync();
            }

            return user;
        }

        public async Task<int?> GetUserIdByEmailId(string email)
        {
            int? userId = null;
            using SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            try
            {
                using SqlCommand cmd = new SqlCommand("sp_Login", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@action", "GetUserIdByEmail");
                cmd.Parameters.AddWithValue("@emailId", email);
                if (con.State == ConnectionState.Closed)
                    await con.OpenAsync();

                using(SqlDataReader sdr=await cmd.ExecuteReaderAsync())
                {
                    if(sdr.HasRows)
                    {
                        while(await sdr.ReadAsync())
                        {
                            userId = sdr["id"] != DBNull.Value ? Convert.ToInt32(sdr["id"]) : null;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                throw;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    await con.CloseAsync();
            }
            return userId;

        }

        public async Task<bool> UpdatePassword(string encryptedPassword, int? userId)
        {
            using SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            try
            {
                using SqlCommand cmd = new SqlCommand("sp_Login", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@action", "UpdatePasswordById");
                cmd.Parameters.AddWithValue("@Password", encryptedPassword);
                cmd.Parameters.AddWithValue("@UserId", userId);

                if (con.State == ConnectionState.Closed)
                    await con.OpenAsync();

                int res = await cmd.ExecuteNonQueryAsync();
                return res > 0;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

    }
}

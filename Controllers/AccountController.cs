using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Data.SqlClient;
using System.Security.Cryptography;
using BumbleBeeFoundation_API.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BumbleBeeFoundation_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration configuration, ILogger<AccountController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Check the database for the user credentials, then allow them to log in

        // POST api/account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string userQuery = "SELECT UserID, Password, Role, FirstName, LastName FROM Users WHERE Email = @Email";
                using (SqlCommand command = new SqlCommand(userQuery, connection))
                {
                    command.Parameters.AddWithValue("@Email", model.Email);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string storedPassword = reader["Password"].ToString();
                            if (VerifyPassword(model.Password, storedPassword))
                            {
                                int userId = reader.GetInt32(reader.GetOrdinal("UserID"));
                                string role = reader.GetString(reader.GetOrdinal("Role"));
                                string firstName = reader.GetString(reader.GetOrdinal("FirstName"));
                                string lastName = reader.GetString(reader.GetOrdinal("LastName"));
                                reader.Close();

                                // For Company role
                                if (role == "Company")
                                {
                                    string companyQuery = "SELECT CompanyID, CompanyName FROM Companies WHERE UserID = @UserID";
                                    using (SqlCommand companyCommand = new SqlCommand(companyQuery, connection))
                                    {
                                        companyCommand.Parameters.AddWithValue("@UserID", userId);
                                        using (SqlDataReader companyReader = await companyCommand.ExecuteReaderAsync())
                                        {
                                            if (await companyReader.ReadAsync())
                                            {
                                                return Ok(new LoginResponse
                                                {
                                                    UserId = userId,
                                                    Role = role,
                                                    CompanyID = companyReader.GetInt32(companyReader.GetOrdinal("CompanyID")),
                                                    CompanyName = companyReader.GetString(companyReader.GetOrdinal("CompanyName")),
                                                    UserEmail = model.Email,
                                                    FirstName = firstName,
                                                    LastName = lastName
                                                });
                                            }
                                            return BadRequest("Company ID not found.");
                                        }
                                    }
                                }

                                // For all other roles (Donor, Admin)
                                return Ok(new LoginResponse
                                {
                                    UserId = userId,
                                    Role = role,
                                    UserEmail = model.Email,
                                    CompanyID = null,
                                    CompanyName = null,
                                    FirstName = firstName,
                                    LastName = lastName
                                });
                            }
                            return Unauthorized("Invalid login attempt.");
                        }
                        return Unauthorized("Invalid login attempt.");
                    }
                }
            }
        }

        // Allow a user to register, then add their details to the database

        // POST api/account/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            _logger.LogInformation($"Registration attempt - Role: {model.Role}, Email: {model.Email}");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string userQuery = @"INSERT INTO Users (FirstName, LastName, Email, Password, Role, CreatedAt) 
                                         VALUES (@FirstName, @LastName, @Email, @Password, @Role, GETDATE());
                                         SELECT SCOPE_IDENTITY();";
                        int userId;
                        using (SqlCommand command = new SqlCommand(userQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@FirstName", model.FirstName);
                            command.Parameters.AddWithValue("@LastName", model.LastName);
                            command.Parameters.AddWithValue("@Email", model.Email);
                            command.Parameters.AddWithValue("@Password", HashPassword(model.Password));
                            command.Parameters.AddWithValue("@Role", model.Role);

                            userId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        if (model.Role == "Company")
                        {
                            string companyQuery = @"INSERT INTO Companies (CompanyName, ContactEmail, ContactPhone, Description, DateJoined, Status, UserID) 
                                                VALUES (@CompanyName, @ContactEmail, @ContactPhone, @Description, GETDATE(), 'Pending', @UserID)";
                            using (SqlCommand command = new SqlCommand(companyQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@CompanyName", model.CompanyName);
                                command.Parameters.AddWithValue("@ContactEmail", model.Email);
                                command.Parameters.AddWithValue("@ContactPhone", model.ContactPhone);
                                command.Parameters.AddWithValue("@Description", model.CompanyDescription);
                                command.Parameters.AddWithValue("@UserID", userId);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        return Ok("Registration successful.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Error during registration process");
                        return BadRequest("Registration failed. Please try again.");
                    }
                }
            }
        }

        // Allow a user to reset their password

        // POST: api/account/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT UserID FROM Users WHERE Email = @Email";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", model.Email);
                    var userId = await command.ExecuteScalarAsync();

                    if (userId != null)
                    {
                        return Ok(new { message = "Email found. Please proceed to reset password." });
                    }
                    else
                    {
                        return NotFound("Email not found.");
                    }
                }
            }
        }

        // Reset the password in the database

        // POST: api/account/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "UPDATE Users SET Password = @Password WHERE Email = @Email";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Password", HashPassword(model.NewPassword));
                    command.Parameters.AddWithValue("@Email", model.Email);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok("Password reset successfully.");
        }


        // Helper methods for password hashing and verification
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            string hashedInput = HashPassword(inputPassword);
            return string.Equals(hashedInput, storedPassword);
        }
    }
}

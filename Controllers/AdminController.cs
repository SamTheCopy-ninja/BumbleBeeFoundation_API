using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation_API.Models;
using System.Text;
using System.Reflection.Metadata;
using Document = BumbleBeeFoundation_API.Models.Document;
using BumbleBeeFoundation_Client;

namespace BumbleBeeFoundation_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly string _connectionString;

        public AdminController(IConfiguration configuration, ILogger<AdminController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Get all dashboard details from the database for the admin

        // GET: api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var dashboardViewModel = new DashboardViewModel();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    command.CommandText = "SELECT COUNT(*) FROM Users";
                    dashboardViewModel.TotalUsers = (int)await command.ExecuteScalarAsync();

                    command.CommandText = "SELECT COUNT(*) FROM Companies";
                    dashboardViewModel.TotalCompanies = (int)await command.ExecuteScalarAsync();

                    command.CommandText = "SELECT COUNT(*) FROM Donations";
                    dashboardViewModel.TotalDonations = (int)await command.ExecuteScalarAsync();

                    command.CommandText = "SELECT COUNT(*) FROM FundingRequests";
                    dashboardViewModel.TotalFundingRequests = (int)await command.ExecuteScalarAsync();
                }
            }

            return Ok(dashboardViewModel);
        }

        // User Management portion of the API
        // Fetch a list of all the users

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = new List<User>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT * FROM Users", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            });
                        }
                    }
                }
            }

            return Ok(users);
        }

        // Get details for a specific user

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            User user = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Users WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new User
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            };
                        }
                    }
                }
            }

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // Allow an admin to create a user

        // POST: api/admin/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO Users (FirstName, LastName, Email, Password, Role) VALUES (@FirstName, @LastName, @Email, @Password, @Role)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@LastName", user.LastName);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Password", user.Password);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.UserID }, user);
        }

        // Allow an admin to edit user details

        // PUT: api/admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] UserForEdit userForEdit)
        {
            if (id != userForEdit.UserID)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Users SET FirstName = @FirstName, LastName = @LastName, Email = @Email, Role = @Role WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userForEdit.UserID);
                    command.Parameters.AddWithValue("@FirstName", userForEdit.FirstName);
                    command.Parameters.AddWithValue("@LastName", userForEdit.LastName);
                    command.Parameters.AddWithValue("@Email", userForEdit.Email);
                    command.Parameters.AddWithValue("@Role", userForEdit.Role);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return NoContent();
        }

        // Allow an admin to delete a user

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if the user is associated with a company
                        string checkCompanySql = "SELECT COUNT(*) FROM Companies WHERE UserID = @UserID";
                        using (var checkCommand = new SqlCommand(checkCompanySql, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@UserID", id);
                            int companyCount = (int)await checkCommand.ExecuteScalarAsync();

                            // If the user is associated with a company, delete the company record
                            if (companyCount > 0)
                            {
                                string deleteCompanySql = "DELETE FROM Companies WHERE UserID = @UserID";
                                using (var deleteCompanyCommand = new SqlCommand(deleteCompanySql, connection, transaction))
                                {
                                    deleteCompanyCommand.Parameters.AddWithValue("@UserID", id);
                                    await deleteCompanyCommand.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // Delete the user record
                        string deleteUserSql = "DELETE FROM Users WHERE UserID = @UserID";
                        using (var deleteUserCommand = new SqlCommand(deleteUserSql, connection, transaction))
                        {
                            deleteUserCommand.Parameters.AddWithValue("@UserID", id);
                            await deleteUserCommand.ExecuteNonQueryAsync();
                        }

                        // Commit the transaction
                        transaction.Commit();
                        return NoContent();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Error occurred while deleting user with ID: {UserID}", id);
                        return StatusCode(500, "Internal server error while deleting user.");
                    }
                }
            }
        }




        // Company Management portion of the API
        // Get a list of all the companies for the admin

        // GET: api/admin/companies
        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = new List<Company>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT * FROM Companies", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            companies.Add(new Company
                            {
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("RejectionReason"))
                            });
                        }
                    }
                }
            }

            return Ok(companies); 
        }

        // Fetch company details for a specific company

        // GET: api/admin/companies/{id}
        [HttpGet("companies/{id}")]
        public async Task<IActionResult> GetCompanyDetails(int id)
        {
            Company company = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Companies WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            company = new Company
                            {
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("RejectionReason"))
                            };
                        }
                    }
                }
            }

            if (company == null)
            {
                return NotFound();
            }

            return Ok(company); 
        }

        // Allow an admin to approve a company

        // POST: api/admin/companies/approve/{id}
        [HttpPost("companies/approve/{id}")]
        public async Task<IActionResult> ApproveCompany(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Companies SET Status = 'Approved' WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok(new { message = "Company approved successfully." });
        }

        // Allow an admin to reject a company

        // POST: api/admin/companies/reject/{id}
        [HttpPost("companies/reject/{id}")]
        public async Task<IActionResult> RejectCompany(int id, [FromBody] string rejectionReason)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Companies SET Status = 'Rejected', RejectionReason = @RejectionReason WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    command.Parameters.AddWithValue("@RejectionReason", rejectionReason ?? string.Empty);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok(new { message = "Company rejected with reason: " + rejectionReason });
        }


        // Donation Management portion of the API

        // Fetch list of all donations
        // GET: api/donations
        [HttpGet("donations")]
        public async Task<ActionResult<IEnumerable<Donation>>> GetDonations()
        {
            var donations = new List<Donation>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                    SELECT d.*, c.CompanyName 
                    FROM Donations d 
                    LEFT JOIN Companies c ON d.CompanyID = c.CompanyID", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                donations.Add(new Donation
                                {
                                    DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                    CompanyID = reader.IsDBNull(reader.GetOrdinal("CompanyID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                    CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName")) ? null : reader.GetString(reader.GetOrdinal("CompanyName")),
                                    DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                    DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                    DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                    DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                    DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail")),
                                    PaymentStatus = reader.IsDBNull(reader.GetOrdinal("PaymentStatus")) ? null : reader.GetString(reader.GetOrdinal("PaymentStatus")),
                                    DocumentFileName = reader.IsDBNull(reader.GetOrdinal("DocumentPath")) ? null : "Attached Document"
                                });
                            }
                        }
                    }
                }
                return Ok(donations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donations");
                return StatusCode(500, "Internal server error while retrieving donations");
            }
        }

        // Get information about a specific donation

        // GET: api/donations/{id}
        [HttpGet("donations/{id}")]
        public async Task<ActionResult<Donation>> GetDonation(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                    SELECT d.*, c.CompanyName 
                    FROM Donations d 
                    LEFT JOIN Companies c ON d.CompanyID = c.CompanyID 
                    WHERE d.DonationID = @DonationID";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@DonationID", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var donation = new Donation
                                {
                                    DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                    CompanyID = reader.IsDBNull(reader.GetOrdinal("CompanyID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                    CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName")) ? null : reader.GetString(reader.GetOrdinal("CompanyName")),
                                    DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                    DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                    DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                    DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                    DonorIDNumber = EncryptionHelper.Decrypt(reader.GetString(reader.GetOrdinal("DonorIDNumber"))),
                                    DonorTaxNumber = EncryptionHelper.Decrypt(reader.GetString(reader.GetOrdinal("DonorTaxNumber"))),
                                    DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail")),
                                    DonorPhone = reader.GetString(reader.GetOrdinal("DonorPhone"))
                                };
                                return Ok(donation);
                            }
                        }
                    }
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donation details for ID: {DonationId}", id);
                return StatusCode(500, "Internal server error while retrieving donation details");
            }
        }

        // Allow an admin to approve a donation

        // PUT: api/donations/{id}/approve
        [HttpPut("donations/{id}/approve")]
        public async Task<ActionResult<Donation>> ApproveDonation(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Update payment status
                    var updateSql = "UPDATE Donations SET PaymentStatus = 'Processed' WHERE DonationID = @DonationID";
                    using (var command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@DonationID", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound();
                        }
                    }

                    // Return the updated donation
                    return await GetDonation(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving donation for ID: {DonationId}", id);
                return StatusCode(500, "Internal server error while approving donation");
            }
        }

        // Get documents associated with a donation

        // GET: api/donations/{id}/document
        [HttpGet("donations/{id}/document")]
        public async Task<IActionResult> GetDocument(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                SELECT DocumentPath, DonorName, DonationDate 
                FROM Donations 
                WHERE DonationID = @DonationID", connection))
                    {
                        command.Parameters.AddWithValue("@DonationID", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync() && !reader.IsDBNull(reader.GetOrdinal("DocumentPath")))
                            {
                                var documentBytes = (byte[])reader["DocumentPath"];
                                var donorName = reader.GetString(reader.GetOrdinal("DonorName"));
                                var donationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate"));

                                // Try to detect file type from bytes
                                string contentType = "application/octet-stream";
                                string extension = ".bin";

                                // Check file signatures
                                if (documentBytes.Length >= 4)
                                {
                                    // PDF signature
                                    if (documentBytes[0] == 0x25 && documentBytes[1] == 0x50 &&
                                        documentBytes[2] == 0x44 && documentBytes[3] == 0x46)
                                    {
                                        contentType = "application/pdf";
                                        extension = ".pdf";
                                    }
                                    // PNG signature
                                    else if (documentBytes[0] == 0x89 && documentBytes[1] == 0x50 &&
                                            documentBytes[2] == 0x4E && documentBytes[3] == 0x47)
                                    {
                                        contentType = "image/png";
                                        extension = ".png";
                                    }
                                    // JPEG signature
                                    else if (documentBytes[0] == 0xFF && documentBytes[1] == 0xD8)
                                    {
                                        contentType = "image/jpeg";
                                        extension = ".jpg";
                                    }
                                    // ZIP signature (which could be docx, xlsx, etc.)
                                    else if (documentBytes[0] == 0x50 && documentBytes[1] == 0x4B &&
                                            documentBytes[2] == 0x03 && documentBytes[3] == 0x04)
                                    {
                                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                                        extension = ".docx";
                                    }
                                    // Check if it might be a text file
                                    else if (IsLikelyTextFile(documentBytes))
                                    {
                                        contentType = "text/plain";
                                        extension = ".txt";
                                    }
                                }

                                var fileName = $"Donation_{donorName}_{donationDate:yyyyMMdd}{extension}";
                                return File(documentBytes, contentType, fileName);
                            }
                        }
                    }
                }
                return NotFound("Document not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document for donation ID: {DonationId}", id);
                return StatusCode(500, "Internal server error while downloading document");
            }
        }

        private bool IsLikelyTextFile(byte[] bytes)
        {
            // Check if file is empty
            if (bytes.Length == 0)
                return false;

            // Check for BOM markers
            if (bytes.Length >= 3 &&
                ((bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) ||    // UTF-8
                 (bytes[0] == 0xFE && bytes[1] == 0xFF) ||                        // UTF-16 BE
                 (bytes[0] == 0xFF && bytes[1] == 0xFE)))                         // UTF-16 LE
            {
                return true;
            }

            // Check if the content contains only valid text characters
            try
            {
                // Try to decode as UTF-8
                string content = Encoding.UTF8.GetString(bytes);

                // Check if the content contains only printable characters, whitespace, or common control characters
                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];
                    if (!(char.IsLetterOrDigit(c) ||
                          char.IsPunctuation(c) ||
                          char.IsWhiteSpace(c) ||
                          char.IsSymbol(c) ||
                          c == '\r' ||
                          c == '\n' ||
                          c == '\t'))
                    {
                        return false;
                    }
                }

                // Additional check: ensure there aren't too many consecutive null bytes
                // which might indicate a binary file
                int consecutiveNulls = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] == 0x00)
                    {
                        consecutiveNulls++;
                        if (consecutiveNulls > 3) 
                            return false;
                    }
                    else
                    {
                        consecutiveNulls = 0;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        // Funding Management portion of the API
        // Get all funding requests in the database

        // GET: api/Admin/FundingRequestManagement
        [HttpGet("FundingRequestManagement")]
        public async Task<IActionResult> GetFundingRequestManagement()
        {
            var fundingRequests = new List<FundingRequest>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
                    SELECT fr.*, c.CompanyName,
                           CASE WHEN EXISTS (
                               SELECT 1 FROM FundingRequestAttachments fra 
                               WHERE fra.RequestID = fr.RequestID
                           ) THEN 1 ELSE 0 END as HasAttachments
                    FROM FundingRequests fr 
                    JOIN Companies c ON fr.CompanyID = c.CompanyID", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fundingRequests.Add(new FundingRequest
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                                AdminMessage = reader.IsDBNull(reader.GetOrdinal("AdminMessage")) ? null : reader.GetString(reader.GetOrdinal("AdminMessage")),
                                HasAttachments = reader.GetInt32(reader.GetOrdinal("HasAttachments")) == 1
                            });
                        }
                    }
                }
            }

            return Ok(fundingRequests);
        }

        // Fetch documents related to a specific funding request

        [HttpGet("FundingRequestAttachments/{requestId}")]
        public async Task<IActionResult> GetFundingRequestAttachments(int requestId)
        {
            var attachments = new List<AttachmentViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(@"
                SELECT AttachmentID, RequestID, FileName, ContentType, UploadedAt
                FROM FundingRequestAttachments 
                WHERE RequestID = @RequestID", connection))
                {
                    command.Parameters.AddWithValue("@RequestID", requestId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            attachments.Add(new AttachmentViewModel
                            {
                                AttachmentID = reader.GetInt32(reader.GetOrdinal("AttachmentID")),
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                                UploadedAt = reader.GetDateTime(reader.GetOrdinal("UploadedAt"))
                            });
                        }
                    }
                }
            }

            return Ok(attachments);
        }

        // Allow an admin to download a document

        [HttpGet("DownloadAttachment/{attachmentId}")]
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                    SELECT FileName, FileContent, ContentType
                    FROM FundingRequestAttachments 
                    WHERE AttachmentID = @AttachmentID", connection))
                    {
                        command.Parameters.AddWithValue("@AttachmentID", attachmentId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var fileName = reader.GetString(reader.GetOrdinal("FileName"));
                                var contentType = reader.IsDBNull(reader.GetOrdinal("ContentType")) ? "application/octet-stream" : reader.GetString(reader.GetOrdinal("ContentType"));
                                var fileContent = (byte[])reader["FileContent"];

                                // Check the file signature to set a more accurate content type if necessary
                                string extension = DetectFileExtensionAndContentType(fileContent, ref contentType);
                                fileName = fileName.Contains('.') ? fileName : $"{fileName}{extension}";

                                return File(fileContent, contentType, fileName);
                            }
                        }
                    }
                }
                return NotFound("Attachment not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment for ID: {AttachmentId}", attachmentId);
                return StatusCode(500, "Internal server error while downloading attachment");
            }
        }

        // Helper method for file type detection
        private string DetectFileExtensionAndContentType(byte[] fileContent, ref string contentType)
        {
            // Default to binary file if detection fails
            string extension = ".bin";

            if (fileContent.Length >= 4)
            {
                // PDF signature
                if (fileContent[0] == 0x25 && fileContent[1] == 0x50 && fileContent[2] == 0x44 && fileContent[3] == 0x46)
                {
                    contentType = "application/pdf";
                    extension = ".pdf";
                }
                // PNG signature
                else if (fileContent[0] == 0x89 && fileContent[1] == 0x50 && fileContent[2] == 0x4E && fileContent[3] == 0x47)
                {
                    contentType = "image/png";
                    extension = ".png";
                }
                // JPEG signature
                else if (fileContent[0] == 0xFF && fileContent[1] == 0xD8)
                {
                    contentType = "image/jpeg";
                    extension = ".jpg";
                }
                // ZIP signature (could be a DOCX, XLSX, etc.)
                else if (fileContent[0] == 0x50 && fileContent[1] == 0x4B && fileContent[2] == 0x03 && fileContent[3] == 0x04)
                {
                    contentType = "application/zip";
                    extension = ".zip"; 
                }
               
            }

            return extension;
        }

        // Fetch details for a specific funding request

        // GET: api/Admin/FundingRequestDetails/{id}
        [HttpGet("FundingRequestDetails/{id}")]
        public async Task<IActionResult> GetFundingRequestDetails(int id)
        {
            FundingRequest fundingRequest = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    SELECT fr.*, c.CompanyName 
                    FROM FundingRequests fr 
                    JOIN Companies c ON fr.CompanyID = c.CompanyID 
                    WHERE fr.RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            fundingRequest = new FundingRequest
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                                AdminMessage = reader.IsDBNull(reader.GetOrdinal("AdminMessage")) ? null : reader.GetString(reader.GetOrdinal("AdminMessage"))
                            };
                        }
                    }
                }
            }

            return fundingRequest != null ? Ok(fundingRequest) : NotFound();
        }

        // Allow an admin to approve a funding request

        // POST: api/Admin/ApproveFundingRequest
        [HttpPost("ApproveFundingRequest")]
        public async Task<IActionResult> ApproveFundingRequest(int id, [FromBody] string adminMessage)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE FundingRequests SET Status = 'Approved', AdminMessage = @AdminMessage WHERE RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    command.Parameters.AddWithValue("@AdminMessage", (object)adminMessage ?? DBNull.Value);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return NoContent();
        }

        // Allow an admin to reject a funding request

        // POST: api/Admin/RejectFundingRequest
        [HttpPost("RejectFundingRequest")]
        public async Task<IActionResult> RejectFundingRequest(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE FundingRequests SET Status = 'Rejected' WHERE RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return NoContent();
        }



        // Document management portion of the API
        // Fetch all documents in the database

        // GET: api/admin/documents
        [HttpGet("documents")]
        public async Task<ActionResult<List<Document>>> GetDocumentsAsync()
        {
            var documents = new List<Document>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
            SELECT 
                d.DocumentID, 
                d.DocumentName, 
                d.DocumentType, 
                d.UploadDate, 
                d.Status, 
                c.CompanyName, 
                fr.ProjectDescription,
                d.CompanyID,
                fr.RequestID
            FROM Documents d
            INNER JOIN Companies c ON d.CompanyID = c.CompanyID
            INNER JOIN FundingRequests fr ON d.RequestID = fr.RequestID
            ORDER BY d.UploadDate DESC";

                using (var cmd = new SqlCommand(query, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        documents.Add(new Document
                        {
                            DocumentID = reader.GetInt32(0),
                            DocumentName = reader.GetString(1),
                            DocumentType = reader.GetString(2),
                            UploadDate = reader.GetDateTime(3),
                            Status = reader.GetString(4),
                            CompanyName = reader.GetString(5),
                            ProjectDescription = reader.GetString(6),
                            CompanyID = reader.GetInt32(7),
                            FundingRequestID = reader.GetInt32(8)
                        });
                    }
                }
            }
            return Ok(documents);
        }


        // Allow an admin to approve a document

        // POST: api/admin/approve-document
        [HttpPost("approve-document")]
        public async Task<IActionResult> ApproveDocumentAsync(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "UPDATE Documents SET Status = 'Approved' WHERE DocumentID = @DocumentID";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }

        // Allow an admin to reject a document

        // POST: api/admin/reject-document
        [HttpPost("reject-document")]
        public async Task<IActionResult> RejectDocumentAsync(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "UPDATE Documents SET Status = 'Rejected' WHERE DocumentID = @DocumentID";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }

        // Allow an admin to mark a document as "received"

        // POST: api/admin/documents-received
        [HttpPost("documents-received")]
        public async Task<IActionResult> DocumentsReceivedAsync(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Update the document status
                var updateDocumentQuery = "UPDATE Documents SET Status = 'Documents Received' WHERE DocumentID = @DocumentID";
                using (var cmd = new SqlCommand(updateDocumentQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Retrieve the associated RequestID
                var getRequestIdQuery = "SELECT RequestID FROM Documents WHERE DocumentID = @DocumentID";
                int? requestId = null;
                using (var cmd = new SqlCommand(getRequestIdQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    requestId = (int?)await cmd.ExecuteScalarAsync();
                }

                // Update the funding request status if RequestID was found
                if (requestId.HasValue)
                {
                    var updateRequestQuery = "UPDATE FundingRequests SET Status = 'Documents Received' WHERE RequestID = @RequestID";
                    using (var cmd2 = new SqlCommand(updateRequestQuery, connection))
                    {
                        cmd2.Parameters.AddWithValue("@RequestID", requestId.Value);
                        await cmd2.ExecuteNonQueryAsync();
                    }
                }
            }
            return Ok();
        }

        // Allow an admin to close a funding request

        // POST: api/admin/close-request
        [HttpPost("close-request")]
        public async Task<IActionResult> CloseRequestAsync(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Update FundingRequests table to set the status to 'Closed'
                var updateRequestQuery = @"UPDATE FundingRequests 
                                   SET Status = 'Closed' 
                                   WHERE RequestID = (SELECT RequestID FROM Documents WHERE DocumentID = @DocumentID)";
                using (var cmd = new SqlCommand(updateRequestQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Retrieve the associated RequestID from the Documents table
                var getRequestIdQuery = "SELECT RequestID FROM Documents WHERE DocumentID = @DocumentID";
                int? requestId = null;
                using (var cmd = new SqlCommand(getRequestIdQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    requestId = (int?)await cmd.ExecuteScalarAsync();
                }

                // If a RequestID was found, update all related documents to 'Closed'
                if (requestId.HasValue)
                {
                    var updateDocumentsQuery = "UPDATE Documents SET Status = 'Closed' WHERE RequestID = @RequestID";
                    using (var cmd2 = new SqlCommand(updateDocumentsQuery, connection))
                    {
                        cmd2.Parameters.AddWithValue("@RequestID", requestId.Value);
                        await cmd2.ExecuteNonQueryAsync();
                    }
                }
            }

            return Ok();
        }

        // Allow an admin to download a document

        // GET: api/admin/download-document/{documentId}
        [HttpGet("download-document/{documentId}")]
        public async Task<IActionResult> DownloadDocumentAsync(int documentId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                SELECT DocumentName, DocumentType, FileContent
                FROM Documents
                WHERE DocumentID = @DocumentID", connection))
                    {
                        command.Parameters.AddWithValue("@DocumentID", documentId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var documentName = reader.GetString(reader.GetOrdinal("DocumentName"));
                                var contentType = reader.GetString(reader.GetOrdinal("DocumentType"));
                                var fileContent = (byte[])reader["FileContent"];

                                // Detect content type if not set in the database
                                if (string.IsNullOrEmpty(contentType))
                                {
                                    contentType = "application/octet-stream";
                                }

                                // Determine file extension based on file signature
                                string extension = DetectFileExtensionAndContentType(fileContent, ref contentType);
                                documentName = documentName.Contains('.') ? documentName : $"{documentName}{extension}";

                                // Return the file with appropriate headers
                                return File(fileContent, contentType, documentName);
                            }
                        }
                    }
                }
                return NotFound("Document not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document for ID: {DocumentId}", documentId);
                return StatusCode(500, "Internal server error while downloading document");
            }
        }


        // Report Management portion of the API
        // Get all information for the report

        // GET: api/admin/donation-report
        [HttpGet("donation-report")]
        public async Task<ActionResult<List<DonationReportItem>>> GetDonationReport()
        {
            List<DonationReportItem> donations = new List<DonationReportItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT d.DonationID, d.DonationDate, d.DonationType, d.DonationAmount, 
                                         d.DonorName, c.CompanyName
                                  FROM Donations d
                                  LEFT JOIN Companies c ON d.CompanyID = c.CompanyID
                                  ORDER BY d.DonationDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            donations.Add(new DonationReportItem
                            {
                                DonationID = reader.GetInt32(0),
                                DonationDate = reader.GetDateTime(1),
                                DonationType = reader.GetString(2),
                                DonationAmount = reader.GetDecimal(3),
                                DonorName = reader.GetString(4),
                                CompanyName = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }

            return Ok(donations);
        }

        // Get all details for the funding report

        // GET: api/admin/funding-request-report
        [HttpGet("funding-request-report")]
        public async Task<ActionResult<List<FundingRequestReportItem>>> GetFundingRequestReport()
        {
            List<FundingRequestReportItem> requests = new List<FundingRequestReportItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
            SELECT 
                fr.RequestID, 
                c.CompanyName,
                c.ContactEmail,
                c.ContactPhone, 
                fr.ProjectDescription,
                fr.ProjectImpact,
                fr.RequestedAmount, 
                fr.Status,
                fr.AdminMessage, 
                fr.SubmittedAt
            FROM FundingRequests fr
            INNER JOIN Companies c ON fr.CompanyID = c.CompanyID
            ORDER BY fr.SubmittedAt DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            requests.Add(new FundingRequestReportItem
                            {
                                RequestID = reader.GetInt32(0),
                                CompanyName = reader.GetString(1),
                                ContactEmail = reader.GetString(2),
                                ContactPhone = reader.GetString(3),
                                ProjectDescription = reader.GetString(4),
                                ProjectImpact = reader.IsDBNull(5) ? null : reader.GetString(5),
                                RequestedAmount = reader.GetDecimal(6),
                                Status = reader.GetString(7),
                                AdminMessage = reader.IsDBNull(8) ? null : reader.GetString(8),
                                SubmittedAt = reader.GetDateTime(9)
                            });
                        }
                    }
                }
            }

            return Ok(requests);
        }

    }
}

using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace BumbleBeeFoundation_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(IConfiguration configuration, ILogger<CompanyController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Get all the details for the company associated with the logged in user

        // GET: api/company/{companyId}
        [HttpGet("{companyId}")]
        public async Task<ActionResult<CompanyViewModel>> GetCompanyInfo(int companyId, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            string query = @"SELECT * FROM Companies WHERE CompanyID = @CompanyID AND UserID = @UserID";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CompanyID", companyId);
            command.Parameters.AddWithValue("@UserID", userId);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var companyViewModel = new CompanyViewModel
                {
                    CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                    CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                    ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                    ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                    DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    RejectionReason = reader["RejectionReason"] as string
                };

                // Add logging to inspect the CompanyViewModel data
                _logger.LogInformation($"CompanyID: {companyViewModel.CompanyID}");
                _logger.LogInformation($"CompanyName: {companyViewModel.CompanyName}");
                _logger.LogInformation($"ContactEmail: {companyViewModel.ContactEmail}");
                _logger.LogInformation($"ContactPhone: {companyViewModel.ContactPhone}");
                _logger.LogInformation($"DateJoined: {companyViewModel.DateJoined}");
                _logger.LogInformation($"Status: {companyViewModel.Status}");
                _logger.LogInformation($"RejectionReason: {companyViewModel.RejectionReason}");

                return companyViewModel;
            }
            return NotFound();
        }

        // Allow a company to request funding

        [HttpPost("RequestFunding")]
        public async Task<ActionResult<int>> RequestFunding([FromForm] FundingRequestViewModel model, [FromForm] List<IFormFile> attachments)
        {
            _logger.LogInformation("Received funding request: {@Model}", model);
            _logger.LogInformation("Received {AttachmentCount} attachments.", attachments?.Count ?? 0);

            // Log the CompanyID and validate it
            if (model.CompanyID == 0)
            {
                _logger.LogError("CompanyID is missing or invalid.");
                return BadRequest("CompanyID is required and must be valid.");
            }
            _logger.LogInformation("CompanyID in model: {CompanyID}", model.CompanyID);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened.");

                // Insert the funding request into the database
                string requestQuery = @"INSERT INTO FundingRequests (CompanyID, ProjectDescription, RequestedAmount, ProjectImpact, Status, SubmittedAt)
                                VALUES (@CompanyID, @ProjectDescription, @RequestedAmount, @ProjectImpact, @Status, GETDATE());
                                SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(requestQuery, connection);
                command.Parameters.AddWithValue("@CompanyID", model.CompanyID);
                command.Parameters.AddWithValue("@ProjectDescription", model.ProjectDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@RequestedAmount", model.RequestedAmount);
                command.Parameters.AddWithValue("@ProjectImpact", model.ProjectImpact ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", "Pending");

                _logger.LogInformation("Executing request insert query.");
                int requestId = Convert.ToInt32(await command.ExecuteScalarAsync());
                _logger.LogInformation("Funding request inserted with ID: {RequestID}", requestId);

                // Process attachments if provided
                if (attachments != null && attachments.Count > 0)
                {
                    _logger.LogInformation("Processing {AttachmentCount} attachments.", attachments.Count);
                    foreach (var file in attachments)
                    {
                        if (file.Length > 0)
                        {
                            try
                            {
                                _logger.LogInformation("Processing attachment: {FileName}", file.FileName);
                                using var memoryStream = new MemoryStream();
                                await file.CopyToAsync(memoryStream);
                                byte[] fileBytes = memoryStream.ToArray();

                                string insertAttachmentQuery = @"INSERT INTO FundingRequestAttachments (RequestID, FileName, FileContent, ContentType, UploadedAt)
                                                        VALUES (@RequestID, @FileName, @FileContent, @ContentType, GETDATE())";

                                using var attachmentCommand = new SqlCommand(insertAttachmentQuery, connection);
                                attachmentCommand.Parameters.AddWithValue("@RequestID", requestId);
                                attachmentCommand.Parameters.AddWithValue("@FileName", file.FileName);
                                attachmentCommand.Parameters.AddWithValue("@FileContent", fileBytes);
                                attachmentCommand.Parameters.AddWithValue("@ContentType", file.ContentType);

                                _logger.LogInformation("Executing attachment insert query for file: {FileName}", file.FileName);
                                await attachmentCommand.ExecuteNonQueryAsync();
                                _logger.LogInformation("Attachment uploaded successfully: {FileName}", file.FileName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing attachment: {FileName}", file.FileName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Attachment {FileName} has no content.", file.FileName);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No attachments provided.");
                }

                return Ok(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing funding request.");
                return BadRequest("An error occurred while processing the funding request.");
            }
        }

        // Provide the company employee with details about their funding request

        // GET: api/company/FundingRequestConfirmation/{id}
        [HttpGet("FundingRequestConfirmation/{id}")]
        public async Task<ActionResult<FundingRequestViewModel>> FundingRequestConfirmation(int id)
        {
            var request = new FundingRequestViewModel
            {
                Attachments = new List<AttachmentViewModel>()
            };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT fr.*, fra.AttachmentID, fra.FileName 
                             FROM FundingRequests fr
                             LEFT JOIN FundingRequestAttachments fra ON fr.RequestID = fra.RequestID
                             WHERE fr.RequestID = @RequestID";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@RequestID", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                request.RequestID = reader.GetInt32(reader.GetOrdinal("RequestID"));
                request.CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID"));
                request.ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription"));
                request.RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount"));
                request.ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact"));
                request.Status = reader.GetString(reader.GetOrdinal("Status"));
                request.SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"));

                do
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("AttachmentID")))
                    {
                        request.Attachments.Add(new AttachmentViewModel
                        {
                            AttachmentID = reader.GetInt32(reader.GetOrdinal("AttachmentID")),
                            FileName = reader.GetString(reader.GetOrdinal("FileName"))
                        });
                    }
                } while (await reader.ReadAsync());

                return Ok(request);
            }

            return NotFound();
        }

        // Allow a company employee to download an attachment

        // GET: api/company/DownloadAttachment/{id}
        [HttpGet("DownloadAttachment/{id}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"SELECT FileName, FileContent, ContentType FROM FundingRequestAttachments WHERE AttachmentID = @AttachmentID";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AttachmentID", id);

                using var reader = await command.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    _logger.LogWarning("Attachment with ID {Id} not found.", id);
                    return NotFound("Attachment not found.");
                }

                await reader.ReadAsync();
                var fileName = reader["FileName"].ToString();
                var fileContent = (byte[])reader["FileContent"];
                var contentType = reader["ContentType"].ToString();

                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = "application/octet-stream";
                    _logger.LogWarning("ContentType is null or empty. Defaulting to application/octet-stream.");
                }

                _logger.LogInformation("Retrieved attachment: {FileName}, ContentType: {ContentType}", fileName, contentType);

                // Explicitly set Content-Disposition header
                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = fileName,
                    Inline = false  
                };

                Response.Headers.Append("Content-Disposition", cd.ToString());

                return File(fileContent, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the attachment with ID: {Id}", id);
                return StatusCode(500, "Internal server error while processing the request.");
            }
        }

        // Upload the documents selected by the user

        // POST: api/company/UploadDocument
        [HttpPost("upload-document")]
        public async Task<IActionResult> UploadDocument([FromForm] int requestId, [FromForm] int companyId, IFormFile document)
        {
            if (document == null || document.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // List of allowed file types
            var allowedTypes = new Dictionary<string, string>
            {
                // Documents
                { ".pdf", "application/pdf" },
                { ".doc", "application/msword" },
                { ".docx", "application/docx" },
                { ".txt", "text/plain" },
        
                // Images
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" }
            };

            // Get file extension
            string fileExtension = Path.GetExtension(document.FileName).ToLowerInvariant();

            // Check if file type is allowed
            if (!allowedTypes.ContainsKey(fileExtension))
            {
                return BadRequest("File type not supported. Supported types are: PDF, DOC, DOCX, TXT, JPG, JPEG, and PNG.");
            }

            // Get simplified content type
            string documentType = allowedTypes[fileExtension];

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var memoryStream = new MemoryStream())
                    {
                        await document.CopyToAsync(memoryStream);
                        byte[] fileContent = memoryStream.ToArray();

                        if (fileContent.Length > 10 * 1024 * 1024) // 10MB limit
                        {
                            return BadRequest("File size exceeds 10MB limit.");
                        }

                        string query = @"INSERT INTO Documents 
                               (DocumentName, DocumentType, UploadDate, Status, CompanyID, FileContent, RequestID) 
                               VALUES 
                               (@DocumentName, @DocumentType, @UploadDate, @Status, @CompanyID, @FileContent, @RequestID)";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@DocumentName", Path.GetFileName(document.FileName));
                            command.Parameters.AddWithValue("@DocumentType", documentType);
                            command.Parameters.AddWithValue("@UploadDate", DateTime.Now);
                            command.Parameters.AddWithValue("@Status", "Pending");
                            command.Parameters.AddWithValue("@CompanyID", companyId);
                            command.Parameters.AddWithValue("@FileContent", fileContent);
                            command.Parameters.AddWithValue("@RequestID", requestId);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Ok(new
                {
                    message = "Document uploaded successfully.",
                    fileName = document.FileName,
                    type = documentType
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Get the funding request history for the company

        // GET: api/company/FundingRequestHistory/{companyId}
        [HttpGet("FundingRequestHistory/{companyId}")]
        public async Task<ActionResult<IEnumerable<FundingRequestViewModel>>> FundingRequestHistory(int companyId)
        {
            var requests = new List<FundingRequestViewModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"SELECT RequestID, CompanyID, ProjectDescription, RequestedAmount, Status, SubmittedAt, AdminMessage 
                             FROM FundingRequests WHERE CompanyID = @CompanyID ORDER BY SubmittedAt DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CompanyID", companyId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                requests.Add(new FundingRequestViewModel
                {
                    RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                    CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                    ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                    RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                    AdminMessage = reader.IsDBNull(reader.GetOrdinal("AdminMessage")) ? null : reader.GetString(reader.GetOrdinal("AdminMessage"))
                });
            }

            return Ok(requests);
        }
    }
}

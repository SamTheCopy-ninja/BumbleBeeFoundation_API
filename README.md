# **Introduction**

Welcome to the **BumbleBee Foundation API**! This API is designed to serve as the bridge between our client application and the backend database, enabling seamless interactions for various features such as donation management, user authentication, company data management, and admin data manipulation.

The primary purpose of this API is to:

-   **Manage Donations**: Handle donation records, including donation details, donor information, and associated documents.
-   **Verify Login Credentials**: Authenticate users and manage their login sessions for secure access to the system.
-   **Company Data Management**: Provide access to company profiles, documents, funding requests, and other relevant data.

By connecting this API to the client application, users can easily access, interact with, and manage their data in real-time.

## **Client Application**

You can also download the client application, which interfaces with this API, from our [Client App Repository](https://github.com/SamTheCopy-ninja/BumbleBeeFoundation_Client.git).

* * * * *

## **Project Authors**

This API was developed by **TerraTech TrailBlazers**. Below are the contributors who worked on the project:

  -   **Asanda Qwabe** -- Project Manager and Database Developer
  -   **Nkosinomusa Hadebe** -- Supporting Backend Developer
  -   **Samkelo Tshabalala** -- Backend Developer and Tester
  -   **Cameron Reese Davaniah** -- UI/Frontend Developer
  -   **Anelisiwe Sibusisiwe Ngema** -- UI/Frontend Developer
  

* * * * *
How This App Was Built
----------------------

The development of this application followed a structured process to ensure quality and alignment with project requirements. Below is an overview of the key steps taken during the app's creation:

1.  **Initial Planning**:

    -   The development team conducted meetings to define project requirements and create detailed user stories.
    -   Once finalized, these requirements and user stories were documented to guide the development process.
2.  **Database Creation**:

    -   The team's database developer created the project's database using SQL Server Management Studio (SSMS).
3.  **UI/Frontend Design**:

    -   The UI/Frontend developers worked on the application's interface designs, ensuring they met the requirements and user stories.
    -   These designs were reviewed and finalized in a team meeting before being handed over to the backend development team.
4.  **Prototype Development**:

    -   The backend development team built an initial all-in-one prototype, implementing most of the app's core functionality.
    -   This prototype was shared among team members via a .zip file in the team's work chat to ensure everyone had access to the code.
5.  **Feedback and Iteration**:

    -   Feedback was collected from the client and other stakeholders during several review sessions.
    -   The team held meetings to discuss and implement changes based on this feedback.
6.  **Splitting Functionality**:

    -   Once all the required functionality was implemented, the team separated the application into two components:
        -   **API Application**: To handle backend logic and database interactions.
        -   **Client Application**: To provide the user interface and client-side functionality.
    -   Both components were pushed to their respective GitHub repositories.
7.  **Development Workflow**:

    -   To ensure code consistency, the team adopted a local development workflow:
        -   Frontend and backend developers worked on the apps locally.
        -   Updates and new builds were shared in the team's work chat for review.
    -   The lead backend developer/tester tested all updates locally, verifying new features and fixes before committing changes to GitHub.
8.  **Streamlined Testing and Updates**:

    -   The tester provided detailed feedback on new features and updates, allowing team members to address issues locally.
    -   This streamlined pipeline ensured that only tested and approved changes were committed to the repositories, maintaining the quality of both the API and client apps.

For team member roles and responsibilities, please refer to the **Project Authors** section above.

* * * * *
* * * * *

### **Below you will find instructions for setting up and running the API, as well as documentation for testing the endpoints**

* * * * *

* * * * *

### **Creating the Database and Tables in SSMS**

Before running the API, you need to create the `BumbleBeeDB` database and the associated tables in your SQL Server instance. Follow these steps:

#### Step 1: **Connect to SQL Server in SSMS**

1.  Open **SQL Server Management Studio (SSMS)**.
2.  Connect to your SQL Server instance using the appropriate server name, authentication method (Windows or SQL Server), and credentials.

#### Step 2: **Create the Database**

1.  In the **Object Explorer** window, right-click on **Databases** and select **New Database**.
2.  Name the database `BumbleBeeDB` and click **OK**.
3.  Alternatively, you can run the following SQL script to create the database:

```
CREATE DATABASE BumbleBeeDB;
GO

```

#### Step 3: **Create the Tables**

1.  After the database is created, make sure it is selected in the **Object Explorer**.
2.  Open a **New Query** window in SSMS.
3.  Copy and paste the SQL script for creating the tables from below:

```sql
USE [BumbleBeeDB];
GO

-- Companies Table
CREATE TABLE [dbo].[Companies] (
    [CompanyID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CompanyName] VARCHAR(255) NULL,
    [ContactEmail] VARCHAR(255) NULL,
    [ContactPhone] VARCHAR(20) NULL,
    [Description] TEXT NULL,
    [DateJoined] DATETIME DEFAULT GETDATE() NULL,
    [Status] VARCHAR(50) DEFAULT 'Pending' NULL,
    [UserID] INT NULL,
    [RejectionReason] NVARCHAR(MAX) NULL,
    CONSTRAINT [FK_Companies_Users] FOREIGN KEY([UserID]) REFERENCES [dbo].[Users]([UserID])
);
GO

-- Documents Table
CREATE TABLE [dbo].[Documents] (
    [DocumentID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CompanyID] INT NULL,
    [DocumentName] VARCHAR(255) NULL,
    [DocumentType] VARCHAR(50) NULL,
    [UploadDate] DATETIME DEFAULT GETDATE() NULL,
    [Status] VARCHAR(50) DEFAULT 'Pending' NULL,
    [FileContent] VARBINARY(MAX) NULL,
    [RequestID] INT NULL,
    CONSTRAINT [FK_Documents_Companies] FOREIGN KEY([CompanyID]) REFERENCES [dbo].[Companies]([CompanyID])
);
GO

-- Donations Table
CREATE TABLE [dbo].[Donations] (
    [DonationID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CompanyID] INT NULL,
    [DonationDate] DATE NULL,
    [DonationType] VARCHAR(50) NULL,
    [DonationAmount] DECIMAL(10, 2) NULL,
    [DonorName] VARCHAR(255) NULL,
    [DonorIDNumber] VARCHAR(50) NULL,
    [DonorTaxNumber] VARCHAR(50) NULL,
    [DonorEmail] VARCHAR(255) NULL,
    [DonorPhone] VARCHAR(20) NULL,
    [DocumentPath] VARBINARY(MAX) NULL,
    [PaymentStatus] NVARCHAR(50) NULL,
    CONSTRAINT [FK_Donations_Companies] FOREIGN KEY([CompanyID]) REFERENCES [dbo].[Companies]([CompanyID])
);
GO

-- DonationSARS Table
CREATE TABLE [dbo].[DonationSARS] (
    [SARSID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [DonationID] INT NULL,
    [GeneratedDate] DATETIME DEFAULT GETDATE() NULL,
    [SARSDocument] VARCHAR(255) NULL,
    CONSTRAINT [FK_DonationSARS_Donations] FOREIGN KEY([DonationID]) REFERENCES [dbo].[Donations]([DonationID])
);
GO

-- FundingRequestAttachments Table
CREATE TABLE [dbo].[FundingRequestAttachments] (
    [AttachmentID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [RequestID] INT NOT NULL,
    [FileName] NVARCHAR(255) NOT NULL,
    [FileContent] VARBINARY(MAX) NOT NULL,
    [ContentType] NVARCHAR(100) NOT NULL,
    [UploadedAt] DATETIME2(7) DEFAULT GETDATE() NOT NULL,
    CONSTRAINT [FK_FundingRequestAttachments_FundingRequests] FOREIGN KEY([RequestID]) REFERENCES [dbo].[FundingRequests]([RequestID])
);
GO

-- FundingRequests Table
CREATE TABLE [dbo].[FundingRequests] (
    [RequestID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CompanyID] INT NULL,
    [ProjectDescription] TEXT NULL,
    [RequestedAmount] DECIMAL(10, 2) NULL,
    [ProjectImpact] TEXT NULL,
    [Status] VARCHAR(50) DEFAULT 'Pending' NULL,
    [SubmittedAt] DATETIME DEFAULT GETDATE() NULL,
    [AdminMessage] NVARCHAR(MAX) NULL,
    CONSTRAINT [FK_FundingRequests_Companies] FOREIGN KEY([CompanyID]) REFERENCES [dbo].[Companies]([CompanyID])
);
GO

-- Users Table
CREATE TABLE [dbo].[Users] (
    [UserID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FirstName] VARCHAR(100) NULL,
    [LastName] VARCHAR(100) NULL,
    [Email] VARCHAR(255) NULL UNIQUE,
    [Password] VARCHAR(255) NULL,
    [Role] VARCHAR(20) CHECK([Role] IN ('Donor', 'Company', 'Admin')) NULL,
    [CreatedAt] DATETIME DEFAULT GETDATE() NULL
);
GO

```

1.  Click **Execute** to run the script and create the tables.

#### Step 4: **Verify Table Creation**

1.  In the **Object Explorer**, right-click on **Tables** under the `BumbleBeeDB` database and select **Refresh**.
2.  You should see the newly created tables listed: `Companies`, `Documents`, `Donations`, `DonationSARS`, `FundingRequestAttachments`, `FundingRequests`, and `Users`.

* * * * *

* * * * *  
## **Installation and Running the API in Visual Studio 2022**

Follow these steps to install and run your Swagger API in Visual Studio 2022:

#### Step 1: **Clone or Download the Project**

-   Clone the repository using Git or download the project as a ZIP file and extract it to your local machine.

#### Step 2: **Open the Project in Visual Studio 2022**

-   Launch Visual Studio 2022.
-   Go to **File** > **Open** > **Project/Solution** and select the `.sln` file from your cloned or extracted project folder.

#### Step 3: **Restore NuGet Packages**

-   Visual Studio will automatically attempt to restore the required NuGet packages when you open the project.
-   If the packages don't restore automatically, go to **Tools** > **NuGet Package Manager** > **Package Manager Console**, then run the following command:

```
Restore

```
## **Updating the Connection String in `appsettings.json`**

To connect your Swagger API to your SQL Server database, you will need to update the connection string in the `appsettings.json` file. Follow these steps:

1.  Open the `appsettings.json` file located at the root of the project.
2.  Locate the `ConnectionStrings` section. It should look something like this:

```
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=YourDatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true"
}

```

1.  Modify the value of `DefaultConnection` to match your SQL Server connection details. For example:

-   **Server**: The name of your SQL Server instance (e.g., `localhost`, `localhost\SQLEXPRESS`, or a remote server address).
-   **Database**: The name of your database.
-   **Authentication**: Use `Trusted_Connection=True;` if you are using Windows authentication or specify `User ID` and `Password` if you're using SQL authentication.

Example connection string for SQL Server authentication:

```
"ConnectionStrings": {
  "DefaultConnection": "Server=your_server_name;Database=your_db_name;User Id=your_user_id;Password=your_password;"
}

```

Save the `appsettings.json` file.

Once updated, your API will be able to connect to the specified SQL Server database.  

#### Step 4: **Build the Project**

-   Press `Ctrl+Shift+B` or go to **Build** > **Build Solution** to build the project.
-   Ensure there are no build errors in the Output window. If there are errors, resolve them before proceeding.

#### Step 5: **Run the API**

-   Set your project as the **Startup Project** by right-clicking the project in the **Solution Explorer** and selecting **Set as StartUp Project**.
-   Press `F5` or click the **Run** button in Visual Studio to start the API.

Once the API starts, it should automatically open Swagger UI in your default web browser, typically at `http://localhost:5000/swagger`.

#### Step 6: **Testing the API**

-   In the Swagger UI, you'll see all your API endpoints.
-   You can test the various API endpoints directly through the UI by clicking the "Try it out" button and providing the necessary parameters.

* * * * *

* * * * *

# **API Endpoint documentaion**

Below you can read the endpoint documentation, detailing how the API functions

* * * * *


* * * * *

Donor Controller API
======================
**Endpoints**

**Get All Funding Requests**

-   **Endpoint**: GET /api/donor/FundingRequests
-   **Description**: Retrieves all funding requests that have been marked as Approved or Closed.
-   **Response**: A list of funding requests with project descriptions, requested amounts, and company details.
-   **Example Response**:

```

  {

    "RequestID": 1,

    "CompanyID": 10,

    "CompanyName": "TechCorp",

    "ProjectDescription": "Developing AI for good",

    "RequestedAmount": 50000,

    "ProjectImpact": "Positive impact on healthcare",

    "Status": "Approved",

    "SubmittedAt": "2024-11-20T10:00:00Z",

    "AdminMessage": "Approved for funding"

  },

  ...

```

* * * * *

**Create a Donation**

-   **Endpoint**: POST /api/donor/Donate
-   **Description**: Allows users to make a donation, including uploading a document related to the donation.
-   **Request Body**:

-   DonationType (string): The type of the donation (e.g., "Financial", "Goods").
-   DonationAmount (decimal): The amount being donated.
-   DonorName (string): The name of the donor.
-   DonorIDNumber (string): The donor's identification number.
-   DonorTaxNumber (string): The donor's tax number.
-   DonorEmail (string): The donor's email address.
-   DonorPhone (string): The donor's phone number.
-   DocumentUpload (file, optional): An optional document file related to the donation.

-   **Response**: A confirmation message and the donation ID upon successful submission.
-   **Example Response**:

```
{

  "Success": true,

  "Data": {

    "DonationId": 123,

    "Success": true,

    "Message": "Donation saved successfully"

  }

}
```
* * * * *

**Get a Specific Donation**

-   **Endpoint**: GET /api/donor/Donation/{id}
-   **Description**: Retrieves detailed information about a specific donation using its donation ID.
-   **Response**: The donation details including donor information and document path (if any).
-   **Example Response**:

```
{

  "DonationId": 123,

  "DonationDate": "2024-11-20T12:00:00Z",

  "DonationType": "Financial",

  "DonationAmount": 500.00,

  "DonorName": "John Doe",

  "DonorIDNumber": "12345",

  "DonorTaxNumber": "67890",

  "DonorEmail": "johndoe@email.com",

  "DonorPhone": "555-5555",

  "DocumentPath": null

}
```
* * * * *

**Get Donations for a User**

-   **Endpoint**: GET /api/donor/Donations/User/{userEmail}
-   **Description**: Retrieves all donations made by a user identified by their email.
-   **Response**: A list of donations for the given user, ordered by the donation date.
-   **Example Response**:

```

  {

    "DonationId": 123,

    "DonationDate": "2024-11-20T12:00:00Z",

    "DonationType": "Financial",

    "DonationAmount": 500.00,

    "DonorName": "John Doe"

  },

  ...

```

* * * * *

**Search Funding Requests**

-   **Endpoint**: GET /api/donor/SearchFundingRequests
-   **Query Parameter**: term (string): The search term for funding request descriptions or company names.
-   **Description**: Searches for funding requests based on the search term in the project description or company name.
-   **Response**: A list of matching funding requests.
-   **Example Response**:

```

  {

    "RequestID": 1,

    "CompanyID": 10,

    "CompanyName": "TechCorp",

    "ProjectDescription": "Developing AI for good",

    "RequestedAmount": 50000,

    "ProjectImpact": "Positive impact on healthcare",

    "Status": "Approved",

    "SubmittedAt": "2024-11-20T10:00:00Z"

  },

  ...

```

* * * * *

**Error Handling**

-   **General Errors**: The API uses standard HTTP status codes to represent success and failure.

-   200 OK: Successful request (for GET or POST requests).
-   400 Bad Request: Invalid data or failed validation (e.g., invalid DonationViewModel).
-   500 Internal Server Error: Internal server error when the donation process fails (e.g., database issues).

Example of error response:
```
{

  "Success": false,

  "Message": "Failed to save donation"

}
```
* * * * *

* * * * *

Company Controller API
======================

This controller is designed to manage company-related operations for a web API, including retrieving company details, requesting funding, and managing documents related to funding requests.

The `CompanyController` provides multiple endpoints for managing company-related functionality, focusing on funding requests. It allows companies to:

-   Retrieve their company details.
-   Submit funding requests.
-   View details of funding requests, including attached documents.
-   Upload and download documents related to funding requests.
-   View their funding request history.

* * * * *

API Endpoints
-------------

### Get Company Info

**Endpoint**: `GET api/company/{companyId}`

**Description**: Fetches details for the company associated with the provided `companyId` and `userId`.

**Parameters**:

-   `companyId` (int): The unique identifier of the company.
-   `userId` (int): The unique identifier of the logged-in user.

**Response**: Returns a `CompanyViewModel` containing company information like `CompanyID`, `CompanyName`, `ContactEmail`, etc.

**Example Request**:

```csharp
GET api/company/123?userId=456

```

* * * * *

### Request Funding

**Endpoint**: `POST api/company/RequestFunding`

**Description**: Allows a company to submit a funding request along with any attachments.

**Parameters**:

-   `model` (FundingRequestViewModel): The funding request data including the `CompanyID`, `ProjectDescription`, `RequestedAmount`, etc.
-   `attachments` (List): A list of files to attach to the funding request.

**Response**: Returns the ID of the created funding request.

**Example Request**:

```
POST api/company/RequestFunding
Content-Type: multipart/form-data

```

* * * * *

### Funding Request Confirmation

**Endpoint**: `GET api/company/FundingRequestConfirmation/{id}`

**Description**: Retrieves the details and attached files for a specific funding request.

**Parameters**:

-   `id` (int): The unique ID of the funding request.

**Response**: Returns a `FundingRequestViewModel` with the funding request details and a list of attachments.

**Example Request**:

```
GET api/company/FundingRequestConfirmation/1

```

* * * * *

### Download Attachment

**Endpoint**: `GET api/company/DownloadAttachment/{id}`

**Description**: Allows a user to download a specific attachment associated with a funding request.

**Parameters**:

-   `id` (int): The ID of the attachment to download.

**Response**: Returns the requested file as a download.

**Example Request**:

```
GET api/company/DownloadAttachment/1

```

* * * * *

### Upload Document

**Endpoint**: `POST api/company/upload-document`

**Description**: Allows a company to upload a document for a specific funding request.

**Parameters**:

-   `requestId` (int): The ID of the funding request.
-   `companyId` (int): The ID of the company.
-   `document` (IFormFile): The document file to upload.

**Response**: Returns a success message along with the document file name and type.

**Example Request**:

```
POST api/company/upload-document
Content-Type: multipart/form-data

```

* * * * *

### Funding Request History

**Endpoint**: `GET api/company/FundingRequestHistory/{companyId}`

**Description**: Retrieves the history of all funding requests submitted by a specific company.

**Parameters**:

-   `companyId` (int): The ID of the company for which the funding request history is to be retrieved.

**Response**: Returns a list of `FundingRequestViewModel` objects representing past funding requests.

**Example Request**:

```
GET api/company/FundingRequestHistory/123

```

* * * * *

Logging
-------

The controller uses `ILogger` to log key actions and data, including:

-   Database operations (e.g., retrieving company details, inserting funding requests).
-   Error messages when something goes wrong (e.g., invalid company ID or attachment processing errors).
-   Attachments and file processing details.

Logging helps track the flow of requests and can be useful for debugging and audit purposes.

**Example Logs**:

```
INFO: Received funding request: {CompanyID: 123, ProjectDescription: "New Project", RequestedAmount: 10000}
ERROR: CompanyID is missing or invalid.

```

* * * * *

Error Handling
--------------

The controller handles errors gracefully:

-   **Invalid data**: Returns appropriate status codes like `BadRequest` for missing or invalid parameters.
-   **Internal errors**: Returns `StatusCode(500)` for unexpected errors.
-   **Not found**: If no data is found for a request (e.g., company or attachment not found), it returns `NotFound()`.

* * * * *

Security
--------

-   **Input Validation**: Parameters such as `companyId` and `userId` are validated before use in queries to prevent SQL injection.
-   **Attachment Size**: The file size for document uploads is capped at 10MB to prevent excessively large file uploads.
-   **Allowed File Types**: Only specific file types (PDF, DOCX, JPG, PNG) are allowed for uploads.

-   **Data Integrity**: The SQL queries ensure that funding requests and documents are correctly associated with the relevant company and request IDs.
-   **Attachments Handling**: The system supports uploading, storing, and retrieving attachments with each funding request, providing a comprehensive solution for managing related documents.
-   **Use of `SqlConnection`**: The controller uses `SqlConnection` and parameterized queries to interact with the database, which helps prevent SQL injection attacks.

* * * * *

* * * * *

Account Controller API
============================================

Overview
--------

The `AccountController` handles user authentication, registration, and password management within the BumbleBeeFoundation API. This controller supports the following key features:

-   User Login
-   User Registration (including Company role)
-   Password Reset (Forgot and Change)

Routes
------

The following routes are available in the `AccountController`:

### `POST api/account/login`

**Description:**\
Authenticates a user by checking their credentials (email and password). Returns user details if the login is successful.

**Request Body:**

```
{
  "email": "user@example.com",
  "password": "userpassword"
}

```

**Response:**

-   **200 OK:** Returns a successful login response with user details (including role and company info for Company users).
-   **401 Unauthorized:** If the credentials are invalid.

**Example Response (Success):**

```
{
  "userId": 1,
  "role": "Company",
  "companyId": 101,
  "companyName": "Company XYZ",
  "userEmail": "user@example.com",
  "firstName": "John",
  "lastName": "Doe"
}

```

### `POST api/account/register`

**Description:**\
Registers a new user and adds them to the database. If the user is registering with the "Company" role, company-specific details are also stored.

**Request Body:**

```
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "user@example.com",
  "password": "userpassword",
  "role": "Company",
  "companyName": "Company XYZ",
  "contactPhone": "1234567890",
  "companyDescription": "A leading company in tech."
}

```

**Response:**

-   **200 OK:** Registration is successful.
-   **400 Bad Request:** If there are issues with the input data.

### `POST api/account/forgot-password`

**Description:**\
Initiates the password reset process by checking if the email exists in the system.

**Request Body:**

```
{
  "email": "user@example.com"
}

```

**Response:**

-   **200 OK:** Email found. Instructions to reset the password will be sent.
-   **404 Not Found:** If the email is not found in the database.

### `POST api/account/reset-password`

**Description:**\
Resets the user's password based on the provided email and new password.

**Request Body:**

```
{
  "email": "user@example.com",
  "newPassword": "newpassword"
}

```

**Response:**

-   **200 OK:** Password successfully updated.
-   **400 Bad Request:** If there are issues with the input data.

* * * * *

* * * * *

Admin Controller API
==========================================

Overview
--------

The `AdminController` provides endpoints for managing users, viewing dashboard statistics, and performing administrative actions within the BumbleBeeFoundation API. Admins can access the following functionality:

-   View dashboard statistics
-   Manage user accounts (view, create, edit, delete)
-   Manage funding requests
-   Manage donations
-   Manage documents

Routes
------

### `GET api/admin/dashboard`

**Description:**\
Fetches the dashboard statistics for the admin. The statistics include counts of users, companies, donations, and funding requests.

**Response:**

-   **200 OK:** Returns a `DashboardViewModel` with the following statistics:
    -   `TotalUsers`: The total number of users in the system.
    -   `TotalCompanies`: The total number of companies.
    -   `TotalDonations`: The total number of donations.
    -   `TotalFundingRequests`: The total number of funding requests.

**Example Response:**

```
{
  "totalUsers": 150,
  "totalCompanies": 50,
  "totalDonations": 200,
  "totalFundingRequests": 30
}

```

### `GET api/admin/users`

**Description:**\
Fetches a list of all users in the system.

**Response:**

-   **200 OK:** Returns a list of users, including their ID, first name, last name, email, and role.

**Example Response:**

```
[
  {
    "userId": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "johndoe@example.com",
    "role": "Admin"
  },
  {
    "userId": 2,
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "janesmith@example.com",
    "role": "Donor"
  }
]

```

### `GET api/admin/users/{id}`

**Description:**\
Fetches the details of a specific user by their `UserID`.

**Response:**

-   **200 OK:** Returns the details of the specified user.
-   **404 Not Found:** If the user with the specified `UserID` does not exist.

**Example Response (Success):**

```
{
  "userId": 1,
  "firstName": "John",
  "lastName": "Doe",
  "email": "johndoe@example.com",
  "role": "Admin"
}

```

### `POST api/admin/users`

**Description:**\
Allows an admin to create a new user in the system.

**Request Body:**

```
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "johndoe@example.com",
  "password": "password123",
  "role": "Admin"
}

```

**Response:**

-   **201 Created:** Returns the created user details.
-   **400 Bad Request:** If the request body is invalid (e.g., missing required fields).

**Example Response (Success):**

```
{
  "userId": 3,
  "firstName": "John",
  "lastName": "Doe",
  "email": "johndoe@example.com",
  "role": "Admin"
}

```

### `PUT api/admin/users/{id}`

**Description:**\
Allows an admin to edit an existing user's details.

**Request Body:**

```
{
  "userId": 1,
  "firstName": "John",
  "lastName": "Doe",
  "email": "johnupdated@example.com",
  "role": "Donor"
}

```

**Response:**

-   **204 No Content:** The user has been successfully updated.
-   **400 Bad Request:** If the request body is invalid.
-   **404 Not Found:** If the user with the specified `UserID` does not exist.

### `DELETE api/admin/users/{id}`

**Description:**\
Allows an admin to delete a user from the system.

**Response:**

-   **204 No Content:** The user has been successfully deleted.
-   **404 Not Found:** If the user with the specified `UserID` does not exist.

* * * * *

* * * * *

### `GET api/admin/companies`

**Description:**\
Fetches a list of all companies in the system. This endpoint is typically used by admins to view company details such as contact information and status.

**Response:**

-   **200 OK:** Returns a list of companies with their details, including:
    -   `CompanyID`: The unique identifier for the company.
    -   `CompanyName`: The name of the company.
    -   `ContactEmail`: The contact email of the company.
    -   `ContactPhone`: The contact phone number of the company.
    -   `Description`: A description of the company.
    -   `DateJoined`: The date the company joined the system.
    -   `Status`: The current status of the company (e.g., Approved, Rejected).
    -   `RejectionReason`: The reason for rejection (if applicable).

**Example Response:**

```
[
  {
    "companyId": 1,
    "companyName": "Tech Innovations",
    "contactEmail": "contact@techinnovations.com",
    "contactPhone": "123-456-7890",
    "description": "Leading provider of tech solutions.",
    "dateJoined": "2023-10-01T12:00:00",
    "status": "Approved",
    "rejectionReason": ""
  },
  {
    "companyId": 2,
    "companyName": "Eco Products",
    "contactEmail": "info@ecoproducts.com",
    "contactPhone": "987-654-3210",
    "description": "Eco-friendly products for a sustainable future.",
    "dateJoined": "2023-09-15T12:00:00",
    "status": "Rejected",
    "rejectionReason": "Incomplete documentation"
  }
]

```

### `GET api/admin/companies/{id}`

**Description:**\
Fetches the details of a specific company by its `CompanyID`.

**Response:**

-   **200 OK:** Returns the details of the specified company.
-   **404 Not Found:** If the company with the specified `CompanyID` does not exist.

**Example Response (Success):**

```
{
  "companyId": 1,
  "companyName": "Tech Innovations",
  "contactEmail": "contact@techinnovations.com",
  "contactPhone": "123-456-7890",
  "description": "Leading provider of tech solutions.",
  "dateJoined": "2023-10-01T12:00:00",
  "status": "Approved",
  "rejectionReason": ""
}

```

### `POST api/admin/companies/approve/{id}`

**Description:**\
Allows an admin to approve a company. The company's `Status` will be updated to "Approved."

**Response:**

-   **200 OK:** A success message indicating the company was approved.

**Example Response:**

```
{
  "message": "Company approved successfully."
}

```

### `POST api/admin/companies/reject/{id}`

**Description:**\
Allows an admin to reject a company. The company's `Status` will be updated to "Rejected," and a rejection reason must be provided.

**Request Body:**

```
{
  "rejectionReason": "Incomplete documentation"
}

```

**Response:**

-   **200 OK:** A success message indicating the company was rejected, along with the rejection reason.

**Example Response:**

```
{
  "message": "Company rejected with reason: Incomplete documentation"
}

```

* * * * *

* * * * *

### 1. **Get all donations**

-   **Endpoint**: `GET: api/admin/donations`
-   **Description**: This endpoint retrieves a list of all donations from the database, including details such as donation ID, company name (if associated), donation date, type, amount, donor information, and payment status.
-   **Response**: A list of donation objects.
-   **Example Response**:

    ```
    [
      {
        "DonationID": 1,
        "CompanyID": 123,
        "CompanyName": "Company ABC",
        "DonationDate": "2023-01-01T00:00:00",
        "DonationType": "Cash",
        "DonationAmount": 500.00,
        "DonorName": "John Doe",
        "DonorEmail": "john.doe@example.com",
        "PaymentStatus": "Processed",
        "DocumentFileName": "Attached Document"
      },
      ...
    ]

    ```

-   **Errors**:
    -   `500 Internal Server Error`: If an error occurs while fetching donations.

### 2. **Get donation details by ID**

-   **Endpoint**: `GET: api/admin/donations/{id}`
-   **Description**: Retrieves detailed information about a specific donation by its ID, including donor information, donation amount, and associated company (if any).
-   **Parameters**:
    -   `id`: The ID of the donation to retrieve.
-   **Response**: A donation object with detailed information.
-   **Example Response**:

    ```
    {
      "DonationID": 1,
      "CompanyID": 123,
      "CompanyName": "Company ABC",
      "DonationDate": "2023-01-01T00:00:00",
      "DonationType": "Cash",
      "DonationAmount": 500.00,
      "DonorName": "John Doe",
      "DonorIDNumber": "1234567890",
      "DonorTaxNumber": "987654321",
      "DonorEmail": "john.doe@example.com",
      "DonorPhone": "123-456-7890"
    }

    ```

-   **Errors**:
    -   `404 Not Found`: If the donation with the specified ID does not exist.
    -   `500 Internal Server Error`: If an error occurs while fetching donation details.

### 3. **Approve a donation**

-   **Endpoint**: `PUT: api/admin/donations/{id}/approve`
-   **Description**: Marks a donation as "Processed" by updating its payment status. Returns the updated donation object.
-   **Parameters**:
    -   `id`: The ID of the donation to approve.
-   **Response**: The updated donation object with the status set to "Processed".
-   **Errors**:
    -   `404 Not Found`: If no donation with the given ID exists.
    -   `500 Internal Server Error`: If an error occurs during the approval process.

### 4. **Get donation document**

-   **Endpoint**: `GET: api/admin/donations/{id}/document`
-   **Description**: Fetches and returns the document associated with a specific donation. This could be a PDF, image, or other file formats.
-   **Parameters**:
    -   `id`: The ID of the donation whose document is to be fetched.
-   **Response**: The document file, with content type determined based on the file signature (e.g., PDF, PNG, JPEG).
-   **Example Response**:
    -   `Content-Type`: `application/pdf`
    -   `Content-Disposition`: `attachment; filename="Donation_JohnDoe_20230101.pdf"`
    -   `File Content`: The binary content of the document.
-   **Errors**:
    -   `404 Not Found`: If the document for the specified donation does not exist.
    -   `500 Internal Server Error`: If an error occurs while retrieving the document.

* * * * *

* * * * *

#### 1. **Get All Funding Requests**

**Endpoint:** `GET: api/Admin/FundingRequestManagement`

**Description:** This endpoint retrieves all funding requests from the database. It also fetches the associated company details and a flag indicating whether the request has any attachments.

**Response Example:**

```
[
    {
        "RequestID": 1,
        "CompanyID": 123,
        "CompanyName": "Company A",
        "ProjectDescription": "Project Description here",
        "RequestedAmount": 50000,
        "ProjectImpact": "Impact description",
        "Status": "Pending",
        "SubmittedAt": "2024-11-24T10:00:00",
        "AdminMessage": null,
        "HasAttachments": true
    }
]

```

#### 2. **Fetch Documents for a Specific Funding Request**

**Endpoint:** `GET: api/Admin/FundingRequestAttachments/{requestId}`

**Description:** This endpoint fetches the documents attached to a specific funding request by request ID.

**Parameters:**

-   `requestId` (required) - The ID of the funding request.

**Response Example:**

```
[
    {
        "AttachmentID": 1,
        "RequestID": 1,
        "FileName": "document.pdf",
        "UploadedAt": "2024-11-24T10:00:00"
    }
]

```

#### 3. **Download an Attachment**

**Endpoint:** `GET: api/Admin/DownloadAttachment/{attachmentId}`

**Description:** This endpoint allows admins to download the attachment for a specific funding request by attachment ID. The content type and file extension are automatically detected based on the file signature.

**Parameters:**

-   `attachmentId` (required) - The ID of the attachment.

**Response:** Returns the file content along with the appropriate content type (e.g., PDF, PNG).

**Error Response:**

-   `404 Not Found` if the attachment doesn't exist.
-   `500 Internal Server Error` if there is an error during the file retrieval process.

#### 4. **Get Details for a Specific Funding Request**

**Endpoint:** `GET: api/Admin/FundingRequestDetails/{id}`

**Description:** This endpoint retrieves detailed information about a specific funding request, including the associated company name and project details.

**Parameters:**

-   `id` (required) - The ID of the funding request.

**Response Example:**

```
{
    "RequestID": 1,
    "CompanyID": 123,
    "CompanyName": "Company A",
    "ProjectDescription": "Detailed description",
    "RequestedAmount": 50000,
    "ProjectImpact": "Impact here",
    "Status": "Pending",
    "SubmittedAt": "2024-11-24T10:00:00",
    "AdminMessage": null
}

```

#### 5. **Approve a Funding Request**

**Endpoint:** `POST: api/Admin/ApproveFundingRequest`

**Description:** This endpoint allows the admin to approve a funding request. The request status is updated to "Approved" along with an optional admin message.

**Parameters:**

-   `id` (required) - The ID of the funding request.
-   `adminMessage` (optional) - An optional message from the admin regarding the approval.

**Response:** Returns a `204 No Content` on successful approval.

#### 6. **Reject a Funding Request**

**Endpoint:** `POST: api/Admin/RejectFundingRequest`

**Description:** This endpoint allows the admin to reject a funding request. The request status is updated to "Rejected".

**Parameters:**

-   `id` (required) - The ID of the funding request.

**Response:** Returns a `204 No Content` on successful rejection.

* * * * *


* * * * *

### 1. **Get All Documents**

**Endpoint**: `GET /api/admin/documents`\
**Description**: Fetches all documents associated with funding requests, including their metadata such as document name, type, upload date, and status.

**Response**: A list of documents with details about the document, associated company, and funding request.

**Example Response**:

```
[
  {
    "DocumentID": 1,
    "DocumentName": "Document A",
    "DocumentType": "PDF",
    "UploadDate": "2024-10-01T12:30:00",
    "Status": "Pending",
    "CompanyName": "Company A",
    "ProjectDescription": "Funding for Project X",
    "CompanyID": 100,
    "FundingRequestID": 200
  },
  ...
]

```

* * * * *

### 2. **Approve Document**

**Endpoint**: `POST /api/admin/approve-document`\
**Description**: Allows an admin to approve a document by updating its status to "Approved".

**Parameters**:

-   `documentId` (int): The ID of the document to approve.

**Example Request**:

```
{
  "documentId": 1
}

```

**Response**: HTTP 200 OK

* * * * *

### 3. **Reject Document**

**Endpoint**: `POST /api/admin/reject-document`\
**Description**: Allows an admin to reject a document by updating its status to "Rejected".

**Parameters**:

-   `documentId` (int): The ID of the document to reject.

**Example Request**:

```
{
  "documentId": 1
}

```

**Response**: HTTP 200 OK

* * * * *

### 4. **Mark Document as Received**

**Endpoint**: `POST /api/admin/documents-received`\
**Description**: Allows an admin to mark a document as "Received". This also updates the associated funding request's status to "Documents Received".

**Parameters**:

-   `documentId` (int): The ID of the document to mark as received.

**Example Request**:

```
{
  "documentId": 1
}

```

**Response**: HTTP 200 OK

* * * * *

### 5. **Close Funding Request**

**Endpoint**: `POST /api/admin/close-request`\
**Description**: Allows an admin to close a funding request by marking its status as "Closed". This also updates the status of all documents associated with the funding request.

**Parameters**:

-   `documentId` (int): The ID of the document associated with the funding request to close.

**Example Request**:

```
{
  "documentId": 1
}

```

**Response**: HTTP 200 OK

* * * * *

### 6. **Download Document**

**Endpoint**: `GET /api/admin/download-document/{documentId}`\
**Description**: Allows an admin to download a document by its ID. The response contains the document's file content.

**Parameters**:

-   `documentId` (int): The ID of the document to download.

**Response**: A file with the appropriate MIME type and filename.

* * * * *

### 7. **Get Donation Report**

**Endpoint**: `GET /api/admin/donation-report`\
**Description**: Fetches a report of all donations, including donation amount, type, date, donor name, and associated company.

**Response**: A list of donations with associated metadata.

**Example Response**:

```
[
  {
    "DonationID": 1,
    "DonationDate": "2024-11-01T09:00:00",
    "DonationType": "Cash",
    "DonationAmount": 500.00,
    "DonorName": "John Doe",
    "CompanyName": "Company A"
  },
  ...
]

```

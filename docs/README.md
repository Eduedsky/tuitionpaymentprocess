# System Design for Tuition Payment Processing

This document provides a detailed system design for integrating the **Mock Bank API** (`MockBankAPI`) and **XYZ University API** (`XYZUniversityAPI`) to handle tuition payment notifications. The system focuses on enabling real-time student validation and payment notification delivery between the two entities using RESTful APIs hosted on Google Cloud virtual machines (VMs). It ensures secure, reliable, and compliant communication through independent APIs, each backed by its own SQL Server database. Below, we explore the system’s architecture, components, workflows, and design considerations in depth.

---

## 1. System Overview

The tuition payment processing system facilitates seamless interaction between `MockBankAPI` (deployed at `34.134.214.146:5000`) and `XYZUniversityAPI` (deployed at `34.31.232.140:5251`). These APIs work together to validate student enrollment and process payment notifications, ensuring that tuition payments are accurately communicated and recorded. The key functionalities include:

- **Student Validation**: When a payment is initiated, `MockBankAPI` queries `XYZUniversityAPI` to confirm a student’s enrollment status, retrieving details such as the student’s name (`StudentName`) and outstanding tuition balance (`FeeBalance`).
- **Payment Notification**: Assuming internal business logic initiates these notifications. `MockBankAPI` notifies `XYZUniversityAPI` of completed payments via webhooks, allowing the university to update its records.
- **Real-Time Notifications**: Webhooks enable near real-time communication, ensuring that both systems stay synchronized with minimal delay.
- **Audit Trails**: Every API interaction is logged in dedicated `AuditLogs` tables to support compliance, debugging, and monitoring.

The system employs a **layered architecture**, consisting of an API layer, a business logic layer, a data access layer, and a webhook notification service. Each API operates on its own Google Cloud VM, with network traffic restricted by firewall rules to only allow communication between these specific servers (`34.134.214.146` and `34.31.232.140`). This isolation enhances security and simplifies deployment.

---

## 2. System Components

The system comprises two primary APIs (`XYZUniversityAPI` and `MockBankAPI`), each with distinct responsibilities, layers, and database schemas. Below, we break down their components in detail.

### 2.1 `XYZUniversityAPI` API Layer

The `XYZUniversityAPI`, hosted at `34.31.232.140:5251`, serves as the university’s interface for handling student validation requests and payment notifications from `MockBankAPI`. It exposes RESTful endpoints to support these operations.

#### 2.1.1 Endpoints

- **Student Validation Endpoint** (`POST /api/students/validate`):
  - **Purpose**: Validates whether a provided `StudentId` corresponds to an enrolled student.
  - **Input**: 
    - A JSON request body, e.g., `{ "StudentId": "2020-TWC-1223" }`.
    - An `X-API-Key` header for authentication.
  - **Output**: 
    - On success (HTTP 200): A JSON response containing student details, e.g., 
      ```json
      { "StudentId": "2020-TWC-1223", "StudentName": "John Doe", "IsEnrolled": true, "FeeBalance": 5000.00 }
      ```
    - On failure:
      - If the student is not found: `{ "error": "Student not found" }` (HTTP 404).
      - If the API key is invalid: `{ "error": "Invalid API key" }` (HTTP 401).
  - **Webhook Notification**: After processing, `XYZUniversityAPI` sends a webhook to `MockBankAPI` with the validation result, ensuring the bank is informed of the outcome.
  - **Audit Logging**: Each request and response is logged in the `xyzuniversity-db`.`AuditLogs` table, capturing details like the timestamp, endpoint, payload, and status.

- **Payment Notification Endpoint** (`POST /api/payments/notification`):
  - **Purpose**: Receives payment notifications from `MockBankAPI` and updates the university’s payment records.
  - **Input**: 
    - A JSON request body, which can be a single payment or a batch (up to 100 entries), e.g.,
      ```json
      [
        { "TransactionId": "TXN123456", "StudentId": "2020-TWC-1223", "Amount": 5000.00, "PaymentDate": "2025-02-25T12:00:00Z" }
      ]
      ```
    - An `X-API-Key` header for authentication.
  - **Output**: 
    - On success (HTTP 200): A JSON response confirming the status, e.g., 
      ```json
      [{ "TransactionId": "TXN123456", "Status": "success" }]
      ```
    - On failure: `{ "error": "Invalid input data" }` (HTTP 400) if the payload is malformed or invalid.
  - **Webhook Notification**: After processing, a webhook is sent to `MockBankAPI` to confirm receipt and status of the payment notification.
  - **Audit Logging**: Interactions are logged in `xyzuniversity-db`.`AuditLogs` for traceability.

#### 2.1.2 Authentication

- **Mechanism**: The API requires an `X-API-Key` header in every request, which is validated against a predefined value stored in the API’s environment variables.
- **Webhook Security**: Webhooks are currently sent over HTTP with an `X-API-Key` header, though HTTPS is planned for future enhancement to encrypt communication.

---

### 2.2 `XYZUniversityAPI` Business Logic Layer

This layer processes incoming requests asynchronously to ensure non-blocking operations. It coordinates data retrieval, validation, storage, and webhook notifications.

- **Student Validation**:
  - Retrieves student data from the database based on the provided `StudentId`.
  - Constructs a response with `StudentName`, `IsEnrolled`, and `FeeBalance`.
  - Triggers a webhook to `MockBankAPI` with the validation result.
- **Payment Notification**:
  - Validates the incoming payment data (e.g., ensuring `TransactionId` is unique and `StudentId` exists).
  - Stores the payment details in the `Payments` table.
  - Sends a webhook to `MockBankAPI` to confirm processing status.

---

### 2.3 `XYZUniversityAPI` Data Access Layer

This layer handles all database interactions using **Entity Framework Core**, an object-relational mapper (ORM) for .NET.

- **Repositories**: 
  - `IStudentRepository`: Manages student data retrieval.
  - `IPaymentRepository`: Handles payment record creation and updates.
  - `IAuditLogRepository`: Logs all API interactions.

---

### 2.4 `MockBankAPI` API Layer

The `MockBankAPI`, hosted at `34.134.214.146:5000`, acts as the bank’s interface for initiating student validation and payment notifications to `XYZUniversityAPI`.

#### 2.4.1 Endpoints

- **Student Validation Request** (`POST /api/payments/validate-student/{universityCode}`):
  - **Purpose**: Requests validation of a student’s enrollment from a specified university (e.g., `XYZ`).
  - **Input**: 
    - A JSON body, e.g., `{ "StudentId": "2020-TWC-1223" }`.
    - A path parameter `universityCode` (e.g., "XYZ").
    - An `X-API-Key` header sourced from the `UniversityConfigs` table.
  - **Output**: Forwards the response from `XYZUniversityAPI` (e.g., student details or error message).
  - **Webhook Notification**: Receives a webhook from `XYZUniversityAPI` with the validation result.
  - **Audit Logging**: Logs the request and response in `mockbank-db`.`AuditLogs`.

- **Payment Notification Request** (`POST /api/payments/send-notification/{universityCode}`):
  - **Purpose**: Sends payment notifications to the specified university.
  - **Input**: 
    - A JSON body, e.g., 
      ```json
      { "TransactionId": "TXN123456", "StudentId": "2020-TWC-1223", "Amount": 5000.00, "PaymentDate": "2025-02-25T12:00:00Z" }
      ```
    - A path parameter `universityCode` (e.g., "XYZ").
    - An `X-API-Key` header from `UniversityConfigs`.
  - **Output**: Forwards the response from `XYZUniversityAPI` (e.g., success or error).
  - **Webhook Notification**: Receives a webhook from `XYZUniversityAPI` confirming receipt.
  - **Audit Logging**: Logs interactions in `mockbank-db`.`AuditLogs`.

#### 2.4.2 Authentication

- **Mechanism**: Uses an `X-API-Key` retrieved from the `UniversityConfigs` table, specific to each university.
- **Webhook Security**: Webhooks use HTTP with an `X-API-Key`, with HTTPS planned for future implementation.

---

### 2.5 `MockBankAPI` Business Logic Layer

This layer manages asynchronous request processing and webhook handling.

- **Student Validation**:
  - Constructs and sends validation requests to `XYZUniversityAPI`.
  - Processes incoming webhooks to update internal state or logs.
- **Payment Notification**:
  - Sends payment notifications to `XYZUniversityAPI`.
  - Updates the `Payments` table based on webhook responses (e.g., marking a payment as "Successful" or "Failed").

---

### 2.6 `MockBankAPI` Data Access Layer

This layer uses **Entity Framework Core** to interact with the `mockbank-db` database.

- **Repositories**: 
  - `IPaymentRepository`: Manages payment records.
  - `IAuditLogRepository`: Logs API interactions.

---

### 2.8 Webhook Notification Service

This service handles real-time communication between the APIs via webhooks.

- **Configuration**: Webhook URLs are stored in the `mockbank-db`.`UniversityConfigs` table (e.g., `http://34.31.232.140:5251` for `XYZUniversityAPI`).
- **Delivery**: Uses a single `HttpClient` instance per API to send webhooks, without connection pooling for simplicity.

---

### 2.9 Audit Logging Middleware

A middleware component logs every API request and response in the respective `AuditLogs` tables. It captures detailed information and triggers webhooks for monitoring purposes, ensuring all interactions are traceable.

---

### 2.10 Databases

Each API uses its own SQL Server database with specific schemas to store and manage data.

#### 2.10.1 Tables

- **XYZUniversityAPI (`xyzuniversity-db`)**:
  - **Students**:
    - `StudentId` (string, primary key): Unique identifier for each student.
    - `StudentName` (string): Full name of the student.
    - `IsEnrolled` (boolean): Indicates enrollment status.
    - `FeeBalance` (decimal(18,2)): Outstanding tuition balance.
  - **Payments**:
    - `Id` (int, primary key): Auto-incrementing identifier.
    - `TransactionId` (string, unique): Bank-assigned transaction ID.
    - `StudentId` (string, foreign key to `Students`): Links payment to a student.
    - `Amount` (decimal(18,2)): Payment amount.
    - `PaymentDate` (datetime): Date and time of payment.
    - `Status` (string): Processing status (e.g., "Pending", "Completed").
    - `CreatedAt` (datetime): Record creation timestamp.
    - `UpdatedAt` (datetime): Last update timestamp.
  - **AuditLogs**:
    - `Id` (int, primary key): Auto-incrementing identifier.
    - `Timestamp` (datetime): When the interaction occurred.
    - `User` (string): API user or system identifier.
    - `Endpoint` (string): API endpoint called.
    - `RequestPayload` (string): JSON request body.
    - `ResponseStatus` (int): HTTP status code.
    - `ErrorMessage` (string): Error details, if any.

- **MockBankAPI (`mockbank-db`)**:
  - **UniversityConfigs**:
    - `Id` (int, primary key): Auto-incrementing identifier.
    - `UniversityCode` (string): Unique code (e.g., "XYZ").
    - `BaseUrl` (string): University API URL (e.g., `http://34.31.232.140:5251`).
    - `ApiKey` (string): Authentication key for the university API.
  - **Payments**:
    - `Id` (int, primary key): Auto-incrementing identifier.
    - `TransactionId` (string, nullable): Bank transaction ID.
    - `StudentId` (string, nullable): Student identifier.
    - `Amount` (decimal): Payment amount.
    - `PaymentDate` (datetime): Payment timestamp.
    - `Status` (string, nullable): e.g., "Sent", "Failed", "Successful".
    - `ResponseDetails` (string, nullable): Additional response data.
    - `CreatedAt` (datetime): Record creation timestamp.
    - `UpdatedAt` (datetime): Last update timestamp.
  - **AuditLogs**:
    - Same structure as `xyzuniversity-db`.`AuditLogs`.

#### 2.10.2 Constraints and Indexes

- **Constraints**:
  - Foreign key: `xyzuniversity-db`.`Payments.StudentId` references `Students.StudentId`.
  - Unique constraint: `Payments.TransactionId` ensures no duplicate transactions.
- **Indexes**:
  - `Students.StudentId`: Speeds up validation lookups.
  - `Payments.TransactionId`: Optimizes payment lookups.
  - `AuditLogs.Timestamp`: Enhances log retrieval by time.

---

## 3. System Flow

The system operates through two primary workflows: student validation and payment notification.

### 3.1 Student Validation Flow

1. **Request Initiation**: `MockBankAPI` sends a `POST /api/payments/validate-student/XYZ` request with `{ "StudentId": "2020-TWC-1223" }` to `XYZUniversityAPI`.
2. **Processing**: `XYZUniversityAPI` queries its `Students` table, retrieves the student’s data, and responds with details or an error. It then sends a webhook to `MockBankAPI` with the result.
3. **Logging**: Both APIs record the interaction in their respective `AuditLogs` tables, including request details and response status.

### 3.2 Payment Notification Flow

1. **Request Initiation**: `MockBankAPI` sends a `POST /api/payments/send-notification/XYZ` request with payment data to `XYZUniversityAPI`.
2. **Processing**: `XYZUniversityAPI` validates and stores the payment in its `Payments` table, then sends a webhook to `MockBankAPI` confirming the status.
3. **Update and Logging**: `MockBankAPI` updates its `Payments` table based on the webhook response; both APIs log the interaction in `AuditLogs`.

---

## 4. Detailed Design Considerations

### 4.1 Scalability

- The system uses a single VM per API, sufficient for current needs. 
- The `UniversityConfigs` table in `mockbank-db` allows easy addition of new universities by storing their API details, supporting future expansion.

### 4.2 Performance

- Batch processing in the payment notification endpoint supports up to 100 payments per request, reducing the number of API calls and improving efficiency for bulk operations.

### 4.3 Error Handling

- Webhook Failures: Use custom retry policies (3 attempts: 1s, 2s, 4s) in WebhookService, logging in AuditLogs and tracking retry attempts.
- API Failures: Handle exceptions in controllers, logging in AuditLogs and sending failure webhooks.
- Logging: Use ILogger and AuditLogs table entries for tracking errors, ensuring webhook-based monitoring.

### 4.4 Security

- **Authentication**: The `X-API-Key` header ensures only authorized requests are processed.
- **Network**: HTTP is currently used, with HTTPS planned to encrypt data in transit. 
  - Firewall rules ensures API calls are restricted to these servers
    - `xyzuniversity-api-server`: Allows inbound traffic on port `5251` from `34.134.214.146` only.
    - `mockbank-api-server`: Allows inbound traffic on port `5000` from `34.31.232.140` only.
- **Data**: Sensitive fields (e.g., `ApiKey`) are stored securely in environment variables or the database.

### 4.5 Compliance

- The `AuditLogs` tables provide a comprehensive record of all API interactions, meeting traceability requirements for audits and regulatory purposes.

---

## 5. Technology Stack

- **Framework**: **ASP.NET Core** provides a robust foundation for building RESTful APIs.
- **Database**: **SQL Server** stores data persistently with schema enforcement.
- **ORM**: **Entity Framework Core** simplifies database interactions with object mapping.
- **Authentication**: Custom API key middleware validates requests.
- **Logging**: Tools like **Serilog** or **Application Insights** capture detailed logs.
- **Real-Time**: Webhooks implemented via **HttpClient** enable near real-time updates.
- **Deployment**: Google Cloud VMs host the APIs without containerization.

---

## Additional Notes
- The project structure includes separate README files:
  - README.md (main): Summarizes MockBankAPI and XYZUniversityAPI.
  - MockBankAPI/README.md: Details MockBankAPI setup and endpoints.
  - XYZUniversityAPI/README.md: Details XYZUniversityAPI setup and endpoints.

This detailed design reflects the current implementation of `MockBankAPI` and `XYZUniversityAPI`, providing a comprehensive view of their architecture, interactions, and operational considerations.
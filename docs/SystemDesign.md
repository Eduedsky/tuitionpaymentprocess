# System Design for Tuition Payment Processing

This document presents a detailed system design for integrating Mock Bank and XYZ University to streamline tuition payment processing. It outlines a robust solution that enables real-time student validation, payment processing, and notification delivery between the two entities, ensuring scalability, security, performance, and compliance. The design leverages ASP.NET Core to create independent APIs, each with its own database, following RESTful principles to facilitate secure and efficient communication for managing student enrollments, fee balances, and transaction data, meeting the needs of a modern educational payment system.

---

## 1. System Overview

The system is designed to enable real-time, reliable communication between Mock Bank (`MockBankAPI`) and XYZ University (`XYZUniversityAPI`) for tuition payment processing. It supports the following key functionalities:

- **Student Validation**: `MockBankAPI` validates student enrollment by sending requests to `XYZUniversityAPI`, receiving real-time responses including `StudentName` and `FeeBalance`.
- **Payment Processing**: `MockBankAPI` checks account balances, debits funds from the payer’s account, credits funds to XYZ University’s account, and sends real-time payment notifications to `XYZUniversityAPI` via webhooks.
- **Real-Time Notifications**: All notifications (payment successes, failures, student validation errors) are handled via webhooks for direct, near real-time communication, ensuring scalability, security, and reliability without a message broker.
- **Batch Processing**: `MockBankAPI` can send multiple payment notifications in a single API call for efficiency, with idempotency handling.
- **Audit Trails**: All API interactions, including errors, core banking calls, and notification attempts, are logged in `AuditLogs` tables for compliance and auditing purposes, with events tracked via logs and webhooks for real-time monitoring.

The system uses a layered architecture (API, business logic, data access, core banking integration, and webhook-based notification service) and follows RESTful principles. It ensures security, scalability, performance, and compliance while handling sensitive student and payment data, focusing on the email’s scope without RabbitMQ.

---

## 2. System Components

### 2.1 `XYZUniversityAPI` API Layer

The API layer in `XYZUniversityAPI` exposes endpoints for `MockBankAPI` to interact with, sending webhooks for real-time notifications.

#### 2.1.1 Endpoints

- **Student Validation Endpoint** (`POST /api/students/validate`):
  - **Function**: Validates whether a student with the given `StudentId` is enrolled, returning their `StudentName` and `FeeBalance`.
  - **Input**:
    - `StudentId` in the request body (JSON): `{ "StudentId": "2020-TWC-1223" }`
    - `X-API-Key` header for authentication.
  - **Output**: JSON response indicating enrollment status, name, and fee balance, or error.
    - Success:
      ```json
      {
        "StudentId": "2020-TWC-1223",
        "StudentName": "John Doe",
        "IsEnrolled": true,
        "FeeBalance": 5000.00
      }
      ```
    - Error: 
      ```json
      {
        "error": "Student not found"
      }
      ```
      (HTTP 404) or `{ "error": "Invalid API key" }` (HTTP 401).
  - **HTTP Status Codes**:
    - `200 OK`: Validation successful.
    - `401 Unauthorized`: Invalid API key.
    - `404 Not Found`: Student not found.
  - **Input Validation**: Validates `StudentId` as a string.
  - **Notification**: Sends webhooks to `MockBankAPI` for validation results (success or failure) for real-time feedback.
  - **Audit Logging**: Logs all interactions in the `AuditLogs` table (`xyzuniversity-db`), sending webhooks to `MockBankAPI` for real-time monitoring.

- **Payment Notification Endpoint** (`POST /api/payments/notification`):
  - **Function**: Receives single or batch payment notifications from `MockBankAPI`, processes them, and sends webhooks.
  - **Input**:
    - Payment details (JSON body, single or array, max 100 payments per batch):
      ```json
      [
        {
          "TransactionId": "TXN123456",
          "StudentId": "2020-TWC-1223",
          "Amount": 5000.00,
          "PaymentDate": "2025-02-25T12:00:00Z"
        }
      ]
      ```
    - `X-API-Key` header.
  - **Output**: JSON array of payment statuses or error.
    - Success: 
      ```json
      [
        { "TransactionId": "TXN123456", "Status": "success" }
      ]
      ```
    - Error: `{ "error": "Invalid input data" }` (HTTP 400).
  - **HTTP Status Codes**:
    - `200 OK`: Payments processed (even if some failed).
    - `400 Bad Request`: Invalid input data (e.g., invalid `StudentId`).
    - `401 Unauthorized`: Invalid API key.
  - **Idempotency**: Uses `TransactionId` to prevent duplicate processing, tracked in the `AuditLogs` table and via webhooks.
  - **Notification**: Sends webhooks to `MockBankAPI` for payment processing results (success or failure).
  - **Audit Logging**: Logs all interactions in the `AuditLogs` table (`xyzuniversity-db`), sending webhooks to `MockBankAPI` for real-time monitoring.

#### 2.1.2 Authentication

- All API requests require an `X-API-Key` header, validated securely (e.g., environment variables).
- Webhook requests use HTTPS with `X-API-Key` or JWT for secure, real-time communication.
- Invalid API keys return `401 Unauthorized`.

---

### 2.2 `XYZUniversityAPI` Business Logic Layer

The business logic layer processes requests asynchronously, sending webhooks for notifications and logging to the `AuditLogs` table.

#### 2.2.1 Key Functions

- **Student Validation** (Async):
  - Retrieve student data using `IStudentRepository.GetStudentByIdAsync`.
  - Check if the student exists; return `404 Not Found` if not.
  - Return `StudentId`, `StudentName`, `IsEnrolled`, and `FeeBalance` in the response.
  - Send webhooks to `MockBankAPI` for validation results via `WebhookService`.
  - Handle exceptions, log in the `AuditLogs` table, and ensure near real-time delivery.

- **Payment Processing** (Async):
  - Validate payment data (`TransactionId`, `StudentId`, `Amount`, `PaymentDate`).
  - Verify `StudentId` exists and is enrolled.
  - Store payment details in `IPaymentRepository`, updating `Status`, `CreatedAt`, `UpdatedAt`.
  - Send webhooks to `MockBankAPI` for payment data (success or failure) via `WebhookService`.
  - Log all steps in the `AuditLogs` table, using custom retry policies for webhook failures (e.g., 3 attempts: 1s, 2s, 4s) for reliability.

---

### 2.3 `XYZUniversityAPI` Data Access Layer

Manages database interactions asynchronously, supporting webhook event triggering and `AuditLogs` storage.

#### 2.3.1 Components

- **Repositories**:
  - **`IStudentRepository`, `StudentRepository`**: Manages `Students` (SQL Server `xyzuniversity-db`).
  - **`IPaymentRepository`, `PaymentRepository`**: Manages `Payments` (SQL Server `xyzuniversity-db`).
  - **`IAuditLogRepository`, `AuditLogRepository`**: Manages `AuditLogs` (SQL Server `xyzuniversity-db`).
- **Database Context**: Uses Entity Framework Core, with indexes and constraints for performance, per Section 2.10.

---

### 2.4 `MockBankAPI` API Layer

The API layer in `MockBankAPI` exposes endpoints to initiate requests, sending and receiving webhooks for notifications.

#### 2.4.1 Endpoints

- **Student Validation Request** (`POST /api/students/validate`):
  - **Function**: Sends a request to `XYZUniversityAPI` to validate a student, receiving responses.
  - **Input**: `StudentId` in the request body (JSON): `{ "StudentId": "2020-TWC-1223" }`, `X-API-Key` header.
  - **Output**: JSON response from `XYZUniversityAPI` or local storage mock.
  - **Notification**: Receives webhooks from `XYZUniversityAPI` for validation results via `WebhookService`.
  - **Audit Logging**: Logs all interactions in the `AuditLogs` table (`mockbank-db`), sending webhooks to `XYZUniversityAPI` for real-time monitoring.

- **Payment Notification Request** (`POST /api/payments/notification`):
  - **Function**: Sends payment notifications to `XYZUniversityAPI`, processes core banking, and receives webhooks.
  - **Input**: Payment details (JSON, single or batch, max 100): `{ "TransactionId": "TXN123456", "StudentId": "2020-TWC-1223", "Amount": 5000.00, "PaymentDate": "2025-02-25T12:00:00Z" }`, `X-API-Key` header.
  - **Output**: JSON response from `XYZUniversityAPI` or local storage.
  - **Notification**: Receives webhooks from `XYZUniversityAPI` for payment results via `WebhookService`.
  - **Audit Logging**: Logs all interactions in the `AuditLogs` table (`mockbank-db`), sending webhooks to `XYZUniversityAPI` for real-time monitoring.

#### 2.4.2 Authentication

- All API requests require an `X-API-Key` header, validated securely.
- Webhook responses from `XYZUniversityAPI` use HTTPS with `X-API-Key` or JWT.

---

### 2.5 `MockBankAPI` Business Logic Layer

Processes requests asynchronously, sending and receiving webhooks for notifications and logging to the `AuditLogs` table.

#### 2.5.1 Key Functions

- **Student Validation** (Async):
  - Send requests to `XYZUniversityAPI`’s `POST /api/students/validate`.
  - Receive webhooks for validation results, store mock data if needed.
  - Handle exceptions, log in the `AuditLogs` table, and ensure near real-time delivery.

- **Payment Processing** (Async):
  - Check account balances via `CoreBankingIntegration` with retries (3 attempts: 1s, 2s, 4s).
  - Debit/credit accounts, store payments in `mockbank-db`.
  - Send payment notifications to `XYZUniversityAPI`’s `POST /api/payments/notification`.
  - Receive webhooks for payment results, updating `PaymentNotifications`.
  - Log all steps in the `AuditLogs` table, using custom retry policies for webhook failures.

---

### 2.6 `MockBankAPI` Data Access Layer

Manages database interactions asynchronously, supporting webhook event handling and `AuditLogs` storage.

#### 2.6.1 Components

- **Repositories**:
  - **`IStudentRepository`, `StudentRepository`**: Manages mock `Students` (SQL Server `mockbank-db`).
  - **`IPaymentRepository`, `PaymentRepository`**: Manages `Payments` and `PaymentNotifications` (SQL Server `mockbank-db`).
  - **`IAuditLogRepository`, `AuditLogRepository`**: Manages `AuditLogs` (SQL Server `mockbank-db`).
- **Database Context**: Uses Entity Framework Core, with indexes and constraints for performance, per Section 2.10.

---

### 2.7 Core Banking Integration Layer (for `MockBankAPI`)

Communicates with the core banking system, ensuring near real-time updates via webhooks.

#### 2.7.1 Key Functions

- **`Task<decimal> GetAccountBalanceAsync`**, **`DebitAccountAsync`**, **`CreditAccountAsync`**:
  - Use secure HTTPS APIs with Polly for retries.
  - Log all calls (successes and failures) in the `AuditLogs` table and send webhooks to `XYZUniversityAPI` for monitoring.
  - Ensure retries and logging for reliability, per Section 2.7.2.

#### 2.7.2 Integration Details

- Uses HTTPS APIs with custom retry logic (3 attempts: 1s, 2s, 4s).
- Logs and sends webhooks for core banking events, ensuring auditability.

---

### 2.8 Webhook Notification Service

Handles all notifications (payment, student validation, audit logs, core banking retries) with custom retries and reliability.

#### 2.8.1 Key Functions

- **Webhooks**:
  - `XYZUniversityAPI` sends webhooks to `MockBankAPI` for payment/validation results.
  - `MockBankAPI` sends webhooks to `XYZUniversityAPI` for core banking updates.
- **Delivery Logic**:
  - Use `HttpClient` with custom retry policies (3 attempts: 1s, 2s, 4s) for failed webhook deliveries, logging errors in `AuditLogs`.
  - Track retry attempts and status in `AuditLogs`, ensuring reliability.
  - Use HTTPS with `X-API-Key` or JWT for secure communication.
- **Configuration**:
  - Configure webhook URLs in `appsettings.json` (e.g., `"WebhookSettings": { "MockBankUrl": "https://mockbank-api/webhooks/payments" }` for `XYZUniversityAPI`).
  - Support multiple webhook endpoints for scalability, per Section 4.1.

---

### 2.9 Audit Logging Middleware

- Logs all audit log events in the `AuditLogs` table for each API and sends webhooks for real-time monitoring by the other API.
- Logs include payment processing, validation, notification attempts, and core banking events, ensuring compliance, per Section 4.5.

---

### 2.10 Databases

Each API maintains its independent database, per the roadmap.

#### 2.10.1 Tables

- **XYZUniversityAPI (`xyzuniversity-db`)**:
  - **Students**:
    - `StudentId` (string, primary key)
    - `StudentName` (string, required, max length 100)
    - `IsEnrolled` (bool, required)
    - `FeeBalance` (decimal(18,2), required)
  - **Payments**:
    - `Id` (int, primary key, auto-generated)
    - `TransactionId` (string, required, unique, max length 50)
    - `StudentId` (string, required, foreign key to `Students.StudentId`)
    - `Amount` (decimal(18,2), required)
    - `PaymentDate` (datetime, required)
    - `Status` (string, required, max length 20)
    - `CreatedAt` (datetime, required)
    - `UpdatedAt` (datetime, required)
  - **AuditLogs**:
    - `Id` (int, primary key, auto-generated)
    - `Timestamp` (datetime, required)
    - `User` (string, required, max length 100)
    - `Endpoint` (string, required, max length 200)
    - `RequestPayload` (string, optional, max length 1000)
    - `ResponseStatus` (int, required)
    - `ErrorMessage` (string, optional, max length 500)

- **MockBankAPI (`mockbank-db`)**:
  - **Students** (mock for validation storage, optional)
    - `Id` (string, primary key)
    - `Name` (string, required, max length 100)
    - `IsEnrolled` (bool, required)
    - `FeeBalance` (decimal(18,2), required)
  - **Payments**:
    - `Id` (int, primary key, auto-generated)
    - `TransactionId` (string, required, unique, max length 50)
    - `StudentId` (string, required)
    - `Amount` (decimal(18,2), required)
    - `PaymentDate` (datetime, required)
  - **PaymentNotifications**:
    - `Id` (int, primary key, auto-generated)
    - `TransactionId` (string, required, unique, max length 50)
    - `StudentId` (string, required)
    - `Amount` (decimal(18,2), required)
    - `PaymentDate` (datetime, required)
    - `SentAt` (datetime, required)
    - `RetryAttempts` (int, required, default 0, max 3) *For webhook retries*
    - `Status` (string, required, max length 20)
  - **AuditLogs**:
    - `Id` (int, primary key, auto-generated)
    - `Timestamp` (datetime, required)
    - `User` (string, required, max length 100)
    - `Endpoint` (string, required, max length 200)
    - `RequestPayload` (string, optional, max length 1000)
    - `ResponseStatus` (int, required)
    - `ErrorMessage` (string, optional, max length 500)

#### 2.10.2 Constraints and Indexes

- Foreign key on `Payments.StudentId` in `xyzuniversity-db` referencing `Students.StudentId`.
- Unique constraint on `Payments.TransactionId`, `PaymentNotifications.TransactionId`, and `AuditLogs.Id` in both databases.
- Indexes on `Students.StudentId`, `Payments.TransactionId`, `PaymentNotifications.TransactionId`, and `AuditLogs.Timestamp` for performance.

---

## 3. System Flow

### 3.1 Student Validation Flow

1. **Request Initiation**:
   - `MockBankAPI` sends `POST /api/students/validate` with `{ "StudentId": "2020-TWC-1223" }` and `X-API-Key` to `XYZUniversityAPI`.
2. **Authentication**:
   - `XYZUniversityAPI` validates `X-API-Key`; returns `401 Unauthorized` if invalid.
   - Logs in `AuditLogs` table, sending a webhook to `MockBankAPI`.
3. **Input Validation**:
   - Validates `StudentId`; returns `400 Bad Request` if invalid.
   - Logs in `AuditLogs` table, sending a webhook to `MockBankAPI`.
4. **Data Retrieval**:
   - Fetches student by `StudentId` from `xyzuniversity-db`; returns `404 Not Found` if not found.
   - Logs in `AuditLogs` table, sending a webhook to `MockBankAPI`.
5. **Response**:
   - Returns `{ "StudentId": "2020-TWC-1223", "StudentName": "John Doe", "IsEnrolled": true, "FeeBalance": 5000.00 }`.
   - Logs in `AuditLogs` table, sending a webhook to `MockBankAPI`.
6. **Notification**:
   - Sends a webhook to `MockBankAPI`’s configured endpoint for validation results (success or failure) via `WebhookService`.
   - Logs in `AuditLogs` table, sending a webhook to `MockBankAPI`.
7. **Audit Logging**:
   - Persists all steps in the `AuditLogs` table (`xyzuniversity-db`), sending webhooks to `MockBankAPI` for real-time monitoring.

---

### 3.2 Payment Processing Flow (Including Batch)

1. **Request Initiation**:
   - `MockBankAPI` sends `POST /api/payments/notification` with payment data (single or batch, max 100) and `X-API-Key` to `XYZUniversityAPI`.
   - Logs in `AuditLogs` table (`mockbank-db`), sending a webhook to `XYZUniversityAPI`.
2. **Authentication**:
   - `XYZUniversityAPI` validates `X-API-Key`; returns `401 Unauthorized` if invalid.
   - Logs in `AuditLogs` table (`xyzuniversity-db`), sending a webhook to `MockBankAPI`.
3. **Input Validation**:
   - Validates each payment; collects errors.
   - Logs in `AuditLogs` table (`xyzuniversity-db`), sending a webhook to `MockBankAPI`.
4. **Student Validation**:
   - Verifies `StudentId` exists and is enrolled in `xyzuniversity-db`.
   - Logs in `AuditLogs` table (`xyzuniversity-db`), sending a webhook to `MockBankAPI`.
5. **Database Storage**:
   - Stores payments in `xyzuniversity-db`, updating `Status`, `CreatedAt`, `UpdatedAt`.
   - Logs in `AuditLogs` table (`xyzuniversity-db`), sending a webhook to `MockBankAPI`.
6. **Notification**:
   - Sends webhooks to `MockBankAPI` for successful payments via `WebhookService`.
   - Logs in `AuditLogs` table (`xyzuniversity-db`), sending a webhook to `MockBankAPI`.
7. **Response**:
   - Returns status for each payment (HTTP 200).
   - Logs in `AuditLogs` table (`xyzuniversity-db`), sending a webhook to `MockBankAPI`.
8. **Audit Logging**:
   - Persists all steps in the `AuditLogs` table (`xyzuniversity-db`), sending webhooks to `MockBankAPI` for real-time monitoring.

---

## 4. Detailed Design Considerations

### 4.1 Scalability

- **API Scaling**: Deploy `XYZUniversityAPI` and `MockBankAPI` behind load balancers (e.g., GCP Load Balancer).
- **Database Scaling**: Use read replicas and sharding for `xyzuniversity-db` and `mockbank-db`.
- **Webhook Scaling**: Use multiple HTTP clients, load-balanced webhook delivery, and batching for high-volume notifications, including `AuditLogs` table writes.

### 4.2 Performance

- **Batch Processing**: Use bulk operations for `Payments` and `AuditLogs` in `XYZUniversityAPI`.
- **Webhooks**: Optimize HTTP client performance with connection pooling and batching for webhooks, minimizing `AuditLogs` latency.

### 4.3 Error Handling

- **Webhook Failures**: Use custom retry policies (3 attempts: 1s, 2s, 4s) in `WebhookService`, logging in `AuditLogs` and tracking retry attempts.
- **API Failures**: Handle exceptions in controllers, logging in `AuditLogs` and sending failure webhooks.
- **Logging**: Use `ILogger` and `AuditLogs` table entries for tracking errors, ensuring webhook-based monitoring.

### 4.4 Security

- **Authentication**: `X-API-Key` for APIs, HTTPS with `X-API-Key` or JWT for webhooks.
- **Data Encryption**: HTTPS for APIs and webhooks, encrypt sensitive data at rest, including `AuditLogs`.
- **Webhook Security**: Secure webhook URLs with HTTPS, restrict access with authentication, protecting `AuditLogs` data.

### 4.5 Compliance

- **Audit Trails**: Maintain real-time logs in `AuditLogs` tables via webhooks and local storage for compliance, defining retention policies.
- **Data Retention**: Define policies for `AuditLogs`, `Payments`, and `Students` in both `xyzuniversity-db` and `mockbank-db`.

---

## 5. Technology Stack

- **Framework**: ASP.NET Core
- **Database**: SQL Server (production) or SQLite (development)
- **ORM**: Entity Framework Core
- **Authentication**: Custom API key middleware
- **Logging**: Serilog or Application Insights
- **Real-Time Communication**: Webhooks via `HttpClient` for notifications, including `AuditLogs`
- **Testing**: xUnit or NUnit, Moq

---

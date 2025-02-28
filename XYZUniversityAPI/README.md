# XYZ University API

The XYZ University API is a RESTful service designed for tuition payment processing, including student validation and payment notifications. It’s built with ASP.NET Core, integrates with SQL Server, and is deployed on a Google Cloud Compute Engine VM.

## Overview

This API provides the following functionality:
- **Student Validation**: `POST /api/students/validate` - Validates student IDs.
- **Payment Notifications**: `POST /api/payments/notification` - Processes payment notifications.
- **Audit Logging**: Logs all requests to the `AuditLogs` table in the database.

The API is accessible at `http://34.31.232.140:5251` on the "xyz-university-api-server" VM within the "xyz-university-network" on Google Cloud.

## Prerequisites

To set up and run the API, ensure you have the following installed:
- **Google Cloud SDK**: For managing the VM and firewall rules.
- **.NET 8 SDK**: Required to build and run the application.
- **SQL Server 2019 Express**: The database engine used by the API.
- **Git**: To clone the repository.
- **curl**: For testing API endpoints.

## Setup Instructions

Follow these steps to set up the API on a Google Cloud VM or a similar environment.

### 1. Clone the Repository
Clone the repository from GitHub:
```bash
git clone https://github.com/Eduedsky/tuitionpaymentprocess.git
cd tuitionpaymentprocess/XYZUniversityAPI
```
If prompted for authentication, use a GitHub Personal Access Token (PAT).

### 2. Set Up SQL Server 2019 Express

Install and configure SQL Server on the VM:

Install SQL Server:
```bash
sudo apt-get update
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl -fsSL https://packages.microsoft.com/config/ubuntu/20.04/mssql-server-2019.list | sudo tee /etc/apt/sources.list.d/mssql-server-2019.list
sudo apt-get install -y mssql-server
sudo /opt/mssql/bin/mssql-conf setup
```
During setup, select the "Express" edition and set the SA password (e.g., Em1!1111).

Enable Remote Access: Edit the SQL Server configuration file:
```bash
sudo nano /etc/mssql/mssql.conf
```

Restart SQL Server:
```bash
sudo systemctl restart mssql-server
```

Create the Database: Create the `xyzuniversity-db` database:
```bash
sqlcmd -S localhost -U SA -P 'Em1!1111' -Q "CREATE DATABASE [xyzuniversity-db]"
```

### 3. Configure appsettings.json
Update the `appsettings.json` file in the project root with the following:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=xyzuniversity-db;User Id=sqlserver;Password=Em1!1111;TrustServerCertificate=True;"
  },
  "Authentication": {
    "ApiKey": "afjrbgt44rw08afsrfb4brj24OBOI89FN4GKDF4BmE"
  },
  "ServerUrl": "http://0.0.0.0:5251"
}
```
Ensure the `DefaultConnection` matches your SQL Server setup.

### 4. Set Up SQL Server Login
Create a SQL Server login for the API:

Create the `sqlserver` Login:
```bash
sqlcmd -S localhost -U SA -P 'Em1!1111' -Q "CREATE LOGIN sqlserver WITH PASSWORD = 'Em1!1111'; USE [xyzuniversity-db]; CREATE USER sqlserver FOR LOGIN sqlserver; ALTER ROLE db_owner ADD MEMBER sqlserver;"
```

### 5. Run Database Migrations
Apply the Entity Framework Core migrations to set up the database schema:

- **Install EF Core Tools** (if not already installed globally):
```bash
dotnet tool install --global dotnet-ef
```

- **Navigate to Project Directory**:
```bash
cd ~/tuitionpaymentprocess/XYZUniversityAPI
```

- **Run Migrations**:
```bash
dotnet ef database update
```
This command applies all pending migrations (e.g., creating `Students`, `Payments`, and `AuditLogs` tables) based on the EF Core model definitions in your project. It uses the connection string from `appsettings.json`.

- **Verify Migration**:
Check that the tables exist:
```bash
sqlcmd -S localhost -U SA -P 'Em1!1111' -Q "USE [xyzuniversity-db]; SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;"
```
You should see `Students`, `Payments`, and `AuditLogs` in the output.

### 6. Build and Run the API
Compile and start the API:
```bash
dotnet build
dotnet run
```
The API will listen on `http://0.0.0.0:5251` and be accessible externally at `http://34.31.232.140:5251`.

### 7. Configure Firewall Rules
Ensure the API port (5251) is open:

Local Firewall (ufw):
```bash
sudo ufw allow 5251/tcp
```

Google Cloud Firewall: Create a firewall rule and tag the VM:
```bash
gcloud compute firewall-rules create allow-api-5251 \
  --network xyz-university-network \
  --action allow \
  --direction ingress \
  --target-tags xyz-university-api-server \
  --source-ranges 0.0.0.0/0 \
  --rules tcp:5251
gcloud compute instances add-tags xyz-university-api-server --tags xyz-university-api-server --zone YOUR_ZONE
```
Replace `YOUR_ZONE` with your VM’s zone (e.g., `us-central1-a`).

## Usage

### Endpoints
- **Validate Student**:
```text
POST /api/students/validate
Headers:
  X-API-Key: afjrbgt44rw08afsrfb4brj24OBOI89FN4GKDF4BmE
  Content-Type: application/json
Body:
  {"StudentId": "2020-TWC-1223"}
```

- **Process Payment Notification**:
```text
POST /api/payments/notification
Headers:
  X-API-Key: afjrbgt44rw08afsrfb4brj24OBOI89FN4GKDF4BmE
  Content-Type: application/json
Body:
  [{"TransactionId": "TXN123", "StudentId": "2020-TWC-1223", "Amount": 1000.00, "PaymentDate": "2025-02-26T22:00:00Z"}]
```

### Testing
- **Local Test**:
```bash
curl -H "X-API-Key: afjrbgt44rw08afsrfb4brj24OBOI89FN4GKDF4BmE" -X POST -d '{"StudentId": "2020-TWC-1223"}' http://localhost:5251/api/students/validate -H "Content-Type: application/json"
```

- **External Test**:
```bash
curl -H "X-API-Key: afjrbgt44rw08afsrfb4brj24OBOI89FN4GKDF4BmE" -X POST -d '{"StudentId": "2020-TWC-1223"}' http://34.31.232.140:5251/api/students/validate -H "Content-Type: application/json"
```

### Troubleshooting
- **Connection Refused**:
  - Verify the API is bound to `0.0.0.0:5251`:
    ```bash
    sudo netstat -tuln | grep 5251
    ```
  - Check firewall status:
    ```bash
    sudo ufw status
    gcloud compute firewall-rules list
    ```

- **Database Connection Errors**:
  - Ensure `appsettings.json` credentials match the SQL Server login and that SQL Server is running:
    ```bash
    sudo systemctl status mssql-server
    ```

## License
MIT License.

---
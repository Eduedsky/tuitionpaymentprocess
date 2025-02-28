# Mock Bank API

The Mock Bank API is a RESTful service designed to simulate a bank's payment initiation and notification system for tuition payment processing. It integrates with the XYZ University API to validate student enrollment and send payment notifications. Built with ASP.NET Core and backed by SQL Server, it’s deployed on a Google Cloud Compute Engine VM.

## Overview

This API provides the following functionality:
- **Student Validation**: `POST /api/payments/validate-student/{universityCode}` - Validates student IDs via the university API.
- **Payment Notifications**: `POST /api/payments/send-notification/{universityCode}` - Sends payment notifications to the university API.
- **Webhook Handling**: `POST /api/payments/webhook` - Receives payment status updates from the university API.
- **Audit Logging**: Logs all requests and interactions to the `AuditLogs` table in the database.

The API is accessible at `http://34.134.214.146:5000` on the "mockbank-api-server" VM within the "mockbank-network" on Google Cloud.

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
cd tuitionpaymentprocess/MockBankAPI
```
If prompted for authentication, use a GitHub Personal Access Token (PAT).

### 2. Set Up SQL Server 2019 Express
Install and configure SQL Server on the VM:

- **Install SQL Server**:
```bash
sudo apt-get update
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl -fsSL https://packages.microsoft.com/config/ubuntu/20.04/mssql-server-2019.list | sudo tee /etc/apt/sources.list.d/mssql-server-2019.list
sudo apt-get install -y mssql-server
sudo /opt/mssql/bin/mssql-conf setup
```
During setup, select the "Express" edition (option 3) and set the SA password to `K@k@2025!`. Accept the license terms.

- **Enable Remote Access**: Edit the SQL Server configuration file to enable TCP on port 1433:
```bash
sudo nano /etc/mssql/mssql.conf
```
Add or modify the `[network]` section:
```
[network]
tcpport = 1433
```
Save and exit (`Ctrl+O`, `Enter`, `Ctrl+X`), then restart SQL Server:
```bash
sudo systemctl restart mssql-server
```

- **Install SQL Server Command-Line Tools**: Required for `sqlcmd`:
```bash
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl -fsSL https://packages.microsoft.com/config/ubuntu/20.04/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
sudo apt-get update
sudo apt-get install -y mssql-tools unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
source ~/.bashrc
```

- **Create the Database**: Create the `mockbank-db` database:
```bash
sqlcmd -S localhost -U SA -P 'K@k@2025!' -Q "CREATE DATABASE [mockbank-db]"
```

### 3. Configure appsettings.json
Update the `appsettings.json` file in the project root with the following:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=mockbank-db;User Id=sqlserver;Password=K@k@2025!;TrustServerCertificate=True;"
  },
  "ServerUrl": "http://0.0.0.0:5000"
}
```
Ensure the `DefaultConnection` matches your SQL Server setup (using the `sqlserver` user and password `K@k@2025!`).

### 4. Set Up SQL Server Login
Create a SQL Server login for the API:
```bash
sqlcmd -S localhost -U SA -P 'K@k@2025!' -Q "CREATE LOGIN sqlserver WITH PASSWORD = 'K@k@2025!'; USE [mockbank-db]; CREATE USER sqlserver FOR LOGIN sqlserver; ALTER ROLE db_owner ADD MEMBER sqlserver;"
```

### 5. Run Database Migrations
Apply Entity Framework Core migrations to set up the database schema:

- **Install EF Core Tools** (if not already installed globally):
```bash
dotnet tool install --global dotnet-ef
```

- **Navigate to Project Directory**:
```bash
cd ~/tuitionpaymentprocess/MockBankAPI
```

- **Run Migrations**:
```bash
dotnet ef database update
```
This command applies all pending migrations (e.g., creating `UniversityConfigs`, `Payments`, and `AuditLogs` tables) based on the EF Core model definitions in your project. It uses the connection string from `appsettings.json`.

- **Verify Migration**:
Check that the tables exist:
```bash
sqlcmd -S localhost -U SA -P 'K@k@2025!' -Q "USE [mockbank-db]; SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;"
```
You should see `UniversityConfigs`, `Payments`, and `AuditLogs` in the output.

### 6. Build and Run the API
Compile and start the API:
```bash
dotnet build
dotnet run
```
The API will listen on `http://0.0.0.0:5000` and be accessible externally at `http://34.134.214.146:5000`.

### 7. Configure Firewall Rules
Ensure the API port (5000) is open:

- **Local Firewall (ufw)**:
```bash
sudo ufw allow 5000/tcp
```

- **Google Cloud Firewall**: Create a firewall rule and tag the VM:
```bash
gcloud compute firewall-rules create allow-mockbank-5000 \
  --network mockbank-network \
  --action allow \
  --direction ingress \
  --target-tags mockbank-api-server \
  --source-ranges 0.0.0.0/0 \
  --rules tcp:5000
gcloud compute instances add-tags mockbank-api-server --tags mockbank-api-server --zone us-central1-a
```
Replace `us-central1-a` with your VM’s zone (e.g., check with `gcloud compute instances list`).

## Usage

### Endpoints
- **Validate Student**:
```text
POST /api/payments/validate-student/XYZ
Headers:
  Content-Type: application/json
Body:
  {"StudentId": "2020-TWC-1223"}
```

- **Send Payment Notification**:
```text
POST /api/payments/send-notification/XYZ
Headers:
  Content-Type: application/json
Body:
  [{"TransactionId": "TXN123", "StudentId": "2020-TWC-1223", "Amount": 1000.00, "PaymentDate": "2025-02-26T22:00:00Z"}]
```

- **Webhook Endpoint**:
```text
POST /api/payments/webhook
Headers:
  Content-Type: application/json
Body:
  [{"TransactionId": "TXN123", "Status": "success"}]
```

### Testing
- **Local Test**:
```bash
curl -X POST -d '{"StudentId": "2020-TWC-1223"}' http://localhost:5000/api/payments/validate-student/XYZ -H "Content-Type: application/json"
```

- **External Test**:
```bash
curl -X POST -d '{"StudentId": "2020-TWC-1223"}' http://34.134.214.146:5000/api/payments/validate-student/XYZ -H "Content-Type: application/json"
```

### Troubleshooting
- **Connection Refused**:
  - Verify the API is bound to `0.0.0.0:5000`:
    ```bash
    sudo netstat -tuln | grep 5000
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
  - Verify database existence:
    ```bash
    sqlcmd -S localhost -U SA -P 'K@k@2025!' -Q "SELECT name FROM sys.databases WHERE name = 'mockbank-db'"
    ```

- **Migration Errors**:
  - Check EF Core tools installation:
    ```bash
    dotnet ef --version
    ```
  - Review migration logs for details:
    ```bash
    dotnet ef database update --verbose
    ```

## License
MIT License.
```

---
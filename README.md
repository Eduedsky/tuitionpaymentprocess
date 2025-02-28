# Tuition Payment Processing System

This project implements a tuition payment processing system that integrates **MockBankAPI** and **XYZUniversityAPI** to facilitate real-time student validation and payment notifications. Hosted on Google Cloud VMs, the system uses RESTful APIs and webhooks to ensure secure, reliable communication between a mock bank and a university.

## Overview

- **Purpose**: Enable seamless tuition payment processing by validating student enrollment and notifying the university of completed payments.
- **Components**:
  - **MockBankAPI**: Hosted at `34.134.214.146:5000`, manages payment initiation and notifications.
  - **XYZUniversityAPI**: Hosted at `34.31.232.140:5251`, handles student validation and payment record updates.
- **Key Features**:
  - Real-time student validation and payment updates via webhooks.
  - Secure communication with API key authentication.
  - Comprehensive audit logging for compliance and debugging.
- **Deployment**: Each API runs on a dedicated Google Cloud VM with SQL Server databases.

## Project Structure

- **[MockBankAPI/README.md](MockBankApi/README.md)**: Details setup and endpoints for the bank API.
- **[XYZUniversityAPI/README.md](XYZUniversityAPI/README.md)**: Covers setup and endpoints for the university API.

## Getting Started

1. Review the main system design document for architectural details. [Docs/System_Design.md](docs/README.md)
2. Refer to the individual API READMEs for setup instructions and API usage.

For more information, explore the linked READMEs or contact the project maintainers.
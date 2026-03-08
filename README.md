# 🔐 Auth System - Clean Architecture & Identity

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=json-web-tokens&logoColor=white)

This is a robust **Authentication & Authorization System** built with **ASP.NET Core 8**, following the principles of **Clean Architecture** to ensure scalability and high security.

---

## 🚀 Key Features

* **Identity Management:** Full integration with **ASP.NET Core Identity** for managing users, roles, and authorization policies.
* **Secure Authentication:** Secure endpoints using **JWT (JSON Web Tokens)** with custom validation.
* **Token Management:** Advanced **Refresh Tokens** strategy to manage user sessions efficiently.
* **Email Service:** Automated email workflows using **FluentEmail** for Account Activation and Password Reset, featuring **HTML Templates**.
* **Inactivity Monitor:** A specialized **Background Service** that monitors user inactivity to enhance account protection.
* **Global Exception Handling:** Custom **Middleware** for standardized API error responses.

---

## 🏗️ Architecture Layers

The project follows a **3-Layer Clean Architecture** pattern to maintain a strict **Separation of Concerns**:

1. **Auth.Api:** The entry point (Controllers, Middlewares, and Program.cs configuration).
2. **Auth.Core:** The heart of the system, containing **Entities**, Interfaces, DTOs, and Business Logic.
3. **Auth.Infrastructure:** Handles **Persistence** (EF Core), Database Migrations, and External Service implementations (Email).

---

## 🛠️ Prerequisites & Tools

* **SDK:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* **Database:** [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
* **Testing:** [Postman](https://www.postman.com/downloads/) (Collection included).

---

## 🚦 Configuration & Setup

1. **Database & Services:** Update the connection strings and service credentials in the `appsettings.json` file located in the **Auth.Api** project.
2. **Database Sync:** Make sure to update your database to create the Identity tables before running the app.

---

## 📥 How to Clone

```bash
git clone https://github.com/ziad-ahmed-abdulrahman/auth-system.git

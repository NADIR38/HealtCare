# 🏥 Healthcare Management System — Backend API

A comprehensive, production-ready RESTful API for managing healthcare operations including patient records, appointments, prescriptions, lab tests, invoices, payments, and real-time notifications.

Built with **ASP.NET Core 9** following a clean **3-layer architecture** (API → Application → Infrastructure).

---

## 📐 Architecture Overview

```
HealthCare/                         → API Layer (Controllers, Middleware, Auth)
HealthcareSystem.Application/       → Application Layer (Interfaces, DTOs, Models)
HealthcareSystem.Infrastructure/    → Infrastructure Layer (Services, DbContext, Migrations)
HealthcareSystem.Domain/            → Domain Layer (Entities, Enums)
```

---

## ✨ Features

- **Authentication & Authorization** — JWT-based auth with refresh tokens and role-based access control
- **Patient Management** — Full CRUD for patients including medical history tracking
- **Doctor Management** — Doctor profiles, schedules, leave management, and availability checking
- **Appointment Scheduling** — Book, update, cancel, and track appointment status with conflict detection
- **Medical Records** — Create and manage medical records with vital signs
- **Prescriptions** — Issue and manage prescriptions with multi-item support
- **Lab Tests** — Request lab tests, update results, and track status
- **Invoices & Payments** — Auto-generate invoices from appointments, process payments, and export PDFs
- **Real-time Notifications** — SignalR-powered live notifications
- **Email Notifications** — SMTP email alerts for appointments and reminders
- **Background Jobs** — Hangfire-powered scheduled tasks (appointment reminders, daily reports, overdue invoice alerts)
- **Caching** — Redis or in-memory distributed caching with cache invalidation
- **Rate Limiting** — IP-based rate limiting (200 req/min, 2000 req/hour)
- **PDF Generation** — Invoice and report PDF export via QuestPDF
- **Role-based Dashboards** — Separate dashboard views for Admin, Doctor, and Patient
- **API Versioning** — Built-in support for API versioning
- **Swagger UI** — Full interactive API documentation

---

## 🛠️ Tech Stack

| Category | Technology |
|---|---|
| Framework | ASP.NET Core 9 |
| ORM | Entity Framework Core 9 |
| Database | MySQL (Pomelo provider) |
| Caching | Redis / In-memory fallback |
| Auth | JWT Bearer + Refresh Tokens |
| Real-time | SignalR |
| Background Jobs | Hangfire + MySQL Storage |
| Email | MailKit / MimeKit |
| PDF Generation | QuestPDF |
| Password Hashing | BCrypt.Net |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| Logging | Serilog |
| API Docs | Swagger / Swashbuckle |
| Rate Limiting | AspNetCoreRateLimit |

---

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- MySQL 8.0+
- Redis (optional — falls back to in-memory cache if not configured)

---

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/healthcare-system.git
cd healthcare-system/fullcode
```

### 2. Configure `appsettings.json`

Open `HealthCare/appsettings.json` and fill in the required values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HealthcareDB;User=root;Password=yourpassword;",
    "HangFire": "Server=localhost;Database=HangfireDB;User=root;Password=yourpassword;",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "HealthcareSystemAPI",
    "Audience": "HealthcareSystemClient",
    "ExpiryInHours": "2"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "Healthcare System",
    "SenderEmail": "your-email@gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://your-frontend-domain.com"
    ]
  }
}
```

> **Note:** Remove the `Redis` connection string entirely if you do not have Redis — the app will automatically fall back to in-memory caching.

### 3. Apply Database Migrations

```bash
cd HealthCare
dotnet ef database update --project ../HealthcareSystem.Infrastructure
```

### 4. Run the Application

```bash
dotnet run --project HealthCare
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`
- Hangfire Dashboard: `http://localhost:5000/Hangfire` *(Development only)*
- SignalR Hub: `ws://localhost:5000/notificationHub`

---

## 🔐 Roles & Permissions

The system supports the following roles with scoped permissions:

| Role | Description |
|---|---|
| `Admin` | Full system access, user management, dashboards |
| `Doctor` | Manage own appointments, prescriptions, lab tests, medical records |
| `Patient` | View own records, book appointments, view invoices |
| `Nurse` | Limited clinical access |
| `Receptionist` | Appointment and scheduling management |

---

## 📡 API Endpoints

### Authentication — `/Api/Auth`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/register` | Register a new user |
| POST | `/login` | Login and receive JWT + refresh token |
| POST | `/refresh` | Refresh an expired access token |

### Patients — `/api/Patients`
| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | List all patients |
| GET | `/{id}` | Get patient by ID |
| POST | `/` | Create a new patient |
| PUT | `/{id}` | Update patient details |
| DELETE | `/{id}` | Delete a patient |

### Doctors — `/api/Doctor`
| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | List all doctors |
| GET | `/{id}` | Get doctor by ID |
| POST | `/` | Create a new doctor |
| PUT | `/{id}` | Update doctor details |
| POST | `/{id}/schedule` | Set doctor availability schedule |
| POST | `/{id}/leave` | Request doctor leave |
| GET | `/{id}/timeslots` | Get available time slots |
| POST | `/check-availability` | Check doctor availability |

### Appointments — `/api/Appointments`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Book a new appointment |
| GET | `/` | List appointments (filtered by role) |
| GET | `/{id}` | Get appointment details |
| PUT | `/{id}` | Update appointment |
| PATCH | `/{id}/status` | Update appointment status |
| POST | `/{id}/cancel` | Cancel an appointment |

### Medical Records — `/api/MedicalRecords`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Create a medical record |
| GET | `/patient/{patientId}` | Get records for a patient |
| GET | `/{id}` | Get record by ID |
| PUT | `/{id}` | Update medical record |

### Prescriptions — `/api/Prescriptions`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Issue a prescription |
| GET | `/patient/{patientId}` | Get prescriptions for a patient |
| GET | `/{id}` | Get prescription by ID |

### Lab Tests — `/api/LabTests`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Request a lab test |
| GET | `/patient/{patientId}` | Get lab tests for a patient |
| PUT | `/{id}/result` | Update lab test result |

### Invoices — `/api/Invoices`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Create an invoice |
| POST | `/from-appointment` | Auto-create invoice from appointment |
| GET | `/` | List all invoices |
| GET | `/{id}` | Get invoice details |
| GET | `/{id}/pdf` | Export invoice as PDF |
| PUT | `/{id}` | Update invoice |

### Payments — `/api/Payments`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Process a payment |
| GET | `/invoice/{invoiceId}` | Get payments for an invoice |

### Notifications — `/api/Notifications`
| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Get current user's notifications |
| PATCH | `/{id}/read` | Mark notification as read |
| POST | `/` | Create a notification |

### Dashboard — `/api/Dashboard`
| Method | Endpoint | Description |
|---|---|---|
| GET | `/admin` | Admin statistics and overview |
| GET | `/doctor` | Doctor-specific dashboard |
| GET | `/patient` | Patient-specific dashboard |

### Users — `/api/Users`
| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | List all users (Admin) |
| GET | `/{id}` | Get user by ID |
| PUT | `/{id}` | Update user |
| POST | `/assign-role` | Assign role to user |

### Background Jobs — `/api/BackgroundJobs`
| Method | Endpoint | Description |
|---|---|---|
| POST | `/trigger-reminders` | Manually trigger appointment reminders |
| POST | `/trigger-daily-report` | Manually trigger daily report job |

---

## 🔔 Real-time Notifications (SignalR)

Connect to the SignalR hub at `/notificationHub` to receive real-time push notifications.

A test client is available at `/signalr-test.html` in development.

---

## ⚙️ Background Jobs (Hangfire)

Scheduled tasks are configured via `appsettings.json` under `BackgroundJobSettings`:

| Job | Schedule | Description |
|---|---|---|
| Appointment Reminders | Daily | Sends email reminders N hours before appointments |
| Overdue Invoice Alerts | Daily | Notifies patients with unpaid overdue invoices |
| Daily Report | Configurable time | Generates and emails daily system report |

> Hangfire is disabled in production (uses in-memory mode). The Hangfire Dashboard is accessible at `/Hangfire` in development.

---

## 🗂️ Project Structure

```
fullcode/
├── HealthCare/                          # API Entry Point
│   ├── Controllers/                     # API Controllers
│   ├── Attributes/                      # Custom action attributes (Cache, RateLimit)
│   ├── Authorization/                   # Custom authorization handlers
│   ├── Extensions/                      # Hangfire job scheduler configuration
│   ├── Hubs/                            # SignalR hub implementation
│   ├── MiddleWares/                     # Global exception middleware
│   ├── wwwroot/                         # Static files (SignalR test page)
│   ├── Program.cs                       # App configuration and DI setup
│   └── appsettings.json                 # Application configuration
│
├── HealthcareSystem.Application/        # Application Layer
│   ├── Dto/                             # Request/Response DTOs
│   ├── Interfaces/                      # Service contracts
│   ├── Hubs/                            # SignalR hub interface
│   └── Models/                          # Shared models (e.g., EmailMessage)
│
├── HealthcareSystem.Infrastructure/     # Infrastructure Layer
│   ├── Data/                            # ApplicationDbContext
│   ├── Services/                        # Service implementations
│   ├── Helpers/                         # DB helpers and custom exceptions
│   └── Migrations/                      # EF Core migrations
│
└── HealthcareSystem.Domain/             # Domain Layer
    ├── Entities/                        # Database entity models
    └── Enums/                           # Domain enumerations
```

---

## 🌍 Environment Notes

| Environment | Hangfire | HTTPS Redirect | CORS |
|---|---|---|---|
| Development | MySQL storage + Dashboard | Enabled | Allow All |
| Production | Disabled | Disabled (SSL at proxy) | Configured origins only |

---

## 🔒 Security

- Passwords are hashed using **BCrypt**
- JWT tokens expire after 2 hours; refresh tokens are supported
- All sensitive endpoints require authentication
- Custom `DoctorOwnerOrAdmin` policy enforces resource ownership
- IP-based rate limiting prevents abuse
- Global exception middleware prevents stack trace leakage

---

## 📦 Key NuGet Packages

| Package | Purpose |
|---|---|
| `Pomelo.EntityFrameworkCore.MySql` | MySQL database provider |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT authentication |
| `StackExchange.Redis` | Redis distributed cache |
| `Hangfire` + `Hangfire.MySqlStorage` | Background job processing |
| `QuestPDF` | PDF invoice generation |
| `MailKit` | Email sending via SMTP |
| `BCrypt.Net-Next` | Secure password hashing |
| `AutoMapper` | Object-to-object mapping |
| `FluentValidation` | Request validation |
| `AspNetCoreRateLimit` | IP rate limiting |
| `Serilog.AspNetCore` | Structured logging |

---

## 📄 License

This project is licensed under the MIT License.

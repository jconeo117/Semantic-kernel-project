# 🏥 ClinicSimulator

**Multi-tenant AI receptionist system** powered by [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/) — an intelligent booking assistant that adapts to any service business through configuration alone.

> Built with .NET 9 · Semantic Kernel · Clean Architecture · Multi-tenant by design

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Architecture](#-architecture)
- [Project Structure](#-project-structure)
- [Key Features](#-key-features)
- [Getting Started](#-getting-started)
- [Configuration](#-configuration)
- [API Reference](#-api-reference)
- [Security](#-security)
- [Testing](#-testing)
- [Tech Stack](#-tech-stack)

---

## 🔎 Overview

ClinicSimulator exposes a conversational API where an AI agent acts as a receptionist: it can search available time slots, book appointments, cancel bookings, and retrieve appointment information — all while enforcing **patient identity validation** and **multi-tenant isolation**.

Each tenant (e.g. *Clínica Vista Clara*, *Salón Bella*) is fully configured via `appsettings.json` and gets its own data store, service providers, and system prompt — **zero code changes required to onboard a new business**.

```
User ──▶ POST /api/chat ──▶ TenantMiddleware ──▶ SessionContextMiddleware
                                │                         │
                          Resolve tenant            Init patient session
                                │                         │
                                ▼                         ▼
                         InputGuard ──▶ RecepcionistAgent ──▶ OutputFilter
                                              │
                               ┌──────────────┼──────────────┐
                               ▼              ▼              ▼
                        BookingPlugin  BusinessInfoPlugin  AuditLogger
```

---

## 🏗 Architecture

The solution follows **Clean Architecture** with four clearly separated layers:

```
ClinicSimulator.sln
│
├── ClinicSimulator.Core      ← Domain models, interfaces, business logic
├── ClinicSimulator.AI        ← Semantic Kernel agents and plugins
├── ClinicSimulator.Api       ← ASP.NET Core Web API (entry point)
└── ClinicSimulator.Tests     ← Unit and integration tests
```

### Dependency Flow

```
Api ──▶ AI ──▶ Core
Tests ──▶ AI ──▶ Core
```

> `Core` has **zero external dependencies**. `AI` depends only on `Core` and Semantic Kernel. `Api` composes everything via DI.

---

## 📁 Project Structure

### `ClinicSimulator.Core`

| Directory      | Description                                      |
|----------------|--------------------------------------------------|
| `Models/`      | `BookingRecord`, `ServiceProvider`, `TimeSlot`, `TenantConfiguration`, `TenantContext`, `AuditEntry` |
| `Adapters/`    | `IClientDataAdapter` interface + `InMemoryClientAdapter` + `ClientDataAdapterFactory` (tenant-scoped) |
| `Services/`    | `IBookingService` / `BookingService` — slot availability, booking CRUD, patient lookups |
| `Session/`     | `ISessionContext` / `SessionContext` — per-request identity tracking |
| `Security/`    | `IInputGuard` (prompt injection), `IOutputFilter` (PII redaction), `IAuditLogger` |
| `Tenant/`      | `ITenantResolver` / `InMemoryTenantResolver` — multi-tenant resolution |
| `Repositories/`| `IChatSessionRepository` — chat history persistence |

### `ClinicSimulator.AI`

| Directory        | Description                                    |
|------------------|------------------------------------------------|
| `Agents/`        | `RecepcionistAgent` — orchestrates LLM + tool calling |
| `Plugins/`       | `BookingPlugin` (6 kernel functions) · `BusinessInfoPlugin` (clinic metadata) |
| `Configuration/` | `KernelFactory` — provider-agnostic kernel builder |
| `Loggin/`        | `FunctionInvocationFilter` — SK function call logging |

### `ClinicSimulator.Api`

| Directory       | Description                                     |
|-----------------|-------------------------------------------------|
| `Controllers/`  | `ChatController` (main chat endpoint) · `AuditController` (audit logs) |
| `Middleware/`    | `TenantMiddleware` · `SessionContextMiddleware`  |
| `Swagger/`       | `TenantHeaderOperationFilter` — auto-adds `X-Tenant-Id` header in Swagger UI |

### `ClinicSimulator.Tests`

| Directory        | Description                                    |
|------------------|------------------------------------------------|
| `Adapters/`      | Adapter factory and in-memory adapter tests    |
| `Session/`       | 10 test cases for patient identity validation  |
| `Security/`      | Prompt injection, data filter, audit, plugin security tests |
| `Integration/`   | Tenant middleware integration tests            |
| `Plugins/`       | BusinessInfoPlugin tests                       |
| `Services/`      | BookingService and PromptBuilder tests         |
| `Tenant/`        | Tenant resolver tests                          |

---

## ⭐ Key Features

### 🤖 AI Receptionist Agent

- Conversational booking assistant via Semantic Kernel
- Automatic **tool calling** — the LLM decides when to invoke booking functions
- Supports **Google Gemini** and **GROQ/OpenAI-compatible** providers
- Dynamic system prompt generated per-tenant with business context

### 📅 Booking System

| Function                     | Description                                         |
|------------------------------|-----------------------------------------------------|
| `FindAvailableSlots`         | Search by provider name, specialty, or "any"        |
| `GetFirstAvailableAppointment` | Scans ahead N days for earliest opening           |
| `BookAppointment`            | Full validation: name, patientId, phone, email, reason |
| `CancelAppointment`          | Ownership-verified cancellation                     |
| `GetAppointmentInfo`         | Lookup by confirmation code **or** patient document |
| `GetAllAppointmentsByDate`   | Today's schedule (privacy-safe: no client names)    |

### 🔐 Patient Identity Validation

- `ISessionContext` tracks validated patient IDs and confirmation codes per request
- `BookAppointment` requires `patientId` and auto-validates in session
- `GetAppointmentInfo` and `CancelAppointment` enforce ownership verification
- Optional `X-Patient-Id` header for pre-validation via middleware

### 🏢 Multi-Tenant Architecture

- Each tenant is isolated: own data adapter, providers, business info, and prompt
- Resolved at the middleware level via `X-Tenant-Id` HTTP header
- `ClientDataAdapterFactory` creates tenant-scoped data stores
- **Zero code changes** to add a new tenant — just update `appsettings.json`

### 🛡️ Security Pipeline

```
Input ──▶ PromptInjectionGuard ──▶ LLM Agent ──▶ SensitiveDataFilter ──▶ Output
                                                         │
                                                   AuditLogger
```

| Component              | Responsibility                                  |
|------------------------|-------------------------------------------------|
| `PromptInjectionGuard` | Detects and blocks prompt injection attempts     |
| `SensitiveDataFilter`  | Redacts PII and internal patterns from responses |
| `InMemoryAuditLogger`  | Logs all events: messages, blocks, filtered output |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An LLM provider: **GROQ** (local via [LM Studio](https://lmstudio.ai/)) or **Google Gemini** API key

### Setup

```bash
# 1. Clone the repository
git clone https://github.com/jconeo117/Semantic-kernel-project.git
cd Semantic-kernel-project

# 2. Restore dependencies
dotnet restore

# 3. Configure AI provider (see Configuration section below)

# 4. Run the API
dotnet run --project src/ClinicSimulator.Api

# 5. Open Swagger UI
# Navigate to https://localhost:{port}/swagger
```

### Run Tests

```bash
dotnet test src/ClinicSimulator.Tests/
```

---

## ⚙️ Configuration

All configuration lives in [`src/ClinicSimulator.Api/appsettings.json`](src/ClinicSimulator.Api/appsettings.json).

### AI Provider

```jsonc
{
  "AI": {
    "Provider": "GROQ",        // Options: "Google" | "GROQ"

    "GROQ": {
      "Endpoint": "http://localhost:1234/v1",
      "ModelId": "meta-llama-3.1-8b-instruct"
    }
    // For Google Gemini, configure API key via user-secrets:
    // dotnet user-secrets set "AI:Google:ApiKey" "your-key"
  }
}
```

### Adding a New Tenant

Add a new entry under `"Tenants"` in `appsettings.json`:

```jsonc
{
  "Tenants": {
    "my-business": {
      "BusinessName": "My Business",
      "BusinessType": "clinic",
      "Address": "...",
      "Phone": "...",
      "WorkingHours": "Mon-Fri: 9 AM - 6 PM",
      "Services": ["Service A", "Service B"],
      "AcceptedInsurance": [],
      "Pricing": { "Service A": "$50" },
      "Providers": [
        {
          "Id": "PROV01",
          "Name": "Dr. Example",
          "Role": "General",
          "WorkingDays": ["Monday", "Tuesday", "Wednesday"],
          "StartTime": "09:00",
          "EndTime": "17:00",
          "SlotDurationMinutes": 30
        }
      ]
    }
  }
}
```

---

## 📡 API Reference

### `POST /api/chat`

Conversational endpoint. Requires `X-Tenant-Id` header.

**Headers:**

| Header         | Required | Description                          |
|----------------|----------|--------------------------------------|
| `X-Tenant-Id`  | ✅       | Tenant identifier (e.g. `clinica-vista-clara`) |
| `X-Patient-Id` | ❌       | Optional patient pre-validation      |

**Request:**

```json
{
  "sessionId": "00000000-0000-0000-0000-000000000000",
  "message": "Quiero agendar una cita con el Dr. Ramírez"
}
```

> Use `sessionId: "00000000..."` for a new conversation. The API returns the assigned `sessionId` to use in subsequent messages.

**Response:**

```json
{
  "sessionId": "a1b2c3d4-...",
  "response": "¡Claro! ¿Para qué fecha le gustaría la cita con el Dr. Ramírez?"
}
```

### `GET /api/audit`

Returns all audit log entries. Useful for debugging and monitoring.

---

## 🧪 Testing

The project includes **15+ automated tests** across multiple categories:

| Category                  | Tests | Coverage                                    |
|---------------------------|-------|---------------------------------------------|
| Session & Identity        | 10    | Patient validation, ownership, case-insensitivity |
| Security                  | 5+    | Prompt injection, PII filtering, audit logging |
| Adapters                  | 2+    | Factory resolution, in-memory CRUD          |
| Integration               | 1+    | Tenant middleware HTTP pipeline             |
| Plugins                   | 1+    | BusinessInfoPlugin metadata                 |
| Services                  | 2+    | BookingService, PromptBuilder               |

```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter "FullyQualifiedName~Session"
dotnet test --filter "FullyQualifiedName~Security"
```

---

## 🛠 Tech Stack

| Technology                                                                 | Purpose                     |
|----------------------------------------------------------------------------|-----------------------------|
| [.NET 9](https://dotnet.microsoft.com/)                                    | Runtime & framework         |
| [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)      | AI orchestration & tool calling |
| [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)            | Web API                     |
| [xUnit](https://xunit.net/)                                               | Testing framework           |
| [Moq](https://github.com/devlooped/moq)                                   | Mocking library             |
| [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)  | Swagger / OpenAPI           |

---

## 📄 License

This project is for educational and demonstration purposes.

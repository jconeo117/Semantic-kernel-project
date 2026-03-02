# 🤖 ReceptionistAgent

**Enterprise-grade Multi-Tenant AI Receptionist System** powered by [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/).

This system acts as an intelligent, conversational booking assistant that easily adapts to any service-based business (clinics, salons, workshops, etc.) entirely through configuration.

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
- [Security & Rate Limiting](#-security--rate-limiting)
- [Testing](#-testing)
- [Tech Stack](#-tech-stack)

---

## 🔎 Overview

`ReceptionistAgent` exposes a robust REST API where an AI agent interacts with users to handle scheduling: it searches available time slots, books appointments, verifies patient/customer identity, and manages cancellations.

Each tenant (e.g., *Clínica Vista Clara*, *Salón Bella*) is fully isolated. A tenant can operate purely in-memory (for testing) or connect to a dedicated **SQL Server** database with custom field mappings — **zero code changes required to onboard a new business**.

```text
User ──▶ POST /api/chat ──▶ API Key Auth ──▶ Rate Limiter ──▶ Tenant/Session Middleware
                                                                        │
                                   ┌────────────────────────────────────┘
                                   ▼
                            Input Guard (Prompt Injection)
                                   │
                                   ▼
             Chat Orchestrator ──▶ AI Agent (Semantic Kernel)
                                   │
                        ┌──────────┼──────────┐
                        ▼          ▼          ▼
                  BookingPlugin   ...    BusinessInfoPlugin
                        │
                        ▼
                Output Filter (PII Redaction)
                        │
                        ▼
            Audit Logger (Database/Memory)
```

---

## 🏗 Architecture

The solution implements **Clean Architecture**, ensuring the core domain is independent of frameworks, databases, and UI.

```text
ReceptionistAgent.sln
│
├── ReceptionistAgent.Core        ← Domain models, core interfaces, tenant & session context
├── ReceptionistAgent.AI          ← Semantic Kernel setup, plugins, AI Strategy Pattern
├── ReceptionistAgent.Connectors  ← SQL Server implementations (Dapper), Adapter Factories
├── ReceptionistAgent.Api         ← ASP.NET Core Web API, Middleware, Rate Limiting
└── ReceptionistAgent.Tests       ← Unit & Integration Tests (xUnit, Moq)
```

### Dependency Flow
- `Core` has **no external dependencies**.
- `Connectors` and `AI` depend on `Core`.
- `Api` composes everything via Dependency Injection.

---

## 📁 Project Structure

### `ReceptionistAgent.Core`
- **Models:** `BookingRecord`, `ServiceProvider`, `AuditEntry`, `TenantConfiguration`
- **Interfaces:** `IClientDataAdapter`, `IAuditLogger`, `IInputGuard`, `IChatSessionRepository`
- **Logic:** Multi-tenant resolution, Session tracking (`ISessionContext`), Security guards.

### `ReceptionistAgent.AI`
- **Agents:** Semantic Kernel integration. Supports Strategy Pattern for choosing AI Providers (`GoogleAIConfigurator`, `GroqAIConfigurator`).
- **Plugins:** 
  - `BookingPlugin`: Orchestrates the booking workflow.
  - `BusinessInfoPlugin`: Retrieves tenant rules, pricing, and services.
- **Logging:** `FunctionInvocationFilter` to trace LLM tool calls.

### `ReceptionistAgent.Connectors`
- **SQL Adapters:** Pure `Dapper` implementations for SQL Server (`SqlClientDataAdapter`, `SqlChatSessionRepository`, `SqlAuditLogger`).
- **Mapping:** `FieldMappingConfig` translates domain models to custom SQL tables per-tenant.
- **Factory:** Resolves whether a tenant uses `InMemory` or `SqlServer` based on their specific configuration.

### `ReceptionistAgent.Api`
- **Controllers:** `ChatController` (main endpoint) and `AuditController`.
- **Middleware:** `TenantMiddleware`, `SessionContextMiddleware`.
- **Security:** `ApiKeyAuthFilter`, `RateLimiter` (Global 60 req/min).

---

## ⭐ Key Features

### 🤖 LLM Strategy Pattern
- Dynamically swaps between **Google Gemini** and **GROQ (Meta Llama/OpenAI-compatible)**.
- Automatic **Tool Calling** — the LLM strictly follows your business rules to invoke C# functions.

### 🏢 Multi-Tenant & Dynamic SQL Mapping
- Connect multiple businesses to the same API instance.
- Tenants can use their own existing SQL schemas. The `FieldMappingConfig` maps C# properties (like `ClientName`) to custom SQL columns (like `nombre_paciente` or `customer_name_db`).

### 📅 Advanced Booking Rules
- `GetFirstAvailableAppointment`: Scans up to 30 days ahead intelligently.
- `FindAvailableSlots`: Filter by provider specialty or name.
- `CancelAppointment`: Requires identity verification.

### 🔐 Robust Security & Audit Pipeline
- **API Key Auth:** Secures the endpoints natively.
- **Rate Limiting:** Protects against abuse (60 requests/minute fixed window).
- **Session Context:** Binds confirmed appointments to the user's current session via Document ID or Confirmation Code. Prevents unauthorized data access.
- **Prompt Injection Guard:** Pre-flight analysis blocks jailbreak attempts.
- **Output Filter:** Redacts Sensitive Data (PII) before it leaves the API.
- **Audit Logger:** Persists all LLM interactions, blocked prompts, and redacted messages to SQL or Memory.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (Optional, if using SQL configurations)
- An API Key for either **Google Gemini** or **GROQ**.

### Setup

```bash
# 1. Clone the repository
git clone https://github.com/jconeo117/Semantic-kernel-project.git
cd Semantic-kernel-project

# 2. Restore dependencies
dotnet restore

# 3. Add your AI API Keys using dotnet user-secrets
dotnet user-secrets set "AI:Google:ApiKey" "your-google-api-key"
# OR
dotnet user-secrets set "AI:GROQ:ApiKey" "your-groq-api-key"

# 4. Run the API
dotnet run --project src/ReceptionistAgent.Api

# 5. Open Swagger UI
# Navigate to https://localhost:{port}/swagger
```

---

## ⚙️ Configuration

Configure the global system and your tenants via `appsettings.json`.

### Global AI Setup
```jsonc
{
  "AI": {
    "Provider": "Google", // Or "GROQ"
    "Google": {
      "ModelId": "gemini-2.5-flash"
    }
  }
}
```

### Tenant Setup (SQL Server Example)
```jsonc
{
  "Tenants": {
    "clinica-vista-clara": {
      "BusinessName": "Clínica Vista Clara",
      "Connector": {
        "Type": "SqlServer",
        "ConnectionString": "Server=localhost;Database=ClinicaDB;Trusted_Connection=true;",
        "FieldMappings": {
          "BookingsTableName": "dbo.citas",
          "ProvidersTableName": "dbo.doctores",
          "BookingFieldMappings": {
            "Id": "id_cita",
            "ClientName": "nombre_paciente",
            "Status": "estado_cita"
            // ... see example-config.json for full schema
          }
        }
      }
    }
  }
}
```

---

## 📡 API Reference

### `POST /api/chat`
The main conversational endpoint.

**Headers:**
| Header           | Required | Description                                    |
|------------------|----------|------------------------------------------------|
| `X-API-KEY`      | ✅       | System API key for authentication              |
| `X-Tenant-Id`    | ✅       | Tenant identifier (e.g. `clinica-vista-clara`) |
| `X-Patient-Id`   | ❌       | Optional document ID for early identification  |

**Request Body:**
```json
{
  "sessionId": "00000000-0000-0000-0000-000000000000",
  "message": "Necesito una cita para odontología esta semana."
}
```

**Response:**
```json
{
  "sessionId": "b47f2d5a-8e31-4a92-91f8-abc123def456",
  "response": "¡Con gusto! Tenemos disponibilidad con el Dr. Martínez el jueves a las 10:00 AM o a las 3:00 PM. ¿Cuál prefiere?"
}
```

---

## 🧪 Testing

The solution is heavily tested using `xUnit` and `Moq`. Run the tests via CLI:

```bash
# Run all tests
dotnet test

# Run specific categories
dotnet test --filter "FullyQualifiedName~Connectors"
dotnet test --filter "FullyQualifiedName~Security"
dotnet test --filter "FullyQualifiedName~Session"
```

**Coverage includes:**
- Multi-tenancy resolution logic.
- Dapper SQL query generation & field mapping.
- LLM interaction and prompt injection defense.
- Session-based patient resource isolation.

---

## 🛠 Tech Stack

| Component               | Technology                                     |
|-------------------------|------------------------------------------------|
| **Framework**           | .NET 9 Web API                                |
| **AI Orchestration**    | Microsoft Semantic Kernel                     |
| **Database Access**     | Dapper, Microsoft.Data.SqlClient              |
| **Architecture**        | Clean Architecture, Dependency Injection      |
| **Testing**             | xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing  |
| **API Documentation**   | Swashbuckle (Swagger)                         |

---

## 📄 License
This project is intended for educational purposes, software architecture demonstrations, and production-ready references for Semantic Kernel integrations.

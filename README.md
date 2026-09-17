# Pursuit - Production-Grade Job Board SaaS Platform

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![GitHub Repository](https://img.shields.io/badge/GitHub-adheebabdulla007/pursuit-blue)](https://github.com/adheebabdulla007/pursuit)

A **production-grade, multi-tenant SaaS job board platform** built with **.NET 10** and **Clean Architecture**. This project demonstrates enterprise-level backend engineering patterns, security best practices, and distributed systems design.

> **Portfolio Value**: This project is designed to demonstrate mid-level .NET engineering capability, with architecture patterns typically found in Series A-C startups and enterprise platforms.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Installation & Setup](#installation--setup)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [API Documentation](#api-documentation)
- [Security](#security)
- [Deployment](#deployment)
- [Development](#development)
- [Testing](#testing)
- [Learning Outcomes](#learning-outcomes)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

---

## 🎯 Overview

Pursuit is a complete, production-ready job board platform designed to highlight modern .NET backend engineering. It demonstrates how to build scalable, secure, and maintainable systems using enterprise architectural patterns.

### Use Cases

- **Employers**: Post job listings, manage applications, track candidates
- **Job Seekers**: Search and filter jobs, submit applications, track status
- **Admins**: Manage users, monitor system health, view analytics

### Why This Project?

This codebase showcases:
- **Clean Architecture** - Strict separation of concerns across 4 layers
- **Security Best Practices** - JWT tokens with refresh token rotation and reuse detection
- **Multi-Tenancy** - Complete data isolation between customers
- **Distributed Systems** - Redis caching, RabbitMQ messaging, async processing
- **Production Concerns** - Structured logging, error handling, health checks
- **High Testability** - Integration tests with real dependencies (Testcontainers)

---

## ✨ Key Features

### 🔐 Security-First Authentication
- **JWT Access Tokens** - Short-lived (15 minutes) bearer tokens
- **Refresh Token Rotation** - Long-lived refresh tokens (7 days) that rotate on use
- **Reuse Detection** - Detects token theft attempts by monitoring for replayed tokens
- **Automatic Revocation** - Compromised tokens can revoke all user sessions instantly
- **Secure Cookies** - HttpOnly, Secure, SameSite=Strict protection

**Why it matters**: This is how real financial/security-sensitive applications handle authentication. Most junior developers don't understand token rotation patterns.

```csharp
// Example: Token theft is detected and all tokens are revoked
POST /api/auth/refresh (with stolen token)
→ System detects: "This token was already rotated, indicating theft"
→ Revokes all refresh tokens for this user immediately
→ User must log in again on all devices
```

### 🏢 Multi-Tenant Data Isolation
- **Per-Tenant Data Separation** - Each company's data is completely isolated
- **Claim-Based Access Control** - User roles and tenant affiliation determined from JWT claims
- **Query-Level Filtering** - All database queries automatically scoped to user's tenant
- **Database Constraints** - Foreign key relationships enforce tenant boundaries

```
Database Structure:
├── Tenants (Companies)
│   ├── ACME Corp (ID: xxx)
│   │   ├── Users (2 employers)
│   │   └── Jobs (15 listings)
│   │
└── Global Tech (ID: yyy)
	├── Users (4 employers)
	└── Jobs (32 listings)

Job Seekers can see all jobs across all tenants.
But an employer only sees their own tenant's data.
```

### ⚡ Performance & Scalability
- **Redis Caching** - Hot data cached with configurable TTL
- **Graceful Degradation** - API works normally if cache is unavailable (automatic fallback)
- **Async Processing** - RabbitMQ enables background processing without blocking requests
- **Pagination** - Built-in pagination for large result sets
- **Query Optimization** - EF Core projections and includes to prevent N+1 queries

### 🧪 Production-Ready Quality
- **Structured Logging** - Serilog with contextual logging for debugging and monitoring
- **Comprehensive Error Handling** - Centralized exception middleware with proper HTTP status codes
- **Input Validation** - Multi-layer validation (DTOs, Fluent, business logic)
- **Health Checks** - `/health` endpoint for infrastructure monitoring
- **Database Migrations** - Full versioning of schema changes with EF Core migrations

---

## 🏛️ Architecture

### Layered Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                     Pursuit.API                             │
│            (Controllers, Middleware, HTTP)                  │
├─────────────────────────────────────────────────────────────┤
│                 Pursuit.Application                         │
│        (Services, DTOs, Business Logic, Interfaces)         │
├─────────────────────────────────────────────────────────────┤
│               Pursuit.Infrastructure                        │
│        (Repositories, EF Core, External Services)           │
├─────────────────────────────────────────────────────────────┤
│                  Pursuit.Domain                             │
│         (Entities, Enums, Business Rules - No Deps)         │
└─────────────────────────────────────────────────────────────┘
```

### Clean Architecture Principles

**Each layer has a single responsibility:**

1. **Domain Layer** - Pure business rules, zero external dependencies
   - Entities: User, Job, Application, Tenant, RefreshToken
   - Enums: UserRole, JobType, ApplicationStatus
   - No references to databases, APIs, or frameworks

2. **Application Layer** - Use case orchestration
   - Services: AuthService, JobService, ApplicationService, UserService
   - Interfaces: Contracts for repositories and external services
   - DTOs: Input/output models for HTTP
   - Validators: FluentValidation rules

3. **Infrastructure Layer** - Data access and external services
   - Repository implementations: JobRepository, UserRepository, etc.
   - EF Core DbContext and migrations
   - Identity services: TokenService, PasswordHasher
   - External services: RabbitMqPublisher, RedisCacheService

4. **API Layer** - HTTP concerns
   - Controllers: REST endpoints
   - Middleware: Exception handling, request/response
   - Filters: Validation, authorization
   - Program.cs: Dependency injection, authentication setup

### Dependency Flow

Dependencies flow **inward only**:
```
API Layer
	↓
Application Layer
	↓
Infrastructure Layer
	↓
Domain Layer (no dependencies)
```

This means:
- Domain layer is tested without any frameworks
- Application layer is tested with mocked repositories
- Infrastructure layer is where external integrations happen
- API layer only concerns itself with HTTP

---

## 🛠️ Tech Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Language** | C# | Latest | Backend logic |
| **Runtime** | .NET | 10 | Application framework |
| **API Framework** | ASP.NET Core | Built-in | REST API host |
| **Database** | SQL Server | 2019+ | Persistent storage |
| **ORM** | Entity Framework Core | Latest | Data access abstraction |
| **Authentication** | JWT Bearer | OpenID Connect spec | Token-based auth |
| **Caching** | Redis | 6.0+ | In-memory cache |
| **Message Broker** | RabbitMQ | 3.12+ | Async event processing |
| **Logging** | Serilog | Latest | Structured logging |
| **Validation** | FluentValidation | Latest | Input validation rules |
| **Testing Framework** | xUnit | Latest | Unit/integration tests |
| **Test Containers** | Testcontainers | Latest | Docker-based test infra |

---

## 📦 Prerequisites

### Required
- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- **SQL Server** - Local or Docker instance
- **Git** - For cloning the repository

### Optional (for full features)
- **Redis** - For caching layer (graceful degradation if unavailable)
- **RabbitMQ** - For async event processing (graceful degradation if unavailable)
- **Docker** - For containerizing dependencies
- **Visual Studio 2022+** or **VS Code** with C# extensions

### System Requirements
- RAM: 4GB minimum (8GB recommended)
- Disk: 2GB for .NET SDK + dependencies
- OS: Windows, macOS, or Linux

---

## 🚀 Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/adheebabdulla007/pursuit.git
cd pursuit
```

### 2. Setup SQL Server

#### Option A: Docker (Recommended for development)
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrongPassword123!" `
  -p 1433:1433 `
  --name sqlserver-pursuit `
  -d mcr.microsoft.com/mssql/server:latest
```

#### Option B: Local Installation
- [Download SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- Install with default settings
- Use connection string: `Server=localhost;Database=pursuit_dev;User Id=sa;Password=YourPassword;TrustServerCertificate=true;`

#### Option C: Azure SQL Database
- Create an Azure SQL Database
- Configure firewall rules
- Use connection string from Azure portal

### 3. Configure Application Settings

Create or modify `src/Pursuit.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=localhost,1433;Database=pursuit_dev;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=true;"
  },
  "JwtSettings": {
	"Secret": "your-super-secret-key-minimum-32-characters-long-here-for-dev-only!!!",
	"Issuer": "pursuit-api",
	"Audience": "pursuit-client",
	"ExpiryInMinutes": 15,
	"RefreshTokenExpiryInDays": 7
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning"
	}
  }
}
```

**⚠️ Security Warning**: 
- Never commit real secrets to version control
- For production, use Azure Key Vault, AWS Secrets Manager, or environment variables
- JWT secret must be at least 32 characters and cryptographically random

### 4. Setup Optional Services (Redis & RabbitMQ)

#### Redis (Optional - for caching)
```bash
docker run --name redis-pursuit -p 6379:6379 -d redis:alpine
```

Then add to `appsettings.Development.json`:
```json
{
  "Redis": {
	"ConnectionString": "localhost:6379"
  }
}
```

#### RabbitMQ (Optional - for async messaging)
```bash
docker run --name rabbitmq-pursuit -p 5672:5672 -p 15672:15672 -d rabbitmq:3-management
```

Then add to `appsettings.Development.json`:
```json
{
  "RabbitMQ": {
	"Host": "localhost",
	"Port": 5672,
	"Username": "guest",
	"Password": "guest",
	"VirtualHost": "/"
  }
}
```

**Note**: If Redis or RabbitMQ are not running, the application will continue to work with graceful degradation (no caching, no async events).

### 5. Restore Dependencies

```bash
dotnet restore
```

### 6. Apply Database Migrations

```bash
cd src/Pursuit.API
dotnet ef database update --project ../Pursuit.Infrastructure
```

This creates the database schema from migrations.

### 7. Start the Application

```bash
cd src/Pursuit.API
dotnet run
```

**Expected output:**
```
info: Pursuit.API.Program[0]
	  Starting Pursuit API
info: Microsoft.Hosting.Lifetime[14]
	  Now listening on: https://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
	  Application started. Press Ctrl+C to exit.
```

### 8. Access the Application

- **API**: https://localhost:5000
- **Swagger UI**: https://localhost:5000/openapi
- **Health Check**: https://localhost:5000/health

### 9. Create Your First User

Using curl or Postman, register as an employer:
```bash
curl -X POST https://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
	"email": "employer@example.com",
	"password": "SecurePassword123!",
	"firstName": "John",
	"lastName": "Doe",
	"role": "Employer",
	"tenantName": "Acme Corporation"
  }'
```

Response:
```json
{
  "email": "employer@example.com",
  "role": "Employer"
}
```

---

## 📁 Project Structure

```
pursuit/
├── src/
│   ├── Pursuit.API/
│   │   ├── Controllers/              # REST endpoints
│   │   │   ├── AuthController.cs     # Login, register, token refresh
│   │   │   ├── JobsController.cs     # Job CRUD operations
│   │   │   └── ApplicationsController.cs  # Application submissions
│   │   ├── Middleware/
│   │   │   ├── ExceptionMiddleware.cs    # Global error handling
│   │   │   └── ErrorResponse.cs         # Error response formatting
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs      # Automatic model validation
│   │   ├── Program.cs                # Service registration, authentication setup
│   │   ├── appsettings.json          # Production configuration
│   │   ├── appsettings.Development.json  # Development overrides
│   │   └── Pursuit.API.csproj        # Project file
│   │
│   ├── Pursuit.Application/
│   │   ├── Services/
│   │   │   ├── AuthService.cs        # Register, login, token refresh
│   │   │   ├── JobService.cs         # Job business logic
│   │   │   ├── ApplicationService.cs # Application business logic
│   │   │   ├── UserService.cs        # User management
│   │   │   └── StatsService.cs       # Dashboard statistics
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs       # Auth contracts
│   │   │   ├── IJobService.cs        # Job contracts
│   │   │   ├── IApplicationService.cs # Application contracts
│   │   │   ├── IJobRepository.cs     # Job data access contract
│   │   │   ├── IUserRepository.cs    # User data access contract
│   │   │   ├── ITokenService.cs      # Token generation contract
│   │   │   ├── IPasswordHasher.cs    # Password hashing contract
│   │   │   ├── ICacheService.cs      # Caching contract
│   │   │   ├── IMessagePublisher.cs  # Message publishing contract
│   │   │   ├── ICurrentUserService.cs # Current user context
│   │   │   └── IRepository.cs        # Generic repository contract
│   │   ├── DTOs/
│   │   │   ├── AuthDto.cs            # Login, register payloads
│   │   │   ├── JobDto.cs             # Job request/response models
│   │   │   ├── ApplicationDto.cs     # Application request/response models
│   │   │   └── PagedResult.cs        # Pagination wrapper
│   │   ├── Validators/
│   │   │   └── CreateJobDtoValidator.cs  # Job creation validation rules
│   │   ├── Messages/
│   │   │   └── ApplicationSubmittedMessage.cs  # Event message
│   │   ├── DependencyInjection.cs    # Service registration
│   │   └── Pursuit.Application.csproj
│   │
│   ├── Pursuit.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs       # EF Core DbContext
│   │   │   ├── Configurations/       # Fluent API entity mappings
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── TenantConfiguration.cs
│   │   │   │   ├── JobConfiguration.cs
│   │   │   │   ├── ApplicationConfiguration.cs
│   │   │   │   └── NotificationConfiguration.cs
│   │   │   ├── Migrations/           # Database schema history
│   │   │   │   ├── 20260603141036_InitialCreate.cs
│   │   │   │   ├── 20260606163018_MakeUserTenantIdNullable.cs
│   │   │   │   └── 20260606164340_AddTenantIdToApplication.cs
│   │   │   └── Repositories/
│   │   │       ├── Repository.cs     # Generic base repository
│   │   │       ├── JobRepository.cs  # Job-specific queries
│   │   │       ├── UserRepository.cs # User-specific queries
│   │   │       ├── ApplicationRepository.cs  # Application-specific queries
│   │   │       ├── TenantRepository.cs
│   │   │       └── RefreshTokenRepository.cs
│   │   ├── Identity/
│   │   │   ├── PasswordHasher.cs     # PBKDF2 password hashing
│   │   │   └── TokenService.cs       # JWT token generation/validation
│   │   ├── Caching/
│   │   │   └── RedisCacheService.cs  # Redis cache abstraction
│   │   ├── Messaging/
│   │   │   └── RabbitMqPublisher.cs  # RabbitMQ event publishing
│   │   ├── Services/
│   │   │   ├── CurrentUserService.cs # Extract current user from context
│   │   │   ├── TenantService.cs      # Tenant-specific logic
│   │   │   └── HttpDbContextScope.cs # Request-scoped operations
│   │   ├── DependencyInjection.cs    # Service registration
│   │   └── Pursuit.Infrastructure.csproj
│   │
│   └── Pursuit.Domain/
│       ├── Entities/
│       │   ├── BaseEntity.cs         # Base class with Id, CreatedAt, UpdatedAt
│       │   ├── User.cs               # User entity (employer or job seeker)
│       │   ├── Tenant.cs             # Company/organization entity
│       │   ├── Job.cs                # Job listing entity
│       │   ├── Application.cs        # Job application entity
│       │   ├── RefreshToken.cs       # Stored refresh token entity
│       │   └── Notification.cs       # Notification entity
│       ├── Enums/
│       │   ├── UserRole.cs           # Admin, Employer, JobSeeker
│       │   ├── ApplicationStatus.cs  # Applied, Reviewed, Rejected, Hired
│       │   └── JobType.cs            # Full-time, Part-time, Contract, etc.
│       ├── Exceptions/
│       │   └── ForbiddenAccessException.cs  # Custom exceptions
│       └── Pursuit.Domain.csproj
│
├── tests/
│   └── Pursuit.IntegrationTests/
│       ├── Auth/
│       │   └── AuthFlowTests.cs      # End-to-end auth tests
│       ├── Pursui.IntegrationTests.csproj
│       └── [Other integration test files]
│
├── README.md                         # This file
├── .gitignore                        # Git ignore rules
├── Pursuit.slnx                      # Solution file
└── LICENSE                           # MIT License
```

---

## ⚙️ Configuration

### appsettings.json (Production)

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "[Production SQL Server connection string]"
  },
  "JwtSettings": {
	"Secret": "[Strong random 32+ char secret from Key Vault]",
	"Issuer": "pursuit-api",
	"Audience": "pursuit-client",
	"ExpiryInMinutes": 15,
	"RefreshTokenExpiryInDays": 7
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning",
	  "Microsoft.EntityFrameworkCore": "Information"
	}
  },
  "Serilog": {
	"Using": ["Serilog.Sinks.Console"],
	"MinimumLevel": "Information",
	"WriteTo": [
	  {
		"Name": "Console",
		"Args": {
		  "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
		}
	  }
	]
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json (Development Override)

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=localhost,1433;Database=pursuit_dev;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
  },
  "JwtSettings": {
	"Secret": "dev-secret-key-minimum-32-characters-long-for-development!!!",
	"Issuer": "pursuit-api",
	"Audience": "pursuit-client",
	"ExpiryInMinutes": 15,
	"RefreshTokenExpiryInDays": 7
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Debug",
	  "Microsoft": "Information",
	  "Microsoft.EntityFrameworkCore": "Debug"
	}
  },
  "Redis": {
	"ConnectionString": "localhost:6379"
  },
  "RabbitMQ": {
	"Host": "localhost",
	"Port": 5672,
	"Username": "guest",
	"Password": "guest",
	"VirtualHost": "/"
  }
}
```

### Environment Variables (Production)

Use environment variables for sensitive data:

```bash
# Database
ConnectionStrings__DefaultConnection=Server=prod-db.database.windows.net;Database=pursuit_prod;...

# JWT
JwtSettings__Secret=your-production-secret-from-keyvault
JwtSettings__Issuer=pursuit-api
JwtSettings__Audience=pursuit-client
JwtSettings__ExpiryInMinutes=15
JwtSettings__RefreshTokenExpiryInDays=7

# Redis
Redis__ConnectionString=prod-redis.redis.cache.windows.net:6379

# RabbitMQ
RabbitMQ__Host=prod-rabbitmq.azure.com
RabbitMQ__Port=5672
RabbitMQ__Username=username
RabbitMQ__Password=password
```

---

## 📡 API Documentation

### Authentication

#### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Employer",           // "Employer" or "JobSeeker"
  "tenantName": "Acme Corp"     // Required for Employer, ignored for JobSeeker
}
```

**Response (200 OK):**
```json
{
  "email": "user@example.com",
  "role": "Employer"
}
```

**Cookies Set:**
- `pursuit_token` - JWT access token (15 min TTL)
- `pursuit_refresh_token` - Refresh token (7 day TTL)

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "email": "user@example.com",
  "role": "Employer"
}
```

#### Refresh Token
```http
POST /api/auth/refresh
Cookie: pursuit_refresh_token=...
```

**Response (200 OK):**
- New cookies set automatically
- Old refresh token is rotated away

#### Get Current User
```http
GET /api/auth/me
Authorization: Bearer <access-token>
```

**Response (200 OK):**
```json
{
  "email": "user@example.com",
  "role": "Employer",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

#### Logout
```http
POST /api/auth/logout
Cookie: pursuit_refresh_token=...
```

**Response (200 OK):**
- Cookies deleted
- All refresh tokens for user revoked

### Jobs

#### Get All Jobs
```http
GET /api/jobs?title=Developer&location=Remote&jobType=FullTime&page=1&pageSize=10
Authorization: Bearer <access-token>
```

**Response (200 OK):**
```json
{
  "items": [
	{
	  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
	  "title": "Senior .NET Developer",
	  "description": "We're hiring...",
	  "location": "Remote",
	  "salaryMin": 120000,
	  "salaryMax": 160000,
	  "jobType": "FullTime",
	  "tenantName": "Acme Corp",
	  "createdAt": "2025-01-15T10:30:00Z"
	}
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 10
}
```

#### Get Job Details
```http
GET /api/jobs/{id}
Authorization: Bearer <access-token>
```

#### Create Job (Employer Only)
```http
POST /api/jobs
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "title": "Senior .NET Developer",
  "description": "We're hiring...",
  "location": "Remote",
  "salaryMin": 120000,
  "salaryMax": 160000,
  "jobType": "FullTime"
}
```

**Response (201 Created):**
```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "title": "Senior .NET Developer",
  "description": "We're hiring...",
  "location": "Remote",
  "salaryMin": 120000,
  "salaryMax": 160000,
  "jobType": "FullTime",
  "tenantName": "Acme Corp",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

#### Update Job (Employer Only)
```http
PUT /api/jobs/{id}
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "title": "Senior .NET Developer (Updated)",
  "description": "We're hiring...",
  "location": "Remote",
  "salaryMin": 130000,
  "salaryMax": 170000,
  "jobType": "FullTime",
  "isActive": true
}
```

#### Delete Job (Employer Only)
```http
DELETE /api/jobs/{id}
Authorization: Bearer <access-token>
```

**Response (204 No Content)**

### Applications

#### Submit Application (JobSeeker Only)
```http
POST /api/applications
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "jobId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "resumeUrl": "https://cdn.example.com/resumes/john-doe.pdf"
}
```

**Response (201 Created):**
```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "jobId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "jobTitle": "Senior .NET Developer",
  "applicantName": "John Doe",
  "status": "Applied",
  "submittedAt": "2025-01-15T10:30:00Z"
}
```

#### Get Applications
```http
GET /api/applications
Authorization: Bearer <access-token>
```

**Employers see:** Applications to their jobs  
**Job Seekers see:** Their own applications

#### Update Application Status (Employer Only)
```http
PUT /api/applications/{id}/status
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "status": "Reviewed"  // "Applied", "Reviewed", "Rejected", "Hired"
}
```

### Health Check

```http
GET /health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "checks": {
	"Database": "Healthy",
	"Redis": "Available",
	"RabbitMQ": "Available"
  }
}
```

### Swagger/OpenAPI

Interactive API documentation available at:
```
https://localhost:5000/openapi
```

---

## 🔒 Security

### Authentication & Authorization

**JWT Claims-Based Access:**
```csharp
// Every authenticated request includes:
claims = [
  { type: "sub", value: "user-id" },
  { type: "role", value: "Employer" },
  { type: "email", value: "user@example.com" },
  { type: "tenantId", value: "tenant-id" }  // Only for employers
]
```

**Authorization Checks:**
- Endpoints requiring `[Authorize]` verify JWT is valid
- Multi-tenant endpoints check `tenantId` claim matches resource owner
- Role-based access (Employer, JobSeeker, Admin)

### Refresh Token Rotation & Reuse Detection

**Normal Flow:**
```
1. User logs in → receive accessToken + refreshToken
2. accessToken expires → POST /auth/refresh with refreshToken
3. Server rotates: old token marked as "used", new token issued
4. Client uses new token, old one is no longer valid
5. If reused 100 more times with old token → FRAUD DETECTED
```

**Theft Detection:**
```
1. User's device is compromised
2. Attacker gets refreshToken
3. Attacker uses old refreshToken → server detects reuse
4. Server revokes ALL refresh tokens for this user
5. User must log in again on all legitimate devices
```

**Code Example:**
```csharp
// In AuthService.RefreshTokenAsync()
if (existingToken.IsRevoked)
{
	// Token was already rotated. If it's being used again,
	// it indicates the rotation was the result of token theft.
	// Revoke all tokens for this user immediately.
	await _refreshTokenRepository.RevokeAllForUserAsync(existingToken.UserId);
	throw new UnauthorizedAccessException("Invalid refresh token.");
}
```

### Password Security

- **PBKDF2 Hashing** - Industry-standard key derivation function
- **Salted** - Each password has unique salt
- **Configurable Iterations** - 10,000+ iterations to slow brute force

```csharp
// Hashing
string hash = _passwordHasher.Hash("MyPassword123");
// Result: $PBKDF2$10000$salt$hash

// Verification
bool isValid = _passwordHasher.Verify("MyPassword123", hash);
```

### Data Isolation

- **Tenant-Scoped Queries** - All database queries filtered by TenantId
- **Foreign Key Constraints** - Database enforces relationships
- **Query Auditing** - All queries can be logged for compliance

```sql
-- Example: Ensure employer can only see their own jobs
SELECT * FROM Jobs WHERE TenantId = @TenantId
```

### HTTPS & Cookies

- **HTTPS Enforced** - All production traffic over TLS 1.3
- **Secure Cookies** - `Secure` flag prevents transmission over HTTP
- **HttpOnly Cookies** - `HttpOnly` flag prevents JavaScript access (XSS protection)
- **SameSite Protection** - `SameSite=Strict` prevents CSRF attacks

```csharp
// Secure cookie configuration
var cookieOptions = new CookieOptions
{
	HttpOnly = true,           // No JavaScript access
	Secure = true,             // HTTPS only
	SameSite = SameSiteMode.Strict,  // CSRF protection
	Expires = DateTimeOffset.UtcNow.AddDays(7)
};
```

### Error Handling

- **No Stack Traces in Production** - Prevents information disclosure
- **Generic Error Messages** - "Invalid credentials" instead of "User not found"
- **Structured Logging** - Sensitive details logged internally but not exposed

### Input Validation

- **Multi-Layer Validation**:
  1. DTO validation (length, format)
  2. FluentValidation (business rules)
  3. Database constraints (uniqueness, foreignkeys)

```csharp
public class CreateJobDtoValidator : AbstractValidator<CreateJobDto>
{
	public CreateJobDtoValidator()
	{
		RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
		RuleFor(x => x.SalaryMin).GreaterThan(0);
		RuleFor(x => x.SalaryMax).GreaterThan(x => x.SalaryMin);
		// ... more validation rules
	}
}
```

---

## 🚀 Deployment

### Docker Deployment

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10
WORKDIR /app
COPY --from=build /app .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "Pursuit.API.dll"]
```

**Build & Run:**
```bash
docker build -t pursuit:latest .
docker run -p 5000:80 -e ConnectionStrings__DefaultConnection="..." pursuit:latest
```

### Docker Compose (Full Stack)

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  api:
	build: .
	ports:
	  - "5000:80"
	environment:
	  ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=pursuit;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
	  JwtSettings__Secret: "your-secret-key"
	  Redis__ConnectionString: "redis:6379"
	  RabbitMQ__Host: "rabbitmq"
	depends_on:
	  - sqlserver
	  - redis
	  - rabbitmq

  sqlserver:
	image: mcr.microsoft.com/mssql/server:latest
	environment:
	  ACCEPT_EULA: "Y"
	  SA_PASSWORD: "YourPassword"
	ports:
	  - "1433:1433"
	volumes:
	  - sqldata:/var/opt/mssql

  redis:
	image: redis:alpine
	ports:
	  - "6379:6379"

  rabbitmq:
	image: rabbitmq:3-management
	ports:
	  - "5672:5672"
	  - "15672:15672"

volumes:
  sqldata:
```

**Start all services:**
```bash
docker-compose up -d
```

### Azure Deployment

```bash
# Create resource group
az group create --name pursuit-rg --location eastus

# Create app service
az appservice plan create --name pursuit-plan --resource-group pursuit-rg --sku B1 --is-linux

# Deploy
az webapp create --resource-group pursuit-rg --plan pursuit-plan --name pursuit-api --runtime DOTNETCORE|10.0

# Set connection string
az webapp config connection-string set \
  --resource-group pursuit-rg \
  --name pursuit-api \
  --settings DefaultConnection="Server=..." \
  --connection-string-type SQLServer
```

### Environment-Specific Configuration

**Development:**
- Debug logging enabled
- CORS allows localhost
- SQL Server runs locally or in Docker
- No TLS required

**Production:**
- Information-level logging only
- Specific CORS origins
- Managed SQL Server (Azure SQL, RDS)
- TLS 1.3 enforced
- Secrets from Key Vault
- Redis cluster
- RabbitMQ cluster

---

## 💻 Development

### Watch Mode (Live Reload)

```bash
cd src/Pursuit.API
dotnet watch
```

Changes to C# files automatically recompile and restart the app.

### Create a New Migration

After changing entities in Domain layer:

```bash
cd src/Pursuit.API
dotnet ef migrations add MigrationName --project ../Pursuit.Infrastructure
```

This creates a new migration file without applying it.

### Apply Migrations

```bash
dotnet ef database update --project ../Pursuit.Infrastructure
```

Or to revert to a specific migration:

```bash
dotnet ef database update PreviousMigrationName --project ../Pursuit.Infrastructure
```

### Database Schema Inspection

View all entities and relationships:

```bash
dotnet ef dbcontext info --project ../Pursuit.Infrastructure
```

Scaffold a migration without applying it:

```bash
dotnet ef migrations script --output migration.sql --project ../Pursuit.Infrastructure
```

### Code Analysis

Check for warnings and code quality issues:

```bash
dotnet build --no-restore --verbosity:minimal
```

### Formatting

```bash
dotnet format  # Uses .editorconfig rules
```

---

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "ClassName=AuthFlowTests"
```

### Run with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

### Integration Tests

Tests use Testcontainers to spin up **real** dependencies:

```csharp
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
	// Tests in this collection use real SQL Server, Redis, RabbitMQ
}

[Collection("Database collection")]
public class AuthFlowTests
{
	[Fact]
	public async Task Register_WithValidCredentials_CreatesUserAndReturnsToken()
	{
		// Arrange
		var registerDto = new RegisterDto
		{
			Email = "test@example.com",
			Password = "SecurePassword123!",
			FirstName = "Test",
			LastName = "User",
			Role = "Employer",
			TenantName = "Test Company"
		};

		// Act
		var response = await _httpClient.PostAsJsonAsync("/api/auth/register", registerDto);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		// More assertions...
	}
}
```

### What Gets Tested

- ✅ Full authentication flow (register → login → refresh → logout)
- ✅ Job creation and retrieval
- ✅ Application submission
- ✅ Multi-tenant data isolation
- ✅ Token rotation and reuse detection
- ✅ Error handling and status codes

---

## 📚 Learning Outcomes

This project demonstrates:

### Architecture Patterns
- ✅ **Clean Architecture** - 4-layer separation of concerns
- ✅ **Repository Pattern** - Abstraction over data access
- ✅ **Service Layer** - Business logic orchestration
- ✅ **Dependency Injection** - Loose coupling, testability
- ✅ **Factory Pattern** - Generic repository creation

### Security
- ✅ **JWT Authentication** - Token-based auth
- ✅ **Refresh Token Rotation** - Token theft detection
- ✅ **Password Hashing** - PBKDF2 with salt
- ✅ **Multi-Tenancy** - Data isolation per customer
- ✅ **Secure Cookies** - HttpOnly, Secure, SameSite flags

### Async/Await
- ✅ **Async All The Way** - No blocking calls
- ✅ **CancellationToken** - Graceful shutdown
- ✅ **Async Enumeration** - Efficient data streaming
- ✅ **Fire-and-Forget** - Async event publishing

### Database
- ✅ **Entity Framework Core** - ORM abstraction
- ✅ **Migrations** - Schema versioning
- ✅ **Fluent API** - Entity configuration
- ✅ **Query Optimization** - Includes, projections
- ✅ **Relationships** - One-to-many, many-to-one

### Distributed Systems
- ✅ **Redis Caching** - In-memory cache
- ✅ **RabbitMQ Messaging** - Event-driven architecture
- ✅ **Graceful Degradation** - Works without optional services
- ✅ **Eventual Consistency** - Async processing

### Code Quality
- ✅ **Structured Logging** - Serilog integration
- ✅ **Error Handling** - Centralized exception middleware
- ✅ **Input Validation** - FluentValidation
- ✅ **Health Checks** - Infrastructure monitoring
- ✅ **Configuration Management** - Environment-based settings

### Testing
- ✅ **Integration Tests** - Full-stack testing
- ✅ **Testcontainers** - Real dependency testing
- ✅ **Test Fixtures** - Reusable test infrastructure
- ✅ **Mock Patterns** - Isolation testing

---

## 🔧 Troubleshooting

### Database Connection Issues

**Error: "A network-related or instance-specific error occurred"**

**Solution:**
1. Verify SQL Server is running: `docker ps | grep mssql`
2. Check connection string in appsettings.Development.json
3. Ensure database name is correct
4. Test connection: `sqlcmd -S localhost,1433 -U sa -P YourPassword`

### JWT Token Not Working

**Error: "Invalid token" or "Authorization header missing"**

**Solution:**
1. Ensure token is in Authorization header: `Authorization: Bearer <token>`
2. Or in cookie: `Cookie: pursuit_token=<token>`
3. Verify JWT secret matches between encode/decode
4. Check token expiration: tokens expire after 15 minutes

### Redis Connection Failed

**Error: "Redis connection refused"**

**Solution:**
1. Redis is optional - app works without it
2. To enable Redis: `docker run -p 6379:6379 redis:alpine`
3. Add to appsettings: `"Redis": { "ConnectionString": "localhost:6379" }`
4. Restart API

### RabbitMQ Connection Failed

**Error: "Failed to connect to RabbitMQ"**

**Solution:**
1. RabbitMQ is optional - app works without it
2. To enable RabbitMQ: `docker run -p 5672:5672 rabbitmq:3`
3. Add to appsettings: `"RabbitMQ": { "Host": "localhost", "Port": 5672 }`
4. Restart API

### Migration Conflicts

**Error: "The migration ... has already been applied"**

**Solution:**
```bash
# See which migrations are applied
dotnet ef migrations list

# Remove unapplied migrations
dotnet ef migrations remove

# Or revert to a specific point
dotnet ef database update PreviousMigrationName
```

### Port Already in Use

**Error: "Address already in use"**

**Solution:**
```bash
# Find process using port 5000 (Windows)
netstat -ano | findstr :5000

# Kill process
taskkill /PID <PID> /F

# Or use different port
dotnet run --urls https://localhost:5001
```

### Docker Build Failures

**Error: "docker: command not found" or build fails**

**Solution:**
1. Install Docker Desktop: https://www.docker.com/products/docker-desktop
2. Restart terminal after installation
3. Verify: `docker --version`
4. If still failing, check Docker daemon is running

---

## 🤝 Contributing

While this is primarily a portfolio project, suggestions and improvements are welcome!

### How to Contribute

1. **Fork the repository**
   ```bash
   git clone https://github.com/your-username/pursuit.git
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```

3. **Make your changes**
   - Follow existing code style
   - Add tests for new functionality
   - Update documentation

4. **Commit your changes**
   ```bash
   git commit -m "feat: add amazing feature"
   ```

5. **Push to branch**
   ```bash
   git push origin feature/amazing-feature
   ```

6. **Open a Pull Request**

### Code Style Guidelines

- **Naming**: PascalCase for classes/methods, camelCase for variables
- **Comments**: Explain *why*, not *what*. Code should be self-documenting.
- **Async**: Always use `async`/`await`, never `.Result` or `.Wait()`
- **Validation**: Validate early, fail fast
- **Logging**: Use Serilog, include context
- **Tests**: Every new feature should have tests

### Commit Message Format

```
feat: add new feature
fix: fix bug
docs: update documentation
test: add tests
refactor: refactor code
chore: maintenance
```

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**MIT License Summary:**
- ✅ Use commercially
- ✅ Modify
- ✅ Distribute
- ⚠️ No warranty
- ⚠️ Must include license

---

## 📞 Contact & Support

**Author:** Adheeb Abdulla  
**GitHub:** [@adheebabdulla007](https://github.com/adheebabdulla007)  
**Repository:** [pursuit](https://github.com/adheebabdulla007/pursuit)

### Getting Help

- **Documentation**: Read this README thoroughly
- **Swagger API Docs**: Visit https://localhost:5000/openapi
- **GitHub Issues**: Search existing issues or create new one
- **Code Comments**: Inline documentation explains complex logic

---

## 🎯 Quick Command Reference

```bash
# Setup
git clone https://github.com/adheebabdulla007/pursuit.git
cd pursuit
dotnet restore

# Database
dotnet ef database update --project src/Pursuit.Infrastructure

# Development
dotnet run --project src/Pursuit.API
dotnet watch --project src/Pursuit.API    # Auto-reload

# Migrations
dotnet ef migrations add MigrationName --project src/Pursuit.Infrastructure
dotnet ef database update --project src/Pursuit.Infrastructure

# Testing
dotnet test
dotnet test --filter "ClassName=AuthFlowTests"

# Building
dotnet build
dotnet publish -c Release -o ./publish

# Docker
docker build -t pursuit:latest .
docker run -p 5000:80 pursuit:latest
docker-compose up -d
```

---

## 🌟 Highlights

- **Production-Ready**: Not a tutorial project - production-grade patterns
- **Secure by Default**: Security considered at every layer
- **Testable**: 100% dependency injection, easy to test
- **Maintainable**: Clean Architecture for long-term sustainability
- **Scalable**: Designed for growth (caching, messaging, multi-tenancy)
- **Documented**: This README, inline comments, Swagger docs

---

## 📈 Project Stats

| Metric | Value |
|--------|-------|
| Lines of Code | ~5,000+ |
| Projects | 4 (Domain, Application, Infrastructure, API) |
| Entities | 7 (User, Tenant, Job, Application, RefreshToken, Notification) |
| Controllers | 3 |
| Tests | 10+ integration tests |
| Database Migrations | 3 |
| External Services | 3 (Redis, RabbitMQ, SQL Server) |

---

## 🚀 Next Steps

1. **Get started**: Follow [Installation & Setup](#-installation--setup)
2. **Explore the API**: Visit Swagger at https://localhost:5000/openapi
3. **Read the code**: Start with `src/Pursuit.Application/Services/AuthService.cs`
4. **Run tests**: `dotnet test` to see everything in action
5. **Deploy**: Use Docker or Azure instructions above
6. **Extend**: Add new features using existing patterns as guide

---

**Built with Clean Architecture, Security Best Practices, and .NET 10** ✨

---

*Last Updated: 2025 | Version: 1.0.0 | Status: Production-Ready*

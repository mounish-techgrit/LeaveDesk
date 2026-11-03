# LeaveDesk

A leave management system REST API built with ASP.NET Core (.NET 10) as a bootcamp capstone project. Employees apply for leave, managers approve or reject it, and HR manages employees, leave policies, holidays and reports. See `LeaveDesk-PRD.md` for the full requirements.

## Project structure

The solution follows a layered ("Clean Architecture") structure split across 5 projects, so dependencies only ever point inward:

```
LeaveDesk.Api ──────┐
                     ├──> LeaveDesk.Application ──> LeaveDesk.Domain
LeaveDesk.Infrastructure ─┘
LeaveDesk.Tests ─────────> LeaveDesk.Application
```

| Project | Responsibility |
| --- | --- |
| **LeaveDesk.Domain** | Core business types only: entities (`Entities/`) and enums (`Enums/`). No dependencies on any other project or external package. |
| **LeaveDesk.Application** | Business logic and contracts. `Services/` holds the actual use-case logic (applying for leave, approving a request, balance math, reminders). `Interfaces/` holds abstractions (`IEmailService`, `IRepository<T>`, etc.) that services depend on without knowing the implementation. `DTOs/` holds the request/response shapes exposed over the API. Depends only on `Domain`. |
| **LeaveDesk.Infrastructure** | Concrete implementations of `Application`'s interfaces: `Data/` (EF Core `DbContext`, migrations, seed data), `Identity/` (ASP.NET Core Identity, JWT issuing, role/account management), `Repositories/` (generic EF Core-backed repository), `Email/` (`ConsoleEmailService` for logging-only, `SmtpEmailService` for real Gmail OAuth2 delivery — whichever is registered in `Program.cs`). Depends on `Application` and `Domain`. |
| **LeaveDesk.Api** | The entry point. `Controllers/` expose HTTP endpoints. `Middleware/` holds the global exception handler. `BackgroundJobs/` holds the daily reminder job. `Program.cs` is the composition root — wires `Infrastructure` classes to `Application` interfaces via DI and configures the request pipeline. Depends on `Application` and `Infrastructure`. |
| **LeaveDesk.Tests** | xUnit + Moq unit tests for `Application`'s business-rule-heavy services, using mocked repositories instead of a real database. |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 17 running locally

## Setup

1. **Start PostgreSQL.** This install has no Windows service registered, so after every reboot it needs to be started manually:
   ```bash
   "C:\Program Files\PostgreSQL\17\bin\pg_ctl.exe" start -D "C:\Program Files\PostgreSQL\17\data"
   ```

2. **Configure secrets** (never committed — stored via .NET User Secrets, keyed off `LeaveDesk.Api`'s `UserSecretsId`):
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=leavedesk;Username=postgres;Password=<your-password>" --project LeaveDesk.Api
   dotnet user-secrets set "Jwt:Key" "<a long random string>" --project LeaveDesk.Api
   dotnet user-secrets set "Email:OAuth:ClientId" "<google OAuth2 client id>" --project LeaveDesk.Api
   dotnet user-secrets set "Email:OAuth:ClientSecret" "<google OAuth2 client secret>" --project LeaveDesk.Api
   dotnet user-secrets set "Email:OAuth:RefreshToken" "<google OAuth2 refresh token with Gmail send scope>" --project LeaveDesk.Api
   dotnet user-secrets set "Email:FromAddress" "<the Gmail address the refresh token belongs to>" --project LeaveDesk.Api
   ```

3. **Apply migrations:**
   ```bash
   dotnet ef database update --project LeaveDesk.Infrastructure --startup-project LeaveDesk.Api
   ```

4. **Run the API:**
   ```bash
   dotnet run --project LeaveDesk.Api
   ```
   On first run it automatically seeds roles, an HR Admin account, 2 departments, 4 leave types, 2 holidays, 2 managers and 6 employees (see below). Swagger UI opens at `http://localhost:5152/swagger` in development (see `LeaveDesk.Api/Properties/launchSettings.json` for the exact port).

## Seeded accounts

| Role | Email | Password |
| --- | --- | --- |
| HR Admin | `hradmin@leavedesk.local` | `HrAdmin@123` |
| Manager (Engineering) | `priya.manager@leavedesk.local` | `Manager@123` |
| Manager (Sales) | `rajesh.manager@leavedesk.local` | `Manager@123` |
| Employee ×3 (Engineering, report to Priya) | `asha.employee@`, `bala.employee@`, `chetan.employee@leavedesk.local` | `Employee@123` |
| Employee ×3 (Sales, report to Rajesh) | `divya.employee@`, `esha.employee@`, `farid.employee@leavedesk.local` | `Employee@123` |

Note: the HR Admin account is Identity-only (no linked `Employee` profile) — this is intentional, see **Known limitations** below.

## API overview

All endpoints except `auth/register`, `auth/login`, `auth/forgot-password` and `auth/reset-password` require a JWT bearer token (log in, then use Swagger's **Authorize** button or an `Authorization: Bearer <token>` header).

| Controller | Covers |
| --- | --- |
| `AuthController` | Register, login, forgot/reset password |
| `DepartmentsController`, `LeaveTypesController`, `HolidaysController` | HR-managed master data (read: any authenticated user; write: HR Admin) |
| `EmployeesController` | HR employee CRUD, role assignment, self-service profile (`/me`) |
| `LeaveRequestsController` | Apply/edit/withdraw/cancel, manager approvals, team calendar, HR company-wide search + CSV export |
| `LeaveBalancesController` | View balances, HR manual adjustments |

## Running tests

```bash
dotnet test LeaveDesk.Tests
```

## Known limitations / deliberate scope decisions

- **Onboarding flow**: employees self-register (choosing their own password), then HR links them to a full profile via `POST /api/employees`. The PRD's wording for the welcome email implies HR creates the account first; this was adapted because `Employee.DepartmentId` is required and departments didn't exist yet when auth was built. The welcome email instead confirms the HR linkage.
- **Email delivery**: `IEmailService` has two implementations — `ConsoleEmailService` (logs instead of sending) and `SmtpEmailService` (real delivery via Gmail SMTP using OAuth2, since Gmail no longer accepts plain username/password SMTP auth). `Program.cs` wires up whichever one is registered; swapping back to console-only is a one-line change. The OAuth2 flow exchanges a long-lived refresh token for a short-lived access token before every send (`https://oauth2.googleapis.com/token`), then authenticates to `smtp.gmail.com:587` via MailKit's `SaslMechanismOAuth2`. A send failure is logged, not thrown — a transient email/network issue must not fail the business operation (submitting a leave request, resetting a password) that triggered it.
- **HR Admin has no `Employee` record**: HR actions that record "who did it" (approving a manager-less request, adjusting a balance) tolerate a null actor for this reason — confirmed via tests and live verification.
- **Company-wide leave request reports**: `GET /api/leaverequests` supports filtering by employee/department/leave type/status/date range plus paging; `GET /api/leaverequests/export` returns the same filtered set as CSV.
- **Cross-year leave requests** are not split across two years' balances — a request's entire duration is charged against its `StartDate`'s year.

## Status

All 13 planned phases are complete: project structure, domain model, JWT auth, HR master-data CRUD, employee management, the leave request/balance engine, manager approvals, email notifications, the daily reminder background job, HR reporting, cross-cutting polish (global exception handling, logging, input validation), unit tests, and seed data.

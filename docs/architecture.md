# ClinicFlow — Architecture

## Decision
**Vertical Slice Architecture**, organized by feature rather than by technical
layer. Chosen over Clean Architecture (N-layer) to deepen a different,
currently-relevant .NET pattern rather than repeat what was already built on
the eCommerce project, and because it maps directly onto the BRD's
feature-based structure — easier to learn from and easier to defend in an
interview.

## Project Structure

```
ClinicFlow.Api/
├── Features/
│   ├── Appointments/
│   │   ├── BookAppointment.cs        (Command + Handler + Validator)
│   │   ├── CancelAppointment.cs
│   │   ├── ConfirmAppointment.cs
│   │   ├── CompleteAppointment.cs     (marks Completed or No-show)
│   │   └── GetDoctorSchedule.cs       (Query + Handler)
│   ├── Patients/
│   │   ├── RegisterPatient.cs
│   │   ├── GetPatientRecord.cs
│   │   └── AddMedicalRecordEntry.cs
│   ├── Billing/
│   │   └── RecordPayment.cs
│   ├── Prescriptions/
│   │   ├── CreatePrescription.cs
│   │   └── GetPatientPrescriptions.cs
│   ├── Reports/
│   │   └── GetDashboardSummary.cs
│   ├── Auth/
│   │   ├── Login.cs
│   │   ├── RefreshToken.cs
│   │   └── Register.cs               (staff + patient self-registration)
│   └── Staff/
│       ├── CreateStaffAccount.cs      (Admin only)
│       └── DeactivateStaffAccount.cs
├── Domain/
│   ├── Entities/          (Tenant, User, Patient, Appointment, MedicalRecord,
│   │                        Prescription, Payment)
│   └── Enums/             (AppointmentStatus, UserRole, PaymentMethod)
├── Infrastructure/
│   ├── Data/
│   │   ├── ClinicFlowDbContext.cs     (single DbContext, global TenantId filter)
│   │   ├── Configurations/            (EF Core entity configs, one per entity)
│   │   └── Migrations/
│   └── Auth/
│       └── JwtTokenService.cs
├── Common/
│   ├── Behaviors/         (MediatR pipeline: validation, tenant-scoping)
│   ├── Errors/            (consistent Error/ProblemDetails shape)
│   └── Middleware/        (global exception handling)
└── Program.cs             (DI wiring, middleware pipeline)
```

Each feature file contains its Request (Command/Query), Handler, and
Validator together — one file per capability, matching one row in the BRD's
workflow list.

## Module Boundaries
Each folder under `Features/` is a bounded module. Modules talk to the
database directly via the shared `ClinicFlowDbContext` (this is a monolith,
not microservices — modules share a process and a database, but stay
logically separated by folder and by not referencing each other's handlers
directly).

## Database
- **Single database**, shared across all tenants
- **`TenantId` column on every tenant-scoped table** (User, Patient,
  Appointment, MedicalRecord, Prescription, Payment)
- **EF Core global query filter** on `TenantId` applied at `DbContext` level —
  every query is automatically scoped to the current user's tenant; a
  developer cannot accidentally forget to filter by tenant
- Migrations managed via EF Core Migrations, seeded with sample data (Bogus)
  for demo purposes

## API Design
- **REST** over JSON, versioned under `/api/v1/`
- Thin controllers: each endpoint dispatches a MediatR Command/Query to its
  handler in `Features/`
- Consistent error shape (RFC 7807 `ProblemDetails`) for all 4xx/5xx responses

## Authentication and Authorization
- **JWT bearer tokens** with refresh tokens (matches the CRN project pattern)
- Claims embedded in the token: `UserId`, `TenantId`, `Role`
- **Role-based authorization** via `[Authorize(Roles = "Doctor")]` style
  policies per endpoint
- **Multiple simultaneous sessions allowed** per the confirmed business rule —
  no server-side session/token revocation tracking needed for this scope
- Patient self-registration and staff-created accounts (by Admin) both flow
  through the same `Users`/`Patients` tables, differentiated by Role

## Deployment Model
- Single ASP.NET Core Web API deployed to **Azure App Service (F1 free tier)**
- **Azure SQL (free tier)** for the database; **LocalDB** for local dev
- Angular frontend built separately and served via Azure Static Web Apps (or
  the same App Service, decided in Phase 4)

## Why Not Microservices
Explicitly rejected for this project: the team size is one (you), the data
model is small and tightly related (a Patient's Appointments, Records, and
Prescriptions are constantly queried together), and microservices would add
deployment/operational complexity with zero benefit at this scale. This is
itself a good interview talking point — knowing *when not* to reach for
microservices is as valuable as knowing how to build them.

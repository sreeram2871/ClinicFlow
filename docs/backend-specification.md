# ClinicFlow — Backend Implementation Specification

This is the detailed blueprint for Phase 3 implementation. Every entity,
endpoint, and rule here traces back to `docs/business-requirements.md`.

---

## 1. Entity Definitions

```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string ClinicName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum UserRole { Admin, Doctor, Receptionist }

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Patient
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }      // set if patient has portal login
    public string FullName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum AppointmentStatus { Requested, Confirmed, Completed, Cancelled, NoShow }

public class Appointment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }     // references User where Role = Doctor
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MedicalRecordEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Prescription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string MedicineName { get; set; }
    public string Dosage { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum PaymentMethod { Cash, Other }

public class Payment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
}

public class DoctorSchedule   // simple weekly working hours, no per-day overrides
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DoctorId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
```

## 2. Database

- **DbContext:** single `ClinicFlowDbContext` with a global query filter:
  `modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _currentTenantId)`
  applied to every tenant-scoped entity above (all except none — `Tenant`
  itself is the root and isn't filtered).
- **EF Core Configurations:** one `IEntityTypeConfiguration<T>` class per
  entity under `Infrastructure/Data/Configurations/`, defining keys, required
  fields, string max lengths, and relationships (e.g. `Appointment.PatientId`
  → `Patient.Id`, restrict delete).
- **Migrations:** standard EF Core Migrations (`dotnet ef migrations add`).
- **Seeding:** a `DbSeeder` class using **Bogus** to generate 2-3 demo
  tenants, a handful of staff/doctors per tenant, ~20 patients, and a spread
  of past/future appointments — enough to make the Reports dashboard show
  meaningful numbers.

## 3. API Endpoints

All routes prefixed `/api/v1/`. Auth column shows required role(s);
`Any authenticated` means any logged-in role.

### Auth
| Method | Route | Request | Response | Auth |
|---|---|---|---|---|
| POST | `/auth/register-patient` | `{ fullName, email, password, phone, dateOfBirth }` | `{ userId }` | None (public) |
| POST | `/auth/login` | `{ email, password }` | `{ accessToken, refreshToken, role, tenantId }` | None |
| POST | `/auth/refresh` | `{ refreshToken }` | `{ accessToken, refreshToken }` | None |

**Validation:** email format, password min 8 chars. **Errors:** 401 on bad
credentials, 400 on validation failure — never reveal whether the email
exists (generic "invalid credentials" message).

### Staff (Admin only)
| Method | Route | Request | Response | Auth |
|---|---|---|---|---|
| POST | `/staff` | `{ fullName, email, password, role }` (role: Doctor/Receptionist) | `{ userId }` | Admin |
| PATCH | `/staff/{id}/deactivate` | — | `204` | Admin |

### Appointments
| Method | Route | Request | Response | Auth |
|---|---|---|---|---|
| GET | `/doctors/{doctorId}/available-slots?date=` | — | `[{ start, end }]` | Any authenticated |
| POST | `/appointments` | `{ patientId, doctorId, start, end }` | `{ appointmentId, status }` | Patient (self), Receptionist (any patient) |
| PATCH | `/appointments/{id}/confirm` | — | `204` | Receptionist, Doctor |
| PATCH | `/appointments/{id}/cancel` | — | `204` | Patient (own), Receptionist |
| PATCH | `/appointments/{id}/complete` | `{ status }` (Completed/NoShow) | `204` | Receptionist |
| GET | `/doctors/{doctorId}/schedule?date=` | — | `[Appointment]` | Doctor (own), Receptionist, Admin |

**Business rules enforced in handler:**
- Reject if `start`/`end` falls outside the doctor's `DoctorSchedule` for that
  day of week → `400` with message "Outside doctor's working hours"
- Reject if overlapping with an existing `Confirmed`/`Requested` appointment
  for that doctor → `409 Conflict`
- Patient-initiated booking → status starts at `Requested`;
  Receptionist-initiated → status starts at `Confirmed` directly

### Patients
| Method | Route | Request | Response | Auth |
|---|---|---|---|---|
| POST | `/patients` | `{ fullName, dateOfBirth, phone, email }` | `{ patientId }` | Receptionist |
| GET | `/patients/{id}` | — | `Patient + recent appointments` | Doctor, Receptionist, Admin, Patient (own only) |
| POST | `/patients/{id}/records` | `{ notes, appointmentId? }` | `{ recordId }` | Doctor |
| GET | `/patients/{id}/records` | — | `[MedicalRecordEntry]` | Doctor, Patient (own) |

### Prescriptions
| Method | Route | Request | Response | Auth |
|---|---|---|---|---|
| POST | `/patients/{id}/prescriptions` | `{ medicineName, dosage, notes }` | `{ prescriptionId }` | Doctor |
| GET | `/patients/{id}/prescriptions` | — | `[Prescription]` | Doctor, Patient (own) |

### Billing
| Method | Route | Request | Response | Auth |
|---|---|---|---|---|
| POST | `/appointments/{id}/payment` | `{ amount, method }` | `{ paymentId }` | Receptionist |

### Reports
| Method | Route | Request | Response | Auth |
|---|---|---|---|---|
| GET | `/reports/dashboard` | — | `{ appointmentsToday, revenueThisMonth, totalPatients }` | Admin |

## 4. Cross-Cutting Rules

- **Every request** except `/auth/*` requires a valid JWT; `TenantId` is
  extracted from the token claims and set on the DbContext before any query
  runs — never taken from a request body/query param.
- **Ownership checks** ("Patient (own)", "Doctor (own patients)") are
  enforced in the handler by comparing the resource's `PatientId`/`DoctorId`
  against the caller's identity claim, on top of the role check.
- **Error format:** every 4xx/5xx returns `ProblemDetails`:
  `{ type, title, status, detail, traceId }`.

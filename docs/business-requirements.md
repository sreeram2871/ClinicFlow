# ClinicFlow — Business Requirements Document (BRD)

## 1. Overview
ClinicFlow is a multi-tenant clinic management SaaS for solo doctors and small
clinics. Each clinic is a tenant; a single deployment serves many clinics with
isolated data.

## 2. User Roles and Permissions

| Role | Permissions |
|---|---|
| **Admin** | Manage clinic profile, manage staff accounts (doctors/receptionists), view all reports, full read access to all modules within their tenant |
| **Doctor** | View own appointment schedule, view/update patient medical records for their patients, create prescriptions |
| **Receptionist** | Register patients, book/reschedule/cancel appointments on behalf of patients, record payments, view (not edit) medical records |
| **Patient** | Self-register, book/cancel own appointments, view own records and prescriptions, no access to other patients' data |

All roles are scoped to a single tenant (clinic). A user cannot see or act on
data belonging to another tenant, enforced at the data layer.

## 3. Core Entities and Relationships

- **Tenant (Clinic)** — 1 tenant has many Users, Patients, Appointments
- **User** — belongs to one Tenant; has one Role (Admin/Doctor/Receptionist);
  Patients are a separate entity (not a User) unless self-service portal login
  is enabled, in which case a Patient has an associated login account
- **Patient** — belongs to one Tenant; has many Appointments, Prescriptions,
  Medical Record entries
- **Appointment** — belongs to one Patient and one Doctor; has a status
  (Requested, Confirmed, Completed, Cancelled, No-show)
- **MedicalRecord** — belongs to one Patient; append-only visit notes entries
- **Prescription** — belongs to one Patient, authored by one Doctor; free-text
  medicine/dosage/notes
- **Payment** — belongs to one Appointment/Patient; manually recorded amount,
  method (cash/other), date

## 4. Core Workflows

### 4.1 Patient Self-Booking
1. Patient logs into portal, selects clinic doctor and available slot
2. System creates Appointment with status `Requested`
3. Receptionist or Doctor confirms → status `Confirmed`
4. On visit day, Receptionist marks `Completed` or `No-show`

### 4.2 Receptionist-Assisted Booking
1. Receptionist registers a new patient (or finds existing one) and books
   directly into a slot → status `Confirmed` immediately (no approval step
   needed since staff-initiated)

### 4.3 Consultation and Prescription
1. Doctor opens patient record from their schedule
2. Doctor adds a Medical Record entry (visit notes) and, if needed, a
   Prescription (medicine, dosage, notes)

### 4.4 Billing
1. After an appointment, Receptionist records a Payment (amount, method)
   against the appointment
2. No online payment gateway — cash/manual entry only for this scope

### 4.5 Reporting
1. Admin views dashboard: appointments per day, revenue per month,
   total patient count — scoped to their tenant only

## 5. Business Rules

- A Patient cannot double-book the same Doctor at an overlapping time slot
- A Doctor's available slots are defined by a simple working-hours schedule
  per day (e.g. Mon–Sat 9am–5pm) — no per-day custom overrides in this scope
- Only a Doctor can create/edit Prescriptions and Medical Records for their
  own patients
- Only Admin can create/deactivate staff (Doctor/Receptionist) accounts
- A cancelled appointment frees its slot immediately

## 6. Edge Cases

- Two patients attempt to book the same slot simultaneously → last write
  fails with a conflict error, first write wins
- Patient cancels within a very short window before appointment time →
  allowed, no penalty logic in this scope (flagged as future enhancement)
- Receptionist attempts to book outside doctor's working hours → rejected
  with validation error
- Deactivated staff account attempts login → rejected with clear error,
  no silent failure

## 7. Explicitly Out of Scope (this build)
- Online/gateway payments
- SMS/Email notifications
- Structured drug database / e-prescription standards / PDF generation
- Multi-clinic chains under one Admin (each Admin = one clinic/tenant)
- Insurance/claims processing

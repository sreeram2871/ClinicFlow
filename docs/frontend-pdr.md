# ClinicFlow — Frontend PDR (Product Document Requirement)

References the finished, verified backend API. Every endpoint below has been
manually tested via Swagger and covered by NUnit tests (see
`docs/backend-specification.md` and Phase 3 in `plans/clinicflow-mvp.md`).

Base URL (local dev): `https://localhost:7008/api/v1`

---

## 1. Authentication Flow

### Login
- **Page:** Login (public, no auth required)
- **Request:** `POST /auth/login`
```json
{ "email": "admin@apollo.test", "password": "Password123!" }
```
- **Response (200):**
```json
{
  "accessToken": "eyJhbGciOi...",
  "fullName": "Asha Admin",
  "role": "Admin",
  "tenantId": "11111111-1111-1111-1111-111111111111"
}
```
- **Response (401):** `{ "title": "Unauthorized", "status": 401, "detail": "Invalid email or password." }`
- **On success:** store `accessToken` in memory (Angular service, not localStorage — see Security Notes), store `role` and `tenantId` for conditional rendering, redirect based on role (see Section 3).

### Patient Self-Registration
- **Page:** Register (public)
- **Request:** `POST /auth/register-patient`
```json
{
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "fullName": "Test Patient",
  "email": "patient@example.com",
  "password": "Password123!",
  "phone": "9876543210",
  "dateOfBirth": "1995-06-15"
}
```
- **Response (200):** `{ "patientId": "...", "userId": "..." }`
- **Response (400):** duplicate email — show the `detail` message inline on the email field.
- **After success:** redirect to Login with a success message; do NOT auto-login (keep it simple for this MVP).

### Get Current User
- **Called:** immediately after login, and on app reload if a token exists
- **Request:** `GET /auth/me` (requires `Authorization: Bearer <token>`)
- **Response (200):** `{ "id", "fullName", "email", "role", "tenantId" }`
- **Response (401):** token invalid/expired → clear stored token, redirect to Login

### Session Behavior
- **No refresh token in this build** (deferred, see plan file) — access tokens expire in **15 minutes**. On any `401` with `www-authenticate: Bearer error="invalid_token"`, clear the session and redirect to Login with a "session expired" message. Do not silently retry.
- **Multiple simultaneous sessions are allowed** per the confirmed business rule — no special handling needed for concurrent logins.

### Logout
- Clear the in-memory token and any stored user info, redirect to Login. No API call needed (stateless JWT, nothing to invalidate server-side in this build).

---

## 2. Role-Based Navigation

Read the `role` claim from the login/`/auth/me` response. Show/hide nav items:

| Nav Item | Admin | Doctor | Receptionist | Patient |
|---|---|---|---|---|
| Dashboard (Reports) | ✅ | ❌ | ❌ | ❌ |
| Manage Staff | ✅ | ❌ | ❌ | ❌ |
| My Schedule | ❌ | ✅ | ❌ | ❌ |
| All Appointments | ❌ | ❌ | ✅ | ❌ |
| Register Patient | ❌ | ❌ | ✅ | ❌ |
| My Appointments | ❌ | ❌ | ❌ | ✅ |
| Patients (list/search) | ✅ | ✅ (own patients) | ✅ | ❌ |
| Billing | ❌ | ❌ | ✅ | ❌ |

Every protected route should redirect to Login if no valid token is present (Angular route guard checking the stored token/expiry before allowing navigation).

---

## 3. Per-Page Specifications

### 3.1 Dashboard (Admin only)
- **Endpoint:** `GET /reports/dashboard`
- **Response:** `{ "appointmentsToday": 0, "revenueThisMonth": 500, "totalPatients": 21 }`
- **UI:** three summary cards (Appointments Today, Revenue This Month, Total Patients). No filters/paging — single snapshot.

### 3.2 Manage Staff (Admin only)
- **Create:** `POST /auth/register-staff`
```json
{ "fullName": "Dr. New Doctor", "email": "newdoc@apollo.test", "password": "Password123!", "role": "Doctor" }
```
  - **Important:** `role` must be sent as the string `"Admin"`, `"Doctor"`, or `"Receptionist"` — backend uses `JsonStringEnumConverter`, so string names work directly, not numbers.
- **UI:** simple form with a role dropdown (Admin/Doctor/Receptionist), full name, email, password fields.
- **Validation to mirror client-side** (matches backend FluentValidation rules): full name required (max 200 chars), valid email format, password min 8 characters.

### 3.3 My Schedule (Doctor)
- **Endpoint:** `GET /doctors/{doctorId}/schedule?date=YYYY-MM-DD`
- **Response:**
```json
{
  "bookedSlots": [{ "appointmentId", "start", "end", "status" }],
  "availableSlots": [{ "start", "end" }]
}
```
- **UI:** date picker + a day-view calendar/list showing booked slots (with patient name — requires a follow-up `GET /patients/{id}` call per booked slot, or consider extending the backend response later) and available slots. Doctor's own `doctorId` comes from their `/auth/me` response.

### 3.4 All Appointments (Receptionist)
- **Book:** `POST /appointments`
```json
{ "patientId": "...", "doctorId": "...", "start": "2026-08-10T10:00:00", "end": "2026-08-10T10:30:00", "bookedByStaff": true }
```
- **Confirm:** `PATCH /appointments/{id}/confirm` → `204`
- **Cancel:** `PATCH /appointments/{id}/cancel` → `204`
- **Complete:** `PATCH /appointments/{id}/complete`, body `{ "status": "Completed" }` or `{ "status": "NoShow" }` → `204`
- **UI:** a schedule view (likely reusing the same doctor-schedule endpoint with a doctor picker) plus action buttons per appointment row: Confirm / Cancel / Complete, shown conditionally based on current `status` (see state machine below).

**Appointment status state machine (drives which buttons show):**
```
Requested → Confirmed → Completed
    ↓            ↓
Cancelled    Cancelled
    (Confirmed can also → NoShow via Complete with status=NoShow)
```
- `Requested`: show Confirm, Cancel
- `Confirmed`: show Complete (Completed/NoShow), Cancel
- `Completed`/`Cancelled`/`NoShow`: no action buttons, terminal state

### 3.5 Register Patient (Receptionist)
- **Endpoint:** `POST /patients`
```json
{ "fullName": "...", "dateOfBirth": "1990-01-01", "phone": "...", "email": "..." }
```
- **Note:** this endpoint is listed in the backend spec but wasn't built as a separate feature in Phase 3 — patient creation currently only happens via `POST /auth/register-patient` (self-service, creates both User+Patient). **Flag for backend follow-up:** a Receptionist-created walk-in patient (Patient row only, no login) endpoint may need to be added before this screen can work as specified. Note this as an open item, don't block frontend work on it — build the screen against `register-patient` for now if this isn't resolved first.

### 3.6 My Appointments (Patient)
- **Book (self):** `POST /appointments` with `"bookedByStaff": false` → results in `"status": "Requested"`
- **Cancel own:** `PATCH /appointments/{id}/cancel`
- **View available slots:** `GET /doctors/{doctorId}/schedule?date=`
- **UI:** doctor picker → date picker → available slots list → book button. Below that, "My Upcoming Appointments" list with a Cancel button on non-terminal ones.

### 3.7 Patients (Admin/Doctor/Receptionist)
- **Get profile:** `GET /patients/{id}` → `{ id, fullName, dateOfBirth, phone, email, recentAppointments }`
- **Get medical history:** `GET /patients/{id}/records` → list of `{ id, notes, doctorId, appointmentId, createdAt }`
- **Add note (Doctor only):** `POST /patients/{id}/records`, body `{ "notes": "...", "appointmentId": null }`
- **Access errors to handle in UI:** `403` (Doctor viewing untreated patient, or Patient viewing someone else) — show a clear "you don't have access to this record" message, not a generic error.
- **UI:** patient search/list → detail view with tabs: Profile, Medical History, Prescriptions.

### 3.8 Prescriptions (nested under Patients)
- **Create (Doctor only):** `POST /patients/{id}/prescriptions`, body `{ "medicineName", "dosage", "notes" }`
- **List:** `GET /patients/{id}/prescriptions`
- **UI:** simple list + "Add Prescription" form (Doctor view only), read-only list for other roles.

### 3.9 Billing (Receptionist)
- **Record payment:** `POST /appointments/{id}/payment`, body `{ "amount": 500, "method": "Cash" }`
- **Constraint to reflect in UI:** only show the "Record Payment" action for appointments with `status: "Completed"` that don't already have a payment (backend returns `409` for both wrong-status and duplicate-payment — surface the `detail` message from the error response either way).

---

## 4. Error Handling (applies globally)

Every error follows the same `ProblemDetails` shape:
```json
{ "title": "...", "status": 400, "detail": "...", "instance": "/api/v1/..." }
```
- Build one Angular HTTP interceptor that catches all error responses and surfaces `detail` as a toast/inline message — don't write per-call error handling.
- **Status code → UI behavior:**
  - `400` — show `detail` as inline validation error
  - `401` — clear session, redirect to Login (token invalid/expired)
  - `403` — show "you don't have permission for this" message, don't redirect
  - `404` — show "not found" state
  - `409` — show `detail` as a toast (conflict — e.g. double-booking, duplicate payment)
  - `500` — generic "something went wrong" toast

---

## 6. Visual Design Direction (decided after wireframe review)

Richer dashboard style confirmed as the build target — same Angular stack,
just more components than the original bare-lists approach. No framework
or architecture change; this is a styling/component-count decision only.

### Brand & color
- **Primary brand color:** teal `#0F6E56` — used for all primary action
  buttons across every screen (Book, Add staff, Record payment, etc.)
- **Status badge colors (semantic, consistent everywhere):**
  - Confirmed → blue (`--bg-accent` / `--text-accent`)
  - Requested → amber (`--bg-warning` / `--text-warning`)
  - Completed → teal/green (`--bg-success` / `--text-success`)
  - Cancelled / No-show → red (`--bg-danger` / `--text-danger`)
- **Per-role topbar tint** (visual "which mode am I in" cue only, not
  functional): Admin = purple, Doctor = blue, Receptionist = coral,
  Patient = teal

### Shared components to build (used across multiple screens)
- `StatCardComponent` — colored summary card with label + big number,
  optional mini sparkline/bar visual. `@Input() color`, `label`, `value`.
- `ChartCardComponent` — wraps **ng2-charts** (Angular wrapper around
  Chart.js — same library used in the approved mockups) for line/bar/
  doughnut charts inside a card.
- `ProfileCardComponent` — avatar (initials circle, no photo uploads in
  this MVP) + name + key details, used on Patient detail and dashboard
  side panels.
- `CalendarStripComponent` — horizontal week-strip date picker (used on
  Doctor schedule and Patient detail panel).
- `StatusBadgeComponent` — `@Input() status` renders the correct color
  per the semantic mapping above; single source of truth so the color
  logic never gets duplicated per screen.
- `AppShellComponent` — sidebar + topbar layout wrapper, role-aware nav
  items rendered from the role-based navigation table in Section 2.

### Per-role dashboard content (confirmed via mockup review)
- **Admin:** 3 stat cards (Appointments today, Revenue this month, Total
  patients) + revenue trend line chart + new-patients bar chart + staff
  list panel
- **Doctor:** 3 stat cards (Appointments today, Patients waiting, Avg
  consult time) + appointments-this-week line chart + completed-vs-
  cancelled bar chart + patient profile panel + calendar strip
- **Receptionist:** 3 stat cards (Today's appointments, Pending
  confirmations, Collected today) + booking queue list + appointments-
  by-status doughnut chart + quick-action buttons
- **Patient:** 2 stat cards (Upcoming appointments, Active prescriptions)
  + next-appointment card + latest-prescription card — deliberately
  simpler than staff dashboards, matching a patient's narrower needs

### New dependency
- `ng2-charts` (+ `chart.js` peer dependency) — add during Angular
  scaffolding, before building any dashboard screen.

## 7. Security Notes for Frontend Implementation

- **Never use `localStorage` for the access token** — store it in an Angular service (in-memory) to reduce XSS token-theft risk. This means a full page refresh loses the session (acceptable trade-off for this MVP; call `/auth/me` on app init if a "remember me" pattern is added later).
- Attach `Authorization: Bearer <token>` via an Angular HTTP interceptor, not manually on every call.
- Never log the access token to the browser console in production builds.


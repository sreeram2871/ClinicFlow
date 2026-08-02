# ClinicFlow MVP — Resume Project

Build a real, working multi-tenant clinic management SaaS (ASP.NET Core +
Angular) as a portfolio/resume project. Scope: Appointments, Patient Records,
Billing (manual), Prescriptions (text-only), basic Reports. 4 roles: Admin,
Doctor, Receptionist, Patient. Shared-DB multi-tenancy via TenantId.
Deploy to Azure free tier.

## For Future Agents
As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is
done, set its status to `Complete` and write its **Phase Summary** (what was
done, key decisions, anything needed to continue with zero context); run the
phase's **Verification Plan** and record the result before moving on. When all
phases are done, fill in **Final Recap** and **Deployment Plan**.

Reference docs: `docs/business-requirements.md`, `docs/non-functional-requirements.md`

## Phase 1: Requirements and Planning
Status: Complete

- [x] Define business requirements (roles, entities, workflows, business rules, edge cases)
- [x] Define non-functional requirements (availability, performance, scalability, security, cost, maintainability, reliability, testability)

### Verification Plan
- `docs/business-requirements.md` and `docs/non-functional-requirements.md` exist and are reviewed by Sreeram — confirmed via interview.

### Phase Summary
Interviewed Sreeram to full agreement on scope. Key decisions: 4 roles
(Admin/Doctor/Receptionist/Patient), 5 modules (Appointments, Patient Records,
Billing, Prescriptions, Reports), shared-DB multi-tenancy with TenantId,
no payment gateway, no SMS/email notifications (in-app only), simple
text-only prescriptions, basic reports dashboard, Azure free-tier deployment
target, ~$0/month cost constraint. Full detail in the BRD and NFR docs.

## Phase 2: Architecture and Specification
Status: Complete

- [x] Propose 2-3 architecture options with trade-offs (Modular Monolith vs alternatives)
- [x] Get architecture decision confirmed by Sreeram — chose Vertical Slice Architecture
- [x] Define module boundaries, DB schema approach, API design approach, auth strategy, deployment model
- [x] Generate detailed Backend Implementation Specification (entities, EF Core mappings, endpoints, validation, auth requirements per endpoint)

### Verification Plan
- Architecture document exists in `docs/architecture.md` and is explicitly approved by Sreeram (not just accepted by default).
- `docs/backend-specification.md` covers every entity and endpoint from the BRD with no `[CLARIFICATION NEEDED]` tags remaining.

### Phase Summary
Chose Vertical Slice Architecture over Clean Architecture (already used on the
eCommerce project) and Simple Layered Monolith, for learning diversity and
because it mirrors the BRD's feature-based structure. Documented full project
structure, module boundaries, DB approach (single DB, TenantId global query
filter), REST API design, JWT auth with role-based authorization, and Azure
free-tier deployment target in `docs/architecture.md`. Wrote the complete
backend spec in `docs/backend-specification.md` covering all 7 entities and
every endpoint (Auth, Staff, Appointments, Patients, Prescriptions, Billing,
Reports) with request/response shapes, business rules, and auth requirements.
Ready to begin Phase 3 implementation.

## Phase 3: Backend Implementation
Status: In progress

- [x] Scaffold ASP.NET Core solution per architecture doc (project structure, DI, CLAUDE.md for backend repo)
- [x] Implement entities, EF Core mappings, and migrations
- [x] Implement TenantId global query filter for multi-tenancy isolation
- [x] Seed demo data (DbSeeder with Bogus) — verified in SQL Server Object Explorer
- [x] Implement JWT auth: password hashing (BCrypt), token generation, Login feature — verified via Swagger, real JWT returned
- [x] JWT authentication + authorization enforcement wired up (UseAuthentication/UseAuthorization, Swagger Authorize button, [Authorize] on GetMe) — verified: GET /auth/me returns 200 with valid token, clean 401 without
- [x] Global exception handling middleware with consistent error shape — verified: bad login now returns clean 401 ProblemDetails instead of raw 500
- [x] Register feature (patient self-registration, Admin-created staff accounts) — verified: RegisterPatient, RegisterStaff, role-based [Authorize(Roles="Admin")] all tested (200/403/401 confirmed)
- [x] MediatR ValidationBehavior pipeline (Common/Behaviors) — critical fix: FluentValidation validators were registered in DI but never actually invoked by MediatR until this was added; this applies retroactively to every command built so far (Login, Register, BookAppointment, etc.)
- [ ] Refresh token flow
- [x] Implement Appointments module (booking, conflict detection, status transitions)
  - [x] BookAppointment — working-hours rule, overlap conflict (409), staff-vs-patient status rule all verified in Swagger
  - [x] ConfirmAppointment, CancelAppointment, CompleteAppointment — verified: valid transitions succeed (204), invalid transitions rejected (409), role guards enforced (403 for Doctor on Complete)
  - [x] GetDoctorSchedule (available slots + doctor's appointment list) — verified: booked slots + computed available slots both correct
  - [x] 8-case regression test after ValidationBehavior fix — all passed (validation rejects bad input, state-transition guards correct, no more silent data corruption)
- [x] Implement Patient Records module
  - [x] GetPatientRecord with ownership enforcement (Admin/Receptionist: any patient; Patient: own record only; Doctor: only treated patients) — verified across 5 test cases
  - [x] ForbiddenException (403) added, separated from UnauthorizedAccessException (401) — fixed incorrect 401 on ownership-denied cases
  - [x] PatientAccessGuard extracted to Features/Patients/Shared — shared ownership logic, reused (not duplicated) across GetPatientRecord and AddMedicalRecordEntry
  - [x] AddMedicalRecordEntry (Doctor-only, layered check: [Authorize(Roles="Doctor")] + PatientAccessGuard confirms treatment relationship) — verified 200 for treated patient, 403 for untreated
  - [x] GetPatientMedicalHistory (list of a patient's medical history entries) — verified: Doctor sees their own note for a treated patient, guard reused a third time with zero duplication
- [x] Implement Billing module (manual payment entry) — RecordPayment: Receptionist-only, only Completed appointments billable, duplicate-payment guard — verified 200 success, 409 duplicate, 409 wrong status, 403 wrong role
- [x] Implement Prescriptions module (text-only) — CreatePrescription (Doctor-only, PatientAccessGuard reused a 4th time) + GetPatientPrescriptions — verified 200 for treated patient, 403 for untreated, 403 for wrong role, list correctly shows created prescription
- [x] Implement Reports module (basic aggregates) — GetDashboardSummary (Admin-only): appointments today, revenue this month, total patients — verified 200 with real numbers, 403 for Doctor/Receptionist
- [x] NUnit tests: business rules (booking conflicts, tenant isolation, authorization) + key endpoint integration tests — 31/31 passing, full coverage plan complete
  - [x] **CRITICAL FIX**: ClinicFlowDbContext's tenant query filter used `Expression.Constant(_tenantProvider.TenantId)`, which bakes in the TenantId at first model compilation and never re-reads it — meaning the first tenant to ever hit the server after startup silently became permanent for all future queries, on every entity, for every tenant. Invisible in manual testing because TemporaryTenantProvider always returned the same hardcoded value. Caught by NUnit tests using distinct random tenant IDs per test. Fixed by referencing a `CurrentTenantId` property on the DbContext instance itself (`Expression.Constant(this)` + `Expression.Property`) instead of a baked-in value, so it re-evaluates per-query.
  - [x] BookAppointmentHandlerTests (4): successful booking, overlap conflict (409), working-hours rejection (400), staff-vs-patient status rule
  - [x] TenantIsolationTests (2): basic isolation + A→B→A regression test guarding against the staleness bug above
  - [x] PatientAccessGuardTests (6): Admin/Receptionist always allowed, Patient own-record allowed, Patient other-record forbidden, Doctor treated-patient allowed, Doctor untreated-patient forbidden
  - [x] AppointmentStatusTransitionTests (6): Confirm/Cancel/Complete, one success + one invalid-transition case each
  - [x] RecordPaymentHandlerTests (3): Completed appointment succeeds, non-Completed fails, duplicate payment fails
  - [x] LoginHandlerTests (4): correct credentials, wrong password, non-existent email, deactivated account — using FakePasswordHasher/FakeJwtTokenService to isolate handler logic from real crypto
  - [x] RegisterPatientHandlerTests (2): successful registration (verifies User+Patient link), duplicate email fails
  - [x] GetDoctorScheduleHandlerTests (2): booked slot correctly excluded from available slots, no schedule for a day returns empty
  - [x] ValidationBehaviorTests (2): valid command reaches handler, invalid command throws and handler is never called — the test that would have caught the original silent-validation bug automatically
- [ ] Fresh-session code review pass: code quality, performance, security (three separate passes) — DEFERRED, will circle back before final deployment

### Verification Plan
- `dotnet build` succeeds with zero errors/warnings.
- `dotnet test` — all tests pass.
- Manual: Postman/Swagger walkthrough of booking conflict scenario returns expected 409/validation error.
- Manual: attempt cross-tenant data access returns 403/404, never another tenant's data.

### Phase Summary
_(write when phase completes)_

## Phase 4: Frontend Implementation
Status: In progress

- [x] Generate Frontend Product Document Requirement (PDR) referencing the finalized backend API — docs/frontend-pdr.md: auth flow, role-based nav, per-page endpoint specs for all 9 screens, appointment state machine, global error handling, security notes (no localStorage for tokens). One open item flagged: Receptionist-initiated walk-in patient creation (POST /patients) wasn't built as a separate feature in Phase 3 — only self-registration exists; noted as a backend follow-up, not blocking frontend start.
- [x] Visual design direction confirmed after wireframe/mockup review (richer dashboard style — stat cards, charts via ng2-charts, profile panels, calendar strip — same Angular stack, more components than a bare-lists approach). Documented in PDR Section 6: brand color (teal #0F6E56), semantic status badge colors, per-role topbar tints, 6 shared components to build (StatCard, ChartCard, ProfileCard, CalendarStrip, StatusBadge, AppShell), per-role dashboard content confirmed.
- [ ] Scaffold Angular app (CLAUDE.md for frontend repo)
  - [x] Angular 22 project created (`ng new frontend --routing --style=scss --skip-tests`), Node/Angular CLI version mismatch fixed first (CLI 17→22 to support Node 24)
  - [x] Default template cleared, router-outlet set up, SCSS color palette variables added (brand teal #0F6E56, semantic status colors, per-role tints)
  - [x] Folder structure created: core/{services,interceptors,guards}, shared/components, features/{auth,admin,doctor,receptionist,patient}, models
  - [x] TypeScript models created: user.model.ts, auth.model.ts, appointment.model.ts (mirroring backend response records)
  - **[WORKFLOW NOTE]**: from this point, frontend code is generated via an AI coding agent in VS Code rather than manually typed line-by-line (unlike the backend build). Process: Claude designs each piece and its requirements → provides an exact prompt for the agent → user runs it and pastes the output back → Claude reviews against the design, flags issues, explains key parts. User still directs and verifies every piece, but doesn't hand-type it.
  - [x] AuthService created (agent-generated, 2 review rounds): signal-based currentUser (properly private+readonly via asReadonly() — first draft had a real bug where currentUser was writable from any component, bypassing login()), in-memory token storage, login()/logout()/getToken()
  - [x] authInterceptor created (agent-generated, 1 review round, no issues found): functional HttpInterceptorFn style, attaches Authorization: Bearer header when a token exists, passes through unmodified otherwise
  - [x] provideHttpClient(withInterceptors([authInterceptor])) registered in app.config.ts (agent-generated, no issues found) — without this, HttpClient injection would throw NullInjectorError at runtime, not silently skip the interceptor
  - [x] LoginComponent created (agent-generated, 2 review rounds): standalone component, reactive forms with email/password validators, loading state signal, teal-branded card layout matching PDR. Real bug caught in round 1: extractErrorMessage read error.detail directly instead of error.error?.detail — HttpClient wraps the backend's ProblemDetails body inside HttpErrorResponse.error, so every login failure was silently falling through to a generic "Login failed" message instead of the backend's real detail text. Fixed and verified.
- [ ] Implement auth flow (login, token storage, refresh-on-401, logout)
- [ ] Implement role-based navigation and conditional rendering per role
- [ ] Implement Appointments UI (patient self-booking + receptionist booking view)
- [ ] Implement Patient Records UI
- [ ] Implement Billing UI
- [ ] Implement Prescriptions UI
- [ ] Implement Reports dashboard UI

### Verification Plan
- `ng build` succeeds with zero errors.
- Manual: full login → book appointment → confirm → complete → record payment flow works end-to-end in the browser for each role.

### Phase Summary
_(write when phase completes)_

## Phase 5: Deployment
Status: Not started

- [ ] GitHub Actions CI/CD pipeline (build, test, Docker image for backend; lint/build for frontend)
- [ ] Deploy backend to Azure App Service (F1 free tier) + Azure SQL (free tier)
- [ ] Deploy frontend (Azure Static Web Apps or same App Service)
- [ ] Human review: manual end-to-end test of every user flow across all 4 roles
- [ ] Human review: read through handlers/validators for business-logic errors
- [ ] Human review: edge case testing (empty forms, concurrent booking, DB down)

### Verification Plan
- CI pipeline is green on a test PR.
- Deployed URL is reachable and login works for a seeded demo user of each role.

### Phase Summary
_(write when phase completes)_

## Final Recap
_(write when all phases complete: summary of the entire piece of work)_

## Deployment Plan
_(write when all phases complete: step-by-step deployment instructions)_

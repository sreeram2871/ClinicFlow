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
- [ ] Refresh token flow
- [ ] Implement Appointments module (booking, conflict detection, status transitions)
  - [x] BookAppointment — working-hours rule, overlap conflict (409), staff-vs-patient status rule all verified in Swagger
  - [ ] ConfirmAppointment, CancelAppointment, CompleteAppointment
  - [ ] GetDoctorSchedule (available slots + doctor's appointment list)
- [ ] Implement Patient Records module
- [ ] Implement Billing module (manual payment entry)
- [ ] Implement Prescriptions module (text-only)
- [ ] Implement Reports module (basic aggregates)
- [ ] xUnit tests: business rules (booking conflicts, tenant isolation, authorization) + key endpoint integration tests
- [ ] Fresh-session code review pass: code quality, performance, security (three separate passes)

### Verification Plan
- `dotnet build` succeeds with zero errors/warnings.
- `dotnet test` — all tests pass.
- Manual: Postman/Swagger walkthrough of booking conflict scenario returns expected 409/validation error.
- Manual: attempt cross-tenant data access returns 403/404, never another tenant's data.

### Phase Summary
_(write when phase completes)_

## Phase 4: Frontend Implementation
Status: Not started

- [ ] Generate Frontend Product Document Requirement (PDR) referencing the finalized backend API (exact JSON examples per endpoint)
- [ ] Scaffold Angular app (CLAUDE.md for frontend repo)
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

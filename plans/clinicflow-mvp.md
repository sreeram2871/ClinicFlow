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
- [x] Scaffold Angular app (CLAUDE.md for frontend repo)
  - [x] Angular 22 project created (`ng new frontend --routing --style=scss --skip-tests`), Node/Angular CLI version mismatch fixed first (CLI 17→22 to support Node 24)
  - [x] Default template cleared, router-outlet set up, SCSS color palette variables added (brand teal #0F6E56, semantic status colors, per-role tints)
  - [x] Folder structure created: core/{services,interceptors,guards}, shared/components, features/{auth,admin,doctor,receptionist,patient}, models
  - [x] TypeScript models created: user.model.ts, auth.model.ts, appointment.model.ts (mirroring backend response records)
  - **[WORKFLOW NOTE]**: from this point, frontend code is generated via an AI coding agent in VS Code rather than manually typed line-by-line (unlike the backend build). Process: Claude designs each piece and its requirements → provides an exact prompt for the agent → user runs it and pastes the output back → Claude reviews against the design, flags issues, explains key parts. User still directs and verifies every piece, but doesn't hand-type it.
  - [x] AuthService created (agent-generated, 2 review rounds): signal-based currentUser (properly private+readonly via asReadonly() — first draft had a real bug where currentUser was writable from any component, bypassing login()), in-memory token storage, login()/logout()/getToken()
  - [x] authInterceptor created (agent-generated, 1 review round, no issues found): functional HttpInterceptorFn style, attaches Authorization: Bearer header when a token exists, passes through unmodified otherwise
  - [x] provideHttpClient(withInterceptors([authInterceptor])) registered in app.config.ts (agent-generated, no issues found) — without this, HttpClient injection would throw NullInjectorError at runtime, not silently skip the interceptor
  - [x] LoginComponent created (agent-generated, 2 review rounds): standalone component, reactive forms with email/password validators, loading state signal, teal-branded card layout matching PDR. Real bug caught in round 1: extractErrorMessage read error.detail directly instead of error.error?.detail — HttpClient wraps the backend's ProblemDetails body inside HttpErrorResponse.error, so every login failure was silently falling through to a generic "Login failed" message instead of the backend's real detail text. Fixed and verified.
  - [x] Routing configured: /login lazy-loaded via loadComponent, default '' redirects to /login (agent-generated, no issues found)
  - [x] Login screen rendering confirmed in browser (ng serve, localhost:4200) — first visible screen of ClinicFlow
  - [x] Backend CORS configured (AllowAngularDev policy, localhost:4200 origins only — not AllowAnyOrigin, deliberate security choice) — full login flow verified end-to-end: form submit → real JWT returned → stored in AuthService signal
  - [x] AppShellComponent created (agent-generated, no issues found): topbar with user name/role + logout, sidebar placeholder, nested router-outlet for child routes
  - [x] Placeholder DashboardComponent created, nested routing wired (/dashboard → AppShellComponent → child route → DashboardComponent) — introduces nested router-outlet pattern, distinct from the top-level outlet in app.html
  - [x] Full login → shell → logout loop verified in browser end-to-end
  - **[GAP FLAGGED → FIXED]**: no route guard existed — /dashboard was reachable by typing the URL directly even when logged out (confirmed visually: shell rendered with empty "Welcome," and no user name in topbar). Fixed with authGuard (functional CanActivateFn, agent-generated, no issues found): checks isLoggedIn(), returns router.createUrlTree(['/login']) redirect (not just false) when unauthenticated. Applied to the parent 'dashboard' route only — confirmed this correctly protects all nested child routes too, since children only activate after parent guards pass.
  - [x] Role-based sidebar navigation implemented in AppShellComponent (agent-generated, 2 review rounds): navItems array with per-item roles list, visibleNavItems computed signal filters by current user's role, routerLinkActive for current-page styling. Real bug caught in round 1: template used *ngFor but NgFor was missing from the standalone component's imports array — would have failed to compile ("not a known attribute"), same class of mistake as forgetting to register a MediatR pipeline behavior on the backend. Fixed and verified.
  - [x] Role-based nav verified in browser: Admin correctly sees only Dashboard/Manage Staff/Patients, other role-only items correctly hidden
  - [x] StatCardComponent created (agent-generated, no issues found): reusable shared component, @Input label/value/color, teal-default styling
  - [x] DashboardSummary model + DashboardService created (agent-generated, 1 review round): real bug caught — service file both imported DashboardSummary from models/ AND redeclared the same interface locally in the same file, defeating the point of having a shared model and creating two sources of truth for one type. Fixed: model confirmed to genuinely exist in models/dashboard.model.ts, service now only imports it.
  - [x] Real DashboardComponent wired to live backend data (agent-generated, 1 review round): 3 stat cards in a grid, bound to real GET /reports/dashboard response. Bug caught: subscribe() only handled the success case, no error handling — a failed call would leave the page silently blank with no feedback. Fixed with object-form subscribe (next/error).
  - **[GAP FLAGGED, not yet fixed]**: authGuard only checks isLoggedIn(), not role — a logged-in Doctor/Receptionist could currently navigate to /dashboard directly. Backend correctly rejects with 403, but combined with silent-failure risk this is a real gap. Deferred to fix all at once when building routes for the other roles' screens (Doctor schedule, Receptionist appointments, etc.) rather than patching piecemeal now.
  - **[BACKEND GAPS FILLED for Admin dashboard charts/staff panel]**: two missing endpoints identified before building UI on top of them (same instinct as catching bugs early) —
    - Added GetStaffList (GET /auth/staff, Admin-only): lists all non-Patient Users, tenant-scoped automatically. Verified 200 for Admin, 403 for Doctor.
    - Extended GetDashboardSummary to add revenueByWeek and newPatientsByWeek (6-week bucketed trend arrays, WeeklyDataPoint shared shape). Verified real data returned correctly — heavily lopsided toward one week since all demo/test data was created in a single session, not spread across real weeks; confirmed as expected/correct, not a bug.
  - [x] ng2-charts + chart.js installed, dashboard.model.ts extended with WeeklyDataPoint + trend fields (agent-generated, no issues found)
  - [x] ChartCardComponent created (agent-generated, 1 review round): reusable chart wrapper around ng2-charts' BaseChartDirective. Significant bug caught: first draft used the old @Input() decorator combined with computed() — computed() only reacts to signal dependencies, and @Input() properties aren't signals, so the chart would compute once against the default empty array (before async dashboard data arrives) and then NEVER update again, permanently showing a blank chart despite real data existing — same "computed once, silently stale forever" bug class as the backend's tenant-filter staleness issue. Fixed by converting to Angular's signal-based input() throughout.
  - [x] StaffMember model + StaffService created, DashboardComponent updated with charts + staff panel (agent-generated, no issues found — clean on first pass) — Admin Dashboard is now FULLY COMPLETE per the mockup: 3 stat cards, 2 real trend charts (revenue, new patients), staff list panel, all wired to live backend data
  - **[BUG CAUGHT IN BROWSER, FIXED]**: charts rendered as empty boxes with just the title showing — console showed "category is not a registered scale". Chart.js v4 (used by ng2-charts) no longer auto-registers its chart types/scales/controllers; this must be done explicitly. Fixed by adding provideCharts(withDefaultRegisterables()) to app.config.ts providers. Same pattern as forgetting provideHttpClient earlier — a missing provider registration causing a feature to silently not work.
  - [x] Charts confirmed rendering correctly in browser: line chart peak and bar chart bar both correctly positioned at the exact week matching real backend data (W5) — Admin Dashboard genuinely complete: 3 stat cards + 2 accurate charts + staff panel, all live data, verified visually correct
  - [x] Staff row spacing fixed (staff-row flex/gap/border-bottom in dashboard.component.scss)
  - [x] Chart sizing bugs fixed: canvas was bulging beyond its card (fixed with position:relative + explicit height on .chart-area, max-width/max-height on canvas — Chart.js needs a stable positioned container to measure against for responsive sizing); bar chart bar rendered as an oversized square (fixed with maxBarThickness: 40, barPercentage: 0.5 on the dataset)
  - [x] ManageStaffComponent created (agent-generated, 1 round, no functional bugs — minor style note only: ngOnInit present but "implements OnInit" not declared, harmless since Angular calls lifecycle hooks by method name not interface): reactive form (fullName/email/password/role), createStaff() added to StaffService, list refreshes after successful creation, error handling matches established error.error?.detail pattern
  - [x] /dashboard/staff route wired (agent-generated, no issues found)
  - [x] Manage Staff screen verified end-to-end in browser: form + existing list render, new staff creation confirmed working, list refreshes correctly
- [ ] Implement Patients screen (list, profile, history, add note)
  - **[GAP FLAGGED → FIXED]**: no GET /patients (list all) endpoint existed on the backend — only single-patient lookup was built. Added GetPatientsList with role-aware filtering (Admin/Receptionist see all tenant patients; Doctor sees only patients with at least one appointment together, via an EF Core correlated subquery — a new pattern, distinct from PatientAccessGuard since that guards access to one already-known patient, this filters which patients appear in a list at all). Verified: Admin call returned all 21 patients, Doctor call returned exactly 1 (the same patient from earlier BookAppointment/AddMedicalRecordEntry tests) — filtering confirmed genuinely correct, not just present.
  - [x] Frontend: patient.model.ts, patient.service.ts, PatientsListComponent created (agent-generated, no issues found — both NgFor and DatePipe correctly imported this time, no repeat of the earlier missing-import bug class)
  - [x] /dashboard/patients route wired (agent-generated, no issues found)
  - [x] Patients list verified in browser: all 21 patients render correctly with real data
  - [x] Patient detail models (PatientDetail, MedicalRecordEntry, Prescription) + PatientService extended (getPatientDetail, getMedicalHistory, addMedicalRecordEntry) + new PrescriptionService (getPrescriptions, createPrescription) created (agent-generated, no issues found) — no backend gaps this time, all 5 endpoints were already built and tested in Phase 3
  - [x] PatientDetailComponent base created (agent-generated, 1 review round): tab bar (Profile/History/Prescriptions), Profile tab shows patient info + recent appointments, History/Prescriptions still placeholders. Bug caught: error handler always showed a generic hardcoded message instead of extracting the real backend detail (404 "Patient not found" / 403 "You can only view patients you have treated" from PatientAccessGuard, both already tested in Phase 3) — a regression from the established error.error?.detail pattern used correctly elsewhere. Fixed and verified.
  - **[GAP FLAGGED EARLIER, NOW VISIBLY MANIFESTED]**: logging in as Doctor and landing on /dashboard (Admin's home) shows a broken-looking page — no stat cards, empty chart boxes, "No staff found" — because GetDashboardSummary and GetStaffList are both Admin-only on the backend (correctly returning 403), but the frontend only logs these failures silently instead of showing anything or redirecting. Root cause confirmed as exactly the authGuard role-gap flagged earlier. User decision: keep building remaining screens first (My Schedule for Doctor doesn't exist yet, so there's no good redirect target yet anyway), fix role-aware routing (post-login redirect per role + per-route role checks on authGuard) once more screens exist to redirect to.
  - [x] /dashboard/patients/:id route wired and verified in browser as Admin: clicking a patient row correctly navigates to their detail page, Profile tab shows real name/DOB/phone/email/recent appointments
  - [x] History and Prescriptions tabs built (agent-generated, 1 review round): lazy-loaded on first tab switch (loaded flags prevent refetching), reusing established error.error?.detail pattern. Bug caught: single shared errorMessage signal across all three tabs meant a failure in one tab would incorrectly hide already-successfully-loaded content in a different tab when switching back — fixed with three fully independent error signals (profileError/historyError/prescriptionsError), each tab's rendering now correctly isolated from the others' state.
  - [x] Doctor creation forms added (agent-generated, no issues found): "Add Note" form in History tab, "Save Prescription" form in Prescriptions tab, both gated to isDoctor() computed signal, both correctly refetch their list on success via extracted loadHistory()/loadPrescriptions() methods (shared between setTab and the new save handlers, no duplication), per-tab error isolation from the previous fix correctly preserved through the refactor
  - [x] Full Patients screen verified end-to-end in browser: Doctor sees and can use Add Note / Save Prescription forms, submissions correctly appear in their lists; Admin/Receptionist correctly do NOT see the forms (read-only lists only) — PATIENTS SCREEN FULLY COMPLETE (list, profile, history, prescriptions, role-gated view + create)
  - **[SIGNIFICANT BUG FOUND AND FIXED]**: while building My Schedule, discovered that AuthService.login() has always set currentUser().id to a hardcoded empty string — LoginResponse never included the real user ID, and nothing before now actually depended on it, so this stayed silently wrong for the entire build. My Schedule was the first feature to functionally need the real doctor ID (to fetch their own schedule). Fixed by chaining login() into a GET /auth/me call via switchMap (already-tested Phase 3 endpoint that returns the complete real user object including id), setting currentUserSignal from that response instead of the partial LoginResponse fields. login()'s public return type/behavior for LoginComponent unchanged. This fix retroactively matters for every future screen that needs "my own user id" (My Schedule, My Appointments for Patient, etc.) — good thing it surfaced now rather than being debugged blind later.
  - Model + ScheduleService created (doctor-schedule.model.ts, schedule.service.ts) — agent-generated, no issues found
  - [x] /dashboard/schedule route wired (agent-generated, no issues found)
  - [x] My Schedule verified end-to-end in browser as Doctor: real booked appointments + available slots render correctly (direct proof the AuthService.id fix works), date picker correctly reloads schedule for a different date — MY SCHEDULE SCREEN FULLY COMPLETE (read-only view; Confirm/Cancel/Complete actions intentionally live on the not-yet-built Receptionist Appointments screen instead, per PDR)
- [ ] Implement All Appointments screen (Receptionist)
  - **[GAP FLAGGED → FIXED]**: no way for Receptionist to get a doctor list for booking (GetStaffList was Admin-only). Built dedicated GetDoctorsList (GET /doctors, Admin+Receptionist), returns only active doctors' id/fullName — deliberately minimal response (no email/status needed for a picker), deliberately excludes deactivated doctors from being bookable. Verified: Receptionist gets the list, Doctor correctly gets 403.
  - [x] doctor.model.ts + ScheduleService extended with getDoctors, bookAppointment, confirmAppointment, cancelAppointment, completeAppointment (agent-generated, no issues found)
  - [x] AllAppointmentsComponent created (agent-generated, 1 review round): doctor picker + date picker driving schedule reload, booking form, per-status action buttons (Requested→Confirm+Cancel, Confirmed→Complete+NoShow+Cancel, terminal→none, matching the state machine exactly), per-row actionInProgress disabling (not whole-page). State-machine button logic checked carefully and is correct. Bug caught: appointment times rendered as raw unformatted ISO strings instead of using the date pipe like every other screen — DatePipe wasn't even imported. Fixed.
  - [x] /dashboard/appointments route wired (agent-generated, no issues found)
  - [x] All Appointments verified end-to-end in browser: doctor/date pickers load real schedules, new booking correctly shows Confirmed status (bookedByStaff:true), Confirm/Complete/NoShow/Cancel buttons all work and transition status correctly, conflict booking (409) fails cleanly with visible error instead of crashing — ALL APPOINTMENTS SCREEN FULLY COMPLETE, the biggest and most action-heavy screen in the build
  - **[SIGNIFICANT BACKEND BUG FOUND WHILE BUILDING BILLING, FIXED]**: GetDoctorSchedule's query only ever returned Requested/Confirmed appointments — Completed appointments were silently excluded from the ENTIRE bookedSlots list, not just from conflict-checking. This conflated two genuinely different concerns (what to display vs. what blocks new bookings) into one filtered query. Impact: Completed appointments never appeared in My Schedule or All Appointments displays at all, and Billing's entire design (show payment button on Completed rows) would have been structurally broken since Completed rows were never in the list to check. Also means the earlier "Complete button test passed" in All Appointments may have only shown buttons correctly disappearing, not distinguished from the row vanishing entirely — worth re-verifying. Fixed by splitting into allAppointmentsForDay (all non-Cancelled, for display, now includes Completed) and activeAppointments (Requested/Confirmed only, for the conflict-check math) as two separate lists with distinct purposes. Also added hasPayment field (via a Payments lookup) to BookedSlot so Billing/frontend can know which Completed appointments are already paid. Verified via direct API call: both Completed appointments now appear correctly, hasPayment correctly differentiates paid vs unpaid, availableSlots correctly still treats completed slots as free for new bookings.
  - [x] Re-verified All Appointments' Complete button in browser after the fix: row correctly stays visible and transitions status, doesn't vanish — confirms the earlier test wasn't masking the display bug
  - [x] BillingComponent created (agent-generated, 1 review round — initial version missed the "already paid" UI state entirely, root-caused to the missing hasPayment field, fixed together with the backend bug above): doctor/date pickers, Record Payment button only on unpaid Completed appointments, "Paid" label on paid ones, inline payment form, duplicate/wrong-status payment attempts surface the backend's real 409 message
  - [x] /dashboard/billing route wired (agent-generated, 1 correction round: first attempt duplicated the 'appointments' route entry, harmless since Angular uses the first match, but dead/confusing code — caught and removed)
  - [x] Billing verified end-to-end in browser: paid appointment shows "Paid" label with no button, unpaid Completed appointment shows Record Payment button, submitting flips it to "Paid" correctly — BILLING SCREEN FULLY COMPLETE
- [ ] Implement Register Patient screen (Receptionist)
  - **[GAP FLAGGED → FIXED]**: no walk-in patient creation endpoint existed — only self-registration (creates User+Patient pair) was ever built. Added RegisterWalkInPatient (POST /patients, Receptionist-only): creates Patient row only, UserId=null, finally using the nullable UserId design decision from Phase 3 for its actual intended walk-in use case. Verified: creates successfully, shows up in GET /patients list with no login capability.
  - [x] PatientService extended with registerWalkInPatient, RegisterPatientComponent created (agent-generated, no issues found)
  - [x] /dashboard/register-patient route wired (agent-generated, no issues found, no duplicate this time)
  - [x] Register Patient verified end-to-end in browser: form submits successfully, success message shows, new walk-in patient correctly appears in Patients list — REGISTER PATIENT SCREEN FULLY COMPLETE
  - **[CONFIRMED EXPECTED BEHAVIOR, not a bug]**: full page refresh logs the user out and redirects to /login. This is the direct, correct consequence of the in-memory-only token storage decision from the PDR's security notes (no localStorage/sessionStorage, to reduce XSS token-theft risk) — refresh wipes all JS memory including the AuthService signal. User confirmed: leave as-is for now, revisit alongside the still-deferred refresh token flow later rather than compromise with sessionStorage or build the full HttpOnly-cookie refresh flow right now.
  - **[MINOR, TO FIX]**: staff panel rows render with no spacing between name/role/status ("Asha AdminAdminActive" runs together) — CSS gap issue in .staff-row, cosmetic only, low priority
- [x] Implement auth flow (login, token storage, refresh-on-401, logout) — NOTE: refresh-on-401 deferred along with backend refresh tokens; current behavior on token expiry is clean logout + redirect to login, not silent refresh
- [x] Implement role-based navigation and conditional rendering per role
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

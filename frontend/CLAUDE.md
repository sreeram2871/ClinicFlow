# ClinicFlow Frontend — CLAUDE.md

Conventions and patterns for this Angular project, established through the
actual build. Read this before generating any new code for this repo.

## Stack
- Angular 22, standalone components only (no NgModules)
- Signals for reactive state (`signal`, `computed`, `input()`) — not
  `BehaviorSubject`/RxJS state management. RxJS is still used for HTTP calls
  (`Observable`, `.pipe()`, `catchError`, `switchMap`) since `HttpClient`
  returns Observables natively.
- `inject()` function for dependency injection — not constructor injection
- SCSS, one file per component
- `ng2-charts` (Chart.js v4 wrapper) for all charts

## Folder structure
```
src/app/
├── core/           # app-wide singletons
│   ├── services/   # AuthService, and one service per backend feature area
│   ├── interceptors/
│   └── guards/
├── shared/
│   └── components/ # reusable components (StatCard, ChartCard, ToastContainer, AppShell)
├── features/       # one folder per role area: auth, admin, doctor, receptionist, patient
└── models/         # TypeScript interfaces matching backend response shapes
```

## Critical patterns — do not deviate without a good reason

### Signals for component state, always
Use `signal<T>()` for any component-local state that drives the template.
For anything shared across components (like `AuthService.currentUser`), keep
the writable signal **private**, expose only `.asReadonly()`. A public
writable signal lets any component silently corrupt shared state — this
caused a real bug early in the build (any component could fake being logged
in as Admin by calling `.set()` directly).

### `input()` signals, not `@Input()` decorators, if the input feeds a `computed()`
`computed()` only tracks signal dependencies. A plain `@Input()` decorator
property is not a signal — reading it inside `computed()` registers no
dependency, so the computed value gets stuck on whatever the input was the
first time it was read and never updates again. This caused a real bug
(`ChartCardComponent` charts staying permanently blank). Use
`readonly foo = input<T>(default)` and read it as `this.foo()`.

### Error handling — the established three-part pattern
Every `.subscribe()` that can fail uses this exact shape:
```typescript
.subscribe({
  next: (result) => { /* ... */ },
  error: (error: HttpErrorResponse) => {
    console.error('Some descriptive label', error);
    const detail = error.error?.detail;
    this.errorMessage.set(
      typeof detail === 'string' && detail.trim().length > 0
        ? detail
        : 'Some generic fallback message.',
    );
  },
});
```
- Always type the error parameter as `HttpErrorResponse`
- Always extract from `error.error?.detail` — the backend's actual response
  body is nested inside `.error`, not directly on the top-level error object
  (a common gotcha, caused a real bug where every failure showed a generic
  message instead of the backend's real one)
- Never use `??` for the fallback — an empty string `""` passes `??` but
  isn't useful; always check `typeof === 'string' && .trim().length > 0`
- A global `errorToastInterceptor` also shows a toast for every failed
  request automatically — this **supplements** per-component error handling,
  it doesn't replace it. Don't remove component-level error messages just
  because the toast also fires.

### Per-tab / per-section state must be independent
If a component has multiple independent sections (like tabs), each needs
its OWN error/loading signal, not one shared signal. A single shared
`errorMessage` across tabs caused a real bug: a failure in one tab
incorrectly hid already-successfully-loaded content in a different tab
when switching back.

### Lists vs. individual lookups need separate access-control queries
When adding a "list all X" endpoint, don't assume an existing
single-item access guard (like `PatientAccessGuard`) applies — a guard that
checks "can this user see THIS ONE already-known item" is a different
question from "which items should even appear in this list." Build the
list-filtering logic explicitly (see `GetPatientsList`/`GetDoctorsList` on
the backend for the pattern: role-based `.Where()` filtering baked into the
query itself).

### Route wiring checklist, every time
1. `loadComponent` lazy import in `app.routes.ts`
2. `canActivate: [roleGuard]` + `data: { roles: [...] }` if the screen is
   role-restricted (check `AppShellComponent`'s `navItems` array for which
   roles should see it)
3. Add a sidebar nav item in `AppShellComponent.navItems` if the screen
   should be reachable from the UI at all

### Never assume a backend endpoint exists — verify against the actual
codebase and Phase 3 test history. Several real gaps were found mid-build
this way (no patient-list endpoint, no doctor-list endpoint, no walk-in
patient creation endpoint, no way for a Patient to discover their own
PatientId). If a needed endpoint doesn't exist, flag it and design a
proper new one — don't route around the gap with a workaround.

### Security note: don't assume the frontend hiding something is enforcement
The UI only showing a user their own data is not the same as the backend
enforcing that they can't reach someone else's. `CancelAppointmentHandler`
originally had no ownership check at all — any authenticated user could
cancel any appointment ID in the tenant via a direct API call, even though
the UI only ever showed users their own appointments. Always check the
backend handler itself, not just what the UI displays.

## Styling conventions
- Brand color: `#0F6E56` (teal) — all primary action buttons
- Status colors (semantic, consistent everywhere):
  - Confirmed → blue (`#dbeafe` bg / `#1d4ed8` text)
  - Requested → amber (`#fef3c7` bg / `#b45309` text — or similar warm tone)
  - Completed → teal/green (`#ccfbf1` bg / `#0f766e` text)
  - Cancelled/NoShow → red (`#fee2e2` bg / `#b91c1c` text)
- Form inputs: `.form-input` class, `border: 1px solid #cbd5e1`,
  `border-radius: 0.4rem`, `padding: 0.5rem`–`0.7rem`
- Dates: always via Angular's `date` pipe (`'shortTime'` for times,
  `'mediumDate'` for dates, `'medium'` for full timestamps) — never render
  a raw ISO string directly in a template

## Known, deliberately accepted limitations
- **No `localStorage`/`sessionStorage` for the access token** — stored
  in-memory in `AuthService` only, to reduce XSS token-theft risk. This
  means a full page refresh logs the user out. Accepted trade-off, not a
  bug — revisit only alongside building real refresh tokens.
- **No refresh token flow yet** — access tokens expire after 15 minutes,
  user must log in again. Deferred from Phase 3, not yet built.
- **Manually-typed URLs always cause a full reload**, which triggers
  `authGuard`'s login redirect before `roleGuard` ever gets a chance to run.
  Both guards are correct; this is just a browser navigation behavior, not
  a bug in either guard.

## Testing
No frontend automated tests exist yet (as of this writing). Manual browser
verification has been the standard for every feature built so far. If
adding tests, prefer testing services (`AuthService`, `*Service` HTTP
wrappers) and guards (`authGuard`, `roleGuard`) in isolation — these carry
the most business logic and are the most straightforward to unit test
without needing full component rendering.

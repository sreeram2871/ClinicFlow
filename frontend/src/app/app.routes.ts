import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/components/app-shell/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      {
        path: '',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/admin/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'staff',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/admin/manage-staff/manage-staff.component').then((m) => m.ManageStaffComponent),
      },
      {
        path: 'patients',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Doctor', 'Receptionist'] },
        loadComponent: () =>
          import('./features/admin/patients-list/patients-list.component').then((m) => m.PatientsListComponent),
      },
      {
        path: 'patients/:id',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Doctor', 'Receptionist'] },
        loadComponent: () =>
          import('./features/admin/patient-detail/patient-detail.component').then((m) => m.PatientDetailComponent),
      },
      {
        path: 'schedule',
        canActivate: [roleGuard],
        data: { roles: ['Doctor'] },
        loadComponent: () =>
          import('./features/doctor/my-schedule/my-schedule.component').then((m) => m.MyScheduleComponent),
      },
      {
        path: 'appointments',
        canActivate: [roleGuard],
        data: { roles: ['Receptionist'] },
        loadComponent: () =>
          import('./features/receptionist/all-appointments/all-appointments.component').then((m) => m.AllAppointmentsComponent),
      },
      {
        path: 'billing',
        canActivate: [roleGuard],
        data: { roles: ['Receptionist'] },
        loadComponent: () =>
          import('./features/receptionist/billing/billing.component').then((m) => m.BillingComponent),
      },
      {
        path: 'register-patient',
        canActivate: [roleGuard],
        data: { roles: ['Receptionist'] },
        loadComponent: () =>
          import('./features/receptionist/register-patient/register-patient.component').then((m) => m.RegisterPatientComponent),
      },
      {
        path: 'my-appointments',
        canActivate: [roleGuard],
        data: { roles: ['Patient'] },
        loadComponent: () =>
          import('./features/patient/my-appointments/my-appointments.component').then((m) => m.MyAppointmentsComponent),
      },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
];

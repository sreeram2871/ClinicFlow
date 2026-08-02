import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/components/app-shell/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/admin/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'staff',
        loadComponent: () =>
          import('./features/admin/manage-staff/manage-staff.component').then((m) => m.ManageStaffComponent),
      },
      {
        path: 'patients',
        loadComponent: () =>
          import('./features/admin/patients-list/patients-list.component').then((m) => m.PatientsListComponent),
      },
      {
        path: 'patients/:id',
        loadComponent: () =>
          import('./features/admin/patient-detail/patient-detail.component').then((m) => m.PatientDetailComponent),
      },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
];

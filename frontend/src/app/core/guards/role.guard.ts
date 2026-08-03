import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowedRoles = (route.data['roles'] as string[] | undefined) ?? [];

  if (allowedRoles.length === 0) {
    return true;
  }

  const currentRole = authService.currentUser()?.role;

  if (currentRole && allowedRoles.includes(currentRole)) {
    return true;
  }

  const fallbackRoute = getHomeRouteForRole(currentRole ?? '');
  return router.createUrlTree([fallbackRoute]);
};

function getHomeRouteForRole(role: string): string {
  switch (role) {
    case 'Admin':
      return '/dashboard';
    case 'Doctor':
      return '/dashboard/schedule';
    case 'Receptionist':
      return '/dashboard/appointments';
    case 'Patient':
      return '/dashboard/my-appointments';
    default:
      return '/dashboard';
  }
}

import { NgFor } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [NgFor, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;

  readonly navItems = [
    { label: 'Dashboard', path: '/dashboard', roles: ['Admin'] },
    { label: 'Manage Staff', path: '/dashboard/staff', roles: ['Admin'] },
    { label: 'Patients', path: '/dashboard/patients', roles: ['Admin', 'Doctor', 'Receptionist'] },
    { label: 'My Schedule', path: '/dashboard/schedule', roles: ['Doctor'] },
    { label: 'Appointments', path: '/dashboard/appointments', roles: ['Receptionist'] },
    { label: 'Register Patient', path: '/dashboard/register-patient', roles: ['Receptionist'] },
    { label: 'Billing', path: '/dashboard/billing', roles: ['Receptionist'] },
    { label: 'My Appointments', path: '/dashboard/my-appointments', roles: ['Patient'] },
  ];

  readonly visibleNavItems = computed(() => {
    const role = this.currentUser()?.role;

    return this.navItems.filter((item) => role !== undefined && item.roles.includes(role));
  });

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigateByUrl('/login'));
  }
}

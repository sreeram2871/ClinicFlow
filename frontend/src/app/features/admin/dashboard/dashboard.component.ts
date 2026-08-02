import { Component, computed, inject } from '@angular/core';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent {
  private readonly authService = inject(AuthService);

  readonly fullName = computed(() => this.authService.currentUser()?.fullName ?? '');
}

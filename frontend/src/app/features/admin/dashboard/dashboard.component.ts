import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { AuthService } from '../../../core/services/auth.service';
import { DashboardService } from '../../../core/services/dashboard.service';
import { StaffService } from '../../../core/services/staff.service';
import { DashboardSummary } from '../../../models/dashboard.model';
import { StaffMember } from '../../../models/staff.model';
import { ChartCardComponent } from '../../../shared/components/chart-card/chart-card.component';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [ChartCardComponent, StatCardComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);
  private readonly staffService = inject(StaffService);

  readonly fullName = computed(() => this.authService.currentUser()?.fullName ?? '');
  readonly summary = signal<DashboardSummary | null>(null);
  readonly staffList = signal<StaffMember[]>([]);

  ngOnInit(): void {
    this.dashboardService.getDashboardSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
      },
      error: (err) => {
        console.error('Dashboard summary load failed', err);
      },
    });

    this.staffService.getStaffList().subscribe({
      next: (staff) => {
        this.staffList.set(staff);
      },
      error: (err) => {
        console.error('Staff list load failed', err);
      },
    });
  }
}

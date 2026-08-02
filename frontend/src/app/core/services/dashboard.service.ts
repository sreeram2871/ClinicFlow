import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DashboardSummary } from '../../models/dashboard.model';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private readonly http = inject(HttpClient);

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>('https://localhost:7008/api/v1/reports/dashboard');
  }
}

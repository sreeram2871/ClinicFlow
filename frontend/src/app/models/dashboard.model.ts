export interface WeeklyDataPoint {
  weekLabel: string;
  value: number;
}

export interface DashboardSummary {
  appointmentsToday: number;
  revenueThisMonth: number;
  totalPatients: number;
  revenueByWeek: WeeklyDataPoint[];
  newPatientsByWeek: WeeklyDataPoint[];
}

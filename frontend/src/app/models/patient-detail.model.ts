export interface RecentAppointment {
  id: string;
  scheduledStart: string;
  status: string;
}

export interface PatientDetail {
  id: string;
  fullName: string;
  dateOfBirth: string;
  phone: string;
  email: string;
  recentAppointments: RecentAppointment[];
}

export interface MyAppointment {
  appointmentId: string;
  start: string;
  end: string;
  status: string;
  tokenNumber: number | null;
  doctorName: string;
  appointmentDate: string;
}

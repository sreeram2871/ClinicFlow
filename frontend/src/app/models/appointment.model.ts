export type AppointmentStatus = 'Requested' | 'Confirmed' | 'Completed' | 'Cancelled' | 'NoShow';

export interface Appointment {
  id: string;
  patientId: string;
  doctorId: string;
  scheduledStart: string;
  scheduledEnd: string;
  status: AppointmentStatus;
}
export interface MedicalRecordEntry {
  id: string;
  notes: string;
  doctorId: string;
  appointmentId: string | null;
  createdAt: string;
}

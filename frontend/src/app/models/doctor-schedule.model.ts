export interface BookedSlot {
  appointmentId: string;
  patientId: string;
  patientName: string;
  start: string;
  end: string;
  status: string;
  hasPayment: boolean;
  paidAmount?: number | null;
  paymentMethod?: string | null;
}

export interface AvailableSlot {
  start: string;
  end: string;
}

export interface DoctorSchedule {
  bookedSlots: BookedSlot[];
  availableSlots: AvailableSlot[];
}

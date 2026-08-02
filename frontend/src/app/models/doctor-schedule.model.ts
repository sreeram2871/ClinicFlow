export interface BookedSlot {
  appointmentId: string;
  start: string;
  end: string;
  status: string;
}

export interface AvailableSlot {
  start: string;
  end: string;
}

export interface DoctorSchedule {
  bookedSlots: BookedSlot[];
  availableSlots: AvailableSlot[];
}

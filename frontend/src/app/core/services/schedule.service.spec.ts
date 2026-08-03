import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { ScheduleService } from './schedule.service';
import { DoctorSchedule } from '../../models/doctor-schedule.model';

describe('ScheduleService', () => {
  let service: ScheduleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    service = TestBed.inject(ScheduleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch a doctor\'s schedule', () => {
    const doctorId = 'doctor-1';
    const date = '2026-08-10';

    const expectedSchedule: DoctorSchedule = {
      bookedSlots: [
        {
          appointmentId: 'appt-1',
          start: '2026-08-10T09:00:00',
          end: '2026-08-10T09:30:00',
          status: 'Booked',
          hasPayment: false,
        },
      ],
      availableSlots: [
        {
          start: '2026-08-10T10:00:00',
          end: '2026-08-10T10:30:00',
        },
      ],
    };

    service.getSchedule(doctorId, date).subscribe((schedule) => {
      expect(schedule).toEqual(expectedSchedule);
    });

    const req = httpMock.expectOne(`https://localhost:7008/api/v1/doctors/${doctorId}/schedule?date=${date}`);
    expect(req.request.method).toBe('GET');
    req.flush(expectedSchedule);
  });

  it('should book an appointment', () => {
    const payload = {
      patientId: 'patient-1',
      doctorId: 'doctor-1',
      start: '2026-08-10T10:00:00',
      end: '2026-08-10T10:30:00',
      bookedByStaff: true,
    };

    const expectedResponse = { appointmentId: 'appt-2', status: 'Booked' };

    service.bookAppointment(payload.patientId, payload.doctorId, payload.start, payload.end, payload.bookedByStaff).subscribe((response) => {
      expect(response).toEqual(expectedResponse);
    });

    const req = httpMock.expectOne('https://localhost:7008/api/v1/appointments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(expectedResponse);
  });

  it('should confirm an appointment', () => {
    const appointmentId = 'appt-3';

    service.confirmAppointment(appointmentId).subscribe();

    const req = httpMock.expectOne(`https://localhost:7008/api/v1/appointments/${appointmentId}/confirm`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush(null);
  });
});

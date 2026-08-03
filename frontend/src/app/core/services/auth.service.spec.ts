import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import { LoginRequest, LoginResponse } from '../../models/auth.model';
import { CurrentUser } from '../../models/user.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should start with no logged in user', () => {
    expect(service.currentUser()).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
  });

  it('should set currentUser after successful login', () => {
    const loginRequest: LoginRequest = {
      email: 'patient@example.com',
      password: 'secret123',
    };

    const loginResponse: LoginResponse = {
      accessToken: 'test-token',
      fullName: 'Test Patient',
      role: 'Patient',
      tenantId: 'tenant-1',
    };

    const currentUser: CurrentUser = {
      id: 'user-123',
      fullName: 'Test Patient',
      email: 'patient@example.com',
      role: 'Patient',
      tenantId: 'tenant-1',
    };

    service.login(loginRequest).subscribe((response) => {
      expect(response).toEqual(loginResponse);
    });

    const loginReq = httpMock.expectOne('https://localhost:7008/api/v1/auth/login');
    expect(loginReq.request.method).toBe('POST');
    loginReq.flush(loginResponse);

    const meReq = httpMock.expectOne('https://localhost:7008/api/v1/auth/me');
    expect(meReq.request.method).toBe('GET');
    meReq.flush(currentUser);

    expect(service.currentUser()).toEqual(currentUser);
    expect(service.isLoggedIn()).toBe(true);
    expect(service.getToken()).toBe(loginResponse.accessToken);
  });

  it('should clear currentUser on logout', () => {
    const loginRequest: LoginRequest = {
      email: 'receptionist@example.com',
      password: 'secret123',
    };

    const loginResponse: LoginResponse = {
      accessToken: 'logout-token',
      fullName: 'Receptionist User',
      role: 'Receptionist',
      tenantId: 'tenant-2',
    };

    const currentUser: CurrentUser = {
      id: 'user-456',
      fullName: 'Receptionist User',
      email: 'receptionist@example.com',
      role: 'Receptionist',
      tenantId: 'tenant-2',
    };

    service.login(loginRequest).subscribe();

    const loginReq = httpMock.expectOne('https://localhost:7008/api/v1/auth/login');
    loginReq.flush(loginResponse);

    const meReq = httpMock.expectOne('https://localhost:7008/api/v1/auth/me');
    meReq.flush(currentUser);

    expect(service.currentUser()).toEqual(currentUser);
    expect(service.isLoggedIn()).toBe(true);

    service.logout();

    expect(service.currentUser()).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
    expect(service.getToken()).toBeNull();
  });
});

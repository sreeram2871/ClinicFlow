export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  fullName: string;
  role: 'Admin' | 'Doctor' | 'Receptionist' | 'Patient';
  tenantId: string;
}
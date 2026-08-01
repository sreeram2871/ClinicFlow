export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'Doctor' | 'Receptionist' | 'Patient';
  tenantId: string;
}
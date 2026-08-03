import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';

import { roleGuard } from './role.guard';
import { AuthService } from '../services/auth.service';

describe('roleGuard', () => {
  let authService: { currentUser: () => { role: string } | null };
  let router: { createUrlTree: jest.Mock };

  beforeEach(() => {
    authService = {
      currentUser: jest.fn(() => ({
        role: 'Admin',
        id: '1',
        fullName: 'Admin User',
        email: 'admin@example.com',
        tenantId: 'tenant-1',
      })),
    };

    router = {
      createUrlTree: jest.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('should allow navigation when role matches', () => {
    const route = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;
    const state = {} as never;

    TestBed.runInInjectionContext(() => {
      const result = roleGuard(route, state);
      expect(result).toBe(true);
    });
  });

  it('should redirect to the correct home route when role does not match', () => {
    authService.currentUser = jest.fn(() => ({
      role: 'Doctor',
      id: '2',
      fullName: 'Doctor User',
      email: 'doctor@example.com',
      tenantId: 'tenant-1',
    }));

    const fakeTree = { root: [] } as unknown as UrlTree;
    router.createUrlTree.mockReturnValue(fakeTree);

    const route = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;
    const state = {} as never;

    TestBed.runInInjectionContext(() => {
      const result = roleGuard(route, state);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/dashboard/schedule']);
      expect(result).toBe(fakeTree);
    });
  });

  it('should allow navigation when no roles are restricted', () => {
    const route = { data: {} } as unknown as ActivatedRouteSnapshot;
    const state = {} as never;

    TestBed.runInInjectionContext(() => {
      const result = roleGuard(route, state);
      expect(result).toBe(true);
    });
  });
});

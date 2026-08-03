import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authService: { isLoggedIn: () => boolean };
  let router: { createUrlTree: jest.Mock };

  beforeEach(() => {
    authService = {
      isLoggedIn: jest.fn(() => true),
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

  it('should allow navigation when logged in', () => {
    TestBed.runInInjectionContext(() => {
      const result = authGuard({} as never, {} as never);
      expect(result).toBe(true);
    });
  });

  it('should redirect to login when not logged in', () => {
    authService.isLoggedIn = jest.fn(() => false);
    const urlTree = { root: [] };
    router.createUrlTree.mockReturnValue(urlTree);

    TestBed.runInInjectionContext(() => {
      const result = authGuard({} as never, {} as never);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
      expect(result).toBe(urlTree);
    });
  });
});

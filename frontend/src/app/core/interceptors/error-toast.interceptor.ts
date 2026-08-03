import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorToastInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        const detail = error.error?.detail;
        const message = typeof detail === 'string' && detail.trim().length > 0
          ? detail
          : 'Something went wrong.';
        toastService.show(message, 'error');
      }

      return throwError(() => error);
    })
  );
};

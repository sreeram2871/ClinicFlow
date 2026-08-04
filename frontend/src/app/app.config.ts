import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { firstValueFrom } from 'rxjs';

import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorToastInterceptor } from './core/interceptors/error-toast.interceptor';
import { routes } from './app.routes';
import { AuthService } from './core/services/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, errorToastInterceptor])),
    provideCharts(withDefaultRegisterables()),
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      return firstValueFrom(authService.restoreSession());
    }),
  ],
};

import { ApplicationConfig, ErrorHandler, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';
import { GlobalErrorHandler } from './services/global-error-handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    // HttpClient with auth (attaches JWT) + error (toasts + redirects) interceptors.
    // Order matters: auth runs first, error wraps the response pipeline.
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    // Catches uncaught client-side runtime errors and shows a friendly toast.
    { provide: ErrorHandler, useClass: GlobalErrorHandler }
  ]
};


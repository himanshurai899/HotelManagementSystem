import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { NotificationService } from '../services/notification.service';
import { AuthService } from '../services/auth.service';

/**
 * Functional HTTP error interceptor.
 *
 * - Maps every HttpErrorResponse to a friendly user-facing toast via NotificationService.
 * - Prefers ProblemDetails `detail` from the API when present.
 * - Handles 401 (logout + redirect), 403 (redirect to /unauthorized), 404, 409,
 *   network failures (status 0), and 500.
 * - Exempts /account/login and /account/register from the 401-logout-redirect so
 *   the auth screens can show "Invalid credentials" inline without appearing to log out.
 * - Always re-throws the original error so subscribers can still react.
 *
 * Registered via provideHttpClient(withInterceptors([..., errorInterceptor])).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAuthEndpoint =
    req.url.includes('/account/login') || req.url.includes('/account/register');

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const serverDetail =
        (error.error && typeof error.error === 'object' && (error.error as { detail?: string }).detail) || '';

      let message: string;
      switch (error.status) {
        case 0:
          message = 'Cannot reach the server. Please check your network connection.';
          break;
        case 400:
          message = serverDetail || 'The request was invalid. Please review your input.';
          break;
        case 401:
          message = serverDetail || 'Your session has expired. Please log in again.';
          if (!isAuthEndpoint) {
            auth.logout();
            router.navigate(['/login']);
          }
          break;
        case 403:
          message = serverDetail || 'You do not have permission to perform this action.';
          if (!isAuthEndpoint) {
            router.navigate(['/unauthorized']);
          }
          break;
        case 404:
          message = serverDetail || 'The requested resource could not be found.';
          break;
        case 409:
          message = serverDetail || 'This action conflicts with the current state.';
          break;
        case 500:
        default:
          message = 'An unexpected error occurred. Please try again later.';
          break;
      }

      notify.error(message);
      return throwError(() => error);
    })
  );
};

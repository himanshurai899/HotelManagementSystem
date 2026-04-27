import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Functional HTTP interceptor.
 * Attaches `Authorization: Bearer <token>` to every outgoing request
 * when a valid JWT is present in localStorage.
 * Registered via provideHttpClient(withInterceptors([authInterceptor])) in app.config.ts.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();
  if (token) {
    return next(
      req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    );
  }
  return next(req);
};

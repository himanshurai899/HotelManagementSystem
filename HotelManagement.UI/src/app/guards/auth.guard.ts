import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Functional route guard — protects routes that require authentication.
 * Redirects unauthenticated users to /login.
 *
 * @example { path: 'dashboard', component: ..., canActivate: [authGuard] }
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isLoggedIn()) return true;
  return inject(Router).createUrlTree(['/login']);
};

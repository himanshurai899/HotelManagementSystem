import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Functional route guard — enforces role-based access control.
 * Reads `data.roles` from the route definition; if the authenticated user
 * does not have at least one of those roles they are redirected to /unauthorized.
 *
 * Always combine with authGuard so unauthenticated users are sent to /login first.
 *
 * @example
 * {
 *   path: 'admin',
 *   component: AdminDashboardComponent,
 *   canActivate: [authGuard, roleGuard],
 *   data: { roles: ['Administrator'] }
 * }
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const allowedRoles: string[] = route.data['roles'] ?? [];

  // No roles restriction on this route → allow through
  if (allowedRoles.length === 0) return true;

  if (auth.hasAnyRole(allowedRoles)) return true;

  return inject(Router).createUrlTree(['/unauthorized']);
};

import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Functional HTTP interceptor.
 * 1. Attaches `Authorization: Bearer <token>` when a JWT is present.
 * 2. Attaches `X-Tenant-Id: <id>` when a SuperAdmin has selected an active
 *    tenant via AuthService.setSelectedTenant(). The server's
 *    TenantResolverMiddleware reads this header and scopes all queries to
 *    that tenant (header takes precedence over the JWT claim).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();
  const selectedTenantId = auth.selectedTenantId();

  const headers: Record<string, string> = {};
  if (token)            headers['Authorization']  = `Bearer ${token}`;
  if (selectedTenantId) headers['X-Tenant-Id']    = String(selectedTenantId);

  if (Object.keys(headers).length > 0) {
    return next(req.clone({ setHeaders: headers }));
  }
  return next(req);
};

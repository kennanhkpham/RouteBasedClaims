import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { environment } from '../environments/environment';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const claimsUrl = `${environment.apiUrl}/claims`;
  const adminAccountUrl = `${environment.apiUrl}/auth/admin/accounts`;
  if (!request.url.startsWith(`${environment.apiUrl}/`)) {
    return next(request);
  }

  const requiresAdmin =
    (request.url === claimsUrl && request.method === 'GET') ||
    request.url.startsWith(`${claimsUrl}/pending`) ||
    (request.url === adminAccountUrl && request.method === 'POST') ||
    ['PUT', 'PATCH', 'DELETE'].includes(request.method);
  const token = inject(AuthService).getTokenForRequest(requiresAdmin);
  if (!token) {
    return next(request);
  }

  return next(request.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  }));
};

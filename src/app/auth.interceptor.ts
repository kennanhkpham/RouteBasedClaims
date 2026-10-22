import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).getToken();
  if (!token || !request.url.startsWith('https://localhost:7197/api/claims')) {
    return next(request);
  }

  return next(request.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  }));
};

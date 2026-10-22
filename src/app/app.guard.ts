import { inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAdmin()) {
    return true;
  }

  return router.createUrlTree(['/admin-login'], {
    queryParams: { returnUrl: state.url }
  });
};

export const adminReviewExitGuard: CanDeactivateFn<unknown> = () => {
  inject(AuthService).logout();
  return true;
};
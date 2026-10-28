import { Routes } from '@angular/router';
import {
  adminGuard,
  adminLoginGuard,
  authenticatedGuard,
  signedOutGuard
} from './app.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'intake',
    canActivate: [authenticatedGuard],
    loadComponent: () =>
      import('./features/claim-intake/claim-intake')
        .then(m => m.ClaimIntakeComponent)
  },
  {
    path: 'status',
    canActivate: [authenticatedGuard],
    loadComponent: () =>
      import('./features/claim-status/claim-status')
        .then(m => m.ClaimStatusComponent)
  },
  {
    path: 'login',
    canActivate: [signedOutGuard],
    loadComponent: () =>
      import('./features/admin-login/admin-login')
        .then(m => m.AdminLoginComponent)
  },
  {
    path: 'admin-login',
    canActivate: [adminLoginGuard],
    loadComponent: () =>
      import('./features/admin-review-login/admin-review-login')
        .then(m => m.AdminReviewLoginComponent)
  },
  {
    path: 'review',
    loadComponent: () =>
      import('./features/claim-review/claim-review')
        .then(m => m.ClaimReviewerComponent),
    canActivate: [adminGuard],
  },
  {
    path: 'admin/accounts',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin-accounts/admin-accounts')
        .then(m => m.AdminAccountsComponent)
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
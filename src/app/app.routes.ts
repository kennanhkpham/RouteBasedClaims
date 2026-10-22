import { Routes } from '@angular/router';
import { adminGuard, adminReviewExitGuard } from './app.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'intake',
    pathMatch: 'full'
  },
  {
    path: 'intake',
    loadComponent: () =>
      import('./features/claim-intake/claim-intake')
        .then(m => m.ClaimIntakeComponent)
  },
  {
    path: 'status',
    loadComponent: () =>
      import('./features/claim-status/claim-status')
        .then(m => m.ClaimStatusComponent)
  },
  {
    path: 'admin-login',
    loadComponent: () =>
      import('./features/admin-login/admin-login')
        .then(m => m.AdminLoginComponent)
  },
  {
    path: 'review',
    loadComponent: () =>
      import('./features/claim-review/claim-review')
        .then(m => m.ClaimReviewerComponent),
    canActivate: [adminGuard],
    canDeactivate: [adminReviewExitGuard]
  },
  {
    path: '**',
    redirectTo: 'intake'
  }
];
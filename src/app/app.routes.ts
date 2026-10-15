import { Routes } from '@angular/router';

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
    path: 'review',
    loadComponent: () =>
      import('./features/claim-review/claim-review')
        .then(m => m.ClaimReviewerComponent)
  },
  {
    path: '**',
    redirectTo: 'intake'
  }
];
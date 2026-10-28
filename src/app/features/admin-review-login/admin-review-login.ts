import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../auth.service';

@Component({
  selector: 'app-admin-review-login',
  imports: [FormsModule],
  templateUrl: './admin-review-login.html',
  styleUrl: './admin-review-login.css',
})
export class AdminReviewLoginComponent {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly username = signal('');
  readonly password = signal('');
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');

  signIn(): void {
    if (this.isLoading()) {
      return;
    }

    this.errorMessage.set('');
    this.isLoading.set(true);

    this.authService.loginAdmin(this.username(), this.password()).subscribe({
      next: () => {
        this.isLoading.set(false);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        void this.router.navigateByUrl(returnUrl === '/review' ? returnUrl : '/review');
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.getErrorMessage(error));
      },
    });
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof Error && error.message.includes('does not have administrator access')) {
      return error.message;
    }

    if (!(error instanceof HttpErrorResponse)) {
      return 'Administrator sign in failed. Please try again shortly.';
    }

    if (error.status === 0) {
      return 'Cannot reach the API. Start the API and trust its HTTPS development certificate.';
    }

    if (error.status === 401) {
      return 'The administrator username or password is incorrect.';
    }

    if (error.status >= 500) {
      return 'The API could not complete the request. Check the API terminal for a database or server error.';
    }

    return `Administrator sign in failed (HTTP ${error.status}).`;
  }
}

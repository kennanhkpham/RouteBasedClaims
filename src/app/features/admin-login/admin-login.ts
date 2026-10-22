import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../auth.service';

@Component({
  selector: 'app-admin-login',
  imports: [FormsModule],
  templateUrl: './admin-login.html',
  styleUrl: './admin-login.css',
})
export class AdminLoginComponent {
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

    this.authService.login(this.username(), this.password()).subscribe({
      next: () => {
        this.isLoading.set(false);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        void this.router.navigateByUrl(returnUrl === '/review' ? returnUrl : '/review');
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 401
            ? 'The username or password is incorrect.'
            : 'Sign in failed. Please try again shortly.',
        );
      },
    });
  }
}

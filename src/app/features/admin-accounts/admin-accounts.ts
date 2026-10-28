import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../auth.service';

@Component({
  selector: 'app-admin-accounts',
  imports: [FormsModule],
  templateUrl: './admin-accounts.html',
  styleUrl: './admin-accounts.css',
})
export class AdminAccountsComponent {
  private readonly authService = inject(AuthService);

  readonly username = signal('');
  readonly password = signal('');
  readonly confirmPassword = signal('');
  readonly isAdmin = signal(false);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  createAccount(): void {
    if (this.isLoading()) {
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');
    if (this.password() !== this.confirmPassword()) {
      this.errorMessage.set('The passwords do not match.');
      return;
    }

    this.isLoading.set(true);
    this.authService.createAccount(this.username(), this.password(), this.isAdmin()).subscribe({
      next: (account) => {
        this.isLoading.set(false);
        this.successMessage.set(
          `${account.isAdmin ? 'Administrator' : 'Claimant'} account "${account.username}" was created.`,
        );
        this.username.set('');
        this.password.set('');
        this.confirmPassword.set('');
        this.isAdmin.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.getErrorMessage(error));
      },
    });
  }

  private getErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Account creation failed. Please try again shortly.';
    }

    if (error.status === 0) {
      return 'Cannot reach the API. Check that the API is running.';
    }

    if (error.status === 400) {
      return 'Enter a valid username and a password of at least 12 characters.';
    }

    if (error.status === 401 || error.status === 403) {
      return 'Your administrator session is no longer authorized. Sign in to Claim Review again.';
    }

    if (error.status === 409) {
      return 'An account with that username already exists.';
    }

    if (error.status >= 500) {
      return 'The API could not create the account. Check the API logs for details.';
    }

    return `Account creation failed (HTTP ${error.status}).`;
  }
}

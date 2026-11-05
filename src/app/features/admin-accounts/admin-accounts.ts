import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';

@Component({
  selector: 'app-admin-accounts',
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-accounts.html',
  styleUrl: './admin-accounts.css',
})
export class AdminAccountsComponent {
  private readonly authService = inject(AuthService);

  readonly username = signal('');
  readonly password = signal('');
  readonly confirmPassword = signal('');
  readonly department = signal('');
  readonly isPasswordVisible = signal(false);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  createAccount(): void {
    if (this.isLoading()) {
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');
    if (!/^[A-Za-z0-9._-]{3,50}$/.test(this.username().trim())) {
      this.errorMessage.set('Choose a username with 3–50 letters, numbers, dots, underscores, or hyphens.');
      return;
    }

    if (this.password().length < 12 || this.password().length > 128) {
      this.errorMessage.set('Choose a password between 12 and 128 characters.');
      return;
    }

    if (this.password() !== this.confirmPassword()) {
      this.errorMessage.set('The passwords do not match.');
      return;
    }

    const department = this.department().trim();
    if (!department || department.length > 100) {
      this.errorMessage.set('Enter the department where this administrator works (100 characters or fewer).');
      return;
    }

    this.isLoading.set(true);
    this.authService.createAdminAccount(
      this.username().trim(),
      this.password(),
      department,
    ).subscribe({
      next: (account) => {
        this.isLoading.set(false);
        this.successMessage.set(`Administrator account "${account.username}" was created.`);
        this.username.set('');
        this.password.set('');
        this.confirmPassword.set('');
        this.department.set('');
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.getErrorMessage(error));
      },
    });
  }

  togglePasswordVisibility(): void {
    this.isPasswordVisible.update((visible) => !visible);
  }

  private getErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Administrator account creation failed. Please try again shortly.';
    }

    if (error.status === 0) {
      return 'Cannot reach AuthService. Please try again shortly.';
    }

    if (error.status === 400) {
      return 'Enter valid account details and a department of no more than 100 characters.';
    }

    if (error.status === 401) {
      return 'Your administrator session has expired. Sign in to Claim Review again.';
    }

    if (error.status === 403) {
      return 'Only an administrator can create administrator accounts.';
    }

    if (error.status === 409) {
      return 'An account with that username already exists.';
    }

    if (error.status >= 500) {
      return 'AuthService could not create the account. Please try again shortly.';
    }

    return `Administrator account creation failed (HTTP ${error.status}).`;
  }
}

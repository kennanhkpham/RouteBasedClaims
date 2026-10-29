import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, PLATFORM_ID, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../auth.service';

const REMEMBERED_USERNAME_KEY = 'claimstream-remembered-username';

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
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  readonly errorMessage = signal('');
  private readonly rememberedUsername = this.readRememberedUsername();

  readonly username = signal(this.rememberedUsername ?? '');
  readonly password = signal('');
  readonly confirmPassword = signal('');
  readonly isPasswordVisible = signal(false);
  readonly rememberUsername = signal(this.rememberedUsername !== null);
  readonly isRegistering = signal(false);
  readonly isLoading = signal(false);

  submit(): void {
    if (this.isLoading()) {
      return;
    }

    this.errorMessage.set('');
    if (this.isRegistering() && this.password() !== this.confirmPassword()) {
      this.errorMessage.set('The passwords do not match.');
      return;
    }

    this.isLoading.set(true);

    const request = this.isRegistering()
      ? this.authService.register(this.username(), this.password())
      : this.authService.login(this.username(), this.password());

    request.subscribe({
      next: () => {
        this.persistUsernamePreference();
        this.isLoading.set(false);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const destination = this.authService.isAdmin()
          ? (returnUrl === '/review' ? returnUrl : '/review')
          : (returnUrl === '/status' ? returnUrl : '/intake');
        void this.router.navigateByUrl(destination);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.getErrorMessage(error));
      },
    });
  }

  onUsernameChange(username: string): void {
    this.username.set(username);
    if (this.rememberUsername()) {
      this.persistUsernamePreference();
    }
  }

  onRememberUsernameChange(event: Event): void {
    const target = event.target;
    if (!(target instanceof HTMLInputElement)) {
      return;
    }

    this.rememberUsername.set(target.checked);
    this.persistUsernamePreference();
  }

  togglePasswordVisibility(): void {
    this.isPasswordVisible.update((visible) => !visible);
  }

  private readRememberedUsername(): string | null {
    if (!this.isBrowser) {
      return null;
    }

    try {
      return localStorage.getItem(REMEMBERED_USERNAME_KEY);
    } catch (error) {
      if (error instanceof DOMException) {
        this.errorMessage.set('This browser could not access saved sign-in preferences.');
        return null;
      }

      throw error;
    }
  }

  private persistUsernamePreference(): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      if (this.rememberUsername() && this.username().trim()) {
        localStorage.setItem(REMEMBERED_USERNAME_KEY, this.username().trim());
      } else {
        localStorage.removeItem(REMEMBERED_USERNAME_KEY);
      }
    } catch (error) {
      if (error instanceof DOMException) {
        this.errorMessage.set('Your sign-in succeeded, but this browser could not save the username preference.');
        return;
      }

      throw error;
    }
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof Error && error.message.includes('Administrator accounts')) {
      return error.message;
    }

    if (!(error instanceof HttpErrorResponse)) {
      return this.isRegistering()
        ? 'Account creation failed. Please try again shortly.'
        : 'Sign in failed. Please try again shortly.';
    }

    if (error.status === 0) {
      return 'Cannot reach the local API at https://localhost:7197. Start the API and trust its HTTPS development certificate.';
    }

    if (error.status === 401) {
      return 'The username or password is incorrect.';
    }

    if (error.status === 400) {
      return 'Choose a username with 3–50 letters, numbers, dots, underscores, or hyphens, and a password of at least 12 characters.';
    }

    if (error.status === 404) {
      return 'The account endpoint was not found. Make sure the API is running the latest version.';
    }

    if (error.status === 409) {
      return 'An account with that username already exists.';
    }

    if (error.status >= 500) {
      return 'The API could not complete the request. Check the API terminal for a database or server error.';
    }

    return this.isRegistering()
      ? `Account creation failed (HTTP ${error.status}).`
      : `Sign in failed (HTTP ${error.status}).`;
  }
}

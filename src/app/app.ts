import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  public authService = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    if (!globalThis.confirm('Are you sure you want to sign out?')) {
      return;
    }

    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  exitAdminReview(): void {
    this.authService.logoutAdmin();
    void this.router.navigate(['/intake']);
  }
}

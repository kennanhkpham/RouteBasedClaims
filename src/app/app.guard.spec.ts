import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, RouterOutlet, provideRouter } from '@angular/router';
import { environment } from '../environments/environment';
import { AuthService } from './auth.service';
import {
  adminGuard,
  adminLoginGuard,
  authenticatedGuard,
  signedOutGuard,
} from './app.guard';

@Component({
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
class GuardTestHostComponent {}

@Component({
  template: '<p>Guard target</p>',
})
class GuardTestTargetComponent {}

describe('admin route guards', () => {
  let fixture: ComponentFixture<GuardTestHostComponent>;
  let authService: AuthService;
  let httpTestingController: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuardTestHostComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: '', redirectTo: 'login', pathMatch: 'full' },
          { path: 'review', canActivate: [adminGuard], component: GuardTestTargetComponent },
          { path: 'admin-login', canActivate: [adminLoginGuard], component: GuardTestTargetComponent },
          { path: 'login', canActivate: [signedOutGuard], component: GuardTestTargetComponent },
          { path: 'intake', canActivate: [authenticatedGuard], component: GuardTestTargetComponent },
        ]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GuardTestHostComponent);
    fixture.detectChanges();
    authService = TestBed.inject(AuthService);
    httpTestingController = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    authService.logout();
    httpTestingController.verify();
  });

  it('allows an Admin-only session to access Claim Review', async () => {
    authService.loginAdmin('admin-user', 'admin-password').subscribe();
    httpTestingController.expectOne(`${environment.apiUrl}/auth/admin-login`).flush({
      token: 'admin-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Admin',
    });

    await router.navigateByUrl('/review');

    expect(router.url).toBe('/review');
  });

  it('allows an unauthenticated user to open Admin sign-in', async () => {
    await router.navigateByUrl('/admin-login');

    expect(router.url).toBe('/admin-login');
  });

  it('routes anonymous Claim Review access to Admin sign-in', async () => {
    await router.navigateByUrl('/review');

    expect(router.parseUrl(router.url).root.children['primary'].segments.map((segment) => segment.path))
      .toEqual(['admin-login']);
    expect(router.parseUrl(router.url).queryParams['returnUrl']).toBe('/review');
  });

  it('prompts a signed-in Claimant for the separate Admin session', async () => {
    authService.login('claimant-user', 'claimant-password').subscribe();
    httpTestingController.expectOne(`${environment.apiUrl}/auth/login`).flush({
      token: 'claimant-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Claimant',
    });

    await router.navigateByUrl('/review');

    expect(router.parseUrl(router.url).root.children['primary'].segments.map((segment) => segment.path))
      .toEqual(['admin-login']);
    expect(router.parseUrl(router.url).queryParams['returnUrl']).toBe('/review');
  });

  it('sends an Admin-only session back to Claim Review from sign-in', async () => {
    authService.loginAdmin('admin-user', 'admin-password').subscribe();
    httpTestingController.expectOne(`${environment.apiUrl}/auth/admin-login`).flush({
      token: 'admin-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Admin',
    });

    await router.navigateByUrl('/login');

    expect(router.url).toBe('/review');
  });
});

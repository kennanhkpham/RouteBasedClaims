import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { App } from './app';
import { routes } from './app.routes';
import { AuthService } from './auth.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient()],
    })
      .compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('shows sign-in first and hides navigation until authenticated', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/login');
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Sign in');
    expect(compiled.querySelector('nav')).toBeNull();
  });

  it('shows admin workflows and moves exit-admin action to the header', () => {
    const fixture = TestBed.createComponent(App);
    const authService = TestBed.inject(AuthService);
    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(true);
    vi.spyOn(authService, 'isAdmin').mockReturnValue(true);

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const navigation = compiled.querySelector('nav[aria-label="Main navigation"]');
    expect(navigation?.textContent).toContain('Claim Review');
    expect(navigation?.textContent).toContain('Manage Accounts');
    expect(navigation?.textContent).not.toContain('New Claim');
    expect(navigation?.textContent).not.toContain('Check Status');
    expect(compiled.querySelector('.header-actions')?.textContent).toContain('Exit admin view');
  });

  it('keeps claimant navigation focused on claimant actions', () => {
    const fixture = TestBed.createComponent(App);
    const authService = TestBed.inject(AuthService);
    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(true);
    vi.spyOn(authService, 'isAdmin').mockReturnValue(false);

    fixture.detectChanges();

    const navigation = (fixture.nativeElement as HTMLElement)
      .querySelector('nav[aria-label="Main navigation"]');
    expect(navigation?.textContent).toContain('New Claim');
    expect(navigation?.textContent).toContain('Check Status');
    expect(navigation?.textContent).toContain('Claim Review');
  });

  it('keeps the session active when sign out is cancelled', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    const logoutSpy = vi.spyOn(authService, 'logout');
    const confirmSpy = vi.spyOn(globalThis, 'confirm').mockReturnValue(false);

    app.logout();

    expect(confirmSpy).toHaveBeenCalledWith('Are you sure you want to sign out?');
    expect(logoutSpy).not.toHaveBeenCalled();
  });

  it('signs out after the user confirms', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const authService = TestBed.inject(AuthService);
    const logoutSpy = vi.spyOn(authService, 'logout');
    const confirmSpy = vi.spyOn(globalThis, 'confirm').mockReturnValue(true);
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');

    app.logout();

    expect(confirmSpy).toHaveBeenCalledWith('Are you sure you want to sign out?');
    expect(logoutSpy).toHaveBeenCalledOnce();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });
});

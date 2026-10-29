import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { AdminLoginComponent } from './admin-login';

describe('AdminLoginComponent', () => {
  let httpTestingController: HttpTestingController;
  let authService: AuthService;
  let navigateByUrl: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    localStorage.removeItem('claimstream-remembered-username');
    navigateByUrl = vi.fn().mockResolvedValue(true);
    await TestBed.configureTestingModule({
      imports: [AdminLoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => null } } },
        },
        {
          provide: Router,
          useValue: { navigateByUrl },
        },
      ],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    authService.logout();
    localStorage.removeItem('claimstream-remembered-username');
    httpTestingController.verify();
  });

  it('welcomes users to ClaimStream WorkComp', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Welcome to ClaimStream WorkComp');
    expect(fixture.nativeElement.querySelector('.welcome-icon')).toBeTruthy();
  });

  it('toggles password visibility accessibly', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    fixture.detectChanges();

    const passwordInput = fixture.nativeElement.querySelector('#password') as HTMLInputElement;
    const visibilityButton = fixture.nativeElement.querySelector('.visibility-button') as HTMLButtonElement;
    expect(passwordInput.type).toBe('password');
    expect(visibilityButton.getAttribute('aria-label')).toBe('Show password');

    visibilityButton.click();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('text');
    expect(visibilityButton.getAttribute('aria-label')).toBe('Hide password');
    expect(visibilityButton.getAttribute('aria-pressed')).toBe('true');
  });

  it('remembers only the username when requested', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.username.set('returning.claimant');
    component.password.set('private-password');
    component.rememberUsername.set(true);

    component.submit();

    const request = httpTestingController.expectOne(
      'https://localhost:7197/api/auth/login',
    );
    request.flush({
      token: 'claimant-session-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Claimant',
    });

    expect(localStorage.getItem('claimstream-remembered-username')).toBe('returning.claimant');
    expect(localStorage.getItem('private-password')).toBeNull();
    expect(sessionStorage.getItem('claimstream-user-session')).toContain('claimant-session-token');
  });

  it('shows loading feedback while signing in', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.username.set('returning.claimant');
    component.password.set('private-password');

    component.submit();
    fixture.detectChanges();

    const submitButton = fixture.nativeElement.querySelector('.submit-button') as HTMLButtonElement;
    expect(submitButton.disabled).toBe(true);
    expect(submitButton.getAttribute('aria-busy')).toBe('true');
    expect(submitButton.textContent).toContain('Signing in…');
    expect(submitButton.querySelector('.loading-spinner')).toBeTruthy();

    httpTestingController.expectOne('https://localhost:7197/api/auth/login').flush(
      { message: 'Unauthorized' },
      { status: 401, statusText: 'Unauthorized' },
    );
  });

  it('creates a claimant account and stores its JWT without admin access', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.isRegistering.set(true);
    component.username.set('new.claimant');
    component.password.set('a-long-claimant-password');
    component.confirmPassword.set('a-long-claimant-password');

    component.submit();

    const request = httpTestingController.expectOne(
      'https://localhost:7197/api/auth/register',
    );
    expect(request.request.body).toEqual({
      username: 'new.claimant',
      password: 'a-long-claimant-password',
    });
    request.flush({
      token: 'claimant-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Claimant',
    });

    expect(authService.getToken()).toBe('claimant-jwt');
    expect(authService.isAdmin()).toBe(false);
    expect(navigateByUrl).toHaveBeenCalledWith('/intake');
  });

  it('requires administrator accounts to use the separate claim review sign-in', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.username.set('claim-admin');
    component.password.set('a-long-admin-password');

    component.submit();

    const request = httpTestingController.expectOne(
      'https://localhost:7197/api/auth/login',
    );
    request.flush({
      token: 'admin-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Admin',
    });

    expect(authService.isAuthenticated()).toBe(false);
    expect(authService.isAdmin()).toBe(false);
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('rejects mismatched registration passwords before making a request', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.isRegistering.set(true);
    component.password.set('a-long-claimant-password');
    component.confirmPassword.set('a-different-password');

    component.submit();

    expect(component.errorMessage()).toBe('The passwords do not match.');
    httpTestingController.expectNone(
      'https://localhost:7197/api/auth/register',
    );
  });

  it('explains that the API must be updated when registration is not available', () => {
    const fixture = TestBed.createComponent(AdminLoginComponent);
    const component = fixture.componentInstance;
    component.isRegistering.set(true);
    component.username.set('new.claimant');
    component.password.set('a-long-claimant-password');
    component.confirmPassword.set('a-long-claimant-password');

    component.submit();

    httpTestingController.expectOne('https://localhost:7197/api/auth/register').flush(
      { message: 'Not Found' },
      { status: 404, statusText: 'Not Found' },
    );

    expect(component.errorMessage()).toContain('API is running the latest version');
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { AdminReviewLoginComponent } from './admin-review-login';

describe('AdminReviewLoginComponent', () => {
  let httpTestingController: HttpTestingController;
  let authService: AuthService;
  let navigateByUrl: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    navigateByUrl = vi.fn().mockResolvedValue(true);
    await TestBed.configureTestingModule({
      imports: [AdminReviewLoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => '/review' } } },
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
    httpTestingController.verify();
  });

  it('uses separate admin credentials and preserves the claimant session', () => {
    authService.login('customer', 'customer-password').subscribe();
    httpTestingController.expectOne('https://localhost:7197/api/auth/login').flush({
      token: 'customer-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Claimant',
    });

    const fixture = TestBed.createComponent(AdminReviewLoginComponent);
    const component = fixture.componentInstance;
    component.username.set('claim-admin');
    component.password.set('admin-password');
    component.signIn();

    const request = httpTestingController.expectOne('https://localhost:7197/api/auth/admin-login');
    expect(request.request.body).toEqual({
      username: 'claim-admin',
      password: 'admin-password',
    });
    request.flush({
      token: 'admin-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Admin',
    });

    expect(authService.isAuthenticated()).toBe(true);
    expect(authService.isAdmin()).toBe(true);
    expect(authService.getTokenForRequest(false)).toBe('customer-jwt');
    expect(authService.getTokenForRequest(true)).toBe('admin-jwt');
    expect(navigateByUrl).toHaveBeenCalledWith('/review');

    authService.logoutAdmin();
    expect(authService.isAuthenticated()).toBe(true);
    expect(authService.isAdmin()).toBe(false);
    expect(authService.getTokenForRequest(false)).toBe('customer-jwt');
  });

  it('rejects claimant credentials at the dedicated admin endpoint', () => {
    const fixture = TestBed.createComponent(AdminReviewLoginComponent);
    const component = fixture.componentInstance;
    component.username.set('customer');
    component.password.set('customer-password');
    component.signIn();

    httpTestingController.expectOne('https://localhost:7197/api/auth/admin-login').flush(
      { message: 'Invalid administrator username or password.' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(component.errorMessage()).toContain('administrator username or password is incorrect');
    expect(authService.isAdmin()).toBe(false);
    expect(authService.isAuthenticated()).toBe(false);
    expect(navigateByUrl).not.toHaveBeenCalled();
  });
});

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../../auth.service';
import { authInterceptor } from '../../auth.interceptor';
import { AdminAccountsComponent } from './admin-accounts';

describe('AdminAccountsComponent', () => {
  let httpTestingController: HttpTestingController;
  let authService: AuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminAccountsComponent],
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    authService.loginAdmin('claim-admin', 'admin-password').subscribe();
    httpTestingController.expectOne('https://localhost:7197/api/auth/admin-login').flush({
      token: 'admin-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Admin',
    });
  });

  afterEach(() => {
    authService.logout();
    httpTestingController.verify();
  });

  it('creates an administrator account only through the protected admin endpoint', () => {
    const fixture = TestBed.createComponent(AdminAccountsComponent);
    const component = fixture.componentInstance;
    component.username.set('new-reviewer');
    component.password.set('a-long-secure-password');
    component.confirmPassword.set('a-long-secure-password');
    component.isAdmin.set(true);

    component.createAccount();

    const request = httpTestingController.expectOne('https://localhost:7197/api/auth/accounts');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      username: 'new-reviewer',
      password: 'a-long-secure-password',
      isAdmin: true,
    });
    expect(request.request.headers.get('Authorization')).toBe('Bearer admin-jwt');
    request.flush({ id: 2, username: 'new-reviewer', isAdmin: true });

    expect(component.successMessage()).toContain('Administrator account "new-reviewer" was created');
    expect(component.isAdmin()).toBe(false);
  });

  it('rejects mismatched passwords without sending a request', () => {
    const fixture = TestBed.createComponent(AdminAccountsComponent);
    const component = fixture.componentInstance;
    component.password.set('a-long-secure-password');
    component.confirmPassword.set('different-password');

    component.createAccount();

    expect(component.errorMessage()).toBe('The passwords do not match.');
    httpTestingController.expectNone('https://localhost:7197/api/auth/accounts');
  });
});

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../auth.service';
import { authInterceptor } from '../../auth.interceptor';
import { AdminAccountsComponent } from './admin-accounts';

describe('AdminAccountsComponent', () => {
  let httpTestingController: HttpTestingController;
  let authService: AuthService;
  const adminAccountUrl = `${environment.apiUrl}/auth/admin/accounts`;

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
    httpTestingController.expectOne(`${environment.apiUrl}/auth/admin-login`).flush({
      token: 'admin-jwt',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      role: 'Admin',
    });
  });

  afterEach(() => {
    authService.logout();
    httpTestingController.verify();
  });

  it('creates an administrator account without a client-supplied role', () => {
    const fixture = TestBed.createComponent(AdminAccountsComponent);
    const component = fixture.componentInstance;
    component.username.set('new-reviewer');
    component.password.set('a-long-secure-password');
    component.confirmPassword.set('a-long-secure-password');
    component.department.set('Claims Review');

    component.createAccount();

    const request = httpTestingController.expectOne(adminAccountUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      username: 'new-reviewer',
      password: 'a-long-secure-password',
      department: 'Claims Review',
    });
    expect(request.request.headers.get('Authorization')).toBe('Bearer admin-jwt');
    request.flush({ id: 2, username: 'new-reviewer', isAdmin: true });

    expect(component.successMessage()).toContain('Administrator account "new-reviewer" was created');
    expect(component.department()).toBe('');
  });

  it('rejects missing occupation without sending a request', () => {
    const fixture = TestBed.createComponent(AdminAccountsComponent);
    const component = fixture.componentInstance;
    component.username.set('new-reviewer');
    component.password.set('a-long-secure-password');
    component.confirmPassword.set('a-long-secure-password');

    component.createAccount();

    expect(component.errorMessage()).toBe('Enter the department where this administrator works (100 characters or fewer).');
    httpTestingController.expectNone(adminAccountUrl);
  });

  it('rejects mismatched passwords without sending a request', () => {
    const fixture = TestBed.createComponent(AdminAccountsComponent);
    const component = fixture.componentInstance;
    component.username.set('new-reviewer');
    component.password.set('a-long-secure-password');
    component.confirmPassword.set('different-password');
    component.department.set('Claims Review');

    component.createAccount();

    expect(component.errorMessage()).toBe('The passwords do not match.');
    httpTestingController.expectNone(adminAccountUrl);
  });
});

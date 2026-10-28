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

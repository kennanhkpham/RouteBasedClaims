import { HttpTestingController } from '@angular/common/http/testing';
import { afterEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { ClaimStatusComponent } from './claim-status';

describe('ClaimStatusComponent', () => {
  let component: ClaimStatusComponent;
  let fixture: ComponentFixture<ClaimStatusComponent>;
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimStatusComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ClaimStatusComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('focuses the claim-number input on load', () => {
    const input = fixture.nativeElement.querySelector('#claimNumber') as HTMLInputElement;
    expect(document.activeElement).toBe(input);
  });

  it('clears the claim number and returns focus to the input', () => {
    const input = fixture.nativeElement.querySelector('#claimNumber') as HTMLInputElement;
    component.lookupForm.controls.claimNumber.setValue('CLM-12345678');
    fixture.detectChanges();

    const clearButton = fixture.nativeElement.querySelector('.clear-button') as HTMLButtonElement;
    expect(clearButton).toBeTruthy();
    clearButton.click();
    fixture.detectChanges();

    expect(component.lookupForm.controls.claimNumber.value).toBe('');
    expect(document.activeElement).toBe(input);
    expect(fixture.nativeElement.querySelector('.clear-button')).toBeNull();
  });

  it('submits the lookup when Enter submits the form and renders results inline', () => {
    component.lookupForm.controls.claimNumber.setValue('CLM-12345678');
    fixture.detectChanges();
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    const request = httpTestingController.expectOne(
      `${environment.apiUrl}/claims/CLM-12345678`,
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      claimId: 7,
      claimNumber: 'CLM-12345678',
      policyNumber: 'TEST-POLICY',
      fullName: 'Test User',
      email: 'test@example.com',
      amountCost: 25.5,
      status: 'Pending Authorization',
      createdAt: '2026-10-28T12:00:00Z',
      prescriptionDetail: {
        prescriptionNumber: 'RX-123',
        doctorName: 'Dr. Example',
        datePrescribed: '2026-10-28T00:00:00Z',
        medicationName: 'Test medication',
      },
    });
    fixture.detectChanges();

    expect(component.claim()?.claimNumber).toBe('CLM-12345678');
    expect(component.statusStep(component.claim()?.status)).toBe(1);
    expect(fixture.nativeElement.querySelector('.result-card')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Under Review');
  });

  it('maps terminal and in-review claim statuses to the progress stages', () => {
    expect(component.statusStep('Submitted')).toBe(0);
    expect(component.statusStep('Pending Review')).toBe(1);
    expect(component.statusStep('Approved')).toBe(2);
    expect(component.statusStep('Rejected')).toBe(2);
    expect(component.statusStep('Inactive')).toBe(2);
  });
});

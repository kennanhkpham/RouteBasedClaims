import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ClaimIntakeComponent } from './claim-intake';

describe('ClaimIntakeComponent', () => {
  let component: ClaimIntakeComponent;
  let fixture: ComponentFixture<ClaimIntakeComponent>;
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimIntakeComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ClaimIntakeComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('validates each step before allowing the user to continue', () => {
    component.nextStep();
    expect(component.currentStep()).toBe(0);
    expect(component.claimForm.controls.policyNumber.touched).toBe(true);

    component.claimForm.controls.policyNumber.setValue('POLICY-123');
    component.claimForm.controls.amountCost.setValue(85.5);
    component.nextStep();
    expect(component.currentStep()).toBe(1);

    component.claimForm.controls.fullName.setValue('Alex Example');
    component.claimForm.controls.email.setValue('alex@example.com');
    component.nextStep();
    expect(component.currentStep()).toBe(2);

    component.nextStep();
    expect(component.currentStep()).toBe(2);
    expect(component.claimForm.controls.prescriptionDetail.controls.medicationName.touched).toBe(true);
  });

  it('submits the existing claim API payload from the review step', () => {
    component.claimForm.controls.policyNumber.setValue('POLICY-123');
    component.claimForm.controls.amountCost.setValue(85.5);
    component.nextStep();

    component.claimForm.controls.fullName.setValue('Alex Example');
    component.claimForm.controls.email.setValue('alex@example.com');
    component.nextStep();

    const prescription = component.claimForm.controls.prescriptionDetail.controls;
    prescription.medicationName.setValue('Example medication');
    prescription.prescriptionNumber.setValue('RX-456');
    prescription.doctorName.setValue('Dr. Example');
    prescription.datePrescribed.setValue('2026-10-28');
    component.nextStep();

    expect(component.currentStep()).toBe(3);
    component.onSubmit();

    const request = httpTestingController.expectOne(
      (candidate) => candidate.method === 'POST' && candidate.url.endsWith('/claims'),
    );
    expect(request.request.body).toEqual({
      policyNumber: 'POLICY-123',
      fullName: 'Alex Example',
      email: 'alex@example.com',
      amountCost: 85.5,
      prescriptionDetail: {
        medicationName: 'Example medication',
        prescriptionNumber: 'RX-456',
        doctorName: 'Dr. Example',
        datePrescribed: '2026-10-28',
      },
    });

    request.flush({
      claimId: 7,
      claimNumber: 'CLM-12345678',
      policyNumber: 'POLICY-123',
      fullName: 'Alex Example',
      email: 'alex@example.com',
      amountCost: 85.5,
      status: 'Submitted',
      createdAt: '2026-10-28T12:00:00Z',
      prescriptionDetail: {
        prescriptionNumber: 'RX-456',
        doctorName: 'Dr. Example',
        datePrescribed: '2026-10-28',
        medicationName: 'Example medication',
      },
    });

    expect(component.submittedClaim()?.claimNumber).toBe('CLM-12345678');
    expect(component.currentStep()).toBe(0);
  });
});

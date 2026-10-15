import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Claim } from '../../core/models/Claim.model';
import { ClaimReviewerComponent } from './claim-review';

describe('ClaimReviewerComponent', () => {
  let component: ClaimReviewerComponent;
  let fixture: ComponentFixture<ClaimReviewerComponent>;
  let httpTestingController: HttpTestingController;
  const claim: Claim = {
    claimId: 1,
    claimNumber: 'CLM-123',
    policyNumber: 'POL-123',
    fullName: 'Alex Morgan',
    email: 'alex@example.com',
    amountCost: 25,
    status: 'Pending',
    prescriptionDetail: {
      prescriptionNumber: 'RX-123',
      doctorName: 'Dr. Lee',
      datePrescribed: '2026-10-01',
      medicationName: 'Example medication',
    },
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimReviewerComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ClaimReviewerComponent);
    component = fixture.componentInstance;
    httpTestingController.expectOne('https://localhost:7197/api/claims').flush([claim]);
    await fixture.whenStable();
  });

  afterEach(() => httpTestingController.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('updates a claim status and reflects the saved status', () => {
    component.statusEdits.set({ 1: 'Approved' });

    component.onUpdateStatus(claim);

    const request = httpTestingController.expectOne('https://localhost:7197/api/claims/1/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ status: 'Approved' });
    request.flush(null);

    expect(component.claims()[0].status).toBe('Approved');
    expect(component.successMessage()).toContain('CLM-123');
  });
});

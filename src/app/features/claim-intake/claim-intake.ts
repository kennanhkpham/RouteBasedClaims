import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Claim } from '../../core/models/Claim.model';
import { ClaimService } from '../../core/services/claim.service';

@Component({
  selector: 'app-claim-intake',
  imports: [ReactiveFormsModule],
  templateUrl: './claim-intake.html',
  styleUrl: './claim-intake.css',
})
export class ClaimIntakeComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly claimService = inject(ClaimService);

  readonly isSaving = signal(false);
  readonly errorMessage = signal('');
  readonly submittedClaim = signal<Claim | null>(null);

  readonly claimForm = this.formBuilder.group({
    policyNumber: ['', [Validators.required, Validators.maxLength(40)]],
    fullName: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    amountCost: [0, [Validators.required, Validators.min(0.01)]],
    prescriptionDetail: this.formBuilder.group({
      prescriptionNumber: ['', [Validators.required, Validators.maxLength(60)]],
      doctorName: ['', [Validators.required, Validators.maxLength(120)]],
      datePrescribed: ['', Validators.required],
      medicationName: ['', [Validators.required, Validators.maxLength(120)]],
    }),
  });

  onSubmit(): void {
    this.errorMessage.set('');
    this.submittedClaim.set(null);

    if (this.claimForm.invalid) {
      this.claimForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.claimService.submitClaim(this.claimForm.getRawValue()).subscribe({
      next: (claim) => {
        this.submittedClaim.set(claim);
        this.claimForm.reset();
        this.isSaving.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 0
            ? 'We could not connect to the claims service. Please try again shortly.'
            : 'Your claim could not be submitted. Please review your information and try again.',
        );
        this.isSaving.set(false);
      },
    });
  }
}

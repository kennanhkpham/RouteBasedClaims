import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { afterNextRender, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Claim } from '../../core/models/Claim.model';
import { ClaimService } from '../../core/services/claim.service';

@Component({
  selector: 'app-claim-status',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule],
  templateUrl: './claim-status.html',
  styleUrl: './claim-status.css',
})
export class ClaimStatusComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly claimService = inject(ClaimService);

  readonly claimNumberInput = viewChild<ElementRef<HTMLInputElement>>('claimNumberInput');
  readonly progressSteps = ['Submitted', 'Under Review', 'Decision'];
  readonly isSearching = signal(false);
  readonly errorMessage = signal('');
  readonly claim = signal<Claim | null>(null);

  readonly lookupForm = this.formBuilder.group({
    claimNumber: ['', [Validators.required, Validators.maxLength(40)]],
  });

  constructor() {
    afterNextRender(() => this.claimNumberInput()?.nativeElement.focus());
  }

  onSearch(): void {
    if (this.isSearching()) {
      return;
    }

    this.errorMessage.set('');
    this.claim.set(null);

    if (this.lookupForm.invalid) {
      this.lookupForm.markAllAsTouched();
      return;
    }

    const claimNumber = this.lookupForm.controls.claimNumber.value.trim();
    if (!claimNumber) {
      this.lookupForm.controls.claimNumber.setErrors({ required: true });
      this.lookupForm.controls.claimNumber.markAsTouched();
      return;
    }

    this.isSearching.set(true);
    this.claimService.getClaimByNumber(claimNumber).subscribe({
      next: (result) => {
        this.claim.set(result);
        this.isSearching.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 404
            ? 'We could not find a claim with that number. Check the number and try again.'
            : error instanceof HttpErrorResponse && error.status === 0
              ? 'We could not connect to the claims service. Please try again shortly.'
              : 'The claim status could not be retrieved. Please try again.',
        );
        this.isSearching.set(false);
      },
    });
  }

  clearClaimNumber(): void {
    this.lookupForm.controls.claimNumber.setValue('');
    this.lookupForm.controls.claimNumber.markAsPristine();
    this.lookupForm.controls.claimNumber.markAsUntouched();
    this.errorMessage.set('');
    this.claim.set(null);
    this.claimNumberInput()?.nativeElement.focus();
  }

  statusStep(status: string | undefined): number {
    switch (status) {
      case 'Pending Review':
      case 'Pending Authorization':
        return 1;
      case 'Approved':
      case 'Rejected':
      case 'Inactive':
        return 2;
      default:
        return 0;
    }
  }
}

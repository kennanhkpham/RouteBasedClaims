import { HttpErrorResponse } from '@angular/common/http';
import {
  afterNextRender,
  Component,
  ElementRef,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
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
  private readonly injector = inject(Injector);

  readonly steps = [
    'Claim details',
    'Client information',
    'Prescription details',
    'Review & submit',
  ];
  readonly currentStep = signal(0);
  readonly isSaving = signal(false);
  readonly errorMessage = signal('');
  readonly submittedClaim = signal<Claim | null>(null);
  readonly stepHeading = viewChild<ElementRef<HTMLHeadingElement>>('stepHeading');

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

  nextStep(): void {
    if (this.currentStep() >= this.steps.length - 1 || !this.validateStep(this.currentStep())) {
      return;
    }

    this.errorMessage.set('');
    this.currentStep.update((step) => step + 1);
    this.focusStepHeading();
  }

  previousStep(): void {
    if (this.isSaving() || this.currentStep() === 0) {
      return;
    }

    this.errorMessage.set('');
    this.currentStep.update((step) => step - 1);
    this.focusStepHeading();
  }

  goToStep(step: number): void {
    if (this.isSaving() || step < 0 || step >= this.steps.length - 1) {
      return;
    }

    this.errorMessage.set('');
    this.currentStep.set(step);
    this.focusStepHeading();
  }

  onSubmit(): void {
    if (this.currentStep() < this.steps.length - 1) {
      this.nextStep();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    this.errorMessage.set('');
    this.submittedClaim.set(null);

    if (this.claimForm.invalid) {
      const firstInvalidStep = this.steps
        .slice(0, -1)
        .findIndex((_, index) => !this.isStepValid(index));
      const step = firstInvalidStep === -1 ? 0 : firstInvalidStep;
      this.currentStep.set(step);
      this.validateStep(step);
      this.focusStepHeading();
      return;
    }

    this.isSaving.set(true);
    this.claimService.submitClaim(this.claimForm.getRawValue()).subscribe({
      next: (claim) => {
        this.submittedClaim.set(claim);
        this.claimForm.reset();
        this.currentStep.set(0);
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

  private validateStep(step: number): boolean {
    const controls = this.getStepControls(step);
    controls.forEach((control) => control.markAsTouched());
    return controls.every((control) => control.valid);
  }

  private isStepValid(step: number): boolean {
    return this.getStepControls(step).every((control) => control.valid);
  }

  private getStepControls(step: number) {
    switch (step) {
      case 0:
        return [
          this.claimForm.controls.policyNumber,
          this.claimForm.controls.amountCost,
        ];
      case 1:
        return [
          this.claimForm.controls.fullName,
          this.claimForm.controls.email,
        ];
      case 2:
        return Object.values(this.claimForm.controls.prescriptionDetail.controls);
      default:
        return [];
    }
  }

  private focusStepHeading(): void {
    afterNextRender(() => this.stepHeading()?.nativeElement.focus(), {
      injector: this.injector,
    });
  }
}

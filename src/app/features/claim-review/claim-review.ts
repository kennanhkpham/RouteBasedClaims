import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Claim } from '../../core/models/Claim.model';
import { ClaimService } from '../../core/services/claim.service';

const CLAIM_STATUSES = [
  'Submitted',
  'Pending Review',
  'Pending Authorization',
  'Approved',
  'Rejected',
  'Inactive',
] as const;

@Component({
  selector: 'app-claim-reviewer',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './claim-review.html',
  styleUrl: './claim-review.css',
})
export class ClaimReviewerComponent {
  private readonly claimService = inject(ClaimService);

  readonly statuses = CLAIM_STATUSES;
  readonly claims = signal<Claim[]>([]);
  readonly statusEdits = signal<Record<number, string>>({});
  readonly searchQuery = signal('');
  readonly statusFilter = signal<'all' | 'pending'>('all');
  readonly isLoading = signal(false);
  readonly updatingClaimId = signal<number | null>(null);
  readonly deletingClaimNumber = signal<string | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly pendingCount = computed(
    () => this.claims().filter((claim) =>
      !['Approved', 'Rejected', 'Inactive'].includes(claim.status ?? 'Submitted'),
    ).length,
  );

  readonly filteredClaims = computed(() => {
    const query = this.searchQuery().trim().toLocaleLowerCase();
    const claims = this.statusFilter() === 'pending'
      ? this.claims().filter((claim) =>
        !['Approved', 'Rejected', 'Inactive'].includes(claim.status ?? 'Submitted'),
      )
      : this.claims();

    if (!query) {
      return claims;
    }

    return claims.filter((claim) => [
      claim.claimNumber,
      claim.policyNumber,
      claim.fullName,
      claim.email,
      claim.prescriptionDetail.medicationName,
      claim.prescriptionDetail.prescriptionNumber,
      claim.prescriptionDetail.doctorName,
      claim.status,
    ].some((value) => value?.toLocaleLowerCase().includes(query)));
  });

  constructor() {
    this.loadClaims();
  }

  loadClaims(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.claimService.getClaims().subscribe({
      next: (claims) => {
        this.claims.set(claims);
        this.statusEdits.set({});
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error, 'Claims could not be loaded.'));
        this.isLoading.set(false);
      },
    });
  }

  selectedStatus(claim: Claim): string {
    return claim.claimId === undefined
      ? claim.status ?? 'Submitted'
      : this.statusEdits()[claim.claimId] ?? claim.status ?? 'Submitted';
  }

  setSearchQuery(event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.searchQuery.set(target.value);
    }
  }

  setStatusFilter(filter: 'all' | 'pending'): void {
    this.statusFilter.set(filter);
  }

  statusBadgeClass(status: string | undefined): string {
    if (status === 'Approved') {
      return 'status-approved';
    }
    if (status === 'Rejected') {
      return 'status-rejected';
    }
    if (status === 'Inactive') {
      return 'status-inactive';
    }
    return 'status-pending';
  }

  onStatusSelection(claim: Claim, event: Event): void {
    if (claim.claimId === undefined) {
      this.errorMessage.set('This claim cannot be updated because its identifier is missing.');
      return;
    }
    const claimId = claim.claimId;

    const target = event.target;
    if (!(target instanceof HTMLSelectElement)) {
      this.errorMessage.set('The selected claim status could not be read.');
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');
    this.statusEdits.update((edits) => ({
      ...edits,
      [claimId]: target.value,
    }));
  }

  hasStatusChange(claim: Claim): boolean {
    return this.selectedStatus(claim) !== (claim.status ?? 'Submitted');
  }

  canDeleteClaim(claim: Claim): boolean {
    return ['Rejected', 'Inactive'].includes(claim.status ?? '') &&
      claim.claimNumber !== undefined &&
      claim.claimNumber.trim().length > 0;
  }

  onDeleteClaim(claim: Claim): void {
    if (!this.canDeleteClaim(claim) || this.updatingClaimId() !== null || this.deletingClaimNumber() !== null) {
      return;
    }

    const claimNumber = claim.claimNumber;
    if (!claimNumber) {
      return;
    }
    const confirmed = globalThis.confirm(
      `Delete ${claimNumber}? This permanently removes the claim record.`,
    );
    if (!confirmed) {
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');
    this.deletingClaimNumber.set(claimNumber);

    this.claimService.deleteClaim(claimNumber).subscribe({
      next: () => {
        this.claims.update((claims) => claims.filter((item) => item.claimNumber !== claimNumber));
        this.successMessage.set(`${claimNumber} was deleted.`);
        this.deletingClaimNumber.set(null);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error, `Claim ${claimNumber} could not be deleted.`));
        this.deletingClaimNumber.set(null);
      },
    });
  }

  onUpdateStatus(claim: Claim): void {
    const claimId = claim.claimId;
    if (claimId === undefined) {
      this.errorMessage.set('This claim cannot be updated because its identifier is missing.');
      return;
    }

    const status = this.selectedStatus(claim);
    if (!this.hasStatusChange(claim) || this.updatingClaimId() !== null || this.deletingClaimNumber() !== null) {
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');
    this.updatingClaimId.set(claimId);

    this.claimService.updateStatus(claimId, status).subscribe({
      next: () => {
        this.claims.update((claims) =>
          claims.map((item) => item.claimId === claimId ? { ...item, status } : item),
        );
        this.statusEdits.update((edits) => {
          const { [claimId]: _savedStatus, ...remainingEdits } = edits;
          return remainingEdits;
        });
        this.successMessage.set(`Status updated for ${claim.claimNumber ?? 'claim'}.`);
        this.updatingClaimId.set(null);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error, 'The claim status could not be updated.'));
        this.updatingClaimId.set(null);
      },
    });
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'Could not connect to the claims service. Please try again shortly.';
    }

    return fallback;
  }
}

import { PrescriptionDetails } from './PrescriptionDetails.model';

export interface Claim {
  claimId?: number;
  claimNumber?: string;
  policyNumber: string;
  fullName: string;
  email: string;
  amountCost: number;
  status?: string;
  createdAt?: string | Date;
  prescriptionDetail: PrescriptionDetails;
}

export interface CreateClaimRequest {
  policyNumber: string;
  fullName: string;
  email: string;
  amountCost: number;
  prescriptionDetail: PrescriptionDetails;
}
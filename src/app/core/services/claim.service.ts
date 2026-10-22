import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Claim, CreateClaimRequest } from '../models/Claim.model';

@Injectable({
  providedIn: 'root'
})
export class ClaimService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7197/api/claims';

  submitClaim(data: CreateClaimRequest): Observable<Claim> {
    return this.http.post<Claim>(this.apiUrl, data);
  }

  getClaims(): Observable<Claim[]> {
    return this.http.get<Claim[]>(this.apiUrl);
  }

  getClaimByNumber(claimNumber: string): Observable<Claim> {
    return this.http.get<Claim>(`${this.apiUrl}/${encodeURIComponent(claimNumber)}`);
  }

  getPendingClaims(): Observable<Claim[]> {
    return this.http.get<Claim[]>(`${this.apiUrl}/pending`);
  }

  updateStatus(claimId: number, status: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${claimId}/status`, { status });
  }

  deleteClaim(claimNumber: string): Observable<Claim> {
    return this.http.delete<Claim>(`${this.apiUrl}/${encodeURIComponent(claimNumber)}`);
  }
}
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Claim } from './claim';
import { Client } from './client';

@Injectable({
  providedIn: 'root'
})
export class ClaimService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7123/api/claims'; // Update port as needed

  submitClaim(data: Claim): Observable<Client> {
    return this.http.post<Client>(this.apiUrl, data);
  }

  getClaimByNumber(claimNumber: string): Observable<Client> {
    return this.http.get<Client>(`${this.apiUrl}/${claimNumber}`);
  }

  getPendingClaims(): Observable<Client[]> {
    return this.http.get<Client[]>(`${this.apiUrl}/pending`);
  }

  updateStatus(claimId: number, status: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${claimId}/status`, { status });
  }
}
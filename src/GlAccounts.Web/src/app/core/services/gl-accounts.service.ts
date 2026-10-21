import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccountResponse,
  AccountListResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
  AttributeResponse,
  AddAttributeRequest,
  UpdateAttributeRequest,
  AccountGroupListResponse,
  PeriodSummaryResponse,
  TransactionListResponse,
  CopyFromTemplateRequest
} from '../models/account.model';

@Injectable({ providedIn: 'root' })
export class GlAccountsService {
  private readonly base = `${environment.apiBaseUrl}/api/gl/accounts`;

  constructor(private http: HttpClient) {}

  // ── Accounts ─────────────────────────────────────────────────────────────

  listAccounts(status?: string): Observable<AccountListResponse> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<AccountListResponse>(this.base, { params });
  }

  getAccount(accountId: string): Observable<AccountResponse> {
    return this.http.get<AccountResponse>(`${this.base}/${accountId}`);
  }

  createAccount(request: CreateAccountRequest): Observable<AccountResponse> {
    return this.http.post<AccountResponse>(this.base, request);
  }

  updateAccount(accountId: string, request: UpdateAccountRequest): Observable<AccountResponse> {
    return this.http.put<AccountResponse>(`${this.base}/${accountId}`, request);
  }

  deleteAccount(accountId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${accountId}`);
  }

  // ── Attributes ───────────────────────────────────────────────────────────

  addAttribute(accountId: string, request: AddAttributeRequest): Observable<AttributeResponse> {
    return this.http.post<AttributeResponse>(`${this.base}/${accountId}/attributes`, request);
  }

  updateAttribute(accountId: string, attributeId: string, request: UpdateAttributeRequest): Observable<AttributeResponse> {
    return this.http.put<AttributeResponse>(`${this.base}/${accountId}/attributes/${attributeId}`, request);
  }

  removeAttribute(accountId: string, attributeId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${accountId}/attributes/${attributeId}`);
  }

  // ── Inquiry ──────────────────────────────────────────────────────────────

  listGroups(accountId: string): Observable<AccountGroupListResponse> {
    return this.http.get<AccountGroupListResponse>(`${this.base}/${accountId}/groups`);
  }

  getPeriodSummary(accountId: string, fiscalYear?: number, fromPeriod?: number, toPeriod?: number): Observable<PeriodSummaryResponse> {
    let params = new HttpParams();
    if (fiscalYear  != null) params = params.set('fiscalYear',  fiscalYear.toString());
    if (fromPeriod  != null) params = params.set('fromPeriod',  fromPeriod.toString());
    if (toPeriod    != null) params = params.set('toPeriod',    toPeriod.toString());
    return this.http.get<PeriodSummaryResponse>(`${this.base}/${accountId}/period-summary`, { params });
  }

  getTransactions(accountId: string, fromDate?: string, toDate?: string, postingStatus?: string): Observable<TransactionListResponse> {
    let params = new HttpParams();
    if (fromDate)       params = params.set('fromDate',       fromDate);
    if (toDate)         params = params.set('toDate',         toDate);
    if (postingStatus)  params = params.set('postingStatus',  postingStatus);
    return this.http.get<TransactionListResponse>(`${this.base}/${accountId}/transactions`, { params });
  }

  // ── Templates ────────────────────────────────────────────────────────────

  copyFromTemplate(sourceAccountId: string, request: CopyFromTemplateRequest): Observable<AccountResponse> {
    return this.http.post<AccountResponse>(`${this.base}/${sourceAccountId}/copy`, request);
  }
}

import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  Account,
  AccountAttribute,
  AccountGroupList,
  AccountSummaryPage,
  AttributeInput,
  CreateAccountRequest,
  DownloadedFile,
  JournalEntryDetail,
  JournalEntryPage,
  PeriodSummary,
  PeriodSummaryQuery,
  SearchAccountsQuery,
  TransactionsQuery,
  UpdateAccountRequest,
  WithETag,
} from './models';

const BASE = '/api/gl/accounts';

/** Query string without undefined, null or empty values. */
export function toParams(values: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}

/** File name from a Content-Disposition header, or the given fallback. */
export function fileNameFrom(disposition: string | null, fallback: string): string {
  if (!disposition) return fallback;
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
  if (encoded) return decodeURIComponent(encoded[1]);
  const plain = /filename="?([^";]+)"?/i.exec(disposition);
  return plain ? plain[1] : fallback;
}

/**
 * Typed client for the 15 operations of contracts/openapi.yaml.
 * Methods return observables that fail with an HttpErrorResponse; callers map it with `toApiError`.
 */
@Injectable({ providedIn: 'root' })
export class GlAccountsApi {
  private readonly http = inject(HttpClient);

  private path(accountId: string): string {
    return `${BASE}/${encodeURIComponent(accountId)}`;
  }

  /** API-GLA-015 searchAccounts */
  searchAccounts(query: SearchAccountsQuery = {}): Observable<AccountSummaryPage> {
    return this.http.get<AccountSummaryPage>(BASE, { params: toParams(query) });
  }

  /** API-GLA-001 / API-GLA-016 createAccount (with `templateAccountId` for a template) */
  createAccount(request: CreateAccountRequest): Observable<WithETag<Account>> {
    return this.http
      .post<Account>(BASE, request, { observe: 'response' })
      .pipe(map((response) => this.withETag(response)));
  }

  /** API-GLA-002 getAccount; the ETag is kept for `If-Match` on the next update. */
  getAccount(accountId: string): Observable<WithETag<Account>> {
    return this.http.get<Account>(this.path(accountId), { observe: 'response' }).pipe(map((response) => this.withETag(response)));
  }

  /** API-GLA-003 updateAccount; `If-Match` is required. */
  updateAccount(accountId: string, request: UpdateAccountRequest, etag: string): Observable<WithETag<Account>> {
    const headers = new HttpHeaders({ 'If-Match': etag });
    return this.http
      .put<Account>(this.path(accountId), request, { headers, observe: 'response' })
      .pipe(map((response) => this.withETag(response)));
  }

  /** API-GLA-004 deleteAccount (204). A 409 carries the blockers. */
  deleteAccount(accountId: string): Observable<void> {
    return this.http.delete<void>(this.path(accountId));
  }

  /** API-GLA-005 addAccountAttribute; the response ETag is the account's new version. */
  addAccountAttribute(accountId: string, input: AttributeInput): Observable<WithETag<AccountAttribute>> {
    return this.http
      .post<AccountAttribute>(`${this.path(accountId)}/attributes`, input, { observe: 'response' })
      .pipe(map((response) => this.withETag(response)));
  }

  /** API-GLA-006 updateAccountAttribute */
  updateAccountAttribute(accountId: string, attributeId: string, input: AttributeInput): Observable<WithETag<AccountAttribute>> {
    return this.http
      .put<AccountAttribute>(`${this.path(accountId)}/attributes/${encodeURIComponent(attributeId)}`, input, { observe: 'response' })
      .pipe(map((response) => this.withETag(response)));
  }

  /** API-GLA-007 removeAccountAttribute (204); returns the account's new ETag. */
  removeAccountAttribute(accountId: string, attributeId: string): Observable<string | null> {
    return this.http
      .delete<void>(`${this.path(accountId)}/attributes/${encodeURIComponent(attributeId)}`, { observe: 'response' })
      .pipe(map((response) => response.headers.get('ETag')));
  }

  /** API-GLA-008 listAccountGroups */
  listAccountGroups(accountId: string): Observable<AccountGroupList> {
    return this.http.get<AccountGroupList>(`${this.path(accountId)}/groups`);
  }

  /** API-GLA-009 exportAccountGroups (CSV) */
  exportAccountGroups(accountId: string): Observable<DownloadedFile> {
    return this.download(`${this.path(accountId)}/groups/export`, new HttpParams(), `groups-${accountId}.csv`);
  }

  /** API-GLA-010 getPeriodSummary */
  getPeriodSummary(accountId: string, query: PeriodSummaryQuery = {}): Observable<PeriodSummary> {
    return this.http.get<PeriodSummary>(`${this.path(accountId)}/period-summary`, { params: toParams(query) });
  }

  /** API-GLA-011 exportPeriodSummary (.xlsx) with the same filter and sort as the view */
  exportPeriodSummary(accountId: string, query: PeriodSummaryQuery = {}): Observable<DownloadedFile> {
    return this.download(`${this.path(accountId)}/period-summary/export`, toParams(query), `period-summary-${accountId}.xlsx`);
  }

  /** API-GLA-012 listAccountTransactions */
  listAccountTransactions(accountId: string, query: TransactionsQuery = {}): Observable<JournalEntryPage> {
    return this.http.get<JournalEntryPage>(`${this.path(accountId)}/transactions`, { params: toParams(query) });
  }

  /** API-GLA-013 getAccountTransaction */
  getAccountTransaction(accountId: string, entryId: string): Observable<JournalEntryDetail> {
    return this.http.get<JournalEntryDetail>(`${this.path(accountId)}/transactions/${encodeURIComponent(entryId)}`);
  }

  /** API-GLA-014 exportAccountTransactions (CSV of every matching row, not one page) */
  exportAccountTransactions(accountId: string, query: Omit<TransactionsQuery, 'page' | 'pageSize'> = {}): Observable<DownloadedFile> {
    return this.download(`${this.path(accountId)}/transactions/export`, toParams(query), `transactions-${accountId}.csv`);
  }

  private withETag<T>(response: HttpResponse<T>): WithETag<T> {
    return { body: response.body as T, etag: response.headers.get('ETag') };
  }

  private download(url: string, params: HttpParams, fallbackName: string): Observable<DownloadedFile> {
    return this.http.get(url, { params, responseType: 'blob', observe: 'response' }).pipe(
      map((response) => ({
        blob: response.body as Blob,
        fileName: fileNameFrom(response.headers.get('Content-Disposition'), fallbackName),
      })),
    );
  }
}

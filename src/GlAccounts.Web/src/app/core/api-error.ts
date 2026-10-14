import { HttpErrorResponse } from '@angular/common/http';
import { DeleteBlocker } from './models';

export type ApiErrorKind =
  | 'validation' // 400: field errors
  | 'notFound' // 404
  | 'duplicate' // 409 on create
  | 'staleVersion' // 409 on update (If-Match no longer matches)
  | 'deleteBlocked' // 409 on delete
  | 'server' // 5xx
  | 'network'; // status 0, the API could not be reached

export interface ApiError {
  kind: ApiErrorKind;
  status: number;
  message: string;
  /** Field name to messages, for 400 responses. Names are the API's camelCase fields. */
  fieldErrors: Record<string, string[]>;
  blockers: DeleteBlocker[];
  suggestion?: string;
}

interface ProblemBody {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  blockers?: DeleteBlocker[];
  suggestion?: string;
}

/** What a 409 means depends on the operation that produced it. */
export type ConflictMeaning = 'duplicate' | 'staleVersion';

/** Maps an HTTP failure to a typed error. Messages are the server's `detail`; nothing is invented. */
export function toApiError(error: unknown, conflict: ConflictMeaning = 'duplicate'): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return { kind: 'server', status: 0, message: 'Unexpected error.', fieldErrors: {}, blockers: [] };
  }

  const body = (typeof error.error === 'object' && error.error !== null ? error.error : {}) as ProblemBody;
  const message = body.detail ?? body.title ?? error.statusText ?? 'Request failed.';
  const base = {
    status: error.status,
    message,
    fieldErrors: body.errors ?? {},
    blockers: body.blockers ?? [],
    suggestion: body.suggestion,
  };

  if (error.status === 0) {
    return { ...base, kind: 'network', message: 'The service could not be reached. Check your connection and try again.' };
  }
  if (error.status === 400) return { ...base, kind: 'validation' };
  if (error.status === 404) return { ...base, kind: 'notFound' };
  if (error.status === 409) {
    return { ...base, kind: base.blockers.length > 0 ? 'deleteBlocked' : conflict };
  }
  return { ...base, kind: 'server' };
}

/** All field messages flattened, for an error summary. */
export function flattenFieldErrors(error: ApiError): { field: string; message: string }[] {
  return Object.entries(error.fieldErrors).flatMap(([field, messages]) => messages.map((message) => ({ field, message })));
}

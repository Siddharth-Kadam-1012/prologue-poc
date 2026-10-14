import { HttpErrorResponse } from '@angular/common/http';
import { flattenFieldErrors, toApiError } from './api-error';

function httpError(status: number, body: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, statusText: 'x', error: body });
}

describe('toApiError (NFR-GLA-006: errors shown after submit; stub data only)', () => {
  it('maps 400 to validation with the field errors', () => {
    const error = toApiError(httpError(400, { status: 400, errors: { accountId: ['Department 777 is not an allowed value'] } }));

    expect(error.kind).toBe('validation');
    expect(error.fieldErrors['accountId']).toEqual(['Department 777 is not an allowed value']);
  });

  it('maps 404 to notFound and keeps the server detail', () => {
    const error = toApiError(httpError(404, { status: 404, detail: "Account '9999-999-99' was not found." }));

    expect(error.kind).toBe('notFound');
    expect(error.message).toBe("Account '9999-999-99' was not found.");
  });

  it('maps a plain 409 to duplicate by default (AC-GLA-003)', () => {
    const error = toApiError(httpError(409, { status: 409, detail: "Account ID '1000-100-01' already exists. Please use a unique ID." }));

    expect(error.kind).toBe('duplicate');
    expect(error.message).toContain('already exists');
  });

  it('maps a 409 on update to staleVersion (AC-GLA-056)', () => {
    const error = toApiError(httpError(409, { status: 409, detail: 'changed by someone else' }), 'staleVersion');

    expect(error.kind).toBe('staleVersion');
  });

  it('maps a 409 with blockers to deleteBlocked and keeps counts and suggestion (AC-GLA-038)', () => {
    const error = toApiError(
      httpError(409, {
        status: 409,
        detail: "Cannot delete account '1000-200-01'. It has 10001 posted transaction(s).",
        blockers: [{ kind: 'PostedTransactions', count: 10001 }],
        suggestion: 'Set the status to Inactive instead.',
      }),
      'staleVersion',
    );

    expect(error.kind).toBe('deleteBlocked');
    expect(error.blockers).toEqual([{ kind: 'PostedTransactions', count: 10001 }]);
    expect(error.suggestion).toBe('Set the status to Inactive instead.');
  });

  it('maps status 0 to network and 5xx to server', () => {
    expect(toApiError(httpError(0, null)).kind).toBe('network');
    expect(toApiError(httpError(500, { status: 500, title: 'Boom' })).kind).toBe('server');
  });

  it('does not throw on an empty or non-object body', () => {
    const error = toApiError(httpError(400, 'not json'));

    expect(error.kind).toBe('validation');
    expect(error.fieldErrors).toEqual({});
  });

  it('returns a server error for something that is not an HTTP error', () => {
    expect(toApiError(new Error('x')).kind).toBe('server');
  });

  it('flattens field errors for the error summary', () => {
    const error = toApiError(httpError(400, { errors: { category: ['a', 'b'], currencyId: ['c'] } }));

    expect(flattenFieldErrors(error)).toEqual([
      { field: 'category', message: 'a' },
      { field: 'category', message: 'b' },
      { field: 'currencyId', message: 'c' },
    ]);
  });
});

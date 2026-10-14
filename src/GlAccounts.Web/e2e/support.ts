import AxeBuilder from '@axe-core/playwright';
import { APIRequestContext, Locator, Page, expect } from '@playwright/test';

/** WCAG 2.1 AA axe check on the populated state of the current screen. Any violation fails the test. */
export async function expectNoAxeViolations(page: Page): Promise<void> {
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
  const summary = results.violations.map((v) => `${v.id} (${v.impact}): ${v.nodes.map((n) => n.target.join(' ')).join(' | ')}`);
  expect(summary, 'axe violations').toEqual([]);
}

/**
 * The first Account ID that does not exist yet. Main segment 6000 is not used by the seed data, and the API
 * store is shared state across test files, so a free ID is probed instead of being hard-coded per test.
 */
export async function freeAccountId(request: APIRequestContext): Promise<string> {
  for (const dept of ['100', '200', '300', '400', '500']) {
    for (const sub of ['01', '02', '03', '04', '05']) {
      const id = `6000-${dept}-${sub}`;
      const response = await request.get(`/api/gl/accounts/${id}`);
      if (response.status() === 404) return id;
    }
  }
  throw new Error('No free 6000-xxx-xx Account ID left. Restart the API to reset the stub data.');
}

export interface CreatedAccount {
  accountId: string;
  etag: string;
}

/** Creates a Financial account through the API (not part of the flow under test) and returns its ID and ETag. */
export async function createAccountViaApi(request: APIRequestContext, description = 'E2E account'): Promise<CreatedAccount> {
  const accountId = await freeAccountId(request);
  const response = await request.post('/api/gl/accounts', {
    data: {
      accountId,
      description,
      accountType: 'Financial',
      category: 'Asset',
      currencyId: 'USD',
      status: 'Active',
      balanceType: 'Periodic',
      normalBalance: 'Debit',
    },
  });
  expect(response.status(), await response.text()).toBe(201);
  return { accountId, etag: response.headers()['etag'] ?? '' };
}

/** Opens a Material select by its element id and picks an option by its visible text. */
export async function chooseOption(page: Page, selectId: string, option: string): Promise<void> {
  await page.locator(`#${selectId}`).click();
  await page.getByRole('option', { name: option, exact: true }).click();
}

/** The live regions of the app shell (user-flows.md, Screen-reader announcements). */
export function politeRegion(page: Page): Locator {
  return page.locator('[aria-live="polite"]');
}

export function assertiveRegion(page: Page): Locator {
  return page.locator('[aria-live="assertive"]');
}

/** Waits until a screen has left its loading state. */
export async function waitForIdle(page: Page): Promise<void> {
  await expect(page.locator('[aria-busy="true"]')).toHaveCount(0);
}

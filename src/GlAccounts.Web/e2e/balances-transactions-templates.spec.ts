import { expect, test } from '@playwright/test';
import { chooseOption, expectNoAxeViolations, freeAccountId, waitForIdle } from './support';

// T020: Flows 6, 7 and 8 of ux/user-flows.md. Runs against the real API (stub data only).
// Covers AC-GLA-028, AC-GLA-029, AC-GLA-030, AC-GLA-031, AC-GLA-032, AC-GLA-033, AC-GLA-034, AC-GLA-035,
// AC-GLA-036, AC-GLA-037, AC-GLA-053, AC-GLA-055.

test.describe('Flow 6: review balances', () => {
  test('AC-GLA-028 AC-GLA-029 shows balances by period, by year and the budget versions; the screen passes axe', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/period-summary');
    await waitForIdle(page);

    await expect(page.locator('table[aria-labelledby="periods-heading"] tbody tr')).toHaveCount(15);
    await expect(page.locator('table[aria-labelledby="years-heading"] tbody tr')).toHaveCount(2);
    await expect(page.locator('table[aria-labelledby="budgets-heading"]')).toContainText('Revised');
    await expectNoAxeViolations(page);
  });

  test('AC-GLA-030 filters by fiscal year', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/period-summary');
    await waitForIdle(page);

    await page.getByRole('spinbutton', { name: 'Fiscal year' }).fill('2026');
    await page.getByRole('button', { name: 'Apply' }).click();
    await waitForIdle(page);

    const rows = page.locator('table[aria-labelledby="periods-heading"] tbody tr');
    await expect(rows).toHaveCount(3);
    for (const text of await rows.allTextContents()) expect(text).toContain('2026');
  });

  test('AC-GLA-030 sorts by balance in both directions and exposes the order with aria-sort', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/period-summary');
    await waitForIdle(page);
    const balanceHeader = page.getByRole('columnheader', { name: /Balance/ }).first();
    await expect(balanceHeader).toHaveAttribute('aria-sort', 'none');

    const balances = async () =>
      (await page.locator('table[aria-labelledby="periods-heading"] tbody tr td:nth-child(3)').allTextContents()).map((t) =>
        Number(t.replace(/,/g, '')),
      );

    await balanceHeader.getByRole('button').click();
    await waitForIdle(page);
    await expect(balanceHeader).toHaveAttribute('aria-sort', 'ascending');
    const ascending = await balances();
    expect(ascending).toEqual([...ascending].sort((a, b) => a - b));

    await balanceHeader.getByRole('button').click();
    await waitForIdle(page);
    await expect(balanceHeader).toHaveAttribute('aria-sort', 'descending');
    const descending = await balances();
    expect(descending).toEqual([...descending].sort((a, b) => b - a));
  });

  test('AC-GLA-031 exports to Excel with the current filter', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/period-summary');
    await waitForIdle(page);
    await page.getByRole('spinbutton', { name: 'Fiscal year' }).fill('2025');
    await page.getByRole('button', { name: 'Apply' }).click();
    await waitForIdle(page);

    const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('button', { name: 'Export Excel' }).click()]);
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/);
  });

  test('AC-GLA-032 an account without balances shows the empty state', async ({ page }) => {
    await page.goto('/gl/accounts/8000-100-01/period-summary');
    await expect(page.getByText('No balances for this account.')).toBeVisible();
  });

  test('AC-GLA-032 an unknown account shows a not-found state', async ({ page }) => {
    await page.goto('/gl/accounts/0000-000-00/period-summary');
    await expect(page.getByRole('link', { name: 'Search accounts' })).toBeVisible();
  });
});

test.describe('Flow 7: review transactions', () => {
  test('AC-GLA-033 AC-GLA-037 shows the first 50 of 10,001 entries and a way to reach the rest; the screen passes axe', async ({ page }) => {
    await page.goto('/gl/accounts/1000-200-01/transactions');
    await waitForIdle(page);

    await expect(page.locator('table[aria-label="Transactions"] tbody tr')).toHaveCount(50);
    await expect(page.locator('.count')).toContainText('Showing 1 to 50 of 10,001 entries');
    await expect(page.locator('mat-paginator')).toBeVisible();
    await expectNoAxeViolations(page);
  });

  test('AC-GLA-037 pages forward with the paginator', async ({ page }) => {
    await page.goto('/gl/accounts/1000-200-01/transactions');
    await waitForIdle(page);

    await page.getByRole('button', { name: 'Next page' }).click();
    await waitForIdle(page);
    await expect(page.locator('.count')).toContainText('Showing 51 to 100 of 10,001 entries');
  });

  test('AC-GLA-037 the page size can be raised to 200', async ({ page }) => {
    await page.goto('/gl/accounts/1000-200-01/transactions');
    await waitForIdle(page);

    // Opened by keyboard: Material's touch-target overlay intercepts pointer clicks in Playwright.
    await page.getByRole('combobox', { name: /Items per page/ }).focus();
    await page.keyboard.press('Enter');
    await page.getByRole('option', { name: '200', exact: true }).click();
    await waitForIdle(page);
    await expect(page.locator('table[aria-label="Transactions"] tbody tr')).toHaveCount(200);
    await expect(page.locator('.count')).toContainText('of 10,001 entries');
  });

  test('AC-GLA-037 an account with no transactions shows the empty state', async ({ page }) => {
    await page.goto('/gl/accounts/8000-100-01/transactions');
    await expect(page.getByText('No transactions for this account or filter.')).toBeVisible();
  });

  test('AC-GLA-034 filters by fiscal period', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/transactions');
    await waitForIdle(page);
    const total = await page.locator('.count').textContent();

    await page.getByRole('spinbutton', { name: 'Fiscal year' }).fill('2026');
    await page.getByLabel('Fiscal period').fill('1');
    await page.getByRole('button', { name: 'Apply' }).click();
    await waitForIdle(page);

    await expect(page.locator('.count')).not.toHaveText(total!);
    for (const text of await page.locator('table[aria-label="Transactions"] tbody tr td:nth-child(5)').allTextContents()) {
      expect(text.startsWith('2026-')).toBe(true);
    }
  });

  test('the date range and the fiscal period cannot be combined: filling one disables the other', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/transactions');
    await waitForIdle(page);

    await page.getByLabel('From date').fill('2025-01-01');
    await expect(page.getByRole('spinbutton', { name: 'Fiscal year' })).toBeDisabled();
    await expect(page.getByLabel('Fiscal period')).toBeDisabled();
    await page.getByRole('button', { name: 'Clear filter' }).click();
    await waitForIdle(page);
    await expect(page.getByRole('spinbutton', { name: 'Fiscal year' })).toBeEnabled();

    await page.getByRole('spinbutton', { name: 'Fiscal year' }).fill('2025');
    await expect(page.getByLabel('From date')).toBeDisabled();
    await expect(page.getByLabel('To date')).toBeDisabled();
  });

  test('a fiscal period without a fiscal year is refused inline', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/transactions');
    await waitForIdle(page);
    await page.getByLabel('Fiscal period').fill('3');
    await page.getByRole('button', { name: 'Apply' }).click();

    await expect(page.locator('.filter-error')).toHaveAttribute('role', 'alert');
    await expect(page.locator('.filter-error')).toContainText('fiscal year');
  });

  test('AC-GLA-035 opens the journal entry detail with its lines, reachable by keyboard', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/transactions');
    await waitForIdle(page);

    const first = page.locator('table[aria-label="Transactions"] tbody tr').first().getByRole('button', { name: /View journal entry/ });
    await first.focus();
    await page.keyboard.press('Enter');

    const detail = page.locator('.detail');
    await expect(detail).toBeVisible();
    await expect(detail.locator('tbody tr')).toHaveCount(2);
    await expect(detail.getByRole('heading', { level: 2 })).toBeFocused();
    await expectNoAxeViolations(page);

    await detail.getByRole('button', { name: 'Close detail' }).click();
    await expect(detail).toHaveCount(0);
  });

  test('AC-GLA-036 exports the filtered set as CSV', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01/transactions');
    await waitForIdle(page);
    await page.getByRole('spinbutton', { name: 'Fiscal year' }).fill('2025');
    await page.getByRole('button', { name: 'Apply' }).click();
    await waitForIdle(page);

    const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('button', { name: 'Export CSV' }).click()]);
    expect(download.suggestedFilename()).toMatch(/\.csv$/);
  });
});

test.describe('Flow 8: template library', () => {
  test('AC-GLA-055 lists the template and filters by search text; the screen passes axe', async ({ page }) => {
    await page.goto('/gl/account-templates');
    await waitForIdle(page);

    await expect(page.getByRole('heading', { level: 1, name: 'Account templates' })).toBeVisible();
    await expect(page.getByRole('table', { name: 'Templates' }).locator('tbody tr')).toHaveCount(1);
    await expect(page.getByRole('table', { name: 'Templates' })).toContainText('9000-100-01');
    await expectNoAxeViolations(page);

    await page.getByRole('searchbox', { name: 'Search' }).fill('no-such-template');
    await page.getByRole('button', { name: 'Search' }).click();
    await expect(page.getByText('No templates match your search.')).toBeVisible();
  });

  test('AC-GLA-053 creates an account from the template with its settings copied, and leaves the template unchanged', async ({
    page,
    request,
  }) => {
    const accountId = await freeAccountId(request);
    await page.goto('/gl/account-templates');
    await waitForIdle(page);

    await page.getByRole('button', { name: 'Create account from template 9000-100-01' }).click();
    await expect(page.getByRole('heading', { level: 2, name: 'Create account from template 9000-100-01' })).toBeFocused();
    await expect(page.locator('#tpl-field-description')).toHaveValue('Standard expense template');
    await expectNoAxeViolations(page);

    await page.locator('#tpl-field-accountId').fill(accountId);
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByText('Standard expense template').first()).toBeVisible();
    const template = await (await request.get('/api/gl/accounts/9000-100-01')).json();
    expect(template.status).toBe('Template');
  });

  test('AC-GLA-003 a duplicate Account ID is shown at the field', async ({ page }) => {
    await page.goto('/gl/account-templates');
    await waitForIdle(page);
    await page.getByRole('button', { name: 'Create account from template 9000-100-01' }).click();
    await page.locator('#tpl-field-accountId').fill('1000-100-01');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.locator('app-create-from-template mat-error').first()).toContainText("Account ID '1000-100-01' already exists");
  });

  test('AC-GLA-002 an invalid Account ID is refused with the segment message', async ({ page }) => {
    await page.goto('/gl/account-templates');
    await waitForIdle(page);
    await page.getByRole('button', { name: 'Create account from template 9000-100-01' }).click();
    await page.locator('#tpl-field-accountId').fill('abc');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.locator('app-create-from-template .error-summary')).toContainText('Account ID does not match segment format');
  });

  test('Cancel closes the panel and returns focus to the row action', async ({ page }) => {
    await page.goto('/gl/account-templates');
    await waitForIdle(page);
    const action = page.getByRole('button', { name: 'Create account from template 9000-100-01' });
    await action.click();
    await page.getByRole('button', { name: 'Cancel' }).click();

    await expect(page.locator('app-create-from-template')).toHaveCount(0);
    await expect(action).toBeFocused();
  });

  test('AC-GLA-052 a Template account created from the form is listed in the library', async ({ page, request }) => {
    const accountId = await freeAccountId(request);
    await page.goto('/gl/accounts/new');
    await page.locator('#field-accountId').fill(accountId);
    await page.locator('#field-description').fill('E2E template');
    await chooseOption(page, 'field-category', 'Asset');
    await chooseOption(page, 'field-status', 'Template');
    await chooseOption(page, 'field-balanceType', 'Periodic');
    await chooseOption(page, 'field-normalBalance', 'Debit');
    await page.getByRole('button', { name: 'Save' }).click();
    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByText('Not eligible for new transactions: the account status is Template')).toBeVisible();

    await page.goto('/gl/account-templates');
    await waitForIdle(page);
    await expect(page.getByRole('table', { name: 'Templates' })).toContainText(accountId);
  });
});

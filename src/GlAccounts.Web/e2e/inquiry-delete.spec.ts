import { expect, test } from '@playwright/test';
import { createAccountViaApi, expectNoAxeViolations, waitForIdle } from './support';

// T020: Flows 3 and 5 of ux/user-flows.md and the keyboard path of the delete dialog.
// Runs against the real API (stub data only). Covers AC-GLA-004, AC-GLA-005, AC-GLA-013, AC-GLA-015,
// AC-GLA-020, AC-GLA-038, AC-GLA-039, AC-GLA-040, AC-GLA-042.

test.beforeEach(async ({ page }) => {
  await page.goto('/gl/accounts');
  await waitForIdle(page);
});

test.describe('Entry page: search', () => {
  test('lists accounts, searches by text and links each Account ID to its inquiry page; the screen passes axe', async ({ page }) => {
    await expect(page.getByRole('heading', { level: 1, name: 'Accounts' })).toBeVisible();
    await expect(page.getByRole('table', { name: 'Accounts' })).toContainText('1000-100-01');
    await expectNoAxeViolations(page);

    await page.getByRole('searchbox', { name: 'Search' }).fill('Cash and equivalents');
    await page.getByRole('button', { name: 'Search' }).click();
    await waitForIdle(page);
    await expect(page.getByRole('table', { name: 'Accounts' }).locator('tbody tr')).toHaveCount(1);
    await page.getByRole('link', { name: '1000-100-01' }).click();
    await expect(page).toHaveURL(/\/gl\/accounts\/1000-100-01$/);
  });

  test('shows an empty state when nothing matches', async ({ page }) => {
    await page.getByRole('searchbox', { name: 'Search' }).fill('zzzz-no-such-account');
    await page.getByRole('button', { name: 'Search' }).click();
    await expect(page.getByText('No accounts match your search.')).toBeVisible();
  });
});

test.describe('Flow 3: inquire on an account', () => {
  test('AC-GLA-004 AC-GLA-015 shows details, segments, attributes and groups; the screen passes axe', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01');
    await expect(page.getByRole('heading', { level: 1, name: '1000-100-01' })).toBeVisible();
    await expect(page.getByText('Cash and equivalents').first()).toBeVisible();
    await expect(page.getByRole('table', { name: 'Attributes' })).toContainText('dept');
    await expect(page.getByRole('table', { name: 'Account groups' })).toBeVisible();
    await expect(page.getByText('Eligible for new transactions')).toBeVisible();
    await expectNoAxeViolations(page);
  });

  test('AC-GLA-020 a Hold account says in text that it is not eligible for new transactions', async ({ page }) => {
    await page.goto('/gl/accounts/4000-100-01');
    await expect(page.getByText('Not eligible for new transactions: the account status is Hold')).toBeVisible();
  });

  test('AC-GLA-013 shows an empty groups state for an account in no group', async ({ page }) => {
    await page.goto('/gl/accounts/8000-100-01');
    await expect(page.getByText('This account is in no group.')).toBeVisible();
    await expect(page.getByText('This account has no attributes.')).toBeVisible();
  });

  test('AC-GLA-013 exports the groups as CSV', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01');
    await expect(page.getByRole('table', { name: 'Account groups' })).toBeVisible();
    const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('button', { name: 'Export CSV' }).click()]);
    expect(download.suggestedFilename()).toMatch(/\.csv$/);
  });

  test('AC-GLA-005 an unknown account shows a not-found state with a link to search', async ({ page }) => {
    await page.goto('/gl/accounts/0000-000-00');
    await expect(page.getByRole('heading', { level: 1, name: 'Account not found' })).toBeVisible();
    await page.getByRole('link', { name: 'Search accounts' }).click();
    await expect(page).toHaveURL(/\/gl\/accounts$/);
  });

  test('every linked screen is one step away (SC-005)', async ({ page }) => {
    await page.goto('/gl/accounts/1000-100-01');
    await page.getByRole('link', { name: 'Edit' }).click();
    await expect(page).toHaveURL(/\/1000-100-01\/edit$/);
    await page.goBack();
    await page.getByRole('link', { name: 'Period summary' }).click();
    await expect(page).toHaveURL(/\/1000-100-01\/period-summary$/);
    await page.goBack();
    await page.getByRole('link', { name: 'Transactions' }).click();
    await expect(page).toHaveURL(/\/1000-100-01\/transactions$/);
  });
});

test.describe('Flow 5: delete an account', () => {
  test('AC-GLA-038 deletes an account that has no dependencies and lands on search with a confirmation', async ({ page, request }) => {
    const { accountId } = await createAccountViaApi(request, 'To be deleted');
    await page.goto(`/gl/accounts/${accountId}`);

    await page.getByRole('button', { name: 'Delete' }).click();
    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toContainText(`Delete account ${accountId}?`);
    await expectNoAxeViolations(page);
    await dialog.getByRole('button', { name: 'Delete' }).click();

    await expect(page).toHaveURL(/\/gl\/accounts$/);
    await expect(page.getByText(`Account ${accountId} deleted.`).first()).toBeVisible();
    const gone = await request.get(`/api/gl/accounts/${accountId}`);
    expect(gone.status()).toBe(404);
  });

  test('AC-GLA-039 Cancel leaves the account untouched', async ({ page, request }) => {
    const { accountId } = await createAccountViaApi(request);
    await page.goto(`/gl/accounts/${accountId}`);
    await page.getByRole('button', { name: 'Delete' }).click();
    await page.getByRole('alertdialog').getByRole('button', { name: 'Cancel' }).click();

    await expect(page.getByRole('alertdialog')).toHaveCount(0);
    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    expect((await request.get(`/api/gl/accounts/${accountId}`)).status()).toBe(200);
  });

  test('AC-GLA-040 AC-GLA-042 a blocked delete lists every blocker with its count and offers Inactive instead', async ({ page }) => {
    await page.goto('/gl/accounts/3000-100-01');
    await page.getByRole('button', { name: 'Delete' }).click();
    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toContainText('Delete account 3000-100-01?');
    await dialog.getByRole('button', { name: 'Delete' }).click();

    const items = dialog.locator('.blocker-list li');
    expect(await items.count()).toBeGreaterThan(1);
    await expect(dialog).toContainText('posted transaction(s)');
    await expectNoAxeViolations(page);

    await dialog.getByRole('button', { name: 'Set status to Inactive instead' }).click();
    await expect(page).toHaveURL(/\/gl\/accounts\/3000-100-01\/edit\?status=Inactive$/);
    await expect(page.locator('#field-statusJustification')).toBeVisible();
    await expect(page.locator('#field-status')).toContainText('Inactive');
  });

  test('AC-GLA-040 a large transaction history is reported in the blocker list', async ({ page }) => {
    await page.goto('/gl/accounts/1000-200-01');
    await page.getByRole('button', { name: 'Delete' }).click();
    await expect(page.getByRole('alertdialog')).toContainText('Delete account 1000-200-01?');
    await page.getByRole('alertdialog').getByRole('button', { name: 'Delete' }).click();

    await expect(page.getByRole('alertdialog')).toContainText('10001 posted transaction(s)');
  });
});

test.describe('Keyboard path of the delete dialog', () => {
  test('the dialog traps focus, Escape closes it, and focus returns to the Delete button', async ({ page, request }) => {
    const { accountId } = await createAccountViaApi(request);
    await page.goto(`/gl/accounts/${accountId}`);
    const trigger = page.getByRole('button', { name: 'Delete' });
    await trigger.focus();
    await page.keyboard.press('Enter');

    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toBeVisible();
    for (let i = 0; i < 4; i++) {
      await page.keyboard.press('Tab');
      const inside = await page.evaluate(() => !!document.activeElement?.closest('[role="alertdialog"]'));
      expect(inside, `Tab ${i + 1} stays inside the dialog`).toBe(true);
    }

    await page.keyboard.press('Escape');
    await expect(dialog).toHaveCount(0);
    await expect(trigger).toBeFocused();
  });
});

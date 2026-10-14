import { expect, test } from '@playwright/test';
import { chooseOption, createAccountViaApi, expectNoAxeViolations, freeAccountId, waitForIdle } from './support';

// T020: Flows 1, 2 and 4 of ux/user-flows.md, the keyboard path of the form, and the 409 stale-version path.
// Runs against the real API (stub data only). Covers AC-GLA-001, AC-GLA-002, AC-GLA-003, AC-GLA-006,
// AC-GLA-014, AC-GLA-017, AC-GLA-022, AC-GLA-023, AC-GLA-043, AC-GLA-056.

async function activeId(page: import('@playwright/test').Page): Promise<string> {
  return page.evaluate(() => {
    const el = document.activeElement as HTMLElement | null;
    return el?.id || el?.getAttribute('aria-label') || el?.textContent?.trim() || el?.tagName || '';
  });
}

test.describe('Flow 1: create an account', () => {
  test('AC-GLA-001 creates an account and shows it on the inquiry page; the form passes axe', async ({ page, request }) => {
    const accountId = await freeAccountId(request);
    await page.goto('/gl/accounts/new');
    await expect(page.getByRole('heading', { level: 1, name: 'New account' })).toBeVisible();
    await expect(page.getByText('9999-999-99').first()).toBeVisible();
    await expectNoAxeViolations(page);

    await page.locator('#field-accountId').fill(accountId);
    await page.locator('#field-description').fill('E2E created account');
    await chooseOption(page, 'field-category', 'Asset');
    await chooseOption(page, 'field-balanceType', 'Periodic');
    await chooseOption(page, 'field-normalBalance', 'Debit');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByRole('heading', { level: 1, name: accountId })).toBeVisible();
    await expect(page.getByText('E2E created account').first()).toBeVisible();
  });

  test('AC-GLA-043 an empty form shows an alert summary with a message per field and sends nothing', async ({ page }) => {
    await page.goto('/gl/accounts/new');
    await page.getByRole('button', { name: 'Save' }).click();

    const summary = page.locator('.error-summary');
    await expect(summary).toHaveAttribute('role', 'alert');
    await expect(summary).toContainText('Account ID is required.');
    await expect(summary).toContainText('Description is required.');
    await expect(summary).toContainText('Category is required.');
    await expect(page).toHaveURL(/\/gl\/accounts\/new$/);
    // The first problem is announced and focus moves to the summary.
    await expect(summary).toBeFocused();
  });

  test('AC-GLA-002 an Account ID with a segment that is not allowed is refused at the Account ID field', async ({ page }) => {
    await page.goto('/gl/accounts/new');
    await page.locator('#field-accountId').fill('9999-999-99');
    await page.locator('#field-description').fill('Bad segments');
    await chooseOption(page, 'field-category', 'Asset');
    await chooseOption(page, 'field-balanceType', 'Periodic');
    await chooseOption(page, 'field-normalBalance', 'Debit');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.locator('mat-error').first()).toContainText('Account ID does not match segment format');
    await expect(page.locator('.error-summary')).toContainText('Expected format: 9999-999-99');
    await expect(page).toHaveURL(/\/gl\/accounts\/new$/);
    await expect(page.locator('#field-description')).toHaveValue('Bad segments');
  });

  test('AC-GLA-003 a duplicate Account ID is shown at the field with a link to the existing account', async ({ page }) => {
    await page.goto('/gl/accounts/new');
    await page.locator('#field-accountId').fill('1000-100-01');
    await page.locator('#field-description').fill('Duplicate attempt');
    await chooseOption(page, 'field-category', 'Asset');
    await chooseOption(page, 'field-balanceType', 'Periodic');
    await chooseOption(page, 'field-normalBalance', 'Debit');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.locator('mat-error').first()).toContainText("Account ID '1000-100-01' already exists");
    await page.getByRole('link', { name: 'View existing account 1000-100-01' }).click();
    await expect(page).toHaveURL(/\/gl\/accounts\/1000-100-01$/);
  });

  test('a Statistical account has no balance type or normal balance and a fixed category', async ({ page }) => {
    await page.goto('/gl/accounts/new');
    await chooseOption(page, 'field-accountType', 'Statistical');

    await expect(page.locator('#field-balanceType')).toHaveCount(0);
    await expect(page.locator('#field-normalBalance')).toHaveCount(0);
    await page.locator('#field-category').click();
    await expect(page.getByRole('option')).toHaveText(['Statistical']);
  });
});

test.describe('Keyboard path of the form', () => {
  test('Tab follows the visual order and Enter in a text field does not submit', async ({ page }) => {
    await page.goto('/gl/accounts/new');
    await page.locator('#field-accountId').focus();

    const order: string[] = [await activeId(page)];
    for (let i = 0; i < 10; i++) {
      await page.keyboard.press('Tab');
      order.push(await activeId(page));
    }
    expect(order).toEqual([
      'field-accountId',
      'field-accountType',
      'field-description',
      'field-category',
      'field-currencyId',
      'field-status',
      'field-balanceType',
      'field-normalBalance',
      'Add attribute',
      'Save',
      'Cancel',
    ]);

    await page.locator('#field-description').fill('Enter must not submit');
    await page.locator('#field-description').press('Enter');
    await expect(page).toHaveURL(/\/gl\/accounts\/new$/);
    await expect(page.locator('.error-summary')).toHaveCount(0);
  });

  test('a complete account can be created with the keyboard alone', async ({ page, request }) => {
    const accountId = await freeAccountId(request);
    await page.goto('/gl/accounts/new');
    await page.locator('#field-accountId').focus();
    await page.keyboard.type(accountId);
    await page.keyboard.press('Tab'); // account type, stays Financial
    await page.keyboard.press('Tab');
    await page.keyboard.type('Keyboard only account');
    await page.keyboard.press('Tab'); // category: ArrowDown on a closed select picks the first option
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Tab'); // currency
    await page.keyboard.press('Tab'); // status
    await page.keyboard.press('Tab'); // balance type
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Tab'); // normal balance
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Tab'); // Add attribute
    await page.keyboard.press('Tab'); // Save
    await expect(page.getByRole('button', { name: 'Save' })).toBeFocused();
    await page.keyboard.press('Enter');

    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByText('Keyboard only account').first()).toBeVisible();
  });
});

test.describe('Flow 2: edit an account', () => {
  test('AC-GLA-006 AC-GLA-008 Account ID and type are read-only, a changed description is saved; the form passes axe', async ({ page, request }) => {
    const { accountId } = await createAccountViaApi(request, 'Before edit');
    await page.goto(`/gl/accounts/${accountId}/edit`);
    await expect(page.getByRole('heading', { level: 1, name: `Edit account ${accountId}` })).toBeVisible();
    await expect(page.locator('#field-accountId')).toHaveCount(0);
    await expect(page.locator('#field-accountType')).toHaveCount(0);
    await expect(page.locator('#field-description')).toHaveValue('Before edit');
    await expectNoAxeViolations(page);

    await page.locator('#field-description').fill('After edit');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByText('After edit').first()).toBeVisible();
  });

  test('AC-GLA-022 AC-GLA-023 a status change needs a justification and is then saved', async ({ page, request }) => {
    const { accountId } = await createAccountViaApi(request);
    await page.goto(`/gl/accounts/${accountId}/edit`);
    await expect(page.locator('#field-statusJustification')).toHaveCount(0);

    await chooseOption(page, 'field-status', 'Hold');
    await expect(page.locator('#field-statusJustification')).toBeVisible();
    await page.getByRole('button', { name: 'Save' }).click();
    await expect(page.locator('.error-summary')).toContainText('Justification is required.');
    await expect(page).toHaveURL(/\/edit$/);

    await page.locator('#field-statusJustification').fill('Audit review');
    await page.getByRole('button', { name: 'Save' }).click();
    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByText('Not eligible for new transactions: the account status is Hold')).toBeVisible();
  });

  test('a missing account shows a not-found state with a link to search', async ({ page }) => {
    await page.goto('/gl/accounts/0000-000-00/edit');
    await expect(page.getByRole('heading', { level: 1, name: 'Account not found' })).toBeVisible();
    await page.getByRole('link', { name: 'Search accounts' }).click();
    await expect(page).toHaveURL(/\/gl\/accounts$/);
  });
});

test.describe('Stale version (409)', () => {
  test('AC-GLA-056 a save based on an older version is rejected, the input is kept, and Reload lets the user save again', async ({
    page,
    request,
  }) => {
    const { accountId, etag } = await createAccountViaApi(request, 'Original');
    await page.goto(`/gl/accounts/${accountId}/edit`);
    await expect(page.locator('#field-description')).toHaveValue('Original');

    // Another user saves first, with the same ETag the page loaded.
    const other = await request.put(`/api/gl/accounts/${accountId}`, {
      headers: { 'If-Match': etag },
      data: { description: 'Changed by someone else', category: 'Asset', currencyId: 'USD', status: 'Active' },
    });
    expect(other.status()).toBe(200);

    await page.locator('#field-description').fill('My change');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText('This account was changed by someone else.')).toBeVisible();
    await expect(page.locator('#field-description')).toHaveValue('My change');
    await expect(page).toHaveURL(/\/edit$/);

    await page.getByRole('button', { name: 'Reload latest version' }).click();
    await expect(page.locator('.notice')).toContainText('Your changes are kept');
    await expect(page.locator('#field-description')).toHaveValue('My change');

    await page.getByRole('button', { name: 'Save' }).click();
    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByText('My change').first()).toBeVisible();
  });
});

test.describe('Flow 4: manage attributes', () => {
  test('AC-GLA-014 an attribute is added and saved at once on a saved account (immediate mode); the panel passes axe', async ({
    page,
    request,
  }) => {
    const { accountId } = await createAccountViaApi(request);
    await page.goto(`/gl/accounts/${accountId}/edit`);
    await expect(page.getByText('No attributes yet.')).toBeVisible();

    await page.getByRole('button', { name: 'Add attribute' }).click();
    await page.getByLabel('Attribute type').selectOption('region');
    await page.getByLabel('Attribute value').selectOption('emea');
    await expectNoAxeViolations(page);
    await page.getByRole('button', { name: 'Save row' }).click();

    await expect(page.locator('tbody tr').first()).toContainText('Stored');
    await page.goto(`/gl/accounts/${accountId}`);
    await expect(page.getByRole('table', { name: 'Attributes' })).toContainText('region');
    await expect(page.getByRole('table', { name: 'Attributes' })).toContainText('emea');
  });

  test('AC-GLA-017 staged attribute changes are applied together with the account Save', async ({ page, request }) => {
    const { accountId } = await createAccountViaApi(request);
    await page.goto(`/gl/accounts/${accountId}/edit`);
    await page.getByRole('checkbox', { name: /Apply each attribute change immediately/ }).uncheck();

    await page.getByRole('button', { name: 'Add attribute' }).click();
    await page.getByLabel('Attribute type').selectOption('dept');
    await page.getByLabel('Attribute value').selectOption('sales');
    await expect(page.locator('tbody tr').first()).toContainText('New (staged)');
    await page.getByRole('button', { name: 'Save', exact: true }).click();

    await expect(page).toHaveURL(new RegExp(`/gl/accounts/${accountId}$`));
    await expect(page.getByRole('table', { name: 'Attributes' })).toContainText('sales');
  });

  test('AC-GLA-016 a row with no type or value is refused with a message linked to the field', async ({ page, request }) => {
    const { accountId } = await createAccountViaApi(request);
    await page.goto(`/gl/accounts/${accountId}/edit`);
    await page.getByRole('button', { name: 'Add attribute' }).click();
    await page.getByRole('button', { name: 'Save row' }).click();

    const message = page.locator('.row-error');
    await expect(message).toContainText('Choose an attribute type and a value.');
    const id = await message.getAttribute('id');
    await expect(page.getByLabel('Attribute value')).toHaveAttribute('aria-describedby', id!);
  });
});

test.beforeEach(async ({ page }) => {
  await page.goto('/');
  await waitForIdle(page);
});

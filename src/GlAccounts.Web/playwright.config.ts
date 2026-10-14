import { defineConfig } from '@playwright/test';

// T020: browser tests against the real API (stub data, a fresh process per run) and the Angular dev server,
// which proxies /api to http://localhost:5000 (proxy.conf.json). Run with `npm run e2e`.
const API_URL = 'http://localhost:5000';
const WEB_URL = 'http://localhost:4200';

export default defineConfig({
  testDir: './e2e',
  // The API store is shared in-memory state, so tests run one at a time and each uses its own account.
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  timeout: 30_000,
  expect: { timeout: 7_000 },
  use: {
    baseURL: WEB_URL,
    trace: 'retain-on-failure',
    // The Edge browser on this machine is used, so no browser download is needed.
    channel: process.env['E2E_CHANNEL'] ?? 'msedge',
  },
  webServer: [
    {
      // reuseExistingServer is off: a leftover process would carry state from an earlier run.
      command: 'dotnet run --project ../GlAccounts.Api --launch-profile http --no-build',
      url: `${API_URL}/api/gl/accounts`,
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command: 'npm run start -- --port 4200',
      url: WEB_URL,
      reuseExistingServer: false,
      timeout: 180_000,
    },
  ],
});

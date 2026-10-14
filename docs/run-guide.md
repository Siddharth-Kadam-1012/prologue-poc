# Run Guide — Restore, Build, Test, Run

How to restore, build, test and run the GL Accounts solution (.NET API + Angular UI) on Windows/PowerShell.
All commands run from the repository root unless a `cd` is shown.

## 1. Prerequisites

| Tool | Needed for | Check |
|---|---|---|
| .NET SDK 10 (`net10.0`) | API, .NET tests, `scripts/sdlc/sdlc.cs` | `dotnet --version` |
| Node.js + npm (`npm@11.x`) | Angular UI, UI unit tests, Playwright | `node -v; npm -v` |
| Microsoft Edge | Playwright browser tests (config uses `channel: 'msedge'`, no browser download) | — |

Layout:

| Path | What |
|---|---|
| `GlAccounts.sln` | Solution: API + unit tests + integration tests |
| `src/GlAccounts.Api` | ASP.NET Core API, stub in-memory data, `http://localhost:5000` |
| `src/GlAccounts.Web` | Angular UI, dev server `http://localhost:4200` (not part of the `.sln`) |
| `tests/GlAccounts.Api.UnitTests` | .NET unit tests |
| `tests/GlAccounts.Api.IntegrationTests` | .NET integration tests |
| `src/GlAccounts.Web/e2e` | Playwright browser tests (config: `playwright.config.ts`) |

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, so any compiler warning fails the build.

## 2. Restore

```powershell
# .NET (API + tests)
dotnet restore GlAccounts.sln

# Angular UI
cd src/GlAccounts.Web
npm ci          # use `npm install` if package-lock.json should be updated
cd ../..
```

## 3. Build

```powershell
# .NET — matches the gate command used by the SDLC scripts
dotnet build GlAccounts.sln --no-restore -warnaserror

# Angular UI (production build by default; output in src/GlAccounts.Web/dist)
cd src/GlAccounts.Web
npm run build
# development build instead:
npx ng build --configuration development
cd ../..
```

## 4. Test

### 4.1 .NET unit + integration tests

```powershell
dotnet test GlAccounts.sln --no-build

# one project only
dotnet test tests/GlAccounts.Api.UnitTests
dotnet test tests/GlAccounts.Api.IntegrationTests

# filter by name
dotnet test GlAccounts.sln --no-build --filter "FullyQualifiedName~Account"
```

### 4.2 Angular unit tests (Vitest runner via `ng test`)

```powershell
cd src/GlAccounts.Web
npm test                          # watch mode
npx ng test --no-watch            # single run (CI style)
cd ../..
```

### 4.3 Playwright browser tests (UI + API, includes accessibility via axe)

Playwright starts both servers itself (see `playwright.config.ts`):

- API: `dotnet run --project ../GlAccounts.Api --launch-profile http --no-build` → `http://localhost:5000`
- UI: `npm run start -- --port 4200` → `http://localhost:4200`

`reuseExistingServer` is **false**, so ports 5000 and 4200 must be free (stop any running API/UI first).
The API uses `--no-build`, so build the .NET solution first (section 3).

```powershell
dotnet build GlAccounts.sln --no-restore -warnaserror
cd src/GlAccounts.Web
npm run e2e                        # headless, uses Edge
npx playwright test --headed       # watch the browser
npx playwright test --ui           # Playwright UI mode
npx playwright show-trace test-results/<folder>/trace.zip   # trace of a failed test
$env:E2E_CHANNEL = 'chrome'; npm run e2e                    # use Chrome instead of Edge
cd ../..
```

Tests run serially (one worker) because the API store is shared in-memory state.

## 5. Run the application

Use two terminals.

**Terminal 1 — API** (`http://localhost:5000`, Development environment):

```powershell
dotnet run --project src/GlAccounts.Api --launch-profile http
```

**Terminal 2 — UI** (`http://localhost:4200`):

```powershell
cd src/GlAccounts.Web
npm start
```

Open <http://localhost:4200>. The dev server proxies `/api` to `http://localhost:5000`
(`src/GlAccounts.Web/proxy.conf.json`), so no CORS setup is needed. Quick API check:

```powershell
Invoke-RestMethod http://localhost:5000/api/gl/accounts
```

Data is deterministic synthetic stub data held in memory; it resets whenever the API process restarts.
There is no database and no authentication in this phase.

## 6. SDLC gates (optional)

```powershell
dotnet run scripts/sdlc/sdlc.cs -- help
dotnet run scripts/sdlc/sdlc.cs -- status
dotnet run scripts/sdlc/sdlc.cs -- gate specs/001-gl-account-maintenance contract
dotnet run scripts/sdlc/sdlc.cs -- evidence specs/001-gl-account-maintenance build dotnet build --no-restore -warnaserror
dotnet run scripts/sdlc/sdlc.cs -- secrets .
```

The first run restores NuGet packages and compiles, so it is slow; later runs are cached.
See [`usage-guide.md`](usage-guide.md) for the full workflow.

## 7. Full verification sequence

```powershell
dotnet restore GlAccounts.sln
dotnet build GlAccounts.sln --no-restore -warnaserror
dotnet test GlAccounts.sln --no-build

cd src/GlAccounts.Web
npm ci
npm run build
npx ng test --no-watch
npm run e2e
cd ../..
```

## 8. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Port 5000/4200 already in use` during `npm run e2e` | A dev server is still running; stop it (`Get-NetTCPConnection -LocalPort 5000,4200`) |
| `e2e` fails starting the API with `--no-build` | Build the solution first: `dotnet build GlAccounts.sln` |
| Playwright cannot find the browser | Install Edge, or set `E2E_CHANNEL` to `chrome`, or run `npx playwright install chromium` and set `E2E_CHANNEL` to an empty/valid channel |
| Build fails on a warning | `TreatWarningsAsErrors` is on; fix the warning |
| UI shows API/network errors | API is not running on `http://localhost:5000`, or the UI was not started via `npm start` (proxy not applied) |
| Data changed between runs | Stub data is in-memory; restart the API to reset |

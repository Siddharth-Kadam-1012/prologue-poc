# Test plan: 001-gl-account-maintenance

All tests run against the in-memory **stub data** store seeded deterministically at host start.
Coverage per AC is derived by `sdlc.cs gate ... traceability` from AC IDs in test sources; the per-AC
file map is in `traceability.md`.

## Test matrix

| Area | ACs | Unit (`tests/GlAccounts.Api.UnitTests`) | Integration (`tests/GlAccounts.Api.IntegrationTests`) |
|---|---|---|---|
| Create account | 001, 002, 003, 011, 012, 013, 043, 044, 049, 052, 057 | CreateAccountTests, AccountIdTests, ClassificationRulesTests, AuditStoreTests | CreateAccountEndpointTests |
| Get account, eligibility | 004, 005, 020, 021 | GetAccountTests, StatusRulesTests | GetAccountEndpointTests |
| Update account | 006..010, 022, 023, 045, 050, 051, 056 | UpdateAccountTests, ETagHelperTests | UpdateAccountEndpointTests |
| Delete account | 038..042 | DeleteAccountTests | DeleteAccountEndpointTests |
| Period summary | 028..032 | PeriodSummaryTests | PeriodSummaryEndpointTests |
| Transactions | 033..037 | TransactionsTests | TransactionsEndpointTests |
| Attributes | 014..019, 051 (attribute part) | ManageAttributesTests | ManageAttributesEndpointTests |
| Account groups | 024, 025, 027 | ListAccountGroupsTests | ListAccountGroupsEndpointTests |
| Templates | 053, 054, 055 | TemplatesTests | TemplatesEndpointTests |
| Contract conformance (T012) | API-GLA-001..016 | - | `Contract/ContractConformanceTests` |
| NFR timing (T013) | NFR-GLA-001..005 | - | `Performance/NfrTimingTests` |
| Quickstart scenarios 1..18 (T014) | all scenario ACs listed in `quickstart.md` | - | `Quickstart/QuickstartScenarioTests` |

Retired and therefore not tested: AC-GLA-026, 046, 047, 048.

## Not run or not verified

| Item | Status | Reason |
|---|---|---|
| `contract_drift` | not run | the app has no OpenAPI generator, so there is no generated contract to compare; T012 compares routes and responses with the committed contract instead |
| `integration_tests` check | not run | no separate integration-test command is registered; integration tests run inside `dotnet test` (recorded as `unit_tests`) |
| Playwright / accessibility | not run | no frontend exists (ADR-002 undecided) |
| Security tests (401/403) | not run | authentication and authorization are deferred (Q-001) |
| Durability, transactions, concurrency across processes, migrations, production performance | not verified (stub data) | no database; timing tests describe the in-memory stub only |

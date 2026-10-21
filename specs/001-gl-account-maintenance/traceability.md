# Traceability Matrix: 001-gl-account-maintenance

Coverage is DERIVED by `sdlc.cs traceability` from AC IDs found in real test sources
(tests/, e2e/, *Tests.cs, *.spec.ts, *.feature). Reference the AC ID in each test's name or comment.

## Traceability map

| REQ-ID | AC-ID | API-ID | UX-ID | TASK-ID | TEST-ID (file) | Status |
|---|---|---|---|---|---|---|

## Evidence log

| Date | Check | Command | Exit code | Output SHA-256 |
|---|---|---|---|---|
| 2026-10-16T07:58:59Z | build | `dotnet build --no-restore -warnaserror` | 0 | 961085b75c4429f9 |
| 2026-10-16T08:13:01Z | build | `0` | 1 | 867cd6410810fb1f |
| 2026-10-16T08:13:12Z | build | `dotnet build GlAccounts.slnx --no-restore -warnaserror` | 0 | e07ad147ff8d0873 |
| 2026-10-16T08:17:16Z | unit_tests | `dotnet test GlAccounts.slnx` | 0 | 301c26b6c8378fd3 |
| 2026-10-16T08:21:06Z | unit_tests | `dotnet test GlAccounts.slnx` | 0 | 3fb69ebfaa2af8ac |
| 2026-10-16T08:24:48Z | unit_tests | `dotnet test GlAccounts.slnx` | 0 | 258ef4d9ba977918 |
| 2026-10-16T08:29:36Z | unit_tests | `dotnet test GlAccounts.slnx` | 0 | e9fae2241d642845 |
| 2026-10-16T08:32:09Z | unit_tests | `dotnet test GlAccounts.slnx` | 0 | 227a56568946d1ff |
| 2026-10-16T09:14:28Z | build | `dotnet build --no-restore -warnaserror` | 0 | 3b6dae707b2d3f7d |
| 2026-10-16T09:14:42Z | unit_tests | `dotnet test --no-build` | 0 | 5d269195794b141d |

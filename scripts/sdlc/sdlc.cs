// sdlc.cs — deterministic gate tooling for the repository-native SDLC.
// Single .NET 10 file-based app.  Run:  dotnet run scripts/sdlc/sdlc.cs -- <command> [args]
//
// Commands (<spec-dir> may be omitted for single-argument commands; the current
// feature is then resolved from $SPECIFY_FEATURE_DIRECTORY or .specify/feature.json):
//   init-feature [<spec-dir>]                      Create status.yaml + traceability.md
//   set-stage    <spec-dir> <stage> <status>       pending|in_progress|failed|skipped (never 'approved')
//   approve      <spec-dir> <stage> <who>          HUMAN ONLY: interactive confirmation, hashes artifacts
//   verify-state [<spec-dir>]                      Validate status.yaml, approval ledger, staleness, evidence
//   openapi      <contract.yaml>                   Validate an OpenAPI 3.x document
//   drift        <new.yaml> [--old=<path>]         Breaking-change detection (exit 2 = breaking)
//   spec         [<spec-dir>] [--strict-questions] Validate spec.md
//   traceability [<spec-dir>] [--strict]           AC coverage derived from real test sources
//   evidence     <spec-dir> <check> <cmd> [args]   Run a command, record exit code in status.yaml
//   gate         [<spec-dir>] <gate>               specification|clarification|contract|ux|traceability|state
//   secrets      <path>...                         Scan for accidental secrets (never prints values)
//   selftest                                       Run built-in tests (positive, negative, tamper)
//   help
//
// Exit codes: 0 = pass, 1 = fail/usage error, 2 = breaking change (drift only).
#:property PublishAot=false
#:property NoWarn=IL2026;IL3050
#:package Microsoft.OpenApi@3.10.2
#:package Microsoft.OpenApi.YamlReader@3.10.2
#:package YamlDotNet@18.1.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// ─────────────────────────────────────────────────────────────────────────────
// Entry point
// ─────────────────────────────────────────────────────────────────────────────
if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
{
    PrintHelp();
    return 0;
}

try
{
    var rest = args[1..];
    return args[0].ToLowerInvariant() switch
    {
        "init-feature" => CmdInitFeature(rest),
        "set-stage"    => CmdSetStage(rest),
        "approve"      => CmdApprove(rest),
        "approve-chat" => CmdApproveChat(rest),
        "verify-state" => CmdVerifyState(rest),
        "status"       => CmdStatus(rest),
        "openapi"      => CmdOpenApi(rest),
        "drift"        => CmdDrift(rest),
        "spec"         => CmdSpec(rest),
        "traceability" => CmdTraceability(rest),
        "evidence"     => CmdEvidence(rest),
        "gate"         => CmdGate(rest),
        "secrets"      => CmdSecrets(rest),
        "selftest"     => CmdSelfTest(),
        _              => Fail($"Unknown command '{args[0]}'. Run 'help'.")
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL: {ex.Message}");
    return 1;
}

// ─────────────────────────────────────────────────────────────────────────────
// Output + generic helpers
// ─────────────────────────────────────────────────────────────────────────────
static void Ok(string m)     => Console.WriteLine($"  [ok]   {m}");
static void Warn(string m)   => Console.WriteLine($"  [warn] {m}");
static void Bad(string m)    => Console.WriteLine($"  [FAIL] {m}");
static void Info(string m)   => Console.WriteLine($"         {m}");
static void Banner(string t) => Console.WriteLine($"\n== {t} ==");
static int Fail(string m)    { Console.Error.WriteLine($"ERROR: {m}"); return 1; }

static string Now() => DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

static string Sha(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

// Line-ending-normalised file hash so CRLF/LF checkouts do not cause false staleness.
static string FileSha(string path) => Sha(File.ReadAllText(path).Replace("\r\n", "\n"));

static string RepoRoot(string from)
{
    var d = new DirectoryInfo(Path.GetFullPath(from));
    while (d != null)
    {
        if (Directory.Exists(Path.Combine(d.FullName, ".specify"))) return d.FullName;
        d = d.Parent;
    }
    return Directory.GetCurrentDirectory();
}

// Resolve the spec dir: explicit arg (not starting with "--") > env var > .specify/feature.json
static string ResolveSpecDir(string[] a, int idx = 0)
{
    if (a.Length > idx && !string.IsNullOrWhiteSpace(a[idx]) && !a[idx].StartsWith("--")) return a[idx];

    var env = Environment.GetEnvironmentVariable("SPECIFY_FEATURE_DIRECTORY");
    if (!string.IsNullOrWhiteSpace(env)) return env;

    var root = RepoRoot(Directory.GetCurrentDirectory());
    var fj = Path.Combine(root, ".specify", "feature.json");
    if (File.Exists(fj))
    {
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(fj));
        if (doc.RootElement.TryGetProperty("feature_directory", out var p) && p.GetString() is { Length: > 0 } fd)
            return Path.IsPathRooted(fd) ? fd : Path.Combine(root, fd);
    }
    throw new ArgumentException("No spec directory given and none could be resolved " +
        "(set SPECIFY_FEATURE_DIRECTORY or run /speckit-specify so .specify/feature.json exists).");
}

static IEnumerable<string> Walk(string root, ISet<string> skipDirNames)
{
    if (File.Exists(root)) { yield return root; yield break; }
    if (!Directory.Exists(root)) yield break;
    var stack = new Stack<string>();
    stack.Push(root);
    while (stack.Count > 0)
    {
        var dir = stack.Pop();
        IEnumerable<string> files, dirs;
        try { files = Directory.EnumerateFiles(dir); dirs = Directory.EnumerateDirectories(dir); }
        catch { continue; }
        foreach (var f in files) yield return f;
        foreach (var d in dirs)
            if (!skipDirNames.Contains(Path.GetFileName(d))) stack.Push(d);
    }
}

static (int code, string output) RunProcess(string file, string[] argv)
{
    (int, string) Run(string f, IEnumerable<string> av)
    {
        var psi = new ProcessStartInfo(f)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        foreach (var x in av) psi.ArgumentList.Add(x);
        using var p = new Process { StartInfo = psi };
        var sb = new StringBuilder();
        var gate = new object();
        p.OutputDataReceived += (_, e) => { if (e.Data != null) lock (gate) sb.AppendLine(e.Data); };
        p.ErrorDataReceived  += (_, e) => { if (e.Data != null) lock (gate) sb.AppendLine(e.Data); };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        return (p.ExitCode, sb.ToString());
    }
    try { return Run(file, argv); }
    catch (System.ComponentModel.Win32Exception) when (OperatingSystem.IsWindows())
    {
        // .cmd/.bat shims such as npm/npx
        return Run("cmd.exe", new[] { "/c", file }.Concat(argv));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// status.yaml model (strict: unknown keys are rejected)
// ─────────────────────────────────────────────────────────────────────────────
static IDeserializer YamlIn() => new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance).Build();

static ISerializer YamlOut() => new SerializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();

static StatusDoc LoadStatus(string specDir)
{
    var p = Path.Combine(specDir, "status.yaml");
    if (!File.Exists(p)) throw new FileNotFoundException($"status.yaml not found: {p} (run init-feature)");
    return YamlIn().Deserialize<StatusDoc>(File.ReadAllText(p)) ?? throw new InvalidDataException("status.yaml is empty");
}

static void SaveStatus(string specDir, StatusDoc s)
{
    s.LastUpdated = Now();
    var header = "# Managed by scripts/sdlc/sdlc.cs. Do not hand-edit: approvals are hash-verified by `verify-state`.\n";
    File.WriteAllText(Path.Combine(specDir, "status.yaml"), header + YamlOut().Serialize(s), new UTF8Encoding(false));
}

static string GrantHash(Grant g) =>
    Sha($"{g.Stage}|{g.ApprovedBy}|{g.ApprovedAt}|" + (g.Channel == "terminal" ? "" : $"{g.Channel}|{g.Quote}|") +
        string.Join(",", g.Artifacts.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}")));

static int StageIndex(string stage) => Array.IndexOf(Cfg.Stages, stage);

static void RecordRun(string specDir, string check, int exitCode, string command, string? outputSha = null)
{
    var s = LoadStatus(specDir);
    s.Validation[check] = new RunRec
    {
        Status = exitCode == 0 ? "passed" : "failed",
        Command = command, ExitCode = exitCode, RunAt = Now(), OutputSha = outputSha ?? ""
    };
    SaveStatus(specDir, s);
}

// ─────────────────────────────────────────────────────────────────────────────
// init-feature
// ─────────────────────────────────────────────────────────────────────────────
static int CmdInitFeature(string[] a)
{
    var specDir = ResolveSpecDir(a);
    Banner($"init-feature: {specDir}");
    Directory.CreateDirectory(specDir);

    var statusPath = Path.Combine(specDir, "status.yaml");
    if (File.Exists(statusPath)) Info("status.yaml already exists (left untouched)");
    else
    {
        var name = Path.GetFileName(Path.GetFullPath(specDir).TrimEnd('\\', '/'));
        var s = new StatusDoc { Feature = name, FeatureNumber = Regex.Match(name, @"^\d+").Value,
                                CurrentStage = "specification", NextAction = "write-spec" };
        foreach (var st in Cfg.Stages) s.Stages[st] = new StageRec();
        foreach (var c in Cfg.Checks) s.Validation[c] = new RunRec();
        SaveStatus(specDir, s);
        Ok($"Created {statusPath}");
    }

    var tracePath = Path.Combine(specDir, "traceability.md");
    if (File.Exists(tracePath)) Info("traceability.md already exists (left untouched)");
    else
    {
        File.WriteAllText(tracePath, $"""
# Traceability Matrix: {Path.GetFileName(Path.GetFullPath(specDir).TrimEnd('\\', '/'))}

Coverage is DERIVED by `sdlc.cs traceability` from AC IDs found in real test sources
(tests/, e2e/, *Tests.cs, *.spec.ts, *.feature). Reference the AC ID in each test's name or comment.

## Traceability map

| REQ-ID | AC-ID | API-ID | UX-ID | TASK-ID | TEST-ID (file) | Status |
|---|---|---|---|---|---|---|

## Evidence log

| Date | Check | Command | Exit code | Output SHA-256 |
|---|---|---|---|---|

""", new UTF8Encoding(false));
        Ok($"Created {tracePath}");
    }
    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// set-stage
// ─────────────────────────────────────────────────────────────────────────────
static int CmdSetStage(string[] a)
{
    if (a.Length < 3) return Fail("Usage: set-stage <spec-dir> <stage> <pending|in_progress|failed|skipped>");
    var (specDir, stage, status) = (a[0], a[1], a[2]);

    if (status == "approved") return Fail("'approved' can only be set by a human via 'approve'.");
    if (!Cfg.StageStatuses.Contains(status)) return Fail($"Invalid status '{status}'.");
    if (StageIndex(stage) < 0) return Fail($"Unknown stage '{stage}'. Stages: {string.Join(", ", Cfg.Stages)}");
    if (status == "skipped" && !Cfg.Skippable.Contains(stage))
        return Fail($"Stage '{stage}' cannot be skipped (skippable: {string.Join(", ", Cfg.Skippable)}).");

    var s = LoadStatus(specDir);
    var rec = s.Stages[stage];
    if (rec.Status == "approved")
    {
        s.HumanApprovals.Granted.RemoveAll(g => g.Stage == stage);
        rec.ApprovedBy = ""; rec.ApprovedAt = ""; rec.ArtifactHashes = new();
        s.Decisions.Add($"{Now()} approval for '{stage}' revoked by set-stage -> {status}");
        Warn($"Existing approval of '{stage}' revoked");
    }
    rec.Status = status;
    if (status == "in_progress") s.CurrentStage = stage;
    SaveStatus(specDir, s);
    Ok($"Stage '{stage}' -> {status}");
    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// approve  (human only)
// ─────────────────────────────────────────────────────────────────────────────
static int CmdApprove(string[] a)
{
    if (a.Length < 3) return Fail("Usage: approve <spec-dir> <stage> <approver-name>");
    if (Console.IsInputRedirected)
        return Fail("approve must be run interactively by a human (stdin is redirected). " +
                    "Agents and CI must not approve; request approval from the maintainer instead.");

    Console.Write($"Approve stage '{a[1]}' of '{a[0]}' as '{a[2]}'? Re-type the approver name to confirm: ");
    if (!string.Equals(Console.ReadLine()?.Trim(), a[2], StringComparison.Ordinal))
        return Fail("Confirmation did not match. Nothing changed.");
    return ApproveCore(a[0], a[1], a[2]);
}

// Chat approval: the human's explicit "yes" in the Bob conversation is the approval. The tool cannot verify
// that the user really said it, so the verbatim reply is stored in the hash-protected ledger for review, and
// SDLC_REQUIRE_TERMINAL_APPROVAL=1 disables this path (use for teams that want terminal-only approval).
static int CmdApproveChat(string[] a)
{
    if (a.Length < 4) return Fail("Usage: approve-chat <spec-dir> <stage> <approver-name> <user's verbatim approval reply...>");
    if (Environment.GetEnvironmentVariable("SDLC_REQUIRE_TERMINAL_APPROVAL") == "1")
        return Fail("Chat approval is disabled (SDLC_REQUIRE_TERMINAL_APPROVAL=1). Use 'approve' in a terminal.");
    var quote = string.Join(" ", a.Skip(3)).Trim();
    if (quote.Length < 2) return Fail("The user's verbatim approval reply is required.");
    return ApproveCore(a[0], a[1], a[2], "chat", quote);
}

static int ApproveCore(string specDir, string stage, string who, string channel = "terminal", string quote = "")
{
    Banner($"approve: {stage}");
    if (StageIndex(stage) < 0) return Fail($"Unknown stage '{stage}'.");
    if (string.IsNullOrWhiteSpace(who)) return Fail("Approver name is required.");

    var s = LoadStatus(specDir);

    // 1. earlier stages must be approved or skipped
    foreach (var prior in Cfg.Stages.Take(StageIndex(stage)))
    {
        var st = s.Stages[prior].Status;
        if (st != "approved" && st != "skipped") return Fail($"Refused: earlier stage '{prior}' is '{st}' (must be approved or skipped).");
    }

    // 2. artifacts must exist
    var artifacts = Cfg.StageArtifacts[stage];
    var hashes = new Dictionary<string, string>();
    foreach (var art in artifacts)
    {
        var full = Path.Combine(specDir, art);
        if (!File.Exists(full)) return Fail($"Refused: required artifact missing: {art}");
        hashes[art] = FileSha(full);
    }

    // 3. required deterministic checks must have a recorded, passing run
    var required = Cfg.ApprovalRequires(stage, specDir);
    foreach (var chk in required)
    {
        if (!s.Validation.TryGetValue(chk, out var run) || run.Status != "passed" || run.ExitCode != 0)
            return Fail($"Refused: check '{chk}' has no recorded passing run. Run the gate/evidence command first.");
    }

    var now = Now();
    var grant = new Grant { Stage = stage, ApprovedBy = who, ApprovedAt = now, Artifacts = hashes, Channel = channel, Quote = quote };
    grant.Hash = GrantHash(grant);

    s.HumanApprovals.Granted.RemoveAll(g => g.Stage == stage);
    s.HumanApprovals.Granted.Add(grant);
    s.Stages[stage] = new StageRec { Status = "approved", ApprovedBy = who, ApprovedAt = now, ArtifactHashes = hashes };
    var next = StageIndex(stage) + 1;
    if (next < Cfg.Stages.Length) s.CurrentStage = Cfg.Stages[next];
    s.NextAction = next < Cfg.Stages.Length ? $"start-{Cfg.Stages[next]}" : "ready-for-pr";
    SaveStatus(specDir, s);

    Ok($"Stage '{stage}' approved by '{who}' via {channel} at {now}; {hashes.Count} artifact hash(es) recorded");
    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// verify-state
// ─────────────────────────────────────────────────────────────────────────────
static int CmdVerifyState(string[] a)
{
    var specDir = ResolveSpecDir(a);
    Banner($"verify-state: {specDir}");
    int rc = 0;

    StatusDoc s;
    try { s = LoadStatus(specDir); }
    catch (Exception ex) { Bad($"status.yaml unreadable: {ex.Message}"); return 1; }

    foreach (var st in Cfg.Stages)
        if (!s.Stages.ContainsKey(st)) { Bad($"Missing stage entry: {st}"); rc = 1; }
    foreach (var k in s.Stages.Keys)
        if (StageIndex(k) < 0) { Bad($"Unknown stage entry: {k}"); rc = 1; }
    if (StageIndex(s.CurrentStage) < 0) { Bad($"current_stage '{s.CurrentStage}' is not a known stage"); rc = 1; }

    // stage statuses + approval ledger
    foreach (var st in Cfg.Stages.Where(s.Stages.ContainsKey))
    {
        var rec = s.Stages[st];
        if (!Cfg.StageStatuses.Contains(rec.Status)) { Bad($"{st}: invalid status '{rec.Status}'"); rc = 1; continue; }
        if (rec.Status == "skipped" && !Cfg.Skippable.Contains(st)) { Bad($"{st}: stage may not be skipped"); rc = 1; }
        if (rec.Status != "approved") continue;

        var grant = s.HumanApprovals.Granted.FirstOrDefault(g => g.Stage == st);
        if (grant is null) { Bad($"{st}: marked approved but there is no ledger entry (forged or hand-edited)"); rc = 1; continue; }
        if (GrantHash(grant) != grant.Hash) { Bad($"{st}: ledger entry hash mismatch (tampered)"); rc = 1; continue; }
        if (grant.ApprovedBy != rec.ApprovedBy || grant.ApprovedAt != rec.ApprovedAt ||
            !grant.Artifacts.OrderBy(x => x.Key).SequenceEqual(rec.ArtifactHashes.OrderBy(x => x.Key)))
        { Bad($"{st}: stage record disagrees with ledger entry"); rc = 1; continue; }

        // staleness (skip artifacts re-approved by a later approved stage)
        foreach (var (art, recorded) in grant.Artifacts)
        {
            bool superseded = Cfg.Stages.Skip(StageIndex(st) + 1).Any(later =>
                s.Stages.TryGetValue(later, out var lr) && lr.Status == "approved" && Cfg.StageArtifacts[later].Contains(art));
            if (superseded) continue;
            var full = Path.Combine(specDir, art);
            if (!File.Exists(full)) { Bad($"{st}: approved artifact missing: {art}"); rc = 1; }
            else if (FileSha(full) != recorded) { Bad($"{st}: STALE approval - {art} changed after approval; re-validate and re-approve"); rc = 1; }
        }
        if (rc == 0) Ok($"{st}: approved by {grant.ApprovedBy} via {grant.Channel} ({grant.ApprovedAt}), artifacts unchanged");
    }

    // ledger entries without a matching approved stage
    foreach (var g in s.HumanApprovals.Granted)
        if (!s.Stages.TryGetValue(g.Stage, out var r) || r.Status != "approved")
        { Bad($"ledger entry for '{g.Stage}' but stage is not approved"); rc = 1; }

    // validation evidence
    foreach (var (name, run) in s.Validation)
    {
        if (!Cfg.Checks.Contains(name)) { Bad($"unknown validation entry: {name}"); rc = 1; continue; }
        if (!Cfg.RunStatuses.Contains(run.Status)) { Bad($"validation.{name}: invalid status '{run.Status}'"); rc = 1; continue; }
        if (run.Status == "passed" && (run.ExitCode != 0 || string.IsNullOrEmpty(run.RunAt) || string.IsNullOrEmpty(run.Command)))
        { Bad($"validation.{name}: 'passed' without recorded evidence (exit_code 0, run_at, command)"); rc = 1; }
        if (run.Status == "failed" && (run.ExitCode is null or 0))
        { Bad($"validation.{name}: 'failed' but exit_code is not non-zero"); rc = 1; }
    }

    Console.WriteLine(rc == 0 ? "\n  PASS: workflow state valid" : "\n  FAIL: workflow state invalid");
    return rc;
}

// ─────────────────────────────────────────────────────────────────────────────
// status — read-only progress report (never modifies files). Exit 1 only if status.yaml is unreadable.
// ─────────────────────────────────────────────────────────────────────────────
static int CmdStatus(string[] a)
{
    string specDir;
    try { specDir = ResolveSpecDir(a); }
    catch (ArgumentException ex) { Console.WriteLine($"No active feature: {ex.Message}"); return 1; }

    StatusDoc s;
    try { s = LoadStatus(specDir); }
    catch (Exception ex) { Console.WriteLine($"No readable status.yaml in {specDir}: {ex.Message}"); return 1; }

    Banner($"status: {s.Feature}  ({specDir})");
    Console.WriteLine($"  current stage : {s.CurrentStage}");
    Console.WriteLine($"  next action   : {s.NextAction}");
    Console.WriteLine($"  last updated  : {s.LastUpdated}");

    Console.WriteLine("\n  Stages");
    int done = 0;
    string? nextOpen = null;
    foreach (var st in Cfg.Stages)
    {
        var r = s.Stages.TryGetValue(st, out var x) ? x : new StageRec();
        var tail = r.Status == "approved" ? $" by {r.ApprovedBy} ({r.ApprovedAt})" : "";
        Console.WriteLine($"    {(r.Status is "approved" or "skipped" ? "[x]" : "[ ]")} {st,-20} {r.Status}{tail}");
        if (r.Status is "approved" or "skipped") done++;
        else nextOpen ??= st;
    }
    Console.WriteLine($"\n  progress      : {done}/{Cfg.Stages.Length} stages approved or skipped");
    Console.WriteLine($"  first open    : {nextOpen ?? "none - all stages complete"}");

    Console.WriteLine("\n  Checks (last recorded run)");
    foreach (var c in Cfg.Checks)
    {
        var r = s.Validation.TryGetValue(c, out var x) ? x : new RunRec();
        var shown = r.Status == "pending" ? "not run" : r.Status;
        Console.WriteLine($"    {c,-18} {shown}{(r.Status == "pending" ? "" : $" (exit {r.ExitCode}, {r.RunAt})")}");
    }

    var tasks = Path.Combine(specDir, "tasks.md");
    if (File.Exists(tasks))
    {
        var txt = File.ReadAllText(tasks);
        int open = Regex.Matches(txt, @"^\s*- \[ \]", RegexOptions.Multiline).Count;
        int closed = Regex.Matches(txt, @"^\s*- \[[xX]\]", RegexOptions.Multiline).Count;
        Console.WriteLine($"\n  Tasks         : {closed} done, {open} open");
    }

    var verify = CmdVerifyStateQuiet(specDir);
    Console.WriteLine($"\n  state integrity: {(verify == 0 ? "OK" : "PROBLEM - run verify-state for details (stale, forged or edited approval)")}");

    if (s.Decisions.Count > 0)
    {
        Console.WriteLine("\n  Recent decisions");
        foreach (var d in s.Decisions.TakeLast(5)) Console.WriteLine($"    - {d}");
    }
    return 0;
}

static int CmdVerifyStateQuiet(string specDir)
{
    var realOut = Console.Out;
    try { Console.SetOut(TextWriter.Null); return CmdVerifyState(new[] { specDir }); }
    finally { Console.SetOut(realOut); }
}

// ─────────────────────────────────────────────────────────────────────────────
// openapi
// ─────────────────────────────────────────────────────────────────────────────
static ReadResult ParseOpenApi(string path)
{
    var settings = new OpenApiReaderSettings();
    settings.AddYamlReader();
    return OpenApiDocument.Parse(File.ReadAllText(path), "yaml", settings);
}

static int CmdOpenApi(string[] a)
{
    if (a.Length < 1) return Fail("Usage: openapi <contract.yaml>");
    var path = a[0];
    if (!File.Exists(path)) return Fail($"Contract file not found: {path}");
    Banner($"openapi: {path}");
    int rc = 0;

    ReadResult result;
    try { result = ParseOpenApi(path); }
    catch (Exception ex) { return Fail($"YAML parse error: {ex.Message}"); }

    var doc = result.Document;
    var diag = result.Diagnostic;
    if (diag?.Errors?.Count > 0)
    {
        Bad($"Parse errors ({diag.Errors.Count}):");
        foreach (var e in diag.Errors) Info($"{e.Pointer}: {e.Message}");
        rc = 1;
    }
    else Ok("Parses without errors");
    if (diag?.Warnings?.Count > 0)
    {
        Warn($"Warnings ({diag.Warnings.Count}):");
        foreach (var w in diag.Warnings) Info($"{w.Pointer}: {w.Message}");
    }
    if (doc is null) return 1;

    var idRx = new Regex(@"^[a-z][a-zA-Z0-9]*$");
    var seen = new HashSet<string>();
    bool globalSec = doc.Security?.Count > 0;
    int ops = 0;

    foreach (var (pathKey, item) in doc.Paths ?? new OpenApiPaths())
    {
        if (pathKey != pathKey.ToLowerInvariant()) Warn($"Path should be lowercase: {pathKey}");
        foreach (var (opType, op) in item.Operations ?? new())
        {
            ops++;
            var label = $"{opType.ToString().ToUpperInvariant()} {pathKey}";
            if (string.IsNullOrEmpty(op.OperationId)) { Bad($"Missing operationId: {label}"); rc = 1; }
            else if (!seen.Add(op.OperationId)) { Bad($"Duplicate operationId: {op.OperationId}"); rc = 1; }
            else if (!idRx.IsMatch(op.OperationId)) Warn($"operationId should be camelCase: {op.OperationId}");

            var codes = op.Responses?.Keys.ToHashSet() ?? new HashSet<string>();
            if (!codes.Any(c => c.StartsWith('2'))) { Bad($"No 2xx response: {label}"); rc = 1; }
            foreach (var c in new[] { "400", "401", "500" })
                if (!codes.Contains(c)) Warn($"No {c} response: {label}");
            if (!(op.Security?.Count > 0 || globalSec)) Warn($"No security declaration: {label}");

            bool hasEx = op.Responses?.Values.Any(r => r.Content?.Values.Any(m => m.Examples?.Count > 0 || m.Example is not null) == true) == true;
            if (!hasEx) Warn($"No response example: {label}");
        }
    }
    if (ops == 0) { Bad("Contract declares no operations"); rc = 1; }

    Console.WriteLine(rc == 0 ? "\n  PASS: contract valid" : "\n  FAIL: contract invalid");
    return rc;
}

// ─────────────────────────────────────────────────────────────────────────────
// drift   (exit 2 = breaking changes)
// ─────────────────────────────────────────────────────────────────────────────
static int CmdDrift(string[] a)
{
    if (a.Length < 1) return Fail("Usage: drift <new.yaml> [--old=<path>]");
    var newPath = a[0];
    string? oldPath = a.Skip(1).Where(x => x.StartsWith("--old=")).Select(x => x[6..]).FirstOrDefault();
    if (!File.Exists(newPath)) return Fail($"Contract not found: {newPath}");
    Banner($"drift: {newPath}");

    if (string.IsNullOrEmpty(oldPath) || !File.Exists(oldPath))
    {
        Warn("No baseline contract supplied - nothing to compare (this is NOT evidence of no drift)");
        return 0;
    }
    var newDoc = ParseOpenApi(newPath).Document ?? throw new Exception("Could not parse new contract");
    var oldDoc = ParseOpenApi(oldPath).Document ?? throw new Exception("Could not parse baseline contract");

    var breaking = new List<string>();
    var additive = new List<string>();
    var oldPaths = oldDoc.Paths ?? new OpenApiPaths();
    var newPaths = newDoc.Paths ?? new OpenApiPaths();

    foreach (var (pk, oi) in oldPaths)
        foreach (var (ot, oldOp) in oi.Operations ?? new())
        {
            var L = $"{ot.ToString().ToUpperInvariant()} {pk}";
            if (!newPaths.TryGetValue(pk, out var ni) || ni.Operations is null || !ni.Operations.TryGetValue(ot, out var newOp))
            { breaking.Add($"REMOVED {L}"); continue; }

            if (oldOp.OperationId != newOp.OperationId) breaking.Add($"RENAMED operationId {oldOp.OperationId} -> {newOp.OperationId} ({L})");

            foreach (var code in (oldOp.Responses?.Keys ?? Enumerable.Empty<string>()).Except(newOp.Responses?.Keys ?? Enumerable.Empty<string>()))
                breaking.Add($"REMOVED response {code} from {L}");

            var oldParams = (oldOp.Parameters ?? new List<IOpenApiParameter>()).ToDictionary(p => $"{p.In}:{p.Name}", p => p.Required);
            foreach (var p in newOp.Parameters ?? new List<IOpenApiParameter>())
            {
                var key = $"{p.In}:{p.Name}";
                if (!oldParams.TryGetValue(key, out var wasRequired))
                { if (p.Required) breaking.Add($"NEW required parameter {key} on {L}"); else additive.Add($"NEW optional parameter {key} on {L}"); }
                else if (!wasRequired && p.Required) breaking.Add($"Parameter {key} became required on {L}");
            }
            foreach (var key in oldParams.Keys.Except((newOp.Parameters ?? new List<IOpenApiParameter>()).Select(p => $"{p.In}:{p.Name}")))
                breaking.Add($"REMOVED parameter {key} from {L}");

            if ((newOp.RequestBody?.Required ?? false) && !(oldOp.RequestBody?.Required ?? false))
                breaking.Add($"Request body became required on {L}");
        }

    foreach (var (pk, ni) in newPaths)
        foreach (var (ot, _) in ni.Operations ?? new())
            if (!oldPaths.TryGetValue(pk, out var oi) || oi.Operations is null || !oi.Operations.ContainsKey(ot))
                additive.Add($"NEW {ot.ToString().ToUpperInvariant()} {pk}");

    // Component schemas: added required fields and removed properties are breaking (conservative).
    var oldSchemas = oldDoc.Components?.Schemas;
    var newSchemas = newDoc.Components?.Schemas;
    if (oldSchemas != null && newSchemas != null)
        foreach (var (name, os) in oldSchemas)
        {
            if (!newSchemas.TryGetValue(name, out var ns)) { breaking.Add($"REMOVED schema {name}"); continue; }
            foreach (var r in (ns.Required ?? new HashSet<string>()).Except(os.Required ?? new HashSet<string>()))
                breaking.Add($"Schema {name}: field '{r}' became required");
            foreach (var pr in (os.Properties?.Keys ?? Enumerable.Empty<string>()).Except(ns.Properties?.Keys ?? Enumerable.Empty<string>()))
                breaking.Add($"Schema {name}: property '{pr}' removed");
        }

    if (breaking.Count > 0) { Bad($"{breaking.Count} breaking change(s) - human approval required:"); breaking.ForEach(b => Info($"- {b}")); }
    if (additive.Count > 0) { Warn($"{additive.Count} additive change(s):"); additive.ForEach(x => Info($"- {x}")); }
    if (breaking.Count == 0 && additive.Count == 0) Ok("No drift detected");
    return breaking.Count > 0 ? 2 : 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// spec
// ─────────────────────────────────────────────────────────────────────────────
static int CmdSpec(string[] a)
{
    var specDir = ResolveSpecDir(a);
    bool strict = a.Contains("--strict-questions");
    Banner($"spec: {specDir}{(strict ? " (strict questions)" : "")}");

    var file = Path.Combine(specDir, "spec.md");
    if (!File.Exists(file)) return Fail($"spec.md not found: {file}");
    var c = File.ReadAllText(file);
    int rc = 0;

    foreach (var (label, rx) in new[]
    {
        ("Functional requirements",    @"(?im)^##\s+functional requirements"),
        ("User scenarios / acceptance", @"(?im)^##\s+(user scenarios|acceptance)"),
        ("Assumptions",                @"(?im)^##\s+.*assumption"),
        ("Success criteria",           @"(?im)^##\s+success criteria"),
    })
    {
        if (Regex.IsMatch(c, rx)) Ok($"Section: {label}");
        else { Bad($"Missing section: {label}"); rc = 1; }
    }

    var ids = Regex.Matches(c, @"\b(REQ|AC)-([A-Z]{2,4})-(\d{3,})\b").Cast<Match>().ToList();
    var reqs = ids.Where(m => m.Groups[1].Value == "REQ" && m.Groups[2].Value != "XXX").Select(m => m.Value).Distinct().Count();
    var acs  = ids.Where(m => m.Groups[1].Value == "AC"  && m.Groups[2].Value != "XXX").Select(m => m.Value).Distinct().Count();
    var placeholderIds = ids.Count(m => m.Groups[2].Value == "XXX");

    if (reqs > 0) Ok($"{reqs} distinct REQ ID(s)"); else { Bad("No real REQ-<DOMAIN>-NNN IDs"); rc = 1; }
    if (acs  > 0) Ok($"{acs} distinct AC ID(s)");   else { Bad("No real AC-<DOMAIN>-NNN IDs");  rc = 1; }
    if (placeholderIds > 0) { Bad($"{placeholderIds} template placeholder ID(s) (XXX domain) still present"); rc = 1; }

    var placeholders = Regex.Matches(c, @"\[FEATURE NAME\]|\[DATE\]|\[###-feature-name\]|\[Brief Title\]|\$ARGUMENTS").Count;
    if (placeholders > 0) { Bad($"{placeholders} unfilled template placeholder(s)"); rc = 1; }

    var questions = Regex.Matches(c, @"\[QUESTION:|\[NEEDS CLARIFICATION").Count;
    if (questions == 0) Ok("No unresolved [QUESTION]/[NEEDS CLARIFICATION] markers");
    else if (strict) { Bad($"{questions} unresolved question marker(s)"); rc = 1; }
    else Warn($"{questions} unresolved question marker(s)");

    if (Regex.Matches(c, @"(?i)\b(given|when|then)\b").Count >= 3) Ok("Given/When/Then wording present");
    else Warn("Acceptance criteria may lack Given/When/Then wording");

    Console.WriteLine(rc == 0 ? "\n  PASS: spec" : "\n  FAIL: spec");
    return rc;
}

// ─────────────────────────────────────────────────────────────────────────────
// traceability — coverage is derived from test sources, not from claims in markdown
// ─────────────────────────────────────────────────────────────────────────────
static Dictionary<string, List<string>> FindAcRefsInTests(string root)
{
    var map = new Dictionary<string, List<string>>();
    var skip = new HashSet<string> { "node_modules", "bin", "obj", ".git", ".specify", ".bob", "specs", "scripts", "docs", ".github", "TestResults" };
    var exts = new HashSet<string> { ".cs", ".ts", ".tsx", ".js", ".jsx", ".feature", ".py" };

    foreach (var f in Walk(root, skip))
    {
        if (!exts.Contains(Path.GetExtension(f).ToLowerInvariant())) continue;
        var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
        var dirs = rel.Split('/')[..^1];
        bool isTest = dirs.Any(d => { var l = d.ToLowerInvariant(); return l is "tests" or "test" or "e2e" || l.EndsWith(".tests") || l.EndsWith(".test"); })
                      || Regex.IsMatch(Path.GetFileName(f), @"(Tests?\.cs|\.(spec|test)\.(ts|tsx|js|jsx)|\.feature)$");
        if (!isTest) continue;

        string text;
        try { text = File.ReadAllText(f); } catch { continue; }
        foreach (Match m in Regex.Matches(text, @"\bAC-[A-Z]{2,4}-\d{3,}\b"))
        {
            if (!map.TryGetValue(m.Value, out var l)) map[m.Value] = l = new();
            if (!l.Contains(rel)) l.Add(rel);
        }
    }
    return map;
}

static int CmdTraceability(string[] a, bool forceStrict = false)
{
    var specDir = ResolveSpecDir(a);
    Banner($"traceability: {specDir}");
    int rc = 0;

    var specFile = Path.Combine(specDir, "spec.md");
    if (!File.Exists(specFile)) return Fail($"spec.md not found: {specFile}");
    var acIds = Regex.Matches(File.ReadAllText(specFile), @"\bAC-(?!XXX)[A-Z]{2,4}-\d{3,}\b").Select(m => m.Value).ToHashSet();

    var traceFile = Path.Combine(specDir, "traceability.md");
    if (!File.Exists(traceFile)) { Bad("traceability.md not found"); return 1; }
    var trace = File.ReadAllText(traceFile);

    // strictness: explicit flag, programmatic gate, or feature has reached implementation/verification
    bool strict = forceStrict || a.Contains("--strict");
    if (!strict && File.Exists(Path.Combine(specDir, "status.yaml")))
    {
        try
        {
            var s = LoadStatus(specDir);
            strict = s.Stages.TryGetValue("implementation", out var imp) && imp.Status is "in_progress" or "approved"
                  || s.Stages.TryGetValue("verification", out var ver) && ver.Status != "pending";
        }
        catch { /* verify-state reports status problems */ }
    }
    Info($"{acIds.Count} acceptance criteria in spec; mode: {(strict ? "STRICT" : "advisory (feature not yet in implementation)")}");

    var root = RepoRoot(specDir);
    var refs = FindAcRefsInTests(root);
    var covered = acIds.Where(refs.ContainsKey).OrderBy(x => x).ToList();
    var uncovered = acIds.Except(covered).OrderBy(x => x).ToList();

    if (uncovered.Count > 0)
    {
        if (strict) { Bad($"{uncovered.Count} AC(s) with no test referencing them:"); rc = 1; }
        else Warn($"{uncovered.Count} AC(s) with no test referencing them yet:");
        uncovered.ForEach(id => Info($"- {id}"));
    }
    else if (acIds.Count > 0) Ok($"All {acIds.Count} ACs are referenced by test sources");
    else if (strict) { Bad("Spec has no real AC IDs"); rc = 1; }

    // Claims in traceability.md must be backed by tests
    var claimed = Regex.Matches(trace, @"\bAC-[A-Z]{2,4}-\d{3,}\b[^\n]*(?:covered|✅)", RegexOptions.IgnoreCase)
        .Select(m => Regex.Match(m.Value, @"AC-[A-Z]{2,4}-\d{3,}").Value).Where(x => !x.Contains("XXX")).ToHashSet();
    foreach (var id in claimed.Where(id => !refs.ContainsKey(id)).OrderBy(x => x))
    { Bad($"{id} is claimed covered in traceability.md but no test references it"); rc = 1; }

    var orphans = Regex.Matches(trace, @"\bAC-(?!XXX)[A-Z]{2,4}-\d{3,}\b").Select(m => m.Value).ToHashSet().Except(acIds).ToList();
    if (orphans.Count > 0) { Bad($"Orphan AC reference(s) not in spec: {string.Join(", ", orphans)}"); rc = 1; }

    var missingInTrace = acIds.Where(id => !trace.Contains(id)).ToList();
    if (missingInTrace.Count > 0) Warn($"{missingInTrace.Count} AC(s) not listed in traceability.md map: {string.Join(", ", missingInTrace)}");

    Console.WriteLine($"\n  Coverage (test-derived): {covered.Count}/{acIds.Count}");
    Console.WriteLine(rc == 0 ? "  PASS: traceability" : "  FAIL: traceability");
    return rc;
}

// ─────────────────────────────────────────────────────────────────────────────
// ux component-map check
// ─────────────────────────────────────────────────────────────────────────────
static int CheckComponentMap(string specDir)
{
    var path = Path.Combine(specDir, "ux", "component-map.yaml");
    Banner($"ux: {path}");
    if (!File.Exists(path)) return Fail($"component-map.yaml not found: {path}");
    int rc = 0;

    ComponentMap map;
    try
    {
        map = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties().Build().Deserialize<ComponentMap>(File.ReadAllText(path)) ?? new ComponentMap();
    }
    catch (Exception ex) { Bad($"YAML error: {ex.Message}"); return 1; }

    if (map.Screens.Count == 0) { Bad("No screens defined"); rc = 1; }
    foreach (var sc in map.Screens)
    {
        if (string.IsNullOrWhiteSpace(sc.Id) || sc.Id.Contains("XXX")) { Bad($"Screen '{sc.Name}': missing or placeholder id"); rc = 1; }
        var missing = Cfg.RequiredUiStates.Except(sc.States ?? new()).ToList();
        if (missing.Count > 0) { Bad($"{sc.Id}: missing UI state(s): {string.Join(", ", missing)}"); rc = 1; }
        foreach (var comp in sc.Components ?? new())
            if (comp.Source is not ("existing" or "gap")) { Bad($"{sc.Id}: component '{comp.Name}' source must be 'existing' or 'gap'"); rc = 1; }
    }
    var gaps = map.Screens.SelectMany(s => s.Components ?? new()).Count(c => c.Source == "gap");
    if (gaps > 0) Warn($"{gaps} design-system gap(s) recorded (needs human review)");
    if (rc == 0) Ok($"{map.Screens.Count} screen(s) valid");
    return rc;
}

// ─────────────────────────────────────────────────────────────────────────────
// evidence — run a command and record the real exit code
// ─────────────────────────────────────────────────────────────────────────────
static int CmdEvidence(string[] a)
{
    if (a.Length < 3) return Fail("Usage: evidence <spec-dir> <check> <command> [args...]\n  checks: " + string.Join(", ", Cfg.Checks));
    var (specDir, check, exe) = (a[0], a[1], a[2]);
    var argv = a[3..];
    if (!Cfg.Checks.Contains(check)) return Fail($"Unknown check '{check}'. Allowed: {string.Join(", ", Cfg.Checks)}");

    Banner($"evidence[{check}]: {exe} {string.Join(" ", argv)}");
    var (code, output) = RunProcess(exe, argv);
    Console.Write(output);

    var cmdline = $"{exe} {string.Join(" ", argv)}".Trim();
    RecordRun(specDir, check, code, cmdline, Sha(output));
    AppendEvidenceRow(specDir, check, cmdline, code, Sha(output));
    Console.WriteLine(code == 0 ? $"\n  recorded: {check} PASSED (exit 0)" : $"\n  recorded: {check} FAILED (exit {code})");
    return code;
}

static void AppendEvidenceRow(string specDir, string check, string cmd, int code, string outSha)
{
    var f = Path.Combine(specDir, "traceability.md");
    if (!File.Exists(f)) return;
    var text = File.ReadAllText(f);
    if (!text.EndsWith('\n')) text += "\n";
    text += $"| {Now()} | {check} | `{cmd.Replace("|", "\\|")}` | {code} | {outSha[..16]} |\n";
    File.WriteAllText(f, text, new UTF8Encoding(false));
}

// ─────────────────────────────────────────────────────────────────────────────
// gate — run a built-in check AND record the result in status.yaml
// ─────────────────────────────────────────────────────────────────────────────
static int CmdGate(string[] a)
{
    string specDir, gate;
    if (a.Length == 1) { gate = a[0]; specDir = ResolveSpecDir(Array.Empty<string>()); }
    else if (a.Length >= 2) { specDir = a[0]; gate = a[1]; }
    else return Fail("Usage: gate [<spec-dir>] <specification|clarification|contract|ux|traceability|state>");

    var (check, label, rc) = gate.ToLowerInvariant() switch
    {
        "specification" or "spec" => ("spec", "sdlc.cs spec", CmdSpec(new[] { specDir })),
        "clarification"           => ("spec_strict", "sdlc.cs spec --strict-questions", CmdSpec(new[] { specDir, "--strict-questions" })),
        "contract" or "openapi"   => ("contract_tests", "sdlc.cs openapi",
                                       File.Exists(Path.Combine(specDir, "contracts", "openapi.yaml"))
                                         ? CmdOpenApi(new[] { Path.Combine(specDir, "contracts", "openapi.yaml") })
                                         : Fail("contracts/openapi.yaml not found")),
        "ux"                      => ("ux", "sdlc.cs component-map check", CheckComponentMap(specDir)),
        "traceability" or "trace" => ("traceability", "sdlc.cs traceability --strict", CmdTraceability(new[] { specDir }, true)),
        "state"                   => ("", "", CmdVerifyState(new[] { specDir })),
        _                         => ("", "", Fail($"Unknown gate '{gate}'"))
    };

    if (check != "" && File.Exists(Path.Combine(specDir, "status.yaml")))
        RecordRun(specDir, check, rc, label);
    return rc;
}

// ─────────────────────────────────────────────────────────────────────────────
// secrets — never prints matched values
// ─────────────────────────────────────────────────────────────────────────────
static int CmdSecrets(string[] a)
{
    if (a.Length < 1) return Fail("Usage: secrets <path> [<path>...]");
    Banner("secrets scan");

    var patterns = new (string Name, string Rx)[]
    {
        ("Password assignment",   @"(?i)\b(password|passwd|pwd)\s*[:=]\s*[""']?[^\s""'<>{}$%]{8,}"),
        ("API key assignment",    @"(?i)\b(api[_-]?key|x-api-key)\s*[:=]\s*[""']?[^\s""'<>{}$%]{8,}"),
        ("Connection string",     @"(?i)(Server=[^;]+;.*Password=[^;]+|mongodb(\+srv)?://[^:\s]+:[^@\s]+@)"),
        ("AWS access key",        @"AKIA[0-9A-Z]{16}"),
        ("Hard-coded bearer",     @"\bBearer\s+[A-Za-z0-9_\-\.]{20,}"),
        ("Private key block",     @"-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----"),
        ("Generic secret/token",  @"(?i)\b(client_?secret|auth_?token|access_?token)\s*[:=]\s*[""']?[^\s""'<>{}$%]{12,}"),
    };
    var skipDirs = new HashSet<string> { ".git", "node_modules", "obj", "bin", ".vs", "__pycache__", "runs" };
    var skipExt = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif", ".ico", ".woff", ".woff2", ".ttf", ".zip", ".gz", ".7z", ".exe", ".dll", ".pdb", ".nupkg" };
    // The scanner's own source necessarily contains the patterns and test fixtures.
    var self = Path.GetFullPath(Path.Combine("scripts", "sdlc", "sdlc.cs"));

    int findings = 0;
    foreach (var root in a)
        foreach (var f in Walk(root, skipDirs))
        {
            if (skipExt.Contains(Path.GetExtension(f).ToLowerInvariant())) continue;
            if (string.Equals(Path.GetFullPath(f), self, StringComparison.OrdinalIgnoreCase)) continue;
            string[] lines;
            try { lines = File.ReadAllLines(f); } catch { continue; }
            for (int i = 0; i < lines.Length; i++)
                foreach (var (name, rx) in patterns)
                    if (Regex.IsMatch(lines[i], rx))
                    { findings++; Bad($"{name} at {f}:{i + 1}"); }
        }

    if (findings == 0) Ok("No secrets detected");
    else Console.WriteLine($"\n  FAIL: {findings} potential secret(s). Values are intentionally not printed.");
    return findings > 0 ? 1 : 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// selftest — positive, negative and tamper cases
// ─────────────────────────────────────────────────────────────────────────────
static int CmdSelfTest()
{
    Banner("selftest");
    int passed = 0, failed = 0;

    // Run a case with console output suppressed (keeps the report readable).
    void Case(string name, Func<bool> test)
    {
        var realOut = Console.Out; var realErr = Console.Error;
        bool ok; string? err = null;
        try { Console.SetOut(TextWriter.Null); Console.SetError(TextWriter.Null); ok = test(); }
        catch (Exception ex) { ok = false; err = ex.Message; }
        finally { Console.SetOut(realOut); Console.SetError(realErr); }
        if (ok) { Console.WriteLine($"  [ok]   {name}"); passed++; }
        else { Console.WriteLine($"  [FAIL] {name}{(err != null ? ": " + err : "")}"); failed++; }
    }

    var tmp = Path.Combine(Path.GetTempPath(), $"sdlc-selftest-{Guid.NewGuid():N}");
    Directory.CreateDirectory(Path.Combine(tmp, ".specify"));
    var fdir = Path.Combine(tmp, "specs", "001-demo");

    const string good = """
openapi: "3.1.0"
info: {title: T, version: "1"}
security: [{bearerAuth: []}]
paths:
  /items:
    get:
      operationId: listItems
      parameters:
        - {name: q, in: query, required: false, schema: {type: string}}
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema: {type: array, items: {type: string}}
              examples: {ex: {value: []}}
        "400": {description: bad}
        "401": {description: no}
        "500": {description: err}
components:
  securitySchemes:
    bearerAuth: {type: http, scheme: bearer}
""";
    var contractDir = Path.Combine(tmp, "contracts"); Directory.CreateDirectory(contractDir);
    var goodC = Path.Combine(contractDir, "good.yaml"); File.WriteAllText(goodC, good);

    // ── OpenAPI ──
    Case("openapi: valid contract passes", () => CmdOpenApi(new[] { goodC }) == 0);
    var noId = Path.Combine(contractDir, "noid.yaml");
    File.WriteAllText(noId, "openapi: \"3.1.0\"\ninfo: {title: T, version: \"1\"}\npaths:\n  /x:\n    post:\n      responses:\n        \"200\": {description: ok}\n");
    Case("openapi: missing operationId fails", () => CmdOpenApi(new[] { noId }) == 1);

    // ── drift ──
    Case("drift: no baseline passes (with warning)", () => CmdDrift(new[] { goodC }) == 0);
    var removed = Path.Combine(contractDir, "removed.yaml");
    File.WriteAllText(removed, good.Replace("  /items:\n    get:\n      operationId: listItems", "  /other:\n    get:\n      operationId: listOther"));
    Case("drift: removed operation is breaking (exit 2)", () => CmdDrift(new[] { removed, $"--old={goodC}" }) == 2);
    var newReq = Path.Combine(contractDir, "newreq.yaml");
    File.WriteAllText(newReq, good.Replace("required: false", "required: true"));
    Case("drift: optional->required parameter is breaking (exit 2)", () => CmdDrift(new[] { newReq, $"--old={goodC}" }) == 2);
    var additive = Path.Combine(contractDir, "additive.yaml");
    File.WriteAllText(additive, good.Replace("components:", "  /more:\n    get:\n      operationId: listMore\n      responses:\n        \"200\": {description: ok}\ncomponents:"));
    Case("drift: additive change passes (exit 0)", () => CmdDrift(new[] { additive, $"--old={goodC}" }) == 0);

    // ── feature state machine ──
    Directory.CreateDirectory(fdir);
    Case("init-feature creates status.yaml + traceability.md", () =>
        CmdInitFeature(new[] { fdir }) == 0 && File.Exists(Path.Combine(fdir, "status.yaml")) && File.Exists(Path.Combine(fdir, "traceability.md")));
    Case("verify-state: fresh feature is valid", () => CmdVerifyState(new[] { fdir }) == 0);

    var specPath = Path.Combine(fdir, "spec.md");
    File.WriteAllText(specPath, "# Feature Specification: Demo\n## User scenarios\nGiven a user, When they submit, Then it works\n" +
        "## Functional requirements\n| REQ-DEM-001 | System MUST do X | P1 |\n## Acceptance criteria\n| AC-DEM-001 | REQ-DEM-001 | Given X | When Y | Then Z |\n" +
        "## Open questions and assumptions\n- none\n## Success criteria\n- done\n");

    Case("set-stage: 'approved' is refused", () => CmdSetStage(new[] { fdir, "specification", "approved" }) == 1);
    Case("set-stage: non-skippable stage cannot be skipped", () => CmdSetStage(new[] { fdir, "specification", "skipped" }) == 1);
    Case("approve: refused without a recorded passing spec gate", () => ApproveCore(fdir, "specification", "alice") == 1);
    Case("gate specification: passes and is recorded", () =>
        CmdGate(new[] { fdir, "specification" }) == 0 && LoadStatus(fdir).Validation["spec"].Status == "passed");
    Case("approve: out-of-order stage is refused", () => ApproveCore(fdir, "architecture", "alice") == 1);
    Case("approve: specification succeeds with gate evidence", () => ApproveCore(fdir, "specification", "alice") == 0);
    Case("verify-state: valid after approval", () => CmdVerifyState(new[] { fdir }) == 0);
    Case("approve via CLI is refused when stdin is redirected", () =>
        !Console.IsInputRedirected || CmdApprove(new[] { fdir, "clarification", "bob" }) == 1);

    var original = File.ReadAllText(specPath);
    File.AppendAllText(specPath, "\n- sneaky late edit\n");
    Case("verify-state: edit after approval is detected as STALE", () => CmdVerifyState(new[] { fdir }) == 1);
    File.WriteAllText(specPath, original);
    Case("verify-state: restoring the artifact clears staleness", () => CmdVerifyState(new[] { fdir }) == 0);

    // tamper 1: edit approver in the ledger without recomputing the hash
    var st = LoadStatus(fdir); st.HumanApprovals.Granted[0].ApprovedBy = "mallory"; SaveStatus(fdir, st);
    Case("verify-state: tampered ledger entry is detected", () => CmdVerifyState(new[] { fdir }) == 1);
    st = LoadStatus(fdir); st.HumanApprovals.Granted[0].ApprovedBy = "alice"; SaveStatus(fdir, st);

    // tamper 2: mark a stage approved without any ledger entry
    st = LoadStatus(fdir); st.Stages["clarification"].Status = "approved"; SaveStatus(fdir, st);
    Case("verify-state: approved stage with no ledger entry is detected", () => CmdVerifyState(new[] { fdir }) == 1);
    st = LoadStatus(fdir); st.Stages["clarification"].Status = "pending"; SaveStatus(fdir, st);

    // tamper 3: claim a passing check without evidence
    st = LoadStatus(fdir); st.Validation["build"].Status = "passed"; SaveStatus(fdir, st);
    Case("verify-state: 'passed' without exit-code evidence is detected", () => CmdVerifyState(new[] { fdir }) == 1);
    st = LoadStatus(fdir); st.Validation["build"] = new RunRec(); SaveStatus(fdir, st);

    // unknown key rejection
    var yamlPath = Path.Combine(fdir, "status.yaml");
    var backup = File.ReadAllText(yamlPath);
    File.AppendAllText(yamlPath, "bogus_key: 1\n");
    Case("verify-state: unknown key in status.yaml is rejected", () => CmdVerifyState(new[] { fdir }) == 1);
    File.WriteAllText(yamlPath, backup);

    // ── evidence ──
    Case("evidence: passing command recorded as passed", () =>
        CmdEvidence(new[] { fdir, "build", "dotnet", "--version" }) == 0 && LoadStatus(fdir).Validation["build"].Status == "passed");
    Case("evidence: failing command recorded as failed with real exit code", () =>
        CmdEvidence(new[] { fdir, "unit_tests", "dotnet", "this-subcommand-does-not-exist-xyz" }) != 0
        && LoadStatus(fdir).Validation["unit_tests"] is { Status: "failed", ExitCode: not 0 and not null });
    Case("evidence: unknown check name is refused", () => CmdEvidence(new[] { fdir, "made_up", "dotnet", "--version" }) == 1);

    // ── spec validation ──
    Case("spec: valid spec passes", () => CmdSpec(new[] { fdir }) == 0);
    File.WriteAllText(specPath, "# t\n## Nothing\n");
    Case("spec: missing sections fail", () => CmdSpec(new[] { fdir }) == 1);
    File.WriteAllText(specPath, original.Replace("REQ-DEM-001", "REQ-XXX-001"));
    Case("spec: template placeholder IDs fail", () => CmdSpec(new[] { fdir }) == 1);
    File.WriteAllText(specPath, original + "\n[QUESTION: who approves?]\n");
    Case("spec: open question warns normally, fails when strict", () =>
        CmdSpec(new[] { fdir }) == 0 && CmdSpec(new[] { fdir, "--strict-questions" }) == 1);
    File.WriteAllText(specPath, original);

    // ── traceability (derived from tests) ──
    Case("traceability strict: AC with no test fails", () => CmdTraceability(new[] { fdir }, true) == 1);
    Directory.CreateDirectory(Path.Combine(tmp, "tests"));
    File.WriteAllText(Path.Combine(tmp, "tests", "DemoTests.cs"), "// covers AC-DEM-001\npublic class DemoTests {}\n");
    Case("traceability strict: AC referenced by a test passes", () => CmdTraceability(new[] { fdir }, true) == 0);
    File.Delete(Path.Combine(tmp, "tests", "DemoTests.cs"));
    File.AppendAllText(Path.Combine(fdir, "traceability.md"), "\n| REQ-DEM-001 | AC-DEM-001 | | | T001 | | ✅ covered |\n");
    Case("traceability: claimed-covered without a test is rejected", () => CmdTraceability(new[] { fdir }) == 1);

    // ── ux component map ──
    Directory.CreateDirectory(Path.Combine(fdir, "ux"));
    var cm = Path.Combine(fdir, "ux", "component-map.yaml");
    File.WriteAllText(cm, "screens:\n  - id: UX-DEM-001\n    name: List\n    route: /items\n    components: [{name: Table, source: existing}]\n    states: [loading, empty, populated, error, forbidden]\n");
    Case("ux: complete component map passes", () => CheckComponentMap(fdir) == 0);
    File.WriteAllText(cm, "screens:\n  - id: UX-DEM-001\n    name: List\n    states: [populated]\n");
    Case("ux: missing UI states fail", () => CheckComponentMap(fdir) == 1);

    // ── current-feature resolution ──
    File.WriteAllText(Path.Combine(tmp, ".specify", "feature.json"), $"{{\"feature_directory\": {System.Text.Json.JsonSerializer.Serialize(fdir)}}}");
    Case("resolve: SPECIFY_FEATURE_DIRECTORY env var is honoured", () =>
    {
        Environment.SetEnvironmentVariable("SPECIFY_FEATURE_DIRECTORY", fdir);
        try { return ResolveSpecDir(Array.Empty<string>()) == fdir; }
        finally { Environment.SetEnvironmentVariable("SPECIFY_FEATURE_DIRECTORY", null); }
    });

    // ── secrets ──
    var sf = Path.Combine(tmp, "config.yaml");
    File.WriteAllText(sf, "db:\n  password: SuperSecret1234!\n");
    Case("secrets: detects a hard-coded password", () => CmdSecrets(new[] { sf }) == 1);
    Case("secrets: does not echo the secret value", () =>
    {
        var sw = new StringWriter(); var prev = Console.Out; Console.SetOut(sw);
        try { CmdSecrets(new[] { sf }); } finally { Console.SetOut(prev); }
        return !sw.ToString().Contains("SuperSecret1234");
    });
    File.WriteAllText(sf, "db:\n  host: localhost\n");
    Case("secrets: clean file passes", () => CmdSecrets(new[] { sf }) == 0);

    // ── chat approval ──
    var fdir2 = Path.Combine(tmp, "specs", "002-chat");
    Directory.CreateDirectory(fdir2);
    CmdInitFeature(new[] { fdir2 });
    File.WriteAllText(Path.Combine(fdir2, "spec.md"), original);
    CmdGate(new[] { fdir2, "specification" });
    CmdGate(new[] { fdir2, "clarification" });
    Case("approve-chat: refused without the user's reply", () => CmdApproveChat(new[] { fdir2, "specification", "alice" }) == 1);
    Case("approve-chat: records channel and verbatim reply", () =>
        CmdApproveChat(new[] { fdir2, "specification", "alice", "yes,", "approve" }) == 0
        && LoadStatus(fdir2).HumanApprovals.Granted[0] is { Channel: "chat", Quote: "yes, approve" });
    Case("verify-state: valid chat approval", () => CmdVerifyState(new[] { fdir2 }) == 0);
    st = LoadStatus(fdir2); st.HumanApprovals.Granted[0].Quote = "something else"; SaveStatus(fdir2, st);
    Case("verify-state: edited chat reply is detected", () => CmdVerifyState(new[] { fdir2 }) == 1);
    st = LoadStatus(fdir2); st.HumanApprovals.Granted[0].Quote = "yes, approve"; SaveStatus(fdir2, st);
    Case("approve-chat: disabled by SDLC_REQUIRE_TERMINAL_APPROVAL=1", () =>
    {
        Environment.SetEnvironmentVariable("SDLC_REQUIRE_TERMINAL_APPROVAL", "1");
        try { return CmdApproveChat(new[] { fdir2, "clarification", "alice", "yes" }) == 1
                     && LoadStatus(fdir2).Stages["clarification"].Status != "approved"; }
        finally { Environment.SetEnvironmentVariable("SDLC_REQUIRE_TERMINAL_APPROVAL", null); }
    });
    Case("approve-chat: allowed again once the variable is cleared", () => CmdApproveChat(new[] { fdir2, "clarification", "alice", "yes" }) == 0);

    try { Directory.Delete(tmp, true); } catch { /* best effort */ }

    Banner("selftest summary");
    Console.WriteLine($"  Passed: {passed}   Failed: {failed}");
    return failed > 0 ? 1 : 0;
}

static void PrintHelp() => Console.WriteLine("""
sdlc.cs - deterministic SDLC gate tooling   (dotnet run scripts/sdlc/sdlc.cs -- <command>)

  init-feature [<spec-dir>]                      Create status.yaml + traceability.md
  set-stage    <spec-dir> <stage> <status>       pending|in_progress|failed|skipped
  approve      <spec-dir> <stage> <who>          HUMAN ONLY (interactive); hashes stage artifacts
  approve-chat <spec-dir> <stage> <who> <reply>  Record an approval the user gave in chat (stores their verbatim reply)
  verify-state [<spec-dir>]                      Check status, approval ledger, staleness, evidence
  status       [<spec-dir>]                      Read-only progress report (stages, checks, tasks, integrity)
  openapi      <contract.yaml>                   Validate an OpenAPI 3.x document
  drift        <new.yaml> [--old=<path>]         Breaking-change detection (exit 2 = breaking)
  spec         [<spec-dir>] [--strict-questions] Validate spec.md
  traceability [<spec-dir>] [--strict]           AC coverage derived from test sources
  evidence     <spec-dir> <check> <cmd> [args]   Run command, record real exit code in status.yaml
  gate         [<spec-dir>] <gate>               specification|clarification|contract|ux|traceability|state
  secrets      <path>...                         Scan for secrets (never prints values)
  selftest                                       Run built-in tests

Stages : specification clarification architecture api_contract ux task_decomposition implementation verification
Checks : spec spec_strict contract_tests contract_drift ux traceability build unit_tests integration_tests playwright security
""");

// ─────────────────────────────────────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────────────────────────────────────
static class Cfg
{
    public static readonly string[] Stages =
        { "specification", "clarification", "architecture", "api_contract", "ux", "task_decomposition", "implementation", "verification" };

    public static readonly HashSet<string> Skippable = new() { "clarification", "ux" };
    public static readonly HashSet<string> StageStatuses = new() { "pending", "in_progress", "approved", "failed", "skipped" };
    public static readonly HashSet<string> RunStatuses = new() { "pending", "passed", "failed", "skipped" };

    public static readonly string[] Checks =
        { "spec", "spec_strict", "contract_tests", "contract_drift", "ux", "traceability", "build", "unit_tests", "integration_tests", "playwright", "security" };

    public static readonly string[] RequiredUiStates = { "loading", "empty", "populated", "error", "forbidden" };

    public static readonly Dictionary<string, string[]> StageArtifacts = new()
    {
        ["specification"]      = new[] { "spec.md" },
        ["clarification"]      = new[] { "spec.md" },
        ["architecture"]       = new[] { "plan.md" },
        ["api_contract"]       = new[] { "contracts/openapi.yaml" },
        ["ux"]                 = new[] { "ux/user-flows.md", "ux/component-map.yaml" },
        ["task_decomposition"] = new[] { "tasks.md" },
        ["implementation"]     = Array.Empty<string>(),
        ["verification"]       = new[] { "validation-report.md", "traceability.md" },
    };

    // Deterministic checks that must have a recorded passing run before a human can approve the stage.
    public static string[] ApprovalRequires(string stage, string specDir) => stage switch
    {
        "specification" => new[] { "spec" },
        "clarification" => new[] { "spec_strict" },
        "api_contract"  => new[] { "contract_tests" },
        "ux"            => new[] { "ux" },
        "verification"  => new[] { "spec", "traceability", "build", "unit_tests" }
                              .Concat(File.Exists(Path.Combine(specDir, "contracts", "openapi.yaml")) ? new[] { "contract_tests" } : Array.Empty<string>())
                              .ToArray(),
        _ => Array.Empty<string>()
    };
}

class StatusDoc
{
    public string Feature { get; set; } = "";
    public string FeatureNumber { get; set; } = "";
    public string CurrentStage { get; set; } = "specification";
    public Dictionary<string, StageRec> Stages { get; set; } = new();
    public Dictionary<string, RunRec> Validation { get; set; } = new();
    public ApprovalLedger HumanApprovals { get; set; } = new();
    public List<string> Decisions { get; set; } = new();
    public string NextAction { get; set; } = "";
    public string LastUpdated { get; set; } = "";
}

class StageRec
{
    public string Status { get; set; } = "pending";
    public string ApprovedBy { get; set; } = "";
    public string ApprovedAt { get; set; } = "";
    public Dictionary<string, string> ArtifactHashes { get; set; } = new();
}

class RunRec
{
    public string Status { get; set; } = "pending";
    public string Command { get; set; } = "";
    public int? ExitCode { get; set; }
    public string RunAt { get; set; } = "";
    public string OutputSha { get; set; } = "";
}

class ApprovalLedger
{
    public List<string> Pending { get; set; } = new();
    public List<Grant> Granted { get; set; } = new();
}

class Grant
{
    public string Stage { get; set; } = "";
    public string ApprovedBy { get; set; } = "";
    public string ApprovedAt { get; set; } = "";
    public Dictionary<string, string> Artifacts { get; set; } = new();
    public string Channel { get; set; } = "terminal";   // terminal | chat
    public string Quote { get; set; } = "";             // verbatim user reply, chat channel only
    public string Hash { get; set; } = "";
}

class ComponentMap
{
    public List<ScreenRec> Screens { get; set; } = new();
}

class ScreenRec
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Route { get; set; } = "";
    public List<ComponentRef> Components { get; set; } = new();
    public List<string> States { get; set; } = new();
}

class ComponentRef
{
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
}

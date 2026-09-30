// sq — the SharpQuest helper.
//
//   sq test            build, run every test, show a per-lab summary
//   sq test 02         only Lab 02's contract tests
//   sq update          fetch the latest contracts, reference solutions and catch-up kits
//   sq report          show the last results again, without re-running
//   (from Lab 03 on, sq test also runs your tests against deliberately broken code: see contracts/**/mutation.json)
//   sq help
//
// `sq` is a shortcut for `dotnet run --file sq.cs -- ...` (see sq.cmd and sq.sh).
// MANAGED FILE: `sq update` and CI replace it with the official copy.

#:property PublishAot=false

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

Console.OutputEncoding = Encoding.UTF8;

var root = Repo.FindRoot();
var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
var rest = args.Skip(1).ToArray();

return command switch
{
    "test" => Commands.Test(root, rest),
    "update" => await Commands.Update(root),
    "report" => Commands.ReportOnly(root, rest),
    _ => Commands.Help(),
};

static class Commands
{
    public static int Help()
    {
        Console.WriteLine("""
            sq — SharpQuest helper

              sq test          build and run every test, then show a summary per lab
              sq test 02       only the Lab 02 contract tests
              sq update        get the latest contracts from the course (do this at the start of each lab)
              sq report        show the last results again

            Your tests:        tests/Capstone.Tests      (yours to write)
            Contract tests:    contracts/                (managed — read them, don't edit them)
            Past solutions:    reference/                (managed — read, adapt to your theme, don't copy blindly)
            Catch-up kits:     catchup/                  (managed — borrowed plumbing so a missed lab doesn't stop the next one)
            """);
        return 0;
    }

    /// <summary>
    /// What `sq test` builds and runs. Everything the score comes from, and nothing else —
    /// a UI project, a spike or a scratch console app in the solution can't take the grade down with it.
    /// </summary>
    public static readonly string[] GradedProjects =
    [
        Path.Combine("contracts", "SharpQuest.Contracts.Tests", "SharpQuest.Contracts.Tests.csproj"),
        Path.Combine("tests", "Capstone.Tests", "Capstone.Tests.csproj"),
    ];

    public static int Test(string root, string[] options)
    {
        var ci = options.Contains("--ci");
        var lab = options.FirstOrDefault(o => Regex.IsMatch(o, @"^\d{1,2}$"));
        var resultsDir = Path.Combine(root, ".sq", "results");
        if (Directory.Exists(resultsDir)) Directory.Delete(resultsDir, recursive: true);

        // Labs that grade YOUR tests (Lab 03) first run them against deliberately broken code.
        Mutation.RunAll(root, ci, lab is null ? null : int.Parse(lab).ToString("D2"));

        // The graded projects by name, NOT the whole solution. From Lab 09 a capstone grows a UI
        // project, and a WPF one cannot even build on the Linux machine CI runs on — grading the
        // solution would turn every lab red because of a window. What is scored is your domain
        // library (through the contract tests) and your own tests, so those are what get built.
        var exitCode = 0;
        foreach (var project in GradedProjects)
        {
            if (!File.Exists(Path.Combine(root, project))) continue;

            var dotnetArgs = new List<string>
            {
                "test", project,
                "--logger", "trx",
                "--results-directory", resultsDir,
            };
            if (ci) dotnetArgs.AddRange(["--configuration", "Release"]);
            if (lab is not null) dotnetArgs.AddRange(["--filter", $"FullyQualifiedName~.Lab{int.Parse(lab):D2}."]);

            Console.WriteLine($"> dotnet {string.Join(' ', dotnetArgs)}");
            var result = Run(root, Dotnet, dotnetArgs);
            if (result != 0) exitCode = result;
        }

        var results = Trx.ReadAll(resultsDir);
        if (results.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("No test results. The build probably failed — the first error above is the one to fix.");
            if (ci) Report.WriteCi(root, results, buildFailed: true);
            return exitCode == 0 ? 1 : exitCode;
        }

        Report.Print(root, results);
        if (ci) Report.WriteCi(root, results, buildFailed: false);
        return exitCode;
    }

    public static int ReportOnly(string root, string[] options)
    {
        var results = Trx.ReadAll(Path.Combine(root, ".sq", "results"));
        if (results.Count == 0)
        {
            Console.WriteLine("No results yet. Run: sq test");
            return 1;
        }
        Report.Print(root, results);
        if (options.Contains("--ci")) Report.WriteCi(root, results, buildFailed: false);
        return results.All(r => r.Passed || r.Skipped) ? 0 : 1;
    }

    public static async Task<int> Update(string root)
    {
        var config = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "sharpquest.json")))!;
        var upstream = (string?)config["upstream"] ?? "";
        var branch = (string?)config["branch"] ?? "main";
        var source = Environment.GetEnvironmentVariable("SQ_UPSTREAM_ZIP")
                     ?? $"https://codeload.github.com/{upstream}/zip/refs/heads/{branch}";

        if (upstream.Contains("CHANGE-ME") && Environment.GetEnvironmentVariable("SQ_UPSTREAM_ZIP") is null)
        {
            Console.WriteLine("sharpquest.json still says CHANGE-ME. Ask your instructor for the course repository name.");
            return 1;
        }

        var before = Repo.ReleasedLabs(root);
        var referencesBefore = Repo.ReferenceLabs(root);
        var kitsBefore = Repo.CatchUpLabs(root);
        var temp = Path.Combine(Path.GetTempPath(), "sq-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Console.WriteLine($"Downloading {source} ...");
            var zipPath = Path.Combine(temp, "upstream.zip");
            if (File.Exists(source))
            {
                File.Copy(source, zipPath);
            }
            else
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                await using var download = await http.GetStreamAsync(source);
                await using var file = File.Create(zipPath);
                await download.CopyToAsync(file);
            }

            var extracted = Path.Combine(temp, "x");
            ZipFile.ExtractToDirectory(zipPath, extracted);
            // GitHub zips contain a single top-level folder, e.g. sharpquest-template-main/
            var official = Directory.GetDirectories(extracted) is [var only] && !File.Exists(Path.Combine(extracted, "sq.cs"))
                ? only
                : extracted;

            var changed = new List<string>();
            foreach (var path in Repo.Managed)
            {
                var from = Path.Combine(official, path);
                var to = Path.Combine(root, path);
                if (!File.Exists(from) && !Directory.Exists(from)) continue;
                if (Repo.Hash(from) == Repo.Hash(to)) continue;

                Repo.Replace(from, to);
                changed.Add(path);
            }

            var after = Repo.ReleasedLabs(root);
            Console.WriteLine(changed.Count == 0 ? "Already up to date." : $"Updated: {string.Join(", ", changed)}");
            foreach (var added in after.Except(before))
                Console.WriteLine($"New lab contract: Lab {added} — read contracts/SharpQuest.Contracts/Lab{added}/");
            foreach (var added in Repo.ReferenceLabs(root).Except(referencesBefore))
                Console.WriteLine($"Reference solution published: reference/{added}/");
            foreach (var added in Repo.CatchUpLabs(root).Except(kitsBefore))
                Console.WriteLine($"Catch-up kit published: catchup/{added}/  (read catchup/README.md before you use it)");
            if (changed.Count > 0) Console.WriteLine("Commit these changes along with your own work.");
            return 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or TaskCanceledException)
        {
            Console.WriteLine($"Update failed: {ex.Message}");
            return 1;
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch (IOException) { }
        }
    }

    /// <summary>The dotnet executable. SQ_DOTNET overrides it (used only to test sq itself).</summary>
    public static string Dotnet => Environment.GetEnvironmentVariable("SQ_DOTNET") ?? "dotnet";

    public static int Run(string workingDir, string file, IEnumerable<string> arguments,
                          IDictionary<string, string?>? env = null, bool quiet = false)
    {
        var psi = new ProcessStartInfo(file) { WorkingDirectory = workingDir, RedirectStandardOutput = quiet, RedirectStandardError = quiet };
        foreach (var a in arguments) psi.ArgumentList.Add(a);
        foreach (var name in Mutation.EnvNames) psi.Environment.Remove(name);   // never let a mutant leak into a normal run
        foreach (var (k, v) in env ?? new Dictionary<string, string?>())
            if (v is null) psi.Environment.Remove(k); else psi.Environment[k] = v;
        using var process = Process.Start(psi)!;
        if (quiet)
        {
            var o = process.StandardOutput.ReadToEndAsync();
            var e = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            _ = o.Result + e.Result;
        }
        else process.WaitForExit();
        return process.ExitCode;
    }
}

record TestResult(string Group, string Name, bool Passed, bool Skipped, string? Message);

/// <summary>
/// Mutation testing, for labs that grade the student's own tests.
/// A lab opts in with contracts/**/mutation.json:
///   { "lab": "03", "project": "tests/Capstone.Tests/Capstone.Tests.csproj",
///     "classFilter": "SearchMatcher", "env": "SQ_MUTANT", "mutants": 8 }
/// Run 0: all of the student's tests, correct code. Runs 1..N: only the spec tests, one mutant each.
/// Results go to .sq/mutation/LabNN.json, which that lab's contract tests read.
/// </summary>
static class Mutation
{
    public static IEnumerable<string> EnvNames => ["SQ_MUTANT"];

    public static void RunAll(string root, bool ci, string? onlyLab)
    {
        var contracts = Path.Combine(root, "contracts");
        if (!Directory.Exists(contracts)) return;
        var specs = Directory.EnumerateFiles(contracts, "mutation.json", SearchOption.AllDirectories)
            .Select(f => JsonNode.Parse(File.ReadAllText(f))!)
            .Where(s => onlyLab is null || (string?)s["lab"] == onlyLab)
            .ToList();
        if (specs.Count == 0) return;

        var config = ci ? "Release" : "Debug";
        foreach (var project in Commands.GradedProjects)
        {
            if (!File.Exists(Path.Combine(root, project))) continue;
            Console.WriteLine($"> dotnet build {project} -c {config}   (needed for the mutation check)");
            if (Commands.Run(root, Commands.Dotnet, ["build", project, "-c", config, "-nologo", "-v", "q"], quiet: true) != 0)
                return;   // the main test run will show the build errors
        }

        foreach (var spec in specs) RunOne(root, config, spec);
    }

    private static void RunOne(string root, string config, JsonNode spec)
    {
        var lab = (string)spec["lab"]!;
        var project = (string)spec["project"]!;
        var filter = (string)spec["classFilter"]!;
        var envName = (string)spec["env"]!;
        var count = (int)spec["mutants"]!;
        var projectDir = Path.GetDirectoryName(Path.Combine(root, project))!;
        var assemblyName = Path.GetFileNameWithoutExtension(project);
        var dll = Directory.Exists(Path.Combine(projectDir, "bin", config))
            ? Directory.EnumerateFiles(Path.Combine(projectDir, "bin", config), assemblyName + ".dll", SearchOption.AllDirectories).FirstOrDefault()
            : null;

        var outDir = Path.Combine(root, ".sq", "mutation");
        Directory.CreateDirectory(outDir);
        var outFile = Path.Combine(outDir, $"Lab{lab}.json");

        // Same binaries as last time → same answer. Mutation runs are slow; don't repeat them.
        var key = dll is null ? "" : BinariesKey(Path.GetDirectoryName(dll)!, spec.ToJsonString());
        if (File.Exists(outFile) && key.Length > 0 && (string?)JsonNode.Parse(File.ReadAllText(outFile))?["key"] == key)
        {
            PrintSummary(lab, JsonNode.Parse(File.ReadAllText(outFile))!, cached: true);
            return;
        }

        var work = Path.Combine(outDir, $"Lab{lab}-runs");
        if (Directory.Exists(work)) Directory.Delete(work, recursive: true);

        List<TestResult> RunTests(int mutant, bool onlySpec)
        {
            var dir = Path.Combine(work, mutant.ToString());
            var args = new List<string> { "test", project, "--no-build", "-c", config, "--logger", "trx", "--results-directory", dir };
            if (onlySpec) args.AddRange(["--filter", $"FullyQualifiedName~{filter}"]);
            Commands.Run(root, Commands.Dotnet, args,
                new Dictionary<string, string?> { [envName] = mutant == 0 ? null : mutant.ToString() }, quiet: true);
            return Trx.ReadAll(dir);
        }

        bool IsSpec(TestResult r) => r.Name.Split('.')[0].Contains(filter, StringComparison.OrdinalIgnoreCase);

        Console.WriteLine($"  Lab {lab}: running your tests against {count} broken versions ...");
        var baseline = RunTests(0, onlySpec: false);
        var specTests = baseline.Where(IsSpec).ToList();
        var result = new JsonObject
        {
            ["lab"] = lab,
            ["key"] = key,
            ["testAssembly"] = dll,
            ["ownTotal"] = baseline.Count(r => !r.Skipped),
            ["ownFailed"] = baseline.Count(r => !r.Passed && !r.Skipped),
            ["specTotal"] = specTests.Count(r => !r.Skipped),
            ["specFailed"] = specTests.Count(r => !r.Passed && !r.Skipped),
        };

        var mutants = new JsonObject();
        var usable = specTests.Count > 0 && specTests.All(r => r.Passed || r.Skipped);
        for (var m = 1; m <= count; m++)
        {
            if (!usable) { mutants[m.ToString()] = new JsonObject { ["caught"] = false, ["ran"] = false }; continue; }
            var run = RunTests(m, onlySpec: true);
            // A crashed run (no results at all) also counts as caught: the tests noticed something.
            var caught = run.Count == 0 || run.Any(r => !r.Passed && !r.Skipped);
            mutants[m.ToString()] = new JsonObject { ["caught"] = caught, ["ran"] = true };
        }
        result["mutants"] = mutants;
        File.WriteAllText(outFile, result.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        PrintSummary(lab, result, cached: false);
    }

    /// <summary>Hash of the compiled code that matters (builds are deterministic, so unchanged source = same key).</summary>
    private static string BinariesKey(string binDir, string specText)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(Encoding.UTF8.GetBytes(specText));
        foreach (var f in Directory.EnumerateFiles(binDir, "*.dll").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(f);
            if (!(name.StartsWith("Capstone.") || name.StartsWith("SharpQuest."))) continue;
            sha.AppendData(Encoding.UTF8.GetBytes(name));
            sha.AppendData(File.ReadAllBytes(f));
        }
        return Convert.ToHexStringLower(sha.GetHashAndReset())[..16];
    }

    private static void PrintSummary(string lab, JsonNode r, bool cached)
    {
        var m = r["mutants"]!.AsObject();
        var caught = m.Count(x => (bool?)x.Value?["caught"] == true);
        var note = (int?)r["specTotal"] == 0 ? " (no spec tests found yet)"
                 : (int?)r["specFailed"] > 0 ? " (your spec tests must pass on the correct code first)" : "";
        Console.WriteLine($"  Lab {lab}: broken versions caught {caught}/{m.Count}{note}{(cached ? "  [unchanged since last run]" : "")}");
    }
}

static class Trx
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static List<TestResult> ReadAll(string dir)
    {
        if (!Directory.Exists(dir)) return [];
        return Directory.EnumerateFiles(dir, "*.trx", SearchOption.AllDirectories).SelectMany(Read).ToList();
    }

    private static IEnumerable<TestResult> Read(string path)
    {
        var doc = XDocument.Load(path);
        var classes = doc.Descendants(Ns + "UnitTest").ToDictionary(
            t => (string)t.Attribute("id")!,
            t => (string?)t.Element(Ns + "TestMethod")?.Attribute("className") ?? "");

        foreach (var r in doc.Descendants(Ns + "UnitTestResult"))
        {
            var className = classes.GetValueOrDefault((string)r.Attribute("testId")!, "");
            var outcome = (string?)r.Attribute("outcome") ?? "";
            var message = r.Element(Ns + "Output")?.Element(Ns + "ErrorInfo")?.Element(Ns + "Message")?.Value;
            yield return new TestResult(
                Group(className),
                ShortName((string?)r.Attribute("testName") ?? "?"),
                outcome == "Passed",
                outcome == "NotExecuted",
                message);
        }
    }

    /// <summary>"SharpQuest.Contracts.Tests.Lab02.StoreTests" → "02"; anything else → "own".</summary>
    private static string Group(string className)
    {
        var m = Regex.Match(className, @"^SharpQuest\.Contracts\.Tests\.Lab(\d{2})\.");
        return m.Success ? m.Groups[1].Value : "own";
    }

    private static string ShortName(string testName)
    {
        // "SharpQuest.Contracts.Tests.Lab02.StoreTests.Added_item(seed: 1)" → "StoreTests.Added_item(seed: 1)"
        var paren = testName.IndexOf('(');
        var head = paren < 0 ? testName : testName[..paren];
        var parts = head.Split('.');
        var shortHead = parts.Length >= 2 ? $"{parts[^2]}.{parts[^1]}" : head;
        return paren < 0 ? shortHead : shortHead + testName[paren..];
    }
}

static class Report
{
    public static void Print(string root, List<TestResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("──────────── SharpQuest ────────────");
        var notStarted = new List<string>();
        foreach (var group in Ordered(results))
        {
            if (NotStarted(group))
            {
                notStarted.Add(group.Key);
                continue;
            }
            var (passed, total) = Count(group);
            var label = group.Key == "own" ? "Your tests" : $"Lab {group.Key}";
            var mark = passed == total ? "OK  " : "FAIL";
            var extra = group.Key == "own" ? "" : $"   contract ≈ {ContractPoints(passed, total):0.0} / 4";
            Console.WriteLine($" {mark} {label,-11} {passed,3}/{total,-3}{extra}");

            foreach (var (name, message) in Failures(group))
                Console.WriteLine($"        ✗ {name}\n          {message}");
        }
        if (notStarted.Count > 0)
        {
            Console.WriteLine($" --   Not started: {string.Join(", ", notStarted.Select(l => $"Lab {l}"))}");
            Console.WriteLine("      (a lab starts when src/Capstone.Core/Wiring/ has a class for it)");
        }
        if (Directory.Exists(Path.Combine(root, "reference")))
            Console.WriteLine(" Reference solutions for past labs: reference/");
        if (Directory.Exists(Path.Combine(root, "catchup")))
            Console.WriteLine(" Catch-up kits for missed labs:     catchup/");
        Console.WriteLine("────────────────────────────────────");
        Console.WriteLine("Contract points are indicative. Your score comes from the CI run on GitHub.");
    }

    public static void WriteCi(string root, List<TestResult> results, bool buildFailed)
    {
        var labs = new JsonObject();
        foreach (var group in Ordered(results).Where(g => g.Key != "own"))
        {
            var (passed, total) = Count(group);
            labs[group.Key] = NotStarted(group)
                ? new JsonObject { ["started"] = false }
                : new JsonObject { ["started"] = true, ["passed"] = passed, ["total"] = total };
        }
        var (ownPassed, ownTotal) = Count(results.Where(r => r.Group == "own"));

        var score = new JsonObject
        {
            ["schema"] = 2,
            ["generatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
            ["commit"] = Environment.GetEnvironmentVariable("GITHUB_SHA"),
            ["buildFailed"] = buildFailed,
            ["contractsHash"] = Repo.Hash(Path.Combine(root, "contracts")),
            ["labs"] = labs,
            ["ownTests"] = new JsonObject { ["passed"] = ownPassed, ["total"] = ownTotal },
            ["nullableEscapes"] = new JsonArray(Repo.NullableEscapes(root).Select(f => (JsonNode)f).ToArray()),
        };
        File.WriteAllText(Path.Combine(root, "score.json"),
            score.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

        var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrEmpty(summaryPath)) return;

        var md = new StringBuilder();
        md.AppendLine("## SharpQuest checks").AppendLine();
        if (buildFailed)
        {
            md.AppendLine("**The build failed.** Open the *Build and test* step above and fix the first error.");
        }
        else
        {
            md.AppendLine("| | Passed | Contract (indicative) |").AppendLine("|---|---|---|");
            foreach (var group in Ordered(results))
            {
                var label = group.Key == "own" ? "Your tests" : $"Lab {group.Key}";
                if (NotStarted(group))
                {
                    md.AppendLine($"| ⏳ {label} | not started | — |");
                    continue;
                }
                var (p, t) = Count(group);
                var points = group.Key == "own" ? "—" : $"{ContractPoints(p, t):0.0} / 4";
                md.AppendLine($"| {(p == t ? "✅" : "❌")} {label} | {p} / {t} | {points} |");
            }
            var failures = results.Where(r => !r.Passed && !r.Skipped).ToList();
            if (failures.Count > 0)
            {
                md.AppendLine().AppendLine("### Failing").AppendLine();
                foreach (var (name, message) in Failures(failures))
                    md.AppendLine($"- `{name}` — {message}");
            }
        }
        File.AppendAllText(summaryPath, md.ToString());
    }

    private static IEnumerable<IGrouping<string, TestResult>> Ordered(IEnumerable<TestResult> results) =>
        results.GroupBy(r => r.Group).OrderBy(g => g.Key == "own" ? "zz" : g.Key);

    /// <summary>
    /// Contract tests are never skipped on purpose: they skip only when the lab's wiring class is missing.
    /// So a lab whose every test was skipped hasn't been started.
    /// </summary>
    private static bool NotStarted(IGrouping<string, TestResult> group) =>
        group.Key != "own" && group.All(r => r.Skipped);

    /// <summary>Failures, with many identical messages (e.g. "no wiring class") folded into one line.</summary>
    private static IEnumerable<(string Name, string Message)> Failures(IEnumerable<TestResult> results)
    {
        foreach (var same in results.Where(r => !r.Passed && !r.Skipped).GroupBy(r => FirstLine(r.Message)))
        {
            if (same.Count() >= 3)
                yield return ($"{same.Count()} tests", same.Key);
            else
                foreach (var r in same)
                    yield return (r.Name, same.Key);
        }
    }

    private static (int Passed, int Total) Count(IEnumerable<TestResult> results)
    {
        var counted = results.Where(r => !r.Skipped).ToList();
        return (counted.Count(r => r.Passed), counted.Count);
    }

    private static double ContractPoints(int passed, int total) =>
        total == 0 ? 0 : Math.Round(4.0 * passed / total, 1);

    private static string FirstLine(string? text) =>
        (text ?? "").Split('\n', 2)[0].Trim();
}

static class Repo
{
    /// <summary>Everything `sq update` and CI overwrite with the official copy.</summary>
    public static readonly string[] Managed =
    [
        "contracts",
        "reference",
        "catchup",
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "global.json",
        "sq.cs",
        "sq.cmd",
        "sq.sh",
        ".github/workflows/ci.yml",
    ];

    public static string FindRoot()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SharpQuest.slnx")))
                return dir.FullName;

        Console.WriteLine("Run sq from inside your SharpQuest repository (the folder with SharpQuest.slnx).");
        Environment.Exit(2);
        return "";
    }

    public static List<string> ReleasedLabs(string root)
    {
        var file = Path.Combine(root, "contracts", "RELEASED");
        if (!File.Exists(file)) return [];
        return File.ReadAllLines(file)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToList();
    }

    public static List<string> CatchUpLabs(string root) => LabFolders(root, "catchup");

    public static List<string> ReferenceLabs(string root) => LabFolders(root, "reference");

    private static List<string> LabFolders(string root, string folder)
    {
        var dir = Path.Combine(root, folder);
        return Directory.Exists(dir)
            ? Directory.GetDirectories(dir).Select(Path.GetFileName).OfType<string>().Order().ToList()
            : [];
    }

    /// <summary>Files that switch nullable checking off. Reported to the instructor, not blocked.</summary>
    public static IEnumerable<string> NullableEscapes(string root)
    {
        var src = Path.Combine(root, "src");
        if (!Directory.Exists(src)) yield break;
        var pattern = new Regex(@"^\s*#\s*(nullable\s+disable|pragma\s+warning\s+disable\s+.*CS86)", RegexOptions.Multiline);
        foreach (var file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            if (pattern.IsMatch(File.ReadAllText(file)))
                yield return Path.GetRelativePath(root, file).Replace('\\', '/');
        }
    }

    /// <summary>Content hash of a file or a folder (paths + contents, line endings normalised). Empty if missing.</summary>
    public static string Hash(string path)
    {
        IEnumerable<string> files =
            File.Exists(path) ? [path] :
            Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj"))
                .Order(StringComparer.Ordinal) :
            [];

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var any = false;
        foreach (var f in files)
        {
            any = true;
            var relative = File.Exists(path) ? "" : Path.GetRelativePath(path, f).Replace('\\', '/');
            sha.AppendData(Encoding.UTF8.GetBytes(relative + "\n"));
            sha.AppendData(Encoding.UTF8.GetBytes(File.ReadAllText(f).Replace("\r\n", "\n")));
        }
        return any ? Convert.ToHexStringLower(sha.GetHashAndReset())[..16] : "";
    }

    public static void Replace(string from, string to)
    {
        if (File.Exists(from))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(from, to, overwrite: true);
            return;
        }

        // A folder: drop the old copy (keeping build output out of the way), then copy the new one in.
        if (Directory.Exists(to))
            foreach (var file in Directory.EnumerateFiles(to, "*", SearchOption.AllDirectories)
                         .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj")))
                File.Delete(file);

        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}

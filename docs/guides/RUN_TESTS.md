# How to Run Tests

## Quick start

From the repository root:

```bash
dotnet restore
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj
```

**Windows:** `.\run-tests.bat --all`  
**Linux/macOS:** `./run-tests.sh --all`

**CI parity (Release):** `bash scripts/ci-build-test.sh`  
**Clean artifacts:** `bash scripts/clean.sh`

> Prefer targeting `QualiFrame.Tests` directly — not solution-wide `dotnet test` — when Microsoft.Testing.Platform is enabled (avoids MTP flags reaching `QualiFrame.Runner`). See [docs/context/execution.md](../context/execution.md).

## QualiFrame.Runner CLI

```bash
dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj -- --filter "Category=WebDemo"
dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj -- --parallel
dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj -- --config Release
```

Orchestration is in **`QualiFrame.Runner.Core`** (`QualiFrame.Runner.Core.TestRunner`); the CLI shells out to `dotnet test`.

## Common commands

### Run all tests

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj
```

### Detailed output

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --logger "console;verbosity=detailed"
```

### Filter by name or category

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "FullyQualifiedName~WebDemoApp_AddProductToCart"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=WebDemo"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=WebDemo&Category=P1-High"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Mocking"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Desktop"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Calculator"
```

Desktop tests require **Windows** and FlaUI. `Category=Calculator` is the shipped runnable sample. `Category=Desktop` also includes a login **template** that fails unless `Desktop:AppId` and login locators match a real AUT. See [DESKTOP_AUTOMATION.md](DESKTOP_AUTOMATION.md).

### Parallel / configuration

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --parallel
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --configuration Release
```

### Tests-only solution filter

```bash
dotnet test Qualiframe-CSharp-NUnit_v3.0-TestsOnly.slnf --filter "Category=Web"
```

### TRX / NUnit XML

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --logger "trx;LogFileName=TestResults.trx"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --logger "nunit;LogFileName=TestResults.xml"
```

## Shell scripts

### `run-tests.sh` / `run-tests.bat`

```bash
./run-tests.sh --help
./run-tests.sh --all
./run-tests.sh --web --parallel
./run-tests.sh --filter "Category=WebDemo"
./run-tests.sh --all --clean
```

These scripts expose `--web` / `--api` / `--mobile` shortcuts. There is **no `--desktop` flag**. Run desktop with `--filter "Category=Calculator"` (sample) or `--filter "Category=Desktop"` (includes the login template).

### `scripts/ci-build-test.sh`

Used by GitHub Actions, Azure DevOps, Jenkins, and GitLab CI. Builds and tests `QualiFrame.Tests` in Release with coverage collection. GitLab: `.gitlab-ci.yml` (dotnet 9 SDK image, same script).

## After test execution

| Artifact | Location |
|----------|----------|
| Extent report | `Results/Reports/ExtentReport.html` |
| Allure results | `Results/AllureResults/` |
| Screenshots | `Results/Screenshots/` |
| Video frames (opt-in) | `Results/Videos/` when `TestConfiguration:VideoRecording` is `true` |
| Logs | `Results/Logs/` |
| TRX (if generated) | `test-results.trx` or agent temp directory in CI |

## Troubleshooting

**Tests not found** — rebuild first:

```bash
dotnet build src/QualiFrame.Tests/QualiFrame.Tests.csproj
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj
```

**Reports not generated** — confirm `Reporting:ExtentReport:Enable` in `appsettings.json` and that `Results/` is writable.

**Large `bin/` folders** — run `bash scripts/clean.sh`; build outputs are gitignored.

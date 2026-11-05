# Test execution

## Preferred: test project or tests-only solution filter

Avoid solution-wide `dotnet test` when **Microsoft.Testing.Platform (MTP)** is enabled for the test project and the solution also contains **QualiFrame.Runner**: the host can pass MTP-only flags (`--server`, `--dotnet-test-pipe`) that `QualiFrame.Runner` does not understand.

**Safe patterns:**

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "FullyQualifiedName~PdfValidationEngineTests"
dotnet test Qualiframe-CSharp-NUnit_v3.0-TestsOnly.slnf --filter "Category=Web"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Desktop"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Calculator"
```

`Qualiframe-CSharp-NUnit_v3.0-TestsOnly.slnf` includes Framework, Pdf.Core.Engine, Mocking, and Tests — **not** Runner.

## QualiFrame.Runner CLI

```bash
dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj
dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj -- --filter "Category=WebDemo"
dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj -- --parallel
```

- **CLI host:** `src/QualiFrame.Runner/Program.cs` (`CommandLineParser`).
- **Orchestration:** `src/QualiFrame.Runner.Core` (`QualiFrame.Runner.Core.TestRunner`) — invokes `dotnet test` against `QualiFrame.Tests`; does **not** reference the test project at build time.
- **Runner projects** build **Debug-only** to keep `bin/` small; CI builds **Release** for the test project only.

## Filters and categories

- NUnit: `--filter "FullyQualifiedName~Name"` or category expressions aligned with `[Category("...")]` / Reqnroll tags mapped to categories where configured.
- **Desktop (FlaUI):** Windows-only. `Category=Calculator` is the working sample (Windows Calculator via `Desktop:AppId`). `Category=Desktop` also runs a login template that is not wired to Calculator. Details: [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md).

## Coverage

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Scripts

| Script | Purpose |
|--------|---------|
| `run-tests.sh` / `run-tests.bat` | Interactive filters (Web/API/Mobile, parallel, clean). Targets `QualiFrame.Tests` only. Desktop: `--filter "Category=Calculator"` (no `--desktop` flag). |
| `scripts/ci-build-test.sh` | Shared CI entry point (restore → build → test + coverage). |
| `scripts/clean.sh` | Remove `bin/`, `obj/`, `Results/`, `TestResults/`. |
| `start-appium.bat` | Local Appium helper. |

## CI

All pipelines use `scripts/ci-build-test.sh` against `src/QualiFrame.Tests/QualiFrame.Tests.csproj` in **Release**:

- GitHub Actions: `.github/workflows/ci.yml`
- Azure DevOps: `azure-pipelines.yml`
- Jenkins: `Jenkinsfile`
- GitLab CI: `.gitlab-ci.yml` (same `scripts/ci-build-test.sh` as the others)

See [ci-and-licensing.md](ci-and-licensing.md).

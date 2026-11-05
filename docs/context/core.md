# Core context — QualiFrame C# / NUnit / Reqnroll / Selenium

## Identity

- **Stack:** .NET 8 / 9 / 10 (multi-target), NUnit, Reqnroll (Gherkin), Selenium 4.x, Appium client, FlaUI (Windows desktop / UI Automation), RestSharp, Healenium (shipped `Healenium:Enabled` is `true`; getter falls back to `false` if the key is missing).
- **Version:** 3.1.1 (see `FRAMEWORK_MANIFEST.json` / `CHANGELOG.md`).
- **SDK pin:** `global.json` → SDK 10.0.100, `rollForward: latestFeature` (builds all TFMs).
- **TFMs:** `net8.0;net9.0;net10.0` via `QualiFrameTfms` in `Directory.Build.props` (override with `-p:QualiFrameTfms=net9.0`).

## Layer boundaries (non-negotiable)

| Layer | Project / path | Responsibility |
|--------|----------------|------------------|
| **Framework library** | `src/QualiFrame.Framework` | Reusable automation: POM bases, WebDriver factory/pool/healing, API client, reporting, configuration types, data repositories, PDF integration surface, desktop abstractions, DI extensions. **No application-specific page objects.** |
| **Test implementation** | `src/QualiFrame.Tests` | Application page objects, Reqnroll features/steps/hooks, concrete tests, test-local config copies. |
| **PDF engine** | `src/QualiFrame.Pdf.Core.Engine` | PdfPig-based extraction, comparison, validation pipeline. |
| **Mocking** | `src/QualiFrame.Mocking` | WireMock.Net, OpenAPI-driven stubs, hybrid UI/API helpers. Integration samples: `QualiFrame.Tests/Tests/Integration/Mocking/`. |
| **Runner core** | `src/QualiFrame.Runner.Core` | `TestRunner`, `TestRunnerOptions`, `TestRunnerCommandBuilder` — shells out to `dotnet test`; no Framework/test assembly reference. |
| **Runner CLI** | `src/QualiFrame.Runner` | Thin executable (`CommandLineParser`); Debug-only build output (~2 MB). |

## Canonical commands

```bash
dotnet restore
dotnet build
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj -f net9.0
# or (excludes Runner from solution load)
dotnet test Qualiframe-CSharp-NUnit_v3.0-TestsOnly.slnf -f net9.0
```

Custom runner: `dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj` (supports filters/parallel flags via `QualiFrame.Runner.Core`).

CI parity / clean: `bash scripts/ci-build-test.sh`, `bash scripts/clean.sh`.

## Configuration precedence (summary)

1. **Extent-only:** `extent.properties` overrides JSON Extent keys when both set.
2. **Allure results directory:** env `ALLURE_RESULTS_DIRECTORY` overrides `appsettings.json` when set.
3. **Base:** `appsettings.json` + `appsettings.{Environment}.json`.

Details: [configuration.md](configuration.md).

## Artifact locations

- Allure raw: `Results/AllureResults`
- HTML / Extent-style reports: `Results/Reports`
- Screenshots: `Results/Screenshots`
- Video frames (opt-in): `Results/Videos` when `TestConfiguration:VideoRecording` is `true`
- Logs: `Results/Logs` (when file logging enabled)

## License / compliance

Commercial-use–friendly dependency choices; removed EPPlus, ImageSharp, iText7, NBomber, Applitools Eyes from defaults. Full list: `LICENSE-NOTICE.md`.

# Changelog

## Unreleased (September 2026)

### Data

- **`SqlDataRepository`** — opt-in SQL Server test data via `DataRepositoryFactory` (`SQL` / `SQLSERVER` / `MSSQL` / `DATABASE`). Excel remains the factory default. Uses `Environments:QA:DatabaseConnectionString` and `Database:DefaultTable`.

### Configuration

- Parallel settings read nested `TestConfiguration:ParallelExecution:*` first, then fall back to legacy root `ParallelExecution:*` (`TestConfiguration` and `RunnerConfiguration`).
- **`TestConfiguration:VideoRecording`** (default `false`) — opt-in PNG frame capture via `VideoRecordingManager` under `Results/Videos`. Cloud `Video` capability is set only when the flag is true.

### Desktop

- Desktop automation uses **FlaUI** ([FlaUI/FlaUI](https://github.com/FlaUI/FlaUI), UIA2/UIA3). `@Desktop` scenarios and `DesktopSessionFactory.GetSession()` launch or attach the AUT in-process. Leftover `WinAppDriverManager` and `WindowsDriverFactory` types were deleted.
- Shipped sample is Windows Calculator (`Category=Calculator`). The `@Desktop` login scenario is a page-object template and does not match Calculator locators. Run commands and the limitation are documented in [DESKTOP_AUTOMATION.md](docs/guides/DESKTOP_AUTOMATION.md), [RUN_TESTS.md](docs/guides/RUN_TESTS.md), and `docs/context/`.

### WebDriver / Healenium / QHARE

- Exclusive provider switch: `SelfHealing:Provider` is `Healenium` (shipped default), `Qhare`, or `Off`. `EnhancedWebDriverFactory` wraps **either** `HealeniumDriverWrapper` **or** `QhareDriverWrapper`, never both.
- QHARE path uses `POST /api/v1/heal` against `Qhare:BaseUrl` (default `http://localhost:8000`). Find-element heals send `action: find`. Only `HEALED` candidates replace a miss. `UNRESOLVED` / `SUGGESTED` outcomes are logged with status, confidence, reason, and top alternative. `heal.failed` / `heal.rejected` telemetry is flushed even when the Reqnroll step fails; `heal.validated` still waits for a passing step. Core outages fail closed.
- `HealeniumDriverWrapper` implements JavaScript executor and screenshot APIs, and resolves session IDs through wrapped drivers.
- Shipped `appsettings.json` sets `Healenium:Enabled` to **true**. The getter still falls back to **false** if the key is missing. Disable it or start the Healenium backend at `Healenium:BackendUrl` before running browser tests.
- Evidence-based healing: successful finds persist versioned DOM baselines under `Healenium:EvidencePath`. Healing scores the current DOM against that history, accepts candidates only at/above `Healenium:ScoreCap`, and promotes a new trusted baseline only after a passing Reqnroll step. Failed or low-confidence attempts are audited and never overwrite history.
- When the exact test locator has no baseline, healing falls back to related locators on the same page (local evidence and `GET /healenium/selector/all`) using prefix/token similarity, then confirms the candidate against the live DOM.

### CI / documentation

- **GitLab CI** (`.gitlab-ci.yml`) uses the .NET 9 SDK image and the same `scripts/ci-build-test.sh` target as GitHub Actions, Azure DevOps, and Jenkins.
- `FRAMEWORK_MANIFEST.json` is a schema-backed knowledge model (`FRAMEWORK_MANIFEST.schema.json`, `docs/FRAMEWORK_MANIFEST_GUIDE.md`, `scripts/validate-framework-manifest.py`). Manifest `2.0.10` records leftover desktop driver-compatibility types removed.

### Consistency

- Packable project `<Version>` attributes are **3.1.1** (Framework, Mocking, Pdf.Core.Engine), matching documented framework version. Runner, Runner.Core, and Tests have no package `Version` (not published).
- Leftover `Applitools:*` keys were removed from `appsettings.json` and `TestConfiguration`. Visual example is `VisualTesterExample`.
- Optional Healenium backend stack lives in `healenium/docker-compose.yml`. Tests still do not start Docker.
- `Skills.md` is a skills inventory only; versions are authoritative in `.csproj` / `FRAMEWORK_MANIFEST.json`.
- CI validates the manifest via `scripts/ci-validate-manifest.sh` (or GitLab `validate-manifest`) without changing `scripts/ci-build-test.sh`.

## Version 3.1.1 (June 2026 — Repository hygiene & runner slimming)

### Runner architecture

- **`QualiFrame.Runner.Core`** — new library for `TestRunner`, `TestRunnerOptions`, `TestRunnerCommandBuilder` (`QualiFrame.Runner.Core` namespace).
- **`QualiFrame.Runner`** references Runner.Core only (not `QualiFrame.Tests`); CLI output ~2 MB (Debug) vs ~1.5 GB previously.
- Runner.Core uses **`RunnerConfiguration`** + **`RunnerLog`** (Microsoft.Extensions.Configuration only; no Framework dependency).

### CI / tooling

- Dual CI: **GitHub Actions** (`.github/workflows/ci.yml`) and **Azure DevOps** (`azure-pipelines.yml`), plus **Jenkins** — all delegate to `scripts/ci-build-test.sh`.
- **`scripts/clean.sh`** — removes local `bin/`, `obj/`, `Results/`, `TestResults/`.
- Root **`.gitignore`** for build outputs, IDE files, and report artifacts.
- Runner projects: **Debug-only** configurations; CI builds **Release** for `QualiFrame.Tests` only.

### Documentation

- Feature guides moved from `documents[md files]/` → **`docs/guides/`**; merge/migration reports → **`docs/archive/`**.
- Updated `README.md`, `docs/context/*`, `MEMORY_MAP.yaml`, `FRAMEWORK_MANIFEST.json`, and module READMEs.

### Cleanup

- Removed unused **`DummyWebDriver`**.
- Mocking sample tests moved to **`QualiFrame.Tests/Tests/Integration/Mocking/`** (`Category=Mocking`); `QualiFrame.Tests` references `QualiFrame.Mocking`.
- Removed empty `Runners/` folder; aligned **`extent.properties`** paths with `Results/`.
- Removed commented PDF test stubs in `PdfValidationSuiteTests.cs`.

## Version 3.1.0 (Post-Reporting Enhancement)

**Framework statistics**

- Target framework: .NET 9.0 (compatible with .NET 8.0+)
- NuGet dependencies: 53+ (including ML.NET); see [FRAMEWORK_MANIFEST.json](FRAMEWORK_MANIFEST.json) and [LICENSE-NOTICE.md](LICENSE-NOTICE.md)
- Projects: six-project solution — Framework, Tests, Runner, Runner.Core, Pdf.Core.Engine, Mocking (see `FRAMEWORK_MANIFEST.json`)
- Reqnroll feature files: Web, API, Mobile, Desktop (under `src/QualiFrame.Tests/Features/`)
- Component library: seven reusable web components (Header, Footer, Navigation, Form, Modal, Table, Dropdown)
- Design patterns: 13+ (including Template Method, Adapter, thread-local reporting)
- Reporting: Extent, Allure, custom generators; reporting surfaces are thread-safe for parallel runs (`AsyncLocal<T>`)
- AI/ML: Healenium self-healing; ML-based test selection (`SmartTestSelector`)

### Dependency refresh (May 2026)

- Upgraded core dependencies: Selenium 4.43, Reqnroll 3.3.4, Allure 2.15, Appium 8.2, Coverlet 10, ML.NET 5, WireMock.Net 2.5, aligned `Microsoft.Extensions.*` 10.0.7.
- `Microsoft.OpenApi` / `Microsoft.OpenApi.Readers` remain on stable 1.6.x (no stable 3.x Readers on NuGet at consolidation time).

### License compliance (February 2026)

- Commercial-use alignment: ClosedXML (MIT) for Excel; SkiaSharp (MIT) for images/visual; removed EPPlus, SixLabors.ImageSharp, iText7, NBomber, Applitools Eyes.Selenium.
- See [LICENSE-NOTICE.md](LICENSE-NOTICE.md) for dependency licenses.

### PDF module consolidation (March 2026)

- Single project `QualiFrame.Pdf.Core.Engine` owns PDF contracts, extraction, comparison, validation, reporting bridge, Selenium download integration; legacy `QualiFrame.Pdf.*` projects removed from the solution.
- PDF tests under `src/QualiFrame.Tests/Tests/PDFTests` use `AssertWithAllure` for Allure-visible assertions.
- `PdfMasterValidationTests` + `PdfMasterValidationConfig.json`: batch base-vs-many comparison (~250 PDFs), artifacts under `src/QualiFrame.Tests/TestResults/PdfMasterValidation`.

### Extensible PDF validation & comparison (March 2026)

- Abstractions: `IPdfValidator`, `IPdfComparer`, `IPdfParser`, `IValidationRule` / `IValidationProfile`, `IValidationReporter`, `IPdfValidatorRegistry`; models `ValidatorResult`, `Finding`, `PdfValidationReport`, `PdfDocumentModel`.
- `PdfValidationEngine`: ordered pipeline, profile toggles, cancellation, no static state.
- Built-in validators: `PdfAdvancedComparisonValidator`, `PdfStructureValidator`, `PdfFontValidator`, `PdfDigitalSignatureValidator`; `PdfParser` → `IPdfParser`; `PdfComparerAdapter` bridges `IPdfComparator` → `IPdfComparer`.
- Config-first: `pdf.validation.json`, `PdfValidationProfileConfig`, `PdfValidationProfileLoader`; `AddQualiFramePdfValidationEngine()` for DI. Details: `src/QualiFrame.Pdf.Core.Engine/README.md`.
- Sample: `Configuration/pdf.validation.json`, tests `PdfValidationEngineTests`.

### Microsoft.Testing.Platform / .NET 10

- Opt-in via `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`), `EnableNUnitRunner` in `Directory.Build.props`, `OutputType=Exe` for the test project when enabled.
- **Runner vs MTP:** solution-wide `dotnet test` with MTP can pass internal flags to `QualiFrame.Runner`. Prefer `dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj` or `dotnet test Qualiframe-CSharp-NUnit_v3.0-TestsOnly.slnf`.

### Reporting framework enhancement (January 2026)

- Thread-safe Extent: `AsyncLocal<T>`, `Lazy<ExtentReports>`, locks on init/flush.
- Java-style `extent.properties` via `PropertiesFileReader` (precedence over JSON for Extent-only settings).
- `ExtentReportStatusMapper` (e.g. DONE→INFO, DEBUG→WARNING).
- `ReqnrollExtentAdapter` for step logging (Java ExtentCucumberAdapter–style).
- `EnhancedAllureReporter`: simplified API, `AsyncLocal<T>`, UUID/lifecycle handling; manual JSON for Reqnroll scenarios.
- `UnifiedReportingService`: single entry for Extent + Allure, config-driven.

### Phase 1 modernization (January 2026)

- Healenium: `HealeniumDriverWrapper`, config section `Healenium` in appsettings.
- `SmartTestSelector` (Microsoft.ML) + historical JSON path `SmartTestSelection:DataPath`.
- `ModularTestCase` base class (template method): driver lifecycle, data, reporting hooks.

### Phase 6

- DI (`ServiceCollectionExtensions`), component POM, driver pool (`DriverPool` / `DriverPoolManager`), `ITestExecutionService` / `IReportingService`, unit + integration tests.

### Phase 4–5

- Analytics (`TestMetrics`, orchestration graph), reporting consolidation (see above), quality and architecture hardening.

### Phase 3

- Visual testing (SkiaSharp), GraphQL client + `QueryBuilder`, mobile gestures/orientation/keyboard.

### Phase 2

- Data repositories (Excel, CSV, JSON, SQL), `EnhancedWebElement`, API client (retry, circuit breaker, schema validation, perf monitor).

### Original Phase 1

- Serilog, custom exceptions, `SecretsManager` (AES + Key Vault), enhanced WebDriver factory and health checks.

## Version 2.0.0

- Initial C# NUnit conversion; Reqnroll BDD; Web/API/Mobile; Extent integration.

## Version 1.0.0

- Original Java Maven QualiFrame lineage.

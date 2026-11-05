# Agent instructions — generating or changing automation

## Boundaries

1. **Do not** add application page objects under `src/QualiFrame.Framework/PageBases` — only `BasePage` / `ComponentPage` belong there.
2. **Do** place new UI page classes under `src/QualiFrame.Tests/PageObjects` (or a subfolder), inheriting `ComponentPage` or `BasePage` from `QualiFrame.Framework.PageBases`.
3. Prefer **`EnhancedWebDriverFactory`** / **`IWebDriverFactory`** over legacy `WebDriverFactory`.
4. For reporting in new code paths, prefer **`UnifiedReportingService`** when both Extent and Allure must stay aligned.

## Reqnroll steps

- Use `[Binding]` classes in `StepDefinitions/`.
- Resolve dependencies through **constructor injection** (BoDi). If a new page object needs WebDriver, follow existing patterns in `WebSteps`. Desktop pages take a `DesktopSession` from `DesktopSessionFactory`.
- Register new services in the same extension points used by existing steps (inspect `Hooks.cs` and DI plugin registration in the test project).

## Modular NUnit tests

- Inherit **`QualiFrame.Framework.Core.ModularTestCase`**.
- Implement **`SetUp`**, **`ExecuteTest`**, **`TearDown`** only — the base `[Test] RunTest()` orchestrates execution.
- Use `Config` for URLs/credentials references, not hard-coded secrets.

## Assertions

- NUnit `Assert.*` is standard; **`FluentAssertions`** is available in the test project for fluent style.

## Data

- Prefer **`DataRepositoryFactory`** + file-backed data under `Data/` (copied to output) over embedding large payloads in source.
- SQL is opt-in: pass `SQL` / `SQLSERVER` / `MSSQL` / `DATABASE` to the factory (`SqlDataRepository`). Excel remains the default. Requires `Environments:QA:DatabaseConnectionString`; do not add a second SQL client.

## Self-healing

- Toggle via **`SelfHealing:Provider`**: `Healenium` (shipped default), `Qhare`, or `Off`. Only one wrapper is applied. Page objects keep using `By`.
- Healenium: shipped `Healenium:Enabled` is **true**. Start the backend at `Healenium:BackendUrl`, or set `Provider` to `Off` / `Enabled` to `false`. The getter falls back to `false` if the key is missing.
- Successful Healenium finds are stored as **versioned local evidence** under `Healenium:EvidencePath`. Reqnroll steps promote that evidence only when the step passes. Healing compares the current DOM to historical baselines, accepts a candidate only at/above `Healenium:ScoreCap` (default `0.5`), and writes an audit record. Failed or low-confidence healings never replace a trusted baseline.
- QHARE: set `SelfHealing:Provider` to `Qhare` and start core at `Qhare:BaseUrl` (default `http://localhost:8000`). `QhareDriverWrapper` calls `POST /api/v1/heal` after a native miss (`action: find`), applies only `HEALED` locators, logs `UNRESOLVED`/`SUGGESTED` with confidence, and flushes failed/rejected telemetry even when the step fails. Dashboard: `http://localhost:3000/healing`.
- Healenium wrapper still inits the SHA report (`POST /healenium/report/init/{sessionId}`), saves successful locators (`POST /healenium`), and records healing (`POST /healenium/healing`) on the optional backend.

## Mobile

- Create Appium sessions through **`AppiumDriverFactory`**. Do not construct `AndroidDriver`/`IOSDriver` in steps.
- Prefer `MobileAppScreen` named locators for new steps. Keep existing id-based Mobile.feature steps working.
- Hybrid WEBVIEW: `MobileContextSwitcher.SwitchToWebView` / `SwitchToNative`.

## Desktop

- Create FlaUI sessions through **`DesktopSessionFactory.GetSession()`**. Do not construct `UIA3Automation` / `Application` in steps.
- Keep using `DesktopElement` / `DesktopActions` and `LocatorDefinition.AccessibilityId` (UIA AutomationId).
- `@Desktop` scenarios are Windows-only (`DesktopHooks`).
- Shared samples (`CalculatorPage`, `LoginWindow`) live under `QualiFrame.Framework/Desktop/Pages`. New AUT screens belong in `QualiFrame.Tests/PageObjects` and inherit `DesktopPage`.
- Shipped `Desktop:AppId` is Windows Calculator. The `@Calculator` scenario is the runnable sample. The `@Desktop` login scenario is a **template** (`UsernameTextBox` / `LoginButton`) and fails against Calculator unless you retarget `AppId` and locators. Guide: [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md).

## Video recording

- Opt-in only (`TestConfiguration:VideoRecording`, default `false`). Do not add a second recorder; `VideoRecordingManager` is wired from `EnhancedWebDriverFactory`.

## Runner / CI changes

- **`QualiFrame.Runner.Core`** holds test orchestration (`TestRunner`, `TestRunnerOptions`); namespace is `QualiFrame.Runner.Core` (not `QualiFrame.Tests.Runners`).
- Do **not** add a project reference from Runner to `QualiFrame.Tests` — the runner invokes `dotnet test` at runtime.
- CI scripts: `scripts/ci-build-test.sh`, `scripts/clean.sh` — update docs under `docs/context/` when changing pipeline behaviour.

## When unsure

- Consult **`docs/MEMORY_MAP.yaml`** for capability → path mapping and **`FRAMEWORK_MANIFEST.json`** (validated against `FRAMEWORK_MANIFEST.schema.json`) for capabilities, reusable components, and dependency categories.
- Narrative guides: **`docs/guides/`**; historical notes: **`docs/archive/`**.

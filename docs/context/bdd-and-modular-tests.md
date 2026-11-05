# BDD (Reqnroll) and modular NUnit tests

## Reqnroll

- **Features:** `src/QualiFrame.Tests/Features/*.feature`
- **Bindings:** `src/QualiFrame.Tests/StepDefinitions/*.cs` — classes marked `[Binding]`, steps as methods with regex/`Given`/`When`/`Then` attributes.
- **Hooks:** `src/QualiFrame.Tests/Hooks/Hooks.cs` — `[BeforeTestRun]`, `[BeforeFeature]`, `[BeforeScenario]`, `[AfterScenario]`, step hooks, driver teardown, reporting, Allure env.
- **DI (BoDi):** Constructor injection in step definitions — register new application services/page objects in Reqnroll plugin / DI setup (see `Reqnroll` dependency injection configuration in test project; `ServiceCollectionExtensions` for Microsoft DI used by modular tests and services).

## Page objects in steps

- Application pages live in `QualiFrame.Tests/PageObjects` and inherit `ComponentPage` or `BasePage` from `QualiFrame.Framework.PageBases`.
- Assign `WebDriver` from `IWebDriverFactory` or scenario context as existing steps do (see `WebSteps` patterns).

## Modular tests (non-BDD)

- Base: **`QualiFrame.Framework.Core.ModularTestCase`** (`src/QualiFrame.Framework/Core/ModularTestCase.cs`).
- **Template methods:** override `SetUp()`, `ExecuteTest()`, and `TearDown()`. The base class exposes `[SetUp] BaseSetUp`, `[Test] RunTest` (calls `ExecuteTest()`), and `[TearDown] BaseTearDown` — do **not** duplicate `[Test]` on `ExecuteTest` in derived classes unless you intentionally bypass the template (not supported by design).
- Inherits driver (`Driver`), `Config` (`TestConfiguration.Instance`), optional `DataRepository`, and reporting/test-execution services from base setup.

## Desktop BDD

- **Feature:** `src/QualiFrame.Tests/Features/Desktop.feature`
- **Steps:** `DesktopSteps.cs` (login template), `CalculatorSteps.cs` (Windows Calculator sample)
- **Hooks:** `src/QualiFrame.Tests/Hooks/DesktopHooks.cs` — `@Desktop` Before/After; skips WebDriver; FlaUI session via `DesktopSessionFactory`
- Desktop window objects inherit `DesktopPage` and take a `DesktopSession` (see `CalculatorPage`, `LoginWindow`)

## Example anchors in repo

- BDD (web): `Web.feature`, `WebSteps.cs`
- BDD (desktop): `Desktop.feature`, `CalculatorSteps.cs`, `DesktopSteps.cs`
- Modular: `Tests/Examples/LoginModularTest.cs` or other classes inheriting `ModularTestCase` under `src/QualiFrame.Tests/Tests/`.

# Reporting (Extent, Allure, unified)

## Design constraints

- **Parallel-safe:** primary managers use `AsyncLocal<T>` (and related patterns) so parallel tests do not cross-pollinate report nodes.
- **Reqnroll + Allure:** Allure.NUnit does not fully drive Reqnroll-generated tests; the framework **writes Allure result JSON** (`{uuid}-result.json`, `{uuid}-container.json`) via `EnhancedAllureReporter` / lifecycle helpers. Serialization uses string enums (`passed`/`failed`/`finished`) for CLI compatibility.
- **Extent on .NET 8 / 9 / 10:** ExtentReports Spark HTML is supported on **net8.0**. On **net9.0** and **net10.0**, `ExtentReportManager` disables Spark generation (RazorEngine incompatibility) and Allure remains the cross-TFM reporting path.
- **Video:** opt-in via `TestConfiguration:VideoRecording` (default `false`). `VideoRecordingManager` writes PNG frames under `Results/Videos`. Existing runs are unchanged.

## Primary APIs (types under `QualiFrame.Framework`)

| Concern | Type | Location |
|---------|------|----------|
| Unified facade | `UnifiedReportingService` | `Reporting/UnifiedReportingService.cs` |
| Extent thread-safe | `ExtentReportManager` | `Reports/ExtentReportManager.cs` |
| Status mapping | `ExtentReportStatusMapper` | `Reports/ExtentReportStatusMapper.cs` |
| Reqnroll ↔ Extent | `ReqnrollExtentAdapter` | `Reports/ReqnrollExtentAdapter.cs` |
| Allure simplified | `EnhancedAllureReporter` | `Reporting/EnhancedAllureReporter.cs` |
| Custom / batch HTML | `ReportGenerator`, `EnhancedReportGenerator` | `Reporting/` |
| Service abstraction | `IReportingService` | `Core/Services` |

## Hooks integration

- `QualiFrame.Tests/Hooks/Hooks.cs` wires scenario/step lifecycle to reporting and Allure directories (see file for ordering).

## ModularTestCase path

- `ModularTestCase` (`QualiFrame.Framework/Core/ModularTestCase.cs`) currently constructs **`ReportingService`** with the classic **`AllureReporter`** + `ExtentReportManager` + `ReportGenerator` in `BaseSetUp()`. BDD scenarios use **`EnhancedAllureReporter`** / **`UnifiedReportingService`** from hooks. When extending modular tests, align new reporting calls with that stack or migrate the base class in a dedicated change.

## CLI report generation

```bash
allure generate Results/AllureResults -o Results/AllureReport --clean
allure open Results/AllureReport
```

## Troubleshooting (compressed)

- **Empty Allure folder:** confirm `Reporting:AllureReport:Enable`, writable results path, hook errors in console / `Results/Logs`.
- **Wrong status in JSON:** ensure status/stage serialized as strings; hooks should use shared reporter helpers.

## PDF tests

- NUnit assertions in PDF tests may be wrapped with `AssertWithAllure` (see `PdfValidationTestBase` under `Tests/PDFTests`) so failures surface as Allure steps.

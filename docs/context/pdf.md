# PDF automation (`QualiFrame.Pdf.Core.Engine`)

## Unified engine

- **Project:** `src/QualiFrame.Pdf.Core.Engine` (single consolidated module; legacy `QualiFrame.Pdf.*` projects removed from the solution).
- **Stack:** PdfPig + SkiaSharp rendering helpers (`PdfPig`, `PdfPig.Rendering.Skia`, `SkiaSharp`); DI via `Microsoft.Extensions.DependencyInjection.Abstractions`.
- **Capabilities:** text/table/image/metadata extraction; base vs expected comparison with tolerances; validation orchestration; diff artifacts for reporting; integration hooks for downloaded files from Selenium flows.

## Extensible validation subsystem

- **Abstractions (plugin-style):** `IPdfValidator`, `IPdfComparer`, `IPdfParser`, `IValidationProfile` / rules, `IValidationReporter`, `IPdfValidatorRegistry` (under `QualiFrame.Pdf.Core.Engine.Abstractions` / related namespaces — see project).
- **`PdfValidationEngine`:** ordered pipeline, profile toggles, cancellation-friendly, no static shared state for parallel safety.
- **Built-in validators:** advanced comparison, structure, fonts, digital signature (detection; certificate chain validation may be incomplete — see code TODOs).
- **Adapter:** `PdfComparerAdapter` bridges legacy `IPdfComparator` to `IPdfComparer`.
- **DI registration:** `AddQualiFramePdfValidationEngine()` registers parser, comparer adapter, default validator registry, engine, reporter.

## Configuration-first

- Profile JSON (e.g. `pdf.validation.json`) with strongly typed loader (`PdfValidationProfileConfig`, `PdfValidationProfileLoader`).
- Artifacts such as `report.json` via `IValidationReporter` / `PdfValidationReporter`.

## Tests (application layer)

Location: **`src/QualiFrame.Tests/Tests/PDFTests`**

| Area | Tests / assets |
|------|----------------|
| Single-pair compare | `PdfComparisonTests`, `PdfCompareConfig.json` |
| Batch / master validation | `PdfMasterValidationTests`, `PdfMasterValidationConfig.json` (large N vs one base; artifacts under `src/QualiFrame.Tests/TestResults/PdfMasterValidation`) |
| Validation engine | `PdfValidationEngineTests` + sample profile `src/QualiFrame.Tests/Configuration/pdf.validation.json` |

## Allure visibility

- PDF tests use **`AssertWithAllure`** (via `PdfValidationTestBase`) so NUnit assertions appear as structured Allure steps.

## Further reading

- Module README: `src/QualiFrame.Pdf.Core.Engine/README.md` (adding validators, DI shapes).

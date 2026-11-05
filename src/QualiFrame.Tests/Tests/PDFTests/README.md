## PDF Tests Overview

This folder contains the QualiFrame 3.0 PDF test suite built on top of the unified `QualiFrame.Pdf.Core.Engine` module. Tests use the shared `PdfValidationTestBase` for DI, logging, and Allure integration.

### Test Files

- `PdfComparisonTests.cs`  
  Config-driven comparison of a single Base PDF vs a single Expected PDF using `PdfCompareConfig.json`. Implements:
  - File-level compare (page count + optional checksum)
  - File size compare
  - Text compare using `IPdfExtractor` + `IPdfComparator`
  - Diff artifact generation via `IPdfDiffReportGenerator`

- `PdfTextValidationTests.cs` / `PdfValidationSuiteTests.cs`  
  Text-centric validations (mandatory clauses, occurrence counts) using the `PdfTextValidator` helper.

- `PdfValidationEngineTests.cs` **(extensible engine)**  
  Uses the plugin-style PDF Validation Engine: loads `Configuration/pdf.validation.json`, validates BasePDF.pdf vs ComparePDF.pdf via `PdfValidationEngine`, and produces a report + artifacts (e.g. `report.json`) under `TestResults/PdfValidationEngine/`. See `QualiFrame.Pdf.Core.Engine/README.md` for how to add a new validator and run this test.

- `PdfMasterValidationTests.cs`  
  Batch validation of many Expected PDFs against a single Base PDF, driven by `PdfMasterValidationConfig.json`. Each comparison:
  - Uses the unified engine (`IPdfExtractor`, `IPdfComparator`, `IPdfDiffReportGenerator`)
  - Logs a file-level summary and page-level mismatches via the standard `Reporting` abstraction
  - Produces a diff artifact for each Expected PDF
  - Wraps assertions with `AssertWithAllure` so comparison results appear as named steps in Allure

### Configuration

1. **Single-pair comparison config**

- File: `src/QualiFrame.Tests/Data/FileValidation/PdfCompareConfig.json`  
  Used by `PdfComparisonTests.cs` for single Base vs Expected comparison (page count, size, text).

2. **Batch/master validation config (Base vs many Expected PDFs)**

- File: `src/QualiFrame.Tests/Data/FileValidation/PdfMasterValidationConfig.json`  
  Used by `PdfMasterValidationTests.cs`.

Key fields:

```json
{
  "BaseFilePath": "Data/FileValidation/BasePDF.pdf",
  "ExpectedFolders": [ "Data/FileValidation/ExpectedPdfs" ],
  "ExpectedFilePatterns": [ "*.pdf" ],
  "MaxDocuments": 250,
  "Categories": {
    "EnableComparisonSummary": true,
    "EnableTextValidation": true,
    "EnableImageValidation": false,
    "EnableTableValidation": false,
    "EnableFontValidation": false,
    "EnableSignatureValidation": false
  },
  "Tolerances": {
    "LayoutTolerancePoints": 2.0,
    "ImageDiffTolerance": 0.01,
    "TextIgnorePatterns": []
  }
}
```

- `BaseFilePath`: Relative path to the base PDF (resolved via `TestPathResolver`).
- `ExpectedFolders`: Relative folders containing the PDFs to validate against the base.
- `ExpectedFilePatterns`: File patterns to include (for example `"*.pdf"`).
- `MaxDocuments`: Optional hard cap on the number of documents to validate (for large batches, up to ~250).
- `Categories`: Toggles for comparison and additional validation categories. The current implementation focuses on comparison summary + text; the other flags are reserved for future image/table/font/signature rules.
- `Tolerances`: Per-category tolerances passed into the engine (for example `LayoutTolerancePoints` into `PdfComparisonOptions.LayoutTolerancePts`).

> **Note:** Do not hardcode absolute paths; all paths should remain relative and resolved via `TestPathResolver` to keep tests CI-friendly.

### Artifacts & Reporting

- Per-test results are written under:

  - `src/QualiFrame.Tests/TestResults/PdfCompare/...` (single-pair comparison)
  - `src/QualiFrame.Tests/TestResults/PdfMasterValidation/...` (batch comparison)

- Each batch run of `PdfMasterValidationTests` produces:

  - A per-run directory named with a UTC timestamp
  - A `DiffReports` subfolder containing diff artifacts for each Expected PDF (currently text-based diff reports created by `IPdfDiffReportGenerator`)

- All tests use:

  - `LogStep(...)` for structured Allure + framework logging
  - `Reporting.AddLog(...)` for detailed logging
  - `AssertWithAllure(...)` to expose assertion outcomes as Allure steps (instead of plain console output)

### How to Run

From the repository root:

```bash
# Run all PDF tests
dotnet test --filter "Category=PDF"

# Run only the master/batch validation tests
dotnet test --filter "FullyQualifiedName~PdfMasterValidationTests"
```

Ensure that:

- The base and expected PDFs referenced in `PdfMasterValidationConfig.json` exist at the configured relative paths.
- The Allure and Extent configuration is set up as described in the root `README.md` for full reporting.


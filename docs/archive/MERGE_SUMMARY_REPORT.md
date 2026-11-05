# QualiFrame v3.0 — Merge Summary Report

> **Historical document (February 2026).** Some paths and CI references below are outdated. Current layout: `docs/guides/`, `docs/archive/`, `QualiFrame.Runner.Core`, dual CI (GitHub + Azure DevOps). See [CHANGELOG.md](../../CHANGELOG.md).

**Source (Feature Source):** `Qualiframe3.0_Selenium-CSharp-Nuget_Truestage`  
**Target (Master):** `Qualiframe-Selenium-CSharp-NUnit_v3.0`  
**Merge Date:** February 2026

---

## 1. Modules Merged

| Module | Type | Action |
|--------|------|--------|
| **QualiFrame.Pdf.Core.Engine** | New project | Added to solution; full project copied from source into `src/QualiFrame.Pdf.Core.Engine/`. |
| **QualiFrame.Mocking** | New project | Added to solution; full project copied from source into `src/QualiFrame.Mocking/`. |
| **QualiFrame.Tests (PDF & utilities)** | Test content | PDF tests, components, validators, config, and `TestPathResolver` merged into existing Tests project. |

---

## 2. New Features Added

### 2.1 PDF Automation (QualiFrame.Pdf.Core.Engine)

- **Parsing:** PdfPig-based parser and document model.
- **Validation engine:** Profile-based validation (`pdf.validation.json`), validators (comparison, structure, font, digital signature).
- **Comparison:** Base vs candidate PDF comparison, text/layout, diff report generation.
- **Reporting:** `PdfValidationReporter`, `PdfReportingBridge` for framework reporting integration.
- **Integration:** `AddQualiFramePdfServices()` and `AddQualiFramePdfValidationEngine()` DI extensions.
- **Dependencies:** PdfPig 0.1.10, PdfPig.Rendering.Skia 0.1.10.4, SkiaSharp 3.119.0, Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1.

### 2.2 API Mocking & Contract Testing (QualiFrame.Mocking)

- **WireMock:** `WireMockServerManager`, `MockServerLifecycle`, `DockerWireMockManager`.
- **OpenAPI:** `OpenApiMockGenerator`, `OpenApiSpecLoader`, stub generation from Swagger/OpenAPI.
- **Contract validation:** `ContractValidator`, `SchemaValidationService`, fixtures for contract tests.
- **Hybrid UI/API:** `HybridTestOrchestrator`, `ApiUiValidationService`, `BackendRouteSwitcher`, `HybridTestFixture`.
- **Stubs:** `UserApiStubs`, `OrderApiStubs`, `PaymentApiStubs`, `DynamicStubRegistry`.
- **Dependencies:** WireMock.Net 1.21.0, Microsoft.OpenApi 1.6.22, YamlDotNet 16.3.0, Docker.DotNet, NUnit, Serilog, etc.

### 2.3 PDF Tests in QualiFrame.Tests

- **Test base:** `PdfValidationTestBase` (DI, reporting, Allure steps, PDF artifact attachment).
- **Test classes:**  
  `PdfValidationEngineTests`, `PdfComparisonTests`, `PdfMasterValidationTests`, `PdfTextValidationTests`, `PdfValidationSuiteTests`.
- **Components:** `PdfCompareConfig`, `PdfMasterValidationConfig` (and related DTOs) under `Components/PdfComparison/`.
- **Validators:** `PdfTextExtractor`, `PdfTextValidator`, `IPdfTextExtractor`, `IPdfTextValidator`, `PdfTextValidationResult` under `Validators/Pdf/`.
- **Utility:** `TestPathResolver` under `Utilities/` for resolving test data and artifact paths (CI-friendly).

---

## 3. Files Updated

### 3.1 Solution and Projects

- **Qualiframe-CSharp-NUnit_v3.0.sln**  
  - Added projects: `QualiFrame.Pdf.Core.Engine`, `QualiFrame.Mocking`.  
  - Added build configurations and NestedProjects entries for both.

- **src/QualiFrame.Tests/QualiFrame.Tests.csproj**  
  - Added: `<ProjectReference Include="..\QualiFrame.Pdf.Core.Engine\QualiFrame.Pdf.Core.Engine.csproj" />`.  
  - No other dependency or target changes.

### 3.2 New Directories/Files (under master)

- **src/QualiFrame.Pdf.Core.Engine/**  
  - Full project (Abstractions, Config, Core, Engine, Integration, Parsing, Reporting, Validators, etc.).

- **src/QualiFrame.Mocking/**  
  - Full project (Config, Contracts, Examples, Fixtures, Generation, HybridTesting, Server, Stubs, Utilities).

- **src/QualiFrame.Tests/**  
  - **Tests/PDFTests/** — `PdfValidationTestBase.cs`, `PdfValidationEngineTests.cs`, `PdfComparisonTests.cs`, `PdfMasterValidationTests.cs`, `PdfTextValidationTests.cs`, `PdfValidationSuiteTests.cs`, `README.md`.  
  - **Components/PdfComparison/** — `PdfCompareConfig.cs`, `PdfMasterValidationConfig.cs`.  
  - **Validators/Pdf/** — `IPdfTextExtractor.cs`, `IPdfTextValidator.cs`, `PdfTextExtractor.cs`, `PdfTextValidator.cs`, `PdfTextValidationResult.cs`.  
  - **Utilities/TestPathResolver.cs** — path resolution for test data and artifacts.  
  - **Configuration/pdf.validation.json** — PDF validation profile.  
  - **Data/FileValidation/PdfCompareConfig.json** — PDF comparison config.  
  - **Data/FileValidation/PdfMasterValidationConfig.json** — master validation config (already present from earlier copy).

### 3.3 Documentation

- **docs/archive/MERGE_ANALYSIS.md** — Framework comparison and merge strategy (Step 1–7).  
- **docs/archive/MERGE_SUMMARY_REPORT.md** — This file.

---

## 4. Dependencies Added

Dependencies are introduced only via the new projects; the main Framework and Tests projects did not get new NuGet packages beyond the new project references.

| Package | Version | Project |
|---------|---------|---------|
| PdfPig | 0.1.10 | QualiFrame.Pdf.Core.Engine |
| PdfPig.Rendering.Skia | 0.1.10.4 | QualiFrame.Pdf.Core.Engine |
| SkiaSharp | 3.119.0 | QualiFrame.Pdf.Core.Engine |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.1 | QualiFrame.Pdf.Core.Engine |
| WireMock.Net | 1.21.0 | QualiFrame.Mocking |
| Microsoft.OpenApi | 1.6.22 | QualiFrame.Mocking |
| Microsoft.OpenApi.Readers | 1.6.22 | QualiFrame.Mocking |
| YamlDotNet | 16.3.0 | QualiFrame.Mocking |
| Microsoft.Extensions.Configuration.* | 10.0.1 | QualiFrame.Mocking |
| Docker.DotNet | 3.125.15 | QualiFrame.Mocking |
| Serilog.* | (existing versions) | QualiFrame.Mocking |
| NUnit | 4.4.0 | QualiFrame.Mocking |

No duplicate or conflicting packages were introduced in the master Framework or Tests projects.

---

## 5. Configuration Alignment

- **Configuration/pdf.validation.json** — Merged into master Tests for PDF validation profile.
- **Data/FileValidation/PdfCompareConfig.json** — Merged for config-driven PDF comparison.
- **Data/FileValidation/PdfMasterValidationConfig.json** — Merged for master PDF validation.
- **extent.properties** — Already present in master; unchanged.
- **appsettings** — Master’s existing appsettings and Configuration copy behavior retained; no overwrite from source.
- **QualiFrame.Mocking** — Uses its own `appsettings.Mocking.json` (included with the copied project).

---

## 6. Namespace and Path Cleanup

- **Namespaces:** All merged code keeps existing namespaces (`QualiFrame.Pdf.Core.Engine.*`, `QualiFrame.Mocking.*`, `QualiFrame.Tests.*`). No renames.
- **References:** Tests project references only `QualiFrame.Framework` and `QualiFrame.Pdf.Core.Engine`. QualiFrame.Mocking is not referenced by Tests (optional for future mock/contract tests).
- **Paths:** `TestPathResolver` resolves paths relative to output directory, `src/QualiFrame.Tests`, or solution root. No hardcoded absolute paths in merged test code.
- **Duplicate classes:** None introduced; no removal of existing master code.

---

## 7. Build and Test Validation (Local)

Build and test could not be run in this environment (NuGet restore failed due to network/sandbox restrictions). You should run the following locally:

```bash
cd /Users/selvamohanchinnasamy/Documents/Qualiframe-Ver3.0/Latest-AsofFeb4-2026/Qualiframe-Selenium-CSharp-NUnit_v3.0

dotnet restore
dotnet build

# Optional: run PDF tests (requires Data/FileValidation/BasePDF.pdf and ComparePDF.pdf for some tests)
dotnet test --filter "Category=PDF" --no-build
```

If you see missing types or references, ensure:

- `QualiFrame.Pdf.Core.Engine` builds after `QualiFrame.Framework`.
- `QualiFrame.Tests` has project reference to `QualiFrame.Pdf.Core.Engine`.
- `TestPathResolver` is in `QualiFrame.Tests.Utilities` and is used by PDF tests.

---

## 8. Breaking Changes

- **None.** The merge is additive:
  - New projects and new test/utility/config files only.
  - Existing solution projects (Framework, Runner, Tests) unchanged in target framework (net9.0) and behavior.
  - No existing APIs or config contracts were changed or removed.

---

## 9. Implementation Rules Followed

- Master was not overwritten blindly; only new projects and new test/config/utility content were added.
- Framework modularity preserved (PDF and Mocking are separate projects).
- Backward compatibility maintained; no breaking changes to existing tests or configuration.
- No duplicate utilities (single `TestPathResolver`, single set of PDF components/validators).
- Architecture kept consistent (DI, reporting, configuration patterns aligned with existing QualiFrame usage).
- Extensibility preserved (Mocking can be referenced from Tests later if needed).

---

## 10. Optional Next Steps

1. **Run build and tests locally** (see Section 7).
2. **Add sample PDFs** under `Data/FileValidation/` (e.g. `BasePDF.pdf`, `ComparePDF.pdf`) if you want to run all PDF tests without ignores.
3. **Reference QualiFrame.Mocking from QualiFrame.Tests** if you want to run WireMock/contract/hybrid tests from the same test project; then run mock/contract tests as needed.
4. **CI:** Use `scripts/ci-build-test.sh` (GitHub Actions / Azure DevOps / Jenkins) against `QualiFrame.Tests`; optionally filter `Category=PDF`.

---

**End of Merge Summary Report**

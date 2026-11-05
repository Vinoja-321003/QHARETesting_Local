# Framework Merge Analysis: Truestage (Source) → QualiFrame Master v3.0

> **Historical document (February 2026).** Mocking samples now live in `QualiFrame.Tests/Tests/Integration/Mocking/`. See [CHANGELOG.md](../../CHANGELOG.md).

**Date:** February 2026  
**Source:** `Qualiframe3.0_Selenium-CSharp-Nuget_Truestage`  
**Target (Master):** `Qualiframe-Selenium-CSharp-NUnit_v3.0`

---

## Step 1 — Framework Analysis Summary

### 1.1 Folder Structure Comparison

| Aspect | Master (Target) | Source (Truestage) |
|--------|-----------------|---------------------|
| **Solution projects** | 3: Framework, Tests, Runner | 5: Framework, Tests, Runner, **Pdf.Core.Engine**, **Mocking** |
| **Target framework** | net9.0 | net9.0 |
| **Tests project** | No PDF/Mocking refs | References QualiFrame.Pdf.Core.Engine |
| **Runner** | Same (CommandLineParser) | Same + DisableTestingPlatformEntryPoint |

### 1.2 New Modules (Source Only)

| Module | Purpose | Merge Action |
|--------|---------|--------------|
| **QualiFrame.Pdf.Core.Engine** | PDF validation, comparison, reporting (PdfPig, SkiaSharp) | Add as new project; add project ref from Tests |
| **QualiFrame.Mocking** | WireMock.Net, OpenAPI mocks, contract validation, hybrid API/UI testing | Add as new project; optional ref from Tests for mock tests |

### 1.3 Modified / Aligned Modules

| Area | Master | Source | Merge Strategy |
|------|--------|--------|----------------|
| QualiFrame.Framework | net9.0, EPPlus removed (ClosedXML), Selenium.Axe | Same; ClosedXML, SkiaSharp, no EPPlus | No change; already aligned |
| QualiFrame.Tests | net9.0, Framework ref only | net9.0, Framework + Pdf ref, OutputType Exe | Add Pdf (and optionally Mocking) ref; keep master’s test style |
| Solution | 3 projects | 5 projects | Add Pdf.Core.Engine and Mocking to solution |

### 1.4 New Dependencies (Source → Master)

| Package | Used In | Master Has? |
|---------|---------|--------------|
| PdfPig 0.1.10 | Pdf.Core.Engine | No → add via new project |
| PdfPig.Rendering.Skia 0.1.10.4 | Pdf.Core.Engine | No → add via new project |
| WireMock.Net 1.21.0 | Mocking | No → add via new project |
| Microsoft.OpenApi 1.6.22 | Mocking | No → add via new project |
| Microsoft.OpenApi.Readers 1.6.22 | Mocking | No → add via new project |
| YamlDotNet 16.3.0 | Mocking | No → add via new project |
| ClosedXML 0.105.0 | Framework (Source uses this) | Master uses ClosedXML in Framework → already aligned |

Master Framework already has SkiaSharp; Pdf.Core.Engine adds PdfPig. No duplicate or conflicting packages.

### 1.5 Configuration Differences

| Config | Master | Source | Action |
|--------|--------|--------|--------|
| appsettings.json | In bin/Release or Configuration | Configuration/ + pdf.validation.json | Ensure Configuration/ exists; add pdf.validation.json |
| extent.properties | Present at repo root | Present | No change |
| pdf.validation.json | N/A | Configuration/pdf.validation.json | Add to master Configuration/ |
| PdfCompareConfig.json, PdfMasterValidationConfig.json | N/A | Data/FileValidation/ | Add to master Data/FileValidation/ |
| appsettings.Mocking.json | N/A | QualiFrame.Mocking/ | Comes with Mocking project copy |

### 1.6 Test Utility / Test Changes

| Item | Location (Source) | Merge |
|------|-------------------|-------|
| PDF test base | Tests/PDFTests/PdfValidationTestBase.cs | Copy to master |
| PDF tests | Tests/PDFTests/*.cs (6 files + README) | Copy to master |
| PdfComparison config classes | Components/PdfComparison/*.cs | Copy to master |
| PDF validators (test-level) | Validators/Pdf/*.cs | Copy to master |
| Mocking tests | `QualiFrame.Tests/Tests/Integration/Mocking/*.cs` | Executable samples; `Category=Mocking` (moved from Mocking/Examples in v3.1.1) |

### 1.7 New Integrations

- **PDF validation:** Full engine in QualiFrame.Pdf.Core.Engine (parsing, comparison, validators, reporting, DI).
- **Mocking:** WireMock server, OpenAPI-driven mocks, contract validation, hybrid UI/API fixtures (in QualiFrame.Mocking).
- **Reporting:** Source uses same Extent/Allure as master; Pdf.Core.Engine has PdfValidationReporter and PdfReportingBridge.

---

## Step 2 — Dependency Alignment

- Master Framework: No package changes required; already net9.0 and same Selenium/stack.
- Master Tests: Add `<ProjectReference Include="..\QualiFrame.Pdf.Core.Engine\QualiFrame.Pdf.Core.Engine.csproj" />`. Optionally add Mocking reference if we want to run mock tests from Tests project.
- New projects Pdf.Core.Engine and Mocking: Use their existing .csproj as-is (they already reference Framework where needed).

---

## Step 3 — Architecture Consistency

- **Core:** No change to Core framework components; new capabilities are in separate projects.
- **Driver / reporting / config:** Unchanged; PDF and Mocking are additive.
- **Namespaces:** Preserve QualiFrame.Pdf.Core.Engine and QualiFrame.Mocking; no conflict with master.
- **Fixtures/hooks:** Mocking has its own fixtures (MockServerFixture, ContractValidationFixture, HybridTestFixture); used only when Tests references Mocking.

---

## Step 4–7 — Merge Execution Plan

1. **Add projects to master:** Copy `QualiFrame.Pdf.Core.Engine` and `QualiFrame.Mocking` from source `src/` to master `src/` (excluding bin/obj).
2. **Solution:** Add Pdf.Core.Engine and Mocking to `Qualiframe-CSharp-NUnit_v3.0.sln` with correct GUIDs and build configs.
3. **Tests csproj:** Add project reference to QualiFrame.Pdf.Core.Engine; optionally QualiFrame.Mocking.
4. **Tests content:** Copy from source to master:
   - Tests/PDFTests/
   - Components/PdfComparison/
   - Validators/Pdf/
   - Configuration/pdf.validation.json
   - Data/FileValidation/*.json
5. **Config:** Ensure master has Configuration/ and Data/FileValidation/ with the above files.
6. **Namespaces/references:** After copy, fix any broken usings (namespace QualiFrame.Tests preserved).
7. **PrintTypes.csproj / test.cs:** Not referenced by source Tests csproj; skip to avoid clutter.

---

## Step 8–9 — Validation

- Restore: `dotnet restore`
- Build: `dotnet build`
- Run a small subset: e.g. run PDF tests or a single NUnit test to validate driver/reporting/config.

---

## Step 10 — Documentation of Changes (Post-Merge)

Merge summary will document:

- Modules merged: QualiFrame.Pdf.Core.Engine, QualiFrame.Mocking (as new projects).
- New features: PDF validation engine, PDF comparison/master validation, Mocking (WireMock, OpenAPI, contract validation, hybrid tests).
- Files updated: Solution, QualiFrame.Tests.csproj, new folders under Tests (PDFTests, Components/PdfComparison, Validators/Pdf), Configuration and Data/FileValidation.
- Dependencies added: Via new projects only (PdfPig, PdfPig.Rendering.Skia, WireMock.Net, Microsoft.OpenApi, YamlDotNet, etc.).
- Breaking changes: None; additive only.

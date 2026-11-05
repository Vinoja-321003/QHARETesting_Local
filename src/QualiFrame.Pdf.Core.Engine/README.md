# QualiFrame.Pdf.Core.Engine – Extensible PDF Validation & Comparison

This document describes the **extensible PDF Validation & Comparison subsystem** under `QualiFrame.Pdf.Core.Engine`: plugin-style validators, config-first profiles, and integration with QualiFrame reporting.

## Architecture Overview

- **Abstractions** – Single-responsibility validators (`IPdfValidator`), optional comparer (`IPdfComparer`), parser (`IPdfParser`), rules/profiles (`IValidationRule`, `IValidationProfile`), and reporter (`IValidationReporter`).
- **Engine** – `PdfValidationEngine` runs validators in a deterministic order with rule/profile selection, feature toggles, and cancellation; no static shared state.
- **Validators** – Four built-in validators: `PdfAdvancedComparisonValidator`, `PdfStructureValidator`, `PdfFontValidator`, `PdfDigitalSignatureValidator`.
- **Config** – JSON profile (e.g. `pdf.validation.json`) with strongly typed models and validation.
- **Reporting** – `IValidationReporter` maps results to QualiFrame (Allure/Extent) and writes artifacts (`report.json`, optional diff images, parsed-model).

## Folder Structure

```
QualiFrame.Pdf.Core.Engine/
├── Abstractions/           # IPdfValidator, IPdfComparer, IPdfParser, IValidationProfile, IValidationReporter, IPdfValidatorRegistry
├── Abstractions/Models/     # ValidationResult, Finding, PdfValidationReport, PdfDocumentModel, options
├── Config/                  # PdfValidationProfileConfig, PdfValidationProfileLoader
├── Engine/                  # PdfValidationEngine, PdfValidatorRegistry, PdfComparerAdapter
├── Parsing/                 # PdfParser (IPdfParser implementation)
├── Validators/              # PdfAdvancedComparisonValidator, PdfStructureValidator, PdfFontValidator, PdfDigitalSignatureValidator
├── Reporting/               # IValidationReporter
├── Reporting/Impl/          # PdfValidationReporter
└── Integration/             # DI (AddQualiFramePdfValidationEngine)
```

## How to Add a New Validator (Open/Closed Principle)

1. **Implement the interface**  
   Create a class that implements `QualiFrame.Pdf.Core.Engine.Abstractions.IPdfValidator`:

   ```csharp
   public sealed class MyCustomValidator : IPdfValidator
   {
       public string Id => "MyCustom";
       public string Name => "My Custom PDF Validator";

       public async Task<ValidatorResult> ValidateAsync(PdfValidationContext request, CancellationToken cancellationToken = default)
       {
           // Use request.BaseModel, request.Profile, request.BasePdfPath, etc.
           var findings = new List<Finding>();
           // ... run checks, add findings ...
           return new ValidatorResult
           {
               ValidatorId = Id,
               ValidatorName = Name,
               Status = findings.Any(f => f.Severity == FindingSeverity.Error) ? ValidationStatus.Fail : ValidationStatus.Pass,
               Findings = findings,
               DurationMs = ...
           };
       }
   }
   ```

2. **Register in DI**  
   In your test or app setup, after `AddQualiFramePdfValidationEngine()`:

   - Register your validator: `services.AddTransient<MyCustomValidator>();`
   - Add it to the registry by resolving `IPdfValidatorRegistry` and calling `Register(myValidator)` (if your DI supports resolving the registry and then adding to it), **or**
   - Extend the registration helper: in `PdfServiceCollectionExtensions.AddQualiFramePdfValidationEngine()`, add your validator to the registry factory (e.g. `registry.Register(sp.GetRequiredService<MyCustomValidator>());`) and register `MyCustomValidator` in the service collection.

   To keep the engine core unchanged (open/closed), prefer registering your validator so the existing registry factory can include it (e.g. by convention or a separate extension that adds to the same registry).

3. **Config**  
   Add a toggle and optional options in `pdf.validation.json` (e.g. `validators.myCustom: true` and a `myCustom` section). Extend `PdfValidationProfileConfig` and `LoadedValidationProfile.GetValidatorOptions("MyCustom")` if you need profile-driven options for your validator.

## How to Run the Sample Test

The minimal NUnit test that loads the profile, validates **BasePDF.pdf** vs **ComparePDF.pdf**, and produces a report + artifacts is in the test project:

- **Test:** `QualiFrame.Tests.Tests.PDFTests.PdfValidationEngineTests.Validate_LoadProfile_BaseVsCompare_ProducesReportAndArtifacts`
- **Profile:** `Configuration/pdf.validation.json` (in test project; copied to output).
- **PDFs:** `Data/FileValidation/BasePDF.pdf`, `Data/FileValidation/ComparePDF.pdf` (create or place sample PDFs there if missing; test will ignore if files are absent).

From the repository root:

```bash
# Run the PDF Validation Engine test only
dotnet test --filter "FullyQualifiedName~PdfValidationEngineTests.Validate_LoadProfile_BaseVsCompare_ProducesReportAndArtifacts"

# Run all PDF tests
dotnet test --filter "Category=PDF"
```

Artifacts (e.g. `report.json`) are written under  
`src/QualiFrame.Tests/TestResults/PdfValidationEngine/<timestamp>/`.

## Configuration (pdf.validation.json)

The sample profile enables/disables validators and sets thresholds. Key sections:

- **validators** – `comparison`, `structure`, `font`, `signature` (true/false).
- **comparison** – Text/visual/layout tolerances and toggles.
- **parsing** – What to include (structure, tables, images, font info, max pages).
- **font** – Allowed families, subset/embedded rules, min/max size.
- **signature** – Require signature, certificate chain, timestamp, allowed signers.
- **structure** – Page order, min blocks per page, table structure.
- **artifacts** – Emit report.json, parsed-model, diff PDF, diff images.

Strongly typed config is validated by `PdfValidationProfileLoader.ValidateConfig()`; invalid profiles throw with clear messages.

## Result Model

- **ValidatorResult** – Per-validator: Status (Pass/Fail/Warning/Skipped), Findings, DurationMs.
- **Finding** – Severity, Code, Message, PageNumber, BoundingBox, Expected/Actual, Attachments.
- **PdfValidationReport** – Aggregates all validator results, overall status, total findings, and artifact list.

## Dependencies

The engine uses **PdfPig** (and optional Skia for visual comparison). External signing/certificate verification (e.g. BouncyCastle) is not included; see `PdfDigitalSignatureValidator` TODOs for certificate chain trust store integration.

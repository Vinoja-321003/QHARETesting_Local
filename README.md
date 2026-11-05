# QualiFrame — C# / NUnit / Selenium / Reqnroll

[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-purple.svg)](https://dotnet.microsoft.com/)
[![NUnit](https://img.shields.io/badge/NUnit-4.6.0-green.svg)](https://nunit.org/)
[![Reqnroll](https://img.shields.io/badge/Reqnroll-3.3.4-blue.svg)](https://reqnroll.net/)
[![Selenium](https://img.shields.io/badge/Selenium-4.43.0-orange.svg)](https://www.selenium.dev/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

Enterprise-oriented test automation for **Web** (Selenium), **API** (RestSharp, GraphQL), **mobile** (Appium), **desktop** (FlaUI / UI Automation), **PDF** validation, **mocking** (WireMock/OpenAPI), with **Extent** / **Allure** reporting, **Healenium** self-healing, and **ML-assisted** test selection.

**License:** Apache 2.0 — third-party packages and commercial-use notes: [`LICENSE-NOTICE.md`](LICENSE-NOTICE.md).

---

## Quick start

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (see [`global.json`](global.json); builds `net8.0`, `net9.0`, and `net10.0`), browsers for local Selenium, optional [Allure CLI](https://docs.qameta.io/allure/) for HTML report generation, Appium toolchain for mobile. Desktop (FlaUI) tests require **Windows**.

```bash
git clone <repository-url>
cd Qualiframe-Selenium-CSharp-NUnit_v3.0
dotnet restore
dotnet build
# Prefer an explicit TFM for local runs (faster than building all three)
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj -f net9.0
```

**Target frameworks:** projects multi-target `net8.0;net9.0;net10.0` via `QualiFrameTfms` in [`Directory.Build.props`](Directory.Build.props). Override with `-p:QualiFrameTfms=net9.0` for a single-TFM build. **ExtentReports** Spark HTML works on **net8.0**; on net9/net10 use **Allure**.

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj -f net8.0
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj -f net9.0
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj -f net10.0
```

**Custom CLI runner:** `dotnet run --project src/QualiFrame.Runner/QualiFrame.Runner.csproj` (see `Program.cs` for flags). Orchestration lives in **`QualiFrame.Runner.Core`** (lightweight; does not reference the test assembly).

**Clean local build artifacts:** `bash scripts/clean.sh`

**When using Microsoft.Testing.Platform on the test project:** avoid solution-wide `dotnet test` that launches `QualiFrame.Runner` with MTP-only arguments. Prefer:

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj
dotnet test Qualiframe-CSharp-NUnit_v3.0-TestsOnly.slnf
```

**Desktop (Windows):** the shipped sample is Windows Calculator. `Category=Desktop` also includes a login **template** that does not match Calculator locators. See [`docs/guides/DESKTOP_AUTOMATION.md`](docs/guides/DESKTOP_AUTOMATION.md).

```bash
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Calculator"
dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "Category=Desktop"
```

---

## Repository map

| Path | Role |
|------|------|
| [`src/QualiFrame.Framework`](src/QualiFrame.Framework) | Shared framework: POM bases, WebDriver factory/pool/healing, API, reporting, config, data, PDF hooks, desktop abstractions, DI. **No app-specific page objects.** |
| [`src/QualiFrame.Tests`](src/QualiFrame.Tests) | App page objects, Reqnroll features/steps/hooks, NUnit tests, `allureConfig.json`. |
| [`src/QualiFrame.Pdf.Core.Engine`](src/QualiFrame.Pdf.Core.Engine) | PdfPig-based PDF engine and validation pipeline. |
| [`src/QualiFrame.Mocking`](src/QualiFrame.Mocking) | WireMock/OpenAPI mocking and hybrid flows (library). |
| [`src/QualiFrame.Runner.Core`](src/QualiFrame.Runner.Core) | `dotnet test` orchestration (`TestRunner`, filters, options). |
| [`src/QualiFrame.Runner`](src/QualiFrame.Runner) | Thin CLI host over Runner.Core (not an MTP test host). |
| [`scripts/`](scripts/) | `ci-build-test.sh` (CI parity), `clean.sh` (remove `bin/`/`obj/`/reports). |
| [`Results/`](Results) | Allure, Extent/HTML, screenshots, logs, opt-in video frames (gitignored in normal workflows). |
| [`docs/guides/`](docs/guides/) | Feature guides (Allure, Appium, desktop, self-healing, etc.). |
| [`docs/context/`](docs/context/) | Agent-oriented framework context (execution, config, CI). |

---

## MCP / LLM context (token-efficient)

| Artifact | Purpose |
|----------|---------|
| [`docs/context/README.md`](docs/context/README.md) | Index of topic context files — **start here** for agents. |
| [`docs/MEMORY_MAP.yaml`](docs/MEMORY_MAP.yaml) | Capability → code paths and execution hints. |
| [`FRAMEWORK_MANIFEST.json`](FRAMEWORK_MANIFEST.json) | AI-readable knowledge model (capabilities, modules, reuse, execution). Schema: [`FRAMEWORK_MANIFEST.schema.json`](FRAMEWORK_MANIFEST.schema.json). Guide: [`docs/FRAMEWORK_MANIFEST_GUIDE.md`](docs/FRAMEWORK_MANIFEST_GUIDE.md). |
| [`CHANGELOG.md`](CHANGELOG.md) | Release history (migrated from the former mega-README). |

Topic files: [`core`](docs/context/core.md) · [`architecture`](docs/context/architecture.md) · [`execution`](docs/context/execution.md) · [`configuration`](docs/context/configuration.md) · [`reporting`](docs/context/reporting.md) · [`webdriver-and-healing`](docs/context/webdriver-and-healing.md) · [`bdd-and-modular-tests`](docs/context/bdd-and-modular-tests.md) · [`api-mocking-mobile-orchestration`](docs/context/api-mocking-mobile-orchestration.md) · [`pdf`](docs/context/pdf.md) · [`agent-instructions`](docs/context/agent-instructions.md) · [`ci-and-licensing`](docs/context/ci-and-licensing.md).

---

## PDF automation (summary)

The **`QualiFrame.Pdf.Core.Engine`** project provides extraction, comparison, validation, and diff artifacts (PdfPig + SkiaSharp). A **plugin-style** subsystem exposes `IPdfValidator`, `IPdfComparer`, `IPdfParser`, profiles, and `PdfValidationEngine` with **`AddQualiFramePdfValidationEngine()`** DI registration. Built-in validators cover advanced comparison, structure, fonts, and digital signature detection.

**Tests:** `src/QualiFrame.Tests/Tests/PDFTests` — e.g. `PdfComparisonTests` + `PdfCompareConfig.json`, `PdfMasterValidationTests` + `PdfMasterValidationConfig.json`, `PdfValidationEngineTests` + `src/QualiFrame.Tests/Configuration/pdf.validation.json`. PDF tests use **`PdfValidationTestBase`** and **`AssertWithAllure`** for Allure-visible assertions.

**Module docs:** [`src/QualiFrame.Pdf.Core.Engine/README.md`](src/QualiFrame.Pdf.Core.Engine/README.md).

---

## CI

Dual CI (same commands via [`scripts/ci-build-test.sh`](scripts/ci-build-test.sh)):

- **GitHub Actions:** [`.github/workflows/ci.yml`](.github/workflows/ci.yml)
- **Azure DevOps:** [`azure-pipelines.yml`](azure-pipelines.yml)
- **Jenkins:** [`Jenkinsfile`](Jenkinsfile)
- **GitLab CI:** [`.gitlab-ci.yml`](.gitlab-ci.yml) — same `ci-build-test.sh` target as the pipelines above

Details: [`docs/context/ci-and-licensing.md`](docs/context/ci-and-licensing.md).

---

## Contributing

Fork → feature branch → changes + tests → `dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj` → PR. Match existing patterns; add XML docs for new public framework APIs; update [`docs/context/`](docs/context/) or [`CHANGELOG.md`](CHANGELOG.md) when behavior or contracts change.

---

## Support

Open an issue, browse [`docs/guides/`](docs/guides/) for narrative guides, and use [`docs/context/`](docs/context/) for execution and CI details.

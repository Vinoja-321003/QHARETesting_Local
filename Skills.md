# QualiFrame Framework - Skills & Technologies

> **Not authoritative for versions.** This file is a human-readable skills inventory. NuGet and project versions come from the `.csproj` files and [`FRAMEWORK_MANIFEST.json`](FRAMEWORK_MANIFEST.json). If they disagree, trust those sources and refresh this list.

This document outlines technologies demonstrated in QualiFrame C# NUnit **v3.1.1**.

Packable projects (`QualiFrame.Framework`, `QualiFrame.Mocking`, `QualiFrame.Pdf.Core.Engine`) set `<Version>3.1.1</Version>`. `QualiFrame.Tests`, `QualiFrame.Runner`, and `QualiFrame.Runner.Core` have no package `Version` — they are not published NuGet packages.

---

## 🎯 Core Technologies

### Programming Languages
- **C#** - Primary language (.NET 9.0)
- **Gherkin** - BDD syntax for test scenarios
- **JSON** - Configuration and data formats
- **XML** - Configuration and data processing
- **SQL** - Database queries and data access

### .NET Ecosystem
- **.NET 9.0** - Latest .NET framework
- **.NET Standard** - Cross-platform compatibility
- **Microsoft.Extensions.Configuration** - Configuration management
- **Microsoft.Extensions.DependencyInjection** - Dependency injection container
- **Microsoft.NET.Test.Sdk** - Test SDK

---

## 🧪 Testing Frameworks & Tools

### Unit & Integration Testing
- **NUnit 4.6.0** - Unit testing framework (4.4.0 in QualiFrame.Framework)
- **NUnit3TestAdapter 6.2.0** - Test adapter for Visual Studio
- **NUnit.Analyzers 4.13.0** - Code analysis for NUnit
- **FluentAssertions 8.9.0** - Fluent assertion library
- **AutoFixture 4.18.1** - Test data generation
- **coverlet 10.0.0** - Code coverage collection

### BDD Framework
- **Reqnroll 3.3.4** - BDD framework (maintained fork of SpecFlow)
- **Reqnroll.NUnit 3.3.4** - NUnit integration
- **Reqnroll.Tools.MsBuild.Generation 3.3.4** - Code generation
- **BoDi 1.5.0** - Dependency injection for Reqnroll

---

## 🌐 Web Automation

### Selenium WebDriver
- **Selenium.WebDriver 4.43.0** - Core WebDriver
- **Selenium.Support 4.43.0** - Support library
- **Selenium.WebDriver.ChromeDriver 143.0.7499.14600** - Chrome automation
- **Selenium.WebDriver.GeckoDriver 0.36.0** - Firefox automation
- **Selenium.WebDriver.MSEdgeDriver 141.0.3537.71** - Edge automation
- **SeleniumExtras.WaitHelpers 1.0.2** - Wait utilities

### Web Automation Patterns
- **Page Object Model (POM)** - Page abstraction pattern
- **Component-Based POM** - Reusable component pattern
- **Factory Pattern** - WebDriver factory
- **Driver Pooling** - Efficient driver management
- **Self-Healing Automation** - Healenium integration for automatic locator healing
- **Visual Testing** - SkiaSharp-based screenshot comparison, baseline management, and diff images (commercial-use safe; no Applitools dependency)

---

## 📱 Mobile Automation

### Appium
- **Appium.WebDriver 8.2.0** - Mobile automation framework
- **Android Automation** - UiAutomator2
- **iOS Automation** - XCUITest
- **Mobile Gestures** - Swipe, tap, pinch, zoom, long press
- **Device Management** - Orientation, keyboard handling

---

## Desktop Automation

### FlaUI (Microsoft UI Automation)
- **FlaUI.Core / FlaUI.UIA3 / FlaUI.UIA2 5.0.0** - In-process Windows UI Automation
- **Session** - `DesktopSessionFactory.GetSession()` / `Quit()`
- **Pages / actions** - `DesktopPage`, `DesktopElement`, `DesktopActions` plus Grid / Tree / Menu / Tab / Dialog / Window / Ribbon helpers
- **Config** - `Desktop:AppId` (exe path or Store AUMID), `AutomationType` (`UIA3` default / `UIA2` for some WinForms), `LaunchMode`
- **Tests** - `Features/Desktop.feature`; `Category=Calculator` is the shipped runnable sample; the `@Desktop` login scenario is a locator template
- **OS** - Windows-only (`DesktopHooks` marks other OS inconclusive). Linux CI can restore/compile with `EnableWindowsTargeting`
- **Guide** - [docs/guides/DESKTOP_AUTOMATION.md](docs/guides/DESKTOP_AUTOMATION.md)

---

## 🔌 API Testing

### REST API
- **RestSharp 113.0.0** - RESTful API client
- **HTTP Methods** - GET, POST, PUT, DELETE, PATCH
- **Request/Response Handling** - Serialization, deserialization
- **Authentication** - Basic, Bearer, OAuth
- **Retry Policies** - Polly integration
- **Circuit Breaker** - Resilience patterns

### GraphQL
- **GraphQL.Client 6.1.0** - GraphQL client
- **GraphQL.Client.Serializer.Newtonsoft 6.1.0** - JSON serialization
- **Query Builder** - Fluent query construction
- **Mutations** - GraphQL mutation execution

### API Testing Features
- **Schema Validation** - JSON schema validation
- **Performance Monitoring** - P95/P99 metrics
- **Request/Response Logging** - Interceptors
- **Contract Testing** - API contract validation

### PDF Automation (QualiFrame.Pdf.Core.Engine)
- **PdfPig 0.1.14** - PDF parsing and text extraction
- **PdfPig.Rendering.Skia 0.1.14.2** - PDF rendering (SkiaSharp)
- **Profile-based validation** - pdf.validation.json, validators (comparison, structure, font, signature)
- **PDF comparison** - Base vs candidate, text/layout/structure, diff reports
- **DI integration** - AddQualiFramePdfServices(), AddQualiFramePdfValidationEngine()
- **Reporting** - PdfValidationReporter, PdfReportingBridge
- **Test support** - PdfValidationTestBase, TestPathResolver, config-driven comparison (PdfCompareConfig, PdfMasterValidationConfig)

### API Mocking & Service Virtualization (QualiFrame.Mocking)
- **WireMock.Net 2.5.0** - In-process mock server; port-based start, exact path and regex path matching (e.g. `/api/users/{id}`)
- **Microsoft.OpenApi 1.6.29** - OpenAPI/Swagger spec loading and parsing
- **YamlDotNet 16.3.0** - YAML config support
- **OpenAPI-driven mocks** - OpenApiMockGenerator, StubDefinitionFactory, ResponseTemplateBuilder
- **Contract validation** - ContractValidator, SchemaValidationService (string-based schema type checks), OpenApiSpecLoader
- **Hybrid UI/API testing** - HybridTestOrchestrator, ApiUiValidationService, BackendRouteSwitcher
- **Fixtures** - MockServerFixture, ContractValidationFixture, HybridTestFixture
- **Docker** - DockerWireMockManager (Docker.DotNet) for containerized mock server
- **Domain stubs** - User, Order, Payment API stubs in Stubs/

### File Processing
- **ClosedXML 0.105.0** - Excel file processing (.xlsx)
- **CsvHelper 33.1.0** - CSV file processing
- **Newtonsoft.Json 13.0.4** - JSON processing
- **System.Xml.XmlDocument 4.3.0** - XML processing
- **PDF (QualiFrame.Pdf.Core.Engine)** - PdfPig 0.1.14, PdfPig.Rendering.Skia 0.1.14.2 for validation, comparison, reporting (Apache 2.0 / permissive)

### Database
- **Microsoft.Data.SqlClient 6.1.3** - SQL Server connectivity
- **Repository Pattern** - Data access abstraction
- **Data Repository Factory** - Multi-source data access

### Test Data
- **Bogus 35.6.5** - Fake data generation
- **Data-Driven Testing** - External test data
- **Test Data Management** - Data repository pattern

---

## 📝 Logging & Monitoring

### Structured Logging
- **Serilog 4.3.1** - Structured logging framework
- **Serilog.Sinks.Console 6.1.1** - Console logging
- **Serilog.Sinks.File 7.0.0** - File logging
- **Serilog.Sinks.Debug 3.0.0** - Debug logging
- **Serilog.Enrichers.Environment 3.0.1** - Environment enrichment
- **Serilog.Enrichers.Thread 4.0.0** - Thread context
- **Serilog.Formatting.Compact 3.0.0** - Compact JSON format

### Observability
- **OpenTelemetry 1.15.3** - Observability framework
- **OpenTelemetry.Exporter.Console 1.15.3** - Console exporter
- **Context Enrichment** - CorrelationId, TestId, Browser
- **Sensitive Data Masking** - Secure logging

---

## 🤖 Machine Learning & AI

### ML.NET Framework
- **Microsoft.ML 5.0.0** - Machine Learning framework
- **Microsoft.ML.TimeSeries 5.0.0** - Time series analysis
- **ML-Based Test Selection** - Intelligent test recommendations
- **Test Execution Analytics** - Historical data analysis
- **Test Statistics** - Success rate, duration tracking

### Self-Healing Automation
- **Healenium Integration** - Automatic locator healing
- **HealeniumDriverWrapper** - Transparent self-healing wrapper
- **Locator Healing** - Automatic element locator recovery
- **Healing Configuration** - Configurable healing attempts
- **System.Net.Http.Json 10.0.7** - HTTP client for Healenium backend
- **Optional local stack** - `healenium/docker-compose.yml` (backend on port 7878); tests do not start it automatically

### Test Execution Patterns
- **Template Method Pattern** - Modular test base class
- **ModularTestCase** - Structured test execution flow
- **Test Data Integration** - Repository pattern integration
- **Automatic Setup/Teardown** - Framework-managed test lifecycle

---

## 📈 Reporting

### Test Reporting
- **ExtentReports 5.0.4** - HTML reporting
  - **Thread-Safe Implementation**: `AsyncLocal<T>` for parallel execution support
  - **Java-Style Configuration**: Properties file support (`extent.properties`)
  - **Status Mapping**: Automatic status conversion (DONE→INFO, DEBUG→WARNING)
  - **Reqnroll Adapter**: Automatic step tracking (`ReqnrollExtentAdapter`)
  - **Thread Isolation**: Equivalent to Java's ThreadLocal pattern
- **Allure.NUnit 2.15.0** - Allure reporting integration
  - **Enhanced Wrapper**: `EnhancedAllureReporter` with simplified API
  - **Thread-Safe**: Uses `AsyncLocal<T>` for parallel execution
  - **Automatic Lifecycle Management**: UUID and result file handling
- **Unified Reporting Service** - Single API for both Extent and Allure
- **Custom Report Generator** - HTML/JSON reports with enhanced features
- **Screenshot Integration** - Failure screenshots with automatic path resolution
- **Test Metrics Visualization** - Analytics and trends

### Report Features
- **Interactive HTML Reports** - Rich UI with Spark theme
- **Thread-Safe Parallel Execution** - Safe for concurrent test execution using AsyncLocal<T>
- **Java Framework Alignment** - Matches Java framework best practices (ThreadLocal pattern, properties files)
- **Configuration Flexibility** - JSON and properties file support (properties file takes precedence)
- **Unified API** - Single service for both Extent and Allure reporting
- **Adapter Pattern** - ReqnrollExtentAdapter for automatic step tracking
- **Status Mapping** - Automatic status conversion (DONE→INFO, DEBUG→WARNING) for consistency
- **Test History** - Historical trends
- **Attachments** - Screenshots, logs, videos
- **Categorization** - Tags, features, suites
- **Step-by-Step Tracking** - Detailed execution logs
- **Automatic Path Resolution** - Relative path conversion for screenshots

---

## 🔒 Security

### Secrets Management
- **Azure.Security.KeyVault.Secrets 4.11.0** - Azure Key Vault
- **AES Encryption** - Secure credential storage
- **Configuration Encryption** - Encrypted configs
- **JWT Support** - System.IdentityModel.Tokens.Jwt 8.18.0

### Security Features
- **Secure Credential Storage** - Encrypted storage
- **Sensitive Data Masking** - Log sanitization
- **Azure Key Vault Integration** - Cloud secrets management

---

## ☁️ Cloud & Integration

### AWS Services
- **AWSSDK.Core 4.0.6.1** - AWS SDK core
- **AWSSDK.S3 4.0.23** - S3 storage
- **AWSSDK.DeviceFarm 4.0.4.23** - Device farm testing

### Azure Services
- **Azure.Identity 1.21.0** - Azure authentication
- **Azure.Storage.Blobs 12.27.0** - Blob storage
- **Azure.Security.KeyVault.Secrets 4.11.0** - Key Vault

### Cloud Testing
- **SauceLabs Integration** - Cloud web testing
- **BrowserStack Integration** - Cloud device testing
- **AWS Device Farm** - Mobile device testing

---

## 🐳 Containerization & DevOps

### Docker
- **Docker.DotNet 3.125.15** - Docker API client
- **Container Management** - Docker operations
- **Container Testing** - Test execution in containers

### CI/CD
- **Jenkins** - CI/CD pipeline (Jenkinsfile)
- **GitHub Actions** - Workflow automation
- **Azure DevOps** - Pipeline integration
- **GitLab CI** - `.gitlab-ci.yml` uses the same `scripts/ci-build-test.sh` as the other pipelines
- **Test Result Publishing** - TRX, NUnit XML
- **Code Coverage** - Coverage collection and reporting

---

## 🔄 Advanced Protocols

### gRPC
- **Grpc.Net.Client 2.71.0** - gRPC client
- **Grpc.Tools 2.76.0** - Protocol buffer tools
- **gRPC Testing** - Service testing

### WebSocket
- **System.Net.WebSockets.Client 4.3.2** - WebSocket client
- **Real-time Communication** - WebSocket testing

### Message Queues
- **Confluent.Kafka 2.12.0** - Kafka integration (2.14.0 in QualiFrame.Tests)
- **Event-Driven Testing** - Message queue testing

---

## 🎨 Image & Document Processing

### Image Processing
- **SkiaSharp 3.119.2** - Image manipulation (MIT; commercial-use safe)
- **Visual Testing** - Screenshot comparison, baseline management, diff images via `VisualTester` and `ScreenshotComparator`
- **Baseline Management** - Image baseline storage
- **Image Comparison** - Pixel-level comparison

### Document Processing
- **QualiFrame.Pdf.Core.Engine** - Built on PdfPig and SkiaSharp (permissive licenses). Two modes: (1) Legacy/rule-based: `IPdfExtractor`, `IPdfComparator`, `IPdfValidator`, `IPdfDiffReportGenerator`, `IPdfReportingBridge`; `AddQualiFramePdfServices()`. (2) Extensible subsystem: plugin validators (`IPdfValidator`, `IPdfParser`, `IPdfComparer`, `IValidationProfile`, `IValidationReporter`, `IPdfValidatorRegistry`), `PdfValidationEngine`, `pdf.validation.json`, `AddQualiFramePdfValidationEngine()`. See `src/QualiFrame.Pdf.Core.Engine/README.md` and the section below.
- **System.Text.Encoding.CodePages 10.0.7** - Encoding support
- **System.IO.Compression 4.3.0** - File compression

---

## ⚡ Performance Testing

### Performance Tools
- *(Performance testing)* - Use external tools (e.g., k6, JMeter) at pipeline level; no NBomber dependency to keep all NuGet packages commercial-use safe.
- **API Performance Monitoring** - P95/P99 metrics (in-framework)
- **Load Testing** - Via external tools (k6, JMeter)
- **Response Time Tracking** - Performance metrics

---

## 🏗️ Design Patterns & Architecture

### Design Patterns
1. **Page Object Model (POM)** - Page abstraction
2. **Component Pattern** - Reusable UI components
3. **Factory Pattern** - Object creation
4. **Repository Pattern** - Data access abstraction
5. **Singleton Pattern** - Single instance management
6. **Builder Pattern** - Object construction
7. **Strategy Pattern** - Algorithm selection
8. **Wrapper Pattern** - Object wrapping
9. **Observer Pattern** - Event handling
10. **Dependency Injection** - Inversion of control
11. **Template Method Pattern** - Modular test base class
12. **Adapter Pattern** - ReqnrollExtentAdapter for reporting integration
13. **Thread-Local Pattern** - AsyncLocal<T> for thread isolation (equivalent to Java ThreadLocal)

### Architecture Patterns
- **Layered Architecture** - Separation of concerns
- **Component-Based Architecture** - Modular design
- **Service Layer** - Business logic abstraction
- **Dependency Injection** - Loose coupling
- **Repository Pattern** - Data access layer
- **Thread-Local Storage Pattern** - AsyncLocal<T> for thread isolation (reporting, driver management)
- **Unified Service Pattern** - Single API for multiple implementations (UnifiedReportingService)

---

## 🛠️ Development Tools & Practices

### Code Quality
- **NUnit.Analyzers** - Static code analysis
- **Code Coverage** - Coverlet integration
- **XML Documentation** - API documentation
- **Nullable Reference Types** - Type safety
- **Implicit Usings** - Modern C# features

### Testing Practices
- **Unit Testing** - Component testing
- **Integration Testing** - System testing
- **BDD Testing** - Behavior-driven development
- **Data-Driven Testing** - External test data
- **Parallel Execution** - Concurrent test execution with thread-safe reporting
- **Test Orchestration** - Dependency management
- **Thread-Safe Reporting** - Parallel execution support for Extent and Allure
- **Java Framework Alignment** - Best practices from Java framework implementation

### Best Practices
- **SOLID Principles** - Object-oriented design
- **DRY (Don't Repeat Yourself)** - Code reusability
- **AAA Pattern** - Arrange, Act, Assert
- **Configuration Management** - Environment-specific configs
- **Error Handling** - Custom exception hierarchy
- **Logging Best Practices** - Structured logging

---

## 📚 Framework Features

### Test Execution
- **Custom Test Runner** — `QualiFrame.Runner` CLI + `QualiFrame.Runner.Core` orchestration (shells out to `dotnet test`)
- **Command-Line Interface** — `CommandLineParser` flags; see `docs/guides/RUN_TESTS.md`
- **Parallel Execution** - Concurrent test runs
- **Test Filtering** - Category and tag filtering
- **Verbose Logging** - Detailed execution logs

### Configuration Management
- **JSON Configuration** - appsettings.json
- **Properties File Support** - Java-style properties files (key=value format)
- **Environment-Specific Configs** - Development, Production
- **Configuration Validation** - Early error detection
- **Type-Safe Configuration** - Strongly-typed configs
- **Multi-Format Support** - JSON and properties file configuration

### Test Analytics
- **Test Metrics Tracking** - Success rate, duration
- **Test Analytics** - JSON export
- **ML-Based Test Selection** - Intelligent test recommendations using Microsoft.ML
- **Test Execution History** - Historical data tracking and analysis
- **Test Statistics** - Success rate, average duration, trend analysis
- **Test Orchestration** - Dependency management
- **Topological Sorting** - Execution order
- **Cycle Detection** - Dependency validation

### Utilities
- **Wait Utilities** - Explicit waits
- **Screenshot Utilities** - Image capture with automatic path resolution
- **JavaScript Execution** - Browser automation
- **Element Interaction** - Enhanced element wrapper
- **Component Library** - Reusable components
- **TestPathResolver** - CI-friendly path resolution for test data and artifacts (QualiFrame.Tests)
- **Properties File Reader** - Java-style properties file parsing (key=value format)
- **Status Mapping** - Automatic status conversion utility
- **Reporting Adapters** - Reqnroll integration adapters

---

## 🎓 Skills Demonstrated

### Technical Skills
- ✅ **Test Automation** - Web, API, Mobile
- ✅ **PDF Validation & Comparison** - QualiFrame.Pdf.Core.Engine, PdfPig, profile-based validation, reporting integration
- ✅ **API Mocking & Contract Testing** - QualiFrame.Mocking, WireMock, OpenAPI-driven mocks, hybrid UI/API tests
- ✅ **Framework Development** - Enterprise-grade framework
- ✅ **BDD Implementation** - Gherkin, Reqnroll
- ✅ **Design Patterns** - 13+ patterns implemented (added Adapter, Thread-Local)
- ✅ **Dependency Injection** - Service container setup
- ✅ **Machine Learning** - ML.NET integration for test selection
- ✅ **Self-Healing Automation** - Healenium integration
- ✅ **Thread-Safe Programming** - AsyncLocal<T>, Lazy<T>, lock statements
- ✅ **Parallel Execution** - Thread-safe reporting and driver management
- ✅ **CI/CD Integration** - Jenkins, GitHub Actions, Azure DevOps
- ✅ **Cloud Integration** - AWS, Azure services
- ✅ **Performance Testing** - Load testing via external tools (k6, JMeter); in-framework API performance monitoring
- ✅ **License Compliance** - All dependencies commercial-use safe; see LICENSE-NOTICE.md
- ✅ **Security** - Secrets management, encryption
- ✅ **Reporting** - Multiple reporting frameworks with thread-safe implementations
- ✅ **Java Framework Alignment** - Best practices from Java framework ported to C#

### Soft Skills
- ✅ **Architecture Design** - Layered, component-based architecture
- ✅ **Code Organization** - Clean, maintainable code
- ✅ **Documentation** - Comprehensive documentation
- ✅ **Problem Solving** - Complex framework challenges
- ✅ **Best Practices** - Industry-standard practices

---

## 📦 Package Count Summary

- **Solution:** 6 projects (QualiFrame.Framework, QualiFrame.Tests, QualiFrame.Runner, QualiFrame.Runner.Core, QualiFrame.Pdf.Core.Engine, QualiFrame.Mocking)
- **Core Framework (QualiFrame.Framework):** 53+ NuGet packages
- **QualiFrame.Pdf.Core.Engine:** PdfPig, PdfPig.Rendering.Skia, SkiaSharp, Microsoft.Extensions.DependencyInjection.Abstractions
- **QualiFrame.Mocking:** WireMock.Net, Microsoft.OpenApi, YamlDotNet, Docker.DotNet, Serilog, NUnit, etc.
- **Testing Frameworks:** 8 packages
- **Web Automation:** 6 packages
- **API Testing:** 4 packages
- **Mobile Testing:** 1 package
- **Data Management:** 3 packages
- **Logging:** 7 packages
- **Reporting:** 2 packages (ExtentReports, Allure.NUnit) + custom implementations
- **Configuration:** 3 packages + PropertiesFileReader (custom)
- **Security:** 2 packages
- **Cloud Services:** 6 packages
- **Machine Learning:** 2 packages (Microsoft.ML, Microsoft.ML.TimeSeries)
- **HTTP Client:** 1 package (System.Net.Http.Json)
- **Utilities:** 10+ packages
- **Reporting Utilities:** 3 custom classes (ExtentReportStatusMapper, ReqnrollExtentAdapter, UnifiedReportingService)

---

## 🚀 Framework Capabilities

### Supported Testing Types
- ✅ **Web UI Testing** - Selenium WebDriver
- ✅ **API Testing** - REST, GraphQL
- ✅ **Mobile Testing** - Android, iOS
- ✅ **PDF Validation & Comparison** - QualiFrame.Pdf.Core.Engine (PdfPig)
- ✅ **API Mocking & Contract Testing** - QualiFrame.Mocking (WireMock, OpenAPI)
- ✅ **Database Testing** - SQL Server
- ✅ **Performance Testing** - Load testing
- ✅ **Visual Testing** - Screenshot comparison
- ✅ **Contract Testing** - API contracts
- ✅ **Self-Healing Testing** - Automatic locator healing
- ✅ **ML-Based Test Selection** - Intelligent test recommendations

### Supported Browsers
- ✅ **Chrome** - Latest versions
- ✅ **Firefox** - Latest versions
- ✅ **Edge** - Latest versions
- ✅ **Safari** - macOS support

### Supported Platforms
- ✅ **Windows** - Full support
- ✅ **Linux** - Full support
- ✅ **macOS** - Full support
- ✅ **Docker** - Container support

---

## 📖 Additional Resources

### Documentation
- **[LICENSE-NOTICE.md](LICENSE-NOTICE.md)** - Third-party licenses and commercial-use status for all dependencies
- Framework documentation in `docs/guides/` and `docs/context/`
- Component README files
- Code XML documentation
- Usage examples

### External Resources
- [NUnit Documentation](https://docs.nunit.org/)
- [Reqnroll Documentation](https://docs.reqnroll.net/)
- [Selenium Documentation](https://www.selenium.dev/documentation/)
- [Appium Documentation](https://appium.io/docs/en/latest/)
- [FlaUI](https://github.com/FlaUI/FlaUI)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)

---

**Last Updated:** September 2026  
**Framework Version:** 3.1.1  
**Authoritative versions:** [`FRAMEWORK_MANIFEST.json`](FRAMEWORK_MANIFEST.json) and the project `.csproj` files. This Skills list is **not** the version source of truth.  
**Solution:** 6 projects (Framework, Tests, Runner, Runner.Core, Pdf.Core.Engine, Mocking)  
**Total Technologies:** 53+ packages in core; additional in Pdf.Core.Engine and Mocking (all commercial-use safe where applicable)  
**New Features:** 
- **Desktop (FlaUI, Sep 2026):** In-process UI Automation via `DesktopSessionFactory`. Windows Calculator sample (`Category=Calculator`). Login scenario is a template. See [DESKTOP_AUTOMATION.md](docs/guides/DESKTOP_AUTOMATION.md).
- **License Compliance (Feb 2026):** ClosedXML (Excel), SkiaSharp (visual testing); removed EPPlus, ImageSharp, iText7, NBomber, Applitools. [LICENSE-NOTICE.md](LICENSE-NOTICE.md) for full dependency list and licenses.
- Self-Healing Automation, ML-Based Test Selection, Modular Test Base Class
- Thread-Safe Reporting (ExtentReports & Allure with AsyncLocal<T>)
- Java-Style Configuration (Properties file support)
- Unified Reporting Service (Single API for Extent and Allure)
- Reqnroll Adapter Pattern (Automatic step tracking)
- Status Mapping Utility (Java-style status conversion)


---

## QualiFrame PDF Engine Skill

This skill describes how to use the unified **QualiFrame PDF engine** for reliable, report-friendly PDF validation and comparison inside the QualiFrame 3.0 framework.

---

### 1. PDF Engine Overview

- **Project**: `QualiFrame.Pdf.Core.Engine`
- **Namespaces**: `QualiFrame.Pdf.Core.Engine.*` (Core, Core.Contracts, Config, Engine, Validators, Parsing, Reporting, Integration)
- **Two usage modes**:
  - **Legacy/rule-based**: `IPdfExtractor`, `IPdfComparator`, `IPdfValidator` (orchestrator), `IPdfDiffReportGenerator`, `IPdfReportingBridge`; rule interfaces (`ITextValidationRule`, etc.); registered with `AddQualiFramePdfServices()`.
  - **Extensible subsystem**: Plugin-style validators (`Abstractions.IPdfValidator`), `IPdfParser`, `IPdfComparer`, `IValidationProfile`, `IValidationReporter`, `IPdfValidatorRegistry`; `PdfValidationEngine`; config via `pdf.validation.json`; registered with `AddQualiFramePdfValidationEngine()`.
- **Key capabilities**:
  - Text and layout comparison (`IPdfComparator` / `IPdfComparer`)
  - Content extraction and normalized document model (text, blocks, tables, images, metadata) (`IPdfExtractor`, `IPdfParser` → `PdfDocumentModel`)
  - Validation: rule orchestration (legacy) or plugin validators (Comparison, Structure, Font, Signature)
  - Diff report generation (`IPdfDiffReportGenerator`)
  - PDF download capture for Selenium tests (`IPdfDownloadCapture`)
  - Reporting bridge and validation reporter (`IPdfReportingBridge`, `IValidationReporter`)

The engine is built on permissively licensed PdfPig and SkiaSharp and is safe for commercial use.

---

### 2. Dependency Injection Setup

- **Where**: Test startup (for example in `PdfValidationTestBase.SetUp`).
- **Legacy PDF services**:

```csharp
var services = new ServiceCollection();
services.AddQualiFrameServices();
services.AddQualiFramePdfServices();       // Legacy: extractor, comparator, PdfValidator, diff generator, reporting bridge
var provider = services.BuildServiceProvider();
```

`AddQualiFramePdfServices()` (in `QualiFrame.Pdf.Core.Engine.Integration`) wires:

- `IPdfExtractor` → `PdfExtractor`
- `IPdfComparator` → `PdfComparator`
- `IPdfDiffReportGenerator` → `PdfDiffReportGenerator`
- `Core.IPdfValidator` → `PdfValidator` (rule-based orchestrator)
- `IPdfDownloadCapture` → `PdfDownloadCapture`
- `IPdfReportingBridge` → `PdfReportingBridge`

- **Extensible validation engine** (plugin validators, profile-based config):

```csharp
services.AddQualiFrameServices();
services.AddQualiFramePdfValidationEngine();  // Adds parser, comparer adapter, registry + 4 validators, engine, IValidationReporter
var provider = services.BuildServiceProvider();
var engine = provider.GetRequiredService<PdfValidationEngine>();
var profile = PdfValidationProfileLoader.LoadFromFile("Configuration/pdf.validation.json");
var report = await engine.ValidateAsync(basePdfPath, profile, candidatePdfPath: comparePath, artifactsOutputPath: outDir);
await provider.GetRequiredService<IValidationReporter>().ReportAsync(report, outDir);
```

---

### 3. Core Contracts You Should Use

- **Extraction** (`QualiFrame.Pdf.Core.Engine.Core` + `.Core.Contracts`)
  - `IPdfExtractor`
  - `PdfExtractionOptions`, `PdfExtractionResult`, `PdfPageContent`, `PdfTextBlock`, `PdfImageInfo`, `PdfTableInfo`

- **Comparison**
  - `IPdfComparator`
  - `PdfComparisonOptions`
  - `PdfComparisonResult`, `PdfPageComparisonDiff`, `PdfComparisonType`

- **Validation**
  - `IPdfValidator`
  - `PdfValidationRequest`
  - Rule interfaces: `ITextValidationRule`, `ITableValidationRule`, `IFontValidationRule`, `IImageValidationRule`, `ISignatureValidationRule`
  - `PdfValidationResult`, `PdfValidationFailure`, `PdfEvidenceAttachment`

- **Diff reporting**
  - `IPdfDiffReportGenerator`, `PdfDiffReportOptions`

- **Configuration** (`QualiFrame.Pdf.Core.Engine.Config`)
  - `PdfModuleConfiguration` (bind to `"PdfAutomation"` section in `appsettings.json`)
  - **Extensible subsystem**: `PdfValidationProfileConfig`, `PdfValidationProfileLoader` for `pdf.validation.json`; `IValidationProfile`, validator toggles, comparison/parsing/font/signature/structure options.

- **Extensible subsystem** (`QualiFrame.Pdf.Core.Engine.Abstractions` + `.Abstractions.Models`)
  - `IPdfValidator` (plugin: `Id`, `Name`, `ValidateAsync(PdfValidationContext)` → `ValidatorResult`)
  - `IPdfComparer` (optional: `CompareAsync` → `PdfCompareResult`), `IPdfParser` (`ParseAsync` → `PdfDocumentModel`), `IPdfValidatorRegistry`, `IValidationReporter`
  - Result types: `ValidationStatus`, `Finding`, `ValidatorResult`, `PdfValidationReport`, `PdfDocumentModel` (pages → blocks → lines → spans), `PdfParseOptions`, `PdfCompareOptions`

---

### 4. Using the Engine in Tests

The recommended entry point for tests is the **PDF test base**:

- `src/QualiFrame.Tests/Tests/PDFTests/PdfValidationTestBase.cs`
  - Sets up DI with `AddQualiFrameServices()` and `AddQualiFramePdfServices()`
  - Exposes `Reporting`, `PdfValidator`, and `PdfTextExtractor`
  - Provides helpers:
    - `LogStep(string stepName, string? detail = null)`
    - `AttachPdfArtifact(string pdfPath, string? textPreview = null, int maxPreviewChars = 2000)`
    - `AssertWithAllure(string stepName, Action assertion)`

**Patterns used in PDF tests** (`PdfComparisonTests`, `PdfTextValidationTests`, `PdfValidationSuiteTests`, `PdfMasterValidationTests`, **`PdfValidationEngineTests`**):

- Resolve and validate file paths with `LogStep` + `Reporting.AddLog`.
- Use `PdfTextExtractor` and `IPdfValidator` / `IPdfComparator` for the actual PDF logic; or use the **extensible engine** (`PdfValidationEngine`, profile from `pdf.validation.json`, `IValidationReporter`) for plugin-based validation and `report.json` artifacts.
- Wrap **all NUnit asserts** (including `Assert.Fail` and `Assert.That`) using `AssertWithAllure` so that pass/fail is visible as named steps in Allure:

```csharp
AssertWithAllure("Assert target text found",
    () => Assert.That(result.ActualCount, Is.GreaterThan(0), result.FailureMessage));
```

This ensures:

- All assertions are tracked as Allure steps.
- Failures automatically log to QualiFrame reporting and mark the Allure step as failed.

---

### 5. Configuration Hints

- **Legacy/module config**: Bind `PdfModuleConfiguration` from `appsettings.json` under the `"PdfAutomation"` section to control:

- Artifact locations (`ArtifactsBasePath`, `DownloadDirectory`)
- Timeouts and polling for downloaded PDFs
- Default comparison options (`PdfComparisonProfile`)
- Default validation options (`PdfValidationProfile`)
- Performance options (`PdfPerformanceProfile`)

Example (high level):

```json
"PdfAutomation": {
  "ArtifactsBasePath": "Results/artifacts/pdfs",
  "DownloadDirectory": "Results/downloads",
  "Comparison": {
    "VisualSimilarityThreshold": 0.99,
    "LayoutTolerancePoints": 2.0
  },
  "Validation": {
    "ValidateText": true,
    "ValidateTables": true
  }
}
```

- **Extensible engine**: Use `Configuration/pdf.validation.json` (see sample in the test project) for profile-driven validation. Top-level keys: `profileName`, `validators` (comparison/structure/font/signature toggles), `comparison`, `parsing`, `font`, `signature`, `structure`, `ignoreRegions`, `artifacts`. Load with `PdfValidationProfileLoader.LoadFromFile(path)` or `LoadFromJson(json)`; invalid config throws with clear messages.

---

### 6. Master / batch PDF validation

For large suites (for example ~250 PDFs validated against a single base), use the **master validation** components in the test project:

- `src/QualiFrame.Tests/Components/PdfComparison/PdfMasterValidationConfig.cs`
- `src/QualiFrame.Tests/Data/FileValidation/PdfMasterValidationConfig.json`
- `src/QualiFrame.Tests/Tests/PDFTests/PdfMasterValidationTests.cs`

Key ideas:

- All paths (base PDF, expected folders) are **relative** and resolved via `TestPathResolver` – no hardcoded absolute paths.
- `PdfMasterValidationTests`:
  - Reads `PdfMasterValidationConfig.json`
  - Resolves the base PDF and discovers expected PDFs via folder + pattern lists, with an optional hard cap (`MaxDocuments`)
  - Uses `IPdfComparator` + `IPdfDiffReportGenerator` to compare each expected PDF to the base and generate diff artifacts
  - Logs file-level and page-level comparison results via `Reporting`
  - Optionally forwards a summary into `IPdfReportingBridge` so PDF results are visible in the main reporting pipeline
  - Uses `AssertWithAllure` for the final "all comparisons passed" assertion so failures appear as a dedicated Allure step

You can tune batch behavior by editing `PdfMasterValidationConfig.json`:

- `BaseFilePath`: Base PDF (relative path).
- `ExpectedFolders`: One or more folders containing PDFs to validate.
- `ExpectedFilePatterns`: Patterns like `"*.pdf"` used under each folder.
- `MaxDocuments`: Cap for very large batches (for example 250).
- `Categories`: Toggles for comparison summary and additional validation categories (text, images, tables, fonts, signatures) – current implementation focuses on comparison and text, with others reserved for future rules.
- `Tolerances`: Extra options (for example `LayoutTolerancePoints`, `ImageDiffTolerance`, `TextIgnorePatterns`) that can be fed through to the engine.

Artifacts for a run are written under:

- `src/QualiFrame.Tests/TestResults/PdfMasterValidation/<timestamp>/`
  - Includes a `DiffReports` folder containing per-file diff outputs created by `IPdfDiffReportGenerator`.

---

### 7. When to Extend the Engine

Extend `QualiFrame.Pdf.Core.Engine` (not the tests) when you need:

- New reusable validation rules (implement and register additional `ITextValidationRule` / `ITableValidationRule` / etc.).
- More advanced diff reporting in `PdfDiffReportGenerator`.
- Additional integration helpers in `Integration` (for example, custom artifact naming or remote storage).

Keep tests focused on **scenarios and expectations**, and keep engine changes in the shared project so that all teams benefit from the same PDF behavior and reporting patterns.

---

### 8. Extensible PDF Validation & Comparison Subsystem

Use the **plugin-style subsystem** when you want profile-driven validation, multiple single-responsibility validators, and machine-readable reports (`report.json`).

- **Entry points**:
  - `PdfValidationProfileLoader.LoadFromFile("Configuration/pdf.validation.json")` → `IValidationProfile`
  - `PdfValidationEngine.ValidateAsync(basePdfPath, profile, candidatePdfPath, artifactsOutputPath, cancellationToken)` → `PdfValidationReport`
  - `IValidationReporter.ReportAsync(report, artifactsOutputPath)` → logs to QualiFrame reporting and writes `report.json` (and optional artifacts)

- **Built-in validators** (registered in `IPdfValidatorRegistry`): `PdfAdvancedComparisonValidator`, `PdfStructureValidator`, `PdfFontValidator`, `PdfDigitalSignatureValidator`. Enable/disable and configure via `pdf.validation.json` (`validators`, `comparison`, `parsing`, `font`, `signature`, `structure`, `artifacts`).

- **Adding a new validator** (open/closed principle):
  1. Implement `QualiFrame.Pdf.Core.Engine.Abstractions.IPdfValidator` (`Id`, `Name`, `ValidateAsync(PdfValidationContext)` returning `ValidatorResult`).
  2. Register the validator in DI and add it to the registry (e.g. in `AddQualiFramePdfValidationEngine()` or a custom extension that calls `registry.Register(myValidator)`).
  3. Optionally extend `PdfValidationProfileConfig` and `LoadedValidationProfile.GetValidatorOptions(id)` for profile-driven options.

- **Test**: `PdfValidationEngineTests.Validate_LoadProfile_BaseVsCompare_ProducesReportAndArtifacts` loads the sample profile, validates `Data/FileValidation/BasePDF.pdf` vs `ComparePDF.pdf`, and asserts report + `report.json`. Run it with:
  - `dotnet test src/QualiFrame.Tests/QualiFrame.Tests.csproj --filter "FullyQualifiedName~PdfValidationEngineTests"` (recommended when using .NET 10 / Microsoft.Testing.Platform to avoid invoking `QualiFrame.Runner`), or
  - `dotnet test Qualiframe-CSharp-NUnit_v3.0-TestsOnly.slnf --filter "..."` (solution filter excludes Runner).

- **Docs**: Full folder layout, config schema, and run instructions: `src/QualiFrame.Pdf.Core.Engine/README.md`.

---

### 9. QualiFrame.Mocking (API Mocking & Service Virtualization)

Use **QualiFrame.Mocking** when tests need to run against mocks instead of (or in addition to) real backends: config-driven modes (Off/Static/Dynamic/Hybrid), OpenAPI/Swagger-driven stubs, contract validation, and hybrid API+UI flows.

- **Project**: `src/QualiFrame.Mocking`
- **Entry points**:
  - **Config**: `MockServerConfig`, `OpenApiConfig`, `DockerConfig` (modes: Off, Static, Dynamic, Hybrid).
  - **Server**: `WireMockServerManager` (in-process WireMock.Net; port-based start), `DockerWireMockManager` (Docker.DotNet), `MockServerLifecycle`.
  - **Contracts**: `OpenApiSpecLoader`, `ContractValidator`, `SchemaValidationService` (string-based schema type checks for OpenAPI compatibility).
  - **Generation**: `OpenApiMockGenerator`, `ResponseTemplateBuilder`, `StubDefinitionFactory`; stubs for User, Order, Payment APIs in `Stubs/`.
  - **Hybrid testing**: `HybridTestOrchestrator`, `ApiUiValidationService`, `BackendRouteSwitcher`.
- **Fixtures**: `MockServerFixture`, `HybridTestFixture`, `ContractValidationFixture` (NUnit).
- **Integration samples**: `SampleSwaggerDrivenMockTest`, `SampleHybridUiApiTest`, `SampleContractValidationTest` under `src/QualiFrame.Tests/Tests/Integration/Mocking/` (`Category=Mocking`).
- **Docs**: Full architecture, usage, and implementation notes: `src/QualiFrame.Mocking/README.md`.

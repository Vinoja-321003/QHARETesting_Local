# License Notice – QualiFrame Test Automation Framework

This document lists the third-party libraries and tools used in the QualiFrame framework and their licenses. **All dependencies listed below (except where noted in "Special considerations") are suitable for commercial use without additional licensing.**

The framework has been curated to avoid libraries that restrict commercial use (e.g., PolyForm Noncommercial, AGPL-only, or "personal use only"). EPPlus, iText7, SixLabors.ImageSharp, and NBomber have been removed or replaced for this reason.

---

## Framework license

This project is licensed under the **Apache License 2.0**. See the [LICENSE](LICENSE) file in the repository root.

---

## Commercial-use status

| Category | Commercial use |
|----------|----------------|
| **All NuGet packages listed below** | ✅ **All** are suitable for commercial use without additional licenses or agreements. |
| **Eyes.Selenium (Applitools)** | ❌ **Not bundled.** It was removed so that every dependency is commercial-use safe. Visual testing is provided by **SkiaSharp** (MIT). To use Applitools, add the package and obtain a commercial license from Applitools separately. |
| **External tools** (e.g., Allure CLI, k6, JMeter) | ✅ Use under their own licenses (typically Apache 2.0 or MIT). Not bundled; used at pipeline level. |

---

## NuGet dependencies and licenses

Dependencies are listed by functional area. Each is used under the license indicated; **all** of these licenses permit commercial use without additional agreements.

### Testing framework

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| NUnit | 4.4.0 | MIT | ✅ Yes |
| NUnit3TestAdapter | 6.0.0 | MIT | ✅ Yes |
| NUnit.Analyzers | 4.11.2 | MIT | ✅ Yes |
| Microsoft.NET.Test.Sdk | 18.0.1 | MIT | ✅ Yes |
| BoDi | 1.5.0 | Apache-2.0 | ✅ Yes |
| Reqnroll | 3.0.0 | Apache-2.0 | ✅ Yes |
| Reqnroll.NUnit | 3.0.0 | Apache-2.0 | ✅ Yes |
| Reqnroll.Tools.MsBuild.Generation | 3.0.0 | Apache-2.0 | ✅ Yes |
| coverlet.collector | 6.0.4 | MIT | ✅ Yes |
| coverlet.msbuild | 6.0.4 | MIT | ✅ Yes |
| FluentAssertions | 8.8.0 | Apache-2.0 | ✅ Yes |
| AutoFixture | 4.18.1 | MIT | ✅ Yes |
| Bogus | 35.6.5 | MIT | ✅ Yes |

### Web and mobile automation

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| Selenium.WebDriver | 4.39.0 | Apache-2.0 | ✅ Yes |
| Selenium.Support | 4.39.0 | Apache-2.0 | ✅ Yes |
| Selenium.WebDriver.ChromeDriver | 143.0.7499.14600 | Apache-2.0 | ✅ Yes |
| Selenium.WebDriver.GeckoDriver | 0.36.0 | MPL-2.0 | ✅ Yes |
| Selenium.WebDriver.MSEdgeDriver | 141.0.3537.71 | MIT | ✅ Yes |
| SeleniumExtras.WaitHelpers | 1.0.2 | Apache-2.0 | ✅ Yes |
| Appium.WebDriver | 8.0.1 | Apache-2.0 | ✅ Yes |
| Selenium.Axe | 1.5.0 | Apache-2.0 | ✅ Yes |

### Desktop automation

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| FlaUI.Core | 5.0.0 | MIT | ✅ Yes |
| FlaUI.UIA3 | 5.0.0 | MIT | ✅ Yes |
| FlaUI.UIA2 | 5.0.0 | MIT | ✅ Yes |

### API and messaging

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| RestSharp | 113.0.0 | Apache-2.0 | ✅ Yes |
| Newtonsoft.Json | 13.0.4 | MIT | ✅ Yes |
| GraphQL.Client | 6.1.0 | MIT | ✅ Yes |
| GraphQL.Client.Serializer.Newtonsoft | 6.1.0 | MIT | ✅ Yes |
| Grpc.Net.Client | 2.71.0 | Apache-2.0 | ✅ Yes |
| Grpc.Tools | 2.76.0 | Apache-2.0 | ✅ Yes |
| System.Net.WebSockets.Client | 4.3.2 | MIT | ✅ Yes |
| Confluent.Kafka | 2.12.0 | Apache-2.0 | ✅ Yes |

### Reporting

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| ExtentReports | 5.0.5-beta1 | Apache-2.0 | ✅ Yes |
| Allure.Net.Commons | 2.14.1 | Apache-2.0 | ✅ Yes |
| Allure.NUnit | 2.14.1 | Apache-2.0 | ✅ Yes |

### Data access and file processing

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| ClosedXML | 0.105.0 | MIT | ✅ Yes |
| CsvHelper | 33.1.0 | MS-PL / Apache-2.0 (dual) | ✅ Yes |
| Microsoft.Data.SqlClient | 6.1.3 | MIT | ✅ Yes |

### Logging and configuration

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| Serilog | 4.3.0 | Apache-2.0 | ✅ Yes |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | ✅ Yes |
| Serilog.Sinks.Console | 6.1.1 | Apache-2.0 | ✅ Yes |
| Serilog.Sinks.Debug | 3.0.0 | Apache-2.0 | ✅ Yes |
| Serilog.Enrichers.Environment | 3.0.1 | Apache-2.0 | ✅ Yes |
| Serilog.Enrichers.Thread | 4.0.0 | Apache-2.0 | ✅ Yes |
| Serilog.Formatting.Compact | 3.0.0 | Apache-2.0 | ✅ Yes |
| Microsoft.Extensions.Configuration | 10.0.1 | MIT | ✅ Yes |
| Microsoft.Extensions.Configuration.Json | 10.0.1 | MIT | ✅ Yes |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 10.0.1 | MIT | ✅ Yes |
| Microsoft.Extensions.DependencyInjection | 10.0.1 | MIT | ✅ Yes |

### Resilience and HTTP

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| Polly | 8.6.5 | BSD-3-Clause | ✅ Yes |
| Polly.Extensions.Http | 3.0.0 | BSD-3-Clause | ✅ Yes |

### Image processing and encoding

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| SkiaSharp | 3.119.0 | MIT | ✅ Yes |
| System.Text.Encoding.CodePages | 10.0.1 | MIT | ✅ Yes |

### Cloud and infrastructure

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| AWSSDK.Core | 4.0.3.6 | Apache-2.0 | ✅ Yes |
| AWSSDK.S3 | 4.0.15.1 | Apache-2.0 | ✅ Yes |
| AWSSDK.DeviceFarm | 4.0.4.4 | Apache-2.0 | ✅ Yes |
| Azure.Identity | 1.17.1 | MIT | ✅ Yes |
| Azure.Storage.Blobs | 12.26.0 | MIT | ✅ Yes |
| Azure.Security.KeyVault.Secrets | 4.8.0 | MIT | ✅ Yes |
| Docker.DotNet | 3.125.15 | Apache-2.0 | ✅ Yes |

### Observability and security

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| OpenTelemetry | 1.14.0 | Apache-2.0 | ✅ Yes |
| OpenTelemetry.Exporter.Console | 1.14.0 | Apache-2.0 | ✅ Yes |
| System.IdentityModel.Tokens.Jwt | 8.15.0 | MIT | ✅ Yes |
| System.Security.Cryptography.Algorithms | 4.3.1 | MIT | ✅ Yes |

### Machine learning

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| Microsoft.ML | 3.0.1 | MIT | ✅ Yes |
| Microsoft.ML.TimeSeries | 3.0.1 | MIT | ✅ Yes |

### System and HTTP

| Package | Version | License | Commercial use |
|---------|---------|---------|----------------|
| System.Net.Http.Json | 10.0.1 | MIT | ✅ Yes |
| System.Xml.XmlDocument | 4.3.0 | MIT | ✅ Yes |
| System.IO.Compression | 4.3.0 | MIT | ✅ Yes |

---

## Special considerations

### Applitools (Eyes.Selenium) – not included

- **Eyes.Selenium** is **not** included in this framework so that **all** dependencies can be used commercially without limitations.
- Visual testing is provided by **SkiaSharp** (MIT) via `VisualTester` and `ScreenshotComparator` (baseline comparison, diff images). No commercial license required.
- If you want to use Applitools Eyes, add the `Eyes.Selenium` NuGet package and obtain a commercial license from [Applitools](https://applitools.com/legal/eula-sdk/). The leftover `Applitools:*` configuration keys were removed from `appsettings.json` and `TestConfiguration`.

### Libraries intentionally not used (license compliance)

The following were removed or never added to keep the framework commercial-use friendly:

- **EPPlus** – Replaced by **ClosedXML** (MIT). EPPlus is free only for non-commercial use (PolyForm Noncommercial).
- **iText7** – Not bundled. AGPL or commercial license required for closed-source/commercial use.
- **SixLabors.ImageSharp** – Replaced by **SkiaSharp** (MIT). ImageSharp requires a commercial license for certain for-profit use.
- **NBomber (v5+)** – Not bundled. Free for personal use only; organizational/commercial use requires a commercial license. Use external tools (e.g., k6, JMeter) for load testing if needed.

### External tools (not NuGet packages)

- **Allure CLI** – Used to generate Allure reports from results. Apache 2.0; commercial use allowed.
- **k6 / JMeter** – Suggested for performance/load testing; use under their respective licenses (AGPL for k6 with commercial options; Apache 2.0 for JMeter).

---

## Verification

License information was taken from NuGet, project repositories, and official license pages. You should confirm current terms at:

- [NuGet.org](https://www.nuget.org/) – package metadata and license URLs  
- Each project’s source repository (e.g., GitHub) – `LICENSE` or `LICENSE.txt`  
- Vendor pages for Applitools and any other commercial offerings  

**Summary:** Every dependency listed above is bundled under licenses that permit **commercial use without additional licensing**. Eyes.Selenium (Applitools) is not included; visual testing uses SkiaSharp for full commercial-use compliance.

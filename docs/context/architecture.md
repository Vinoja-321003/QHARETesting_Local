# Architecture and patterns

## Style

- **Layered:** Framework (shared) vs Tests (application) vs Runner (CLI orchestration).
- **Runner split:** `QualiFrame.Runner` (CLI) → `QualiFrame.Runner.Core` (orchestration) → `dotnet test` on `QualiFrame.Tests` at runtime (no compile-time test assembly reference).
- **POM:** `BasePage` / `ComponentPage` in `QualiFrame.Framework.PageBases`; concrete pages in `QualiFrame.Tests/PageObjects`.
- **Components:** Reusable UI pieces in `QualiFrame.Framework/Web/Components` (Header, Footer, Navigation, Form, Modal, Table, Dropdown).
- **Element wrapper:** `QualiFrame.Framework/Web/Elements` (`EnhancedWebElement`, fluent actions, retries/logging).

## Patterns (where to look)

| Pattern | Typical types / folders |
|---------|-------------------------|
| Factory | `EnhancedWebDriverFactory`, `DataRepositoryFactory` |
| Repository | `QualiFrame.Framework/Data/Repositories` |
| DI | `Microsoft.Extensions.DependencyInjection` + Reqnroll BoDi for step classes |
| Service | `ITestExecutionService`, `IReportingService` (`QualiFrame.Framework/Core/Services`) |
| Template method | `ModularTestCase` (`QualiFrame.Framework/Core/ModularTestCase.cs`) |
| Resilience | Polly retry/circuit breaker in `QualiFrame.Framework/API` |
| Pool | `DriverPool`, `DriverPoolManager` under `Utilities/WebDriver` |

## Visual testing

- SkiaSharp-based screenshot compare and baselines: `QualiFrame.Framework/Web/Visual` (e.g. `VisualTester`, `ScreenshotComparator`, `BaselineManager`).

## Desktop (FlaUI)

- Shared desktop stack under `QualiFrame.Framework/Desktop` (FlaUI session, pages, elements, actions); BDD glue under `QualiFrame.Tests` (`Desktop.feature`, `DesktopSteps`, `CalculatorSteps`, `DesktopHooks`).
- Create sessions with **`DesktopSessionFactory`**.
- Shipped `Desktop:AppId` launches Windows Calculator (`@Calculator`). The `@Desktop` login scenario is a placeholder and is not wired to Calculator.
- Guide and run commands: [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md).

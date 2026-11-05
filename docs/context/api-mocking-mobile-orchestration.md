# API, GraphQL, mocking, mobile, desktop, orchestration

## REST / GraphQL (framework)

- **`EnhancedApiClient`** — `src/QualiFrame.Framework/API/EnhancedApiClient.cs` — retries, circuit breaker hooks, schema validation, perf monitor integration.
- **GraphQL** — `GraphQLClient`, `QueryBuilder` under `src/QualiFrame.Framework/API/GraphQL/`.
- **Supporting types:** `RetryPolicy`, `CircuitBreaker`, `SchemaValidator`, `ApiPerformanceMonitor` in `API/`.

## Service virtualization / mocks

- Project **`QualiFrame.Mocking`**: WireMock.Net, OpenAPI load/generation, contract validation, Docker-hosted mocks, hybrid UI/API orchestration.
- Entry README: `src/QualiFrame.Mocking/README.md`.
- **Integration samples** (executable tests): `src/QualiFrame.Tests/Tests/Integration/Mocking/` — filter with `--filter "Category=Mocking"`.
- `QualiFrame.Tests` references Mocking and copies `QualiFrame.Mocking/Contracts/**` to test output.

## Mobile

- **Appium** types under `src/QualiFrame.Framework/Mobile` — `MobileElement`, `GestureActions`, `DeviceRotator`, `KeyboardHandler`, etc.
- Cloud: AWS Device Farm packages referenced from Framework/Tests (see manifest).

## Desktop

- **FlaUI** (UIA2/UIA3) under `src/QualiFrame.Framework/Desktop`.
- Session: `DesktopSessionFactory.GetSession()`. Tests: `Features/Desktop.feature` (`Category=Desktop` / `Category=Calculator`).
- Narrative guide: [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md).

## Orchestration & analytics

- **Orchestration:** `QualiFrame.Framework/Orchestration` (`TestDependencyGraph`, `TestOrchestrator`).
- **Analytics / ML selection:** `QualiFrame.Framework/Analytics` (`TestMetrics`, `TestAnalytics`, `SmartTestSelector`, execution history models).

## Data repositories

- **Factory:** `DataRepositoryFactory` in `QualiFrame.Framework/Data/Repositories`.
- **Implementations:** Excel (ClosedXML), CSV (CsvHelper), JSON, and SQL Server (`SqlDataRepository` via `DataRepositoryFactory` with `SQL` / `SQLSERVER` / `MSSQL` / `DATABASE`). SQL is opt-in; the factory default remains Excel. Requires `Environments:QA:DatabaseConnectionString`.

## gRPC / Kafka / WebSockets

- Packages present for gRPC client/tools, `Confluent.Kafka`, and `System.Net.WebSockets.Client` — use when enterprise integration tests require them; primary doc focus remains Web/API/UI.

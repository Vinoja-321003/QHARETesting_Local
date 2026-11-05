# Configuration

## Files

| File | Role |
|------|------|
| `src/QualiFrame.Framework/Configuration/appsettings.json` | Primary JSON configuration (copied to output via `QualiFrame.Framework.csproj`) |
| `extent.properties` (repo root) | Java-style Extent keys (optional); **takes precedence over JSON for Extent-only options**. Paths use `Results/Reports` and `Results/Screenshots` (aligned with `appsettings.json`). |
| `src/QualiFrame.Tests/allureConfig.json` | Allure settings; copied to test output |
| `src/QualiFrame.Tests/Configuration/pdf.validation.json` | Sample PDF validation profile for engine tests |
| `src/QualiFrame.Mocking/appsettings.Mocking.json` | Mock server defaults |

Optional environment-specific JSON files (e.g. `appsettings.Development.json`) can be added alongside `appsettings.json` following standard .NET configuration conventions if introduced in your fork.

## Key sections (names only — shapes live in JSON in repo)

- `TestConfiguration` — browser, timeouts, headless, screenshot on failure, **nested** `ParallelExecution` (`Enable`, `MaxParallelTests`, `MaxParallelSuites`), `VideoRecording` (opt-in frame capture; default `false`).
- `Environments` — named URLs (e.g. QA/Production `BaseUrl`, `ApiBaseUrl`) and optional `DatabaseConnectionString`.
- `Database` — SQL client timeouts and optional `DefaultTable` (used by `SqlDataRepository`).
- `Logging` — Serilog levels, file retention.
- `Security:SecretsManager` — encryption, optional Azure Key Vault.
- `Healenium` — enable, backend URL, timeouts, max healing attempts. Shipped `appsettings.json` sets `Enabled` to `true`; the getter falls back to `false` if the key is missing. Start the backend at `BackendUrl`, or set `Enabled` to `false` for runs without healing.
- `SelfHealing:Provider` — exclusive healer: `Healenium` (shipped default), `Qhare`, or `Off`. Do not run both wrappers.
- `Qhare` — core URL (`BaseUrl`, default `http://localhost:8000`), `ProjectId`, `Mode` (`AUTO_HEAL`), timeout, `FailClosed`. Dashboard is separate (`http://localhost:3000/healing`). QHARE core and the Healenium selector-imitator both default to port 8000; do not run both stacks on that port.
- `SmartTestSelection` — ML selector enable, recommendation count, history JSON path.
- `Reporting` — Extent paths/theme, Allure results path, screenshot folder.
- `Desktop` — FlaUI AUT (`AppId`, optional `Arguments` / `ProcessName` / `WindowTitle`), `AutomationType` (`UIA3`/`UIA2`), `LaunchMode` (`Auto` / `Launch` / `Attach` / `StoreApp`), `LaunchTimeoutSeconds`. Shipped `AppId` is Windows Calculator. Full key table: [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md).

## Precedence (Extent / Allure)

1. `extent.properties` — overrides JSON for **Extent** reporter keys only.
2. `ALLURE_RESULTS_DIRECTORY` — env overrides appsettings for Allure output folder.
3. `appsettings*.json` — remaining reporting and framework settings.
4. Code defaults inside managers/reporters.

## Validation

- `ConfigurationValidator` runs early (e.g. from hooks / test run setup); invalid config should fail fast before launching browsers at scale.

## Secrets

- `SecretsManager` (`QualiFrame.Framework/Security`) — AES at rest, optional Azure Key Vault (`Azure.Security.KeyVault.Secrets`).

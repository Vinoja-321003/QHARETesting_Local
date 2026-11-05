# WebDriver, pooling, Healenium

## Factory

- **`EnhancedWebDriverFactory`** (`QualiFrame.Framework/Utilities/WebDriver/EnhancedWebDriverFactory.cs`) — primary entry for creating browsers with framework defaults (implicit/page-load timeouts, download folders, cloud caps as configured).
- **`WebDriverFactoryWrapper`** — DI-friendly adapter around factory patterns.
- **`DriverHealthChecker`** — pre-flight checks where implemented.

## Pooling

- **`DriverPool` / `DriverPoolManager`** — reuse drivers for parallel runs; size and TTL from `appsettings.json` under `DriverPool` / `TestConfiguration` (see live JSON for exact property names).

## Self-healing (Healenium or QHARE)

Choose **one** provider via `SelfHealing:Provider`: `Healenium` (default), `Qhare`, or `Off`. The factory never wraps both.

- **`HealeniumDriverWrapper`** — wraps `IWebDriver` when the provider is Healenium (legacy: `Healenium:Enabled` true and `SelfHealing:Provider` omitted).
- Shipped `appsettings.json` sets `SelfHealing:Provider` to **Healenium** and `Healenium:Enabled` to **true**. Start the backend at `Healenium:BackendUrl` (default `http://localhost:7878`), or set `Provider` to `Off` / `Enabled` to `false` for local runs without healing. The Healenium getter falls back to `false` if the key is missing.
- **Evidence store** (`HealeniumEvidenceStore`) — versioned JSON baselines under `Healenium:EvidencePath` (default `Results/HealeniumEvidence/baselines`). Identity is original locator + normalized page URL. New versions are appended; similar fingerprints only refresh the timestamp.
- **Promotion** — Reqnroll `AfterStep` commits pending captures/healings only when the step passed. Failed steps and scores below `Healenium:ScoreCap` (default `0.5`) write diagnostics under `audits/` and do not replace the trusted baseline.
- **Healing** — failed finds (and finds whose fingerprint is far from the latest trusted baseline) score current DOM candidates against historical node paths, newest trusted version first, then older versions. The highest candidate at/above `ScoreCap` is used.
- Official backend calls remain: `POST /healenium/report/init/{sessionId}`, successful finds `POST /healenium`, healings `POST /healenium/healing`.
- **QHARE** (`QhareDriverWrapper`) — when `SelfHealing:Provider` is `Qhare`, native `FindElement` runs first, then `POST /api/v1/heal` against `Qhare:BaseUrl` (default `http://localhost:8000`) with `action: find`. Only `HEALED` candidates are applied. `SUGGESTED` / `OBSERVED` / `UNRESOLVED` keep the original miss and are logged with confidence, reason, and top alternative. Unresolved heals may `POST /api/v1/recover` and retry the original locator. `heal.validated` flushes after a passing Reqnroll step; `heal.failed` / `heal.rejected` flush even when the step fails. Core outages fail closed (original `NoSuchElementException`).
- **No locator syntax change** — page objects keep using `By`; the active wrapper intercepts `FindElement` / empty `FindElements`.

## Video (opt-in)

- **`VideoRecordingManager`** (`Utilities/WebDriver/VideoRecordingManager.cs`) — no-op unless `TestConfiguration:VideoRecording` is true (default `false`).
- When enabled, captures PNG frames under `Results/Videos` and, for cloud sessions, sets the `Video` capability only if the flag is true (does not force cloud video off).

## Deprecated / legacy

- **`WebDriverFactory`** (legacy) — avoid for new code; prefer `EnhancedWebDriverFactory` + DI.

## Cloud

- Sauce Labs / BrowserStack style caps can be supplied via configuration (see `TestConfiguration` and factory branches — inspect `EnhancedWebDriverFactory` for supported keys).

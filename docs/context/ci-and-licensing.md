# CI/CD and licensing

## Pipelines (dual CI)

This repository supports **GitHub Actions, Azure DevOps, Jenkins, and GitLab CI**. They run the same test commands via [`scripts/ci-build-test.sh`](../../scripts/ci-build-test.sh). Pull requests also validate [`FRAMEWORK_MANIFEST.json`](../../FRAMEWORK_MANIFEST.json) via [`scripts/ci-validate-manifest.sh`](../../scripts/ci-validate-manifest.sh) (GitLab uses a `python:3.12-slim` `validate-manifest` job). That gate does not change restore/build/test.

| Platform | Config file | How to enable |
|----------|-------------|---------------|
| **GitHub Actions** | [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) | Automatic on push/PR to `main`, `master`, or `develop` when the repo is hosted on GitHub |
| **Azure DevOps** | [`azure-pipelines.yml`](../../azure-pipelines.yml) | **Pipelines → New pipeline → Azure Repos Git** → select repo → existing `azure-pipelines.yml` |
| **Jenkins** | [`Jenkinsfile`](../../Jenkinsfile) | Point a multibranch/freestyle job at the repo root |
| **GitLab CI** | [`.gitlab-ci.yml`](../../.gitlab-ci.yml) | Uses `mcr.microsoft.com/dotnet/sdk:10.0` and `scripts/ci-build-test.sh` against `QualiFrame.Tests` |
| **Local / shell** | [`run-tests.sh`](../../run-tests.sh) | `./run-tests.sh --all` (interactive filters) or `bash scripts/ci-build-test.sh` (CI parity) |
| **Clean artifacts** | [`scripts/clean.sh`](../../scripts/clean.sh) | `bash scripts/clean.sh` — removes `bin/`, `obj/`, `Results/`, `TestResults/` |

Root [`.gitignore`](../../.gitignore) excludes build outputs, test/report artifacts, IDE files, caches, and secrets across **GitHub, GitLab, and Azure DevOps**.

### Shared CI behaviour

- **SDK:** `global.json` (.NET 10 SDK; builds `net8.0`, `net9.0`, `net10.0`)
- **Configuration:** `Release` only in CI (Runner projects stay Debug-only locally)
- **Target:** `src/QualiFrame.Tests/QualiFrame.Tests.csproj` only — avoids solution-wide `dotnet test` pulling in `QualiFrame.Runner` / MTP conflicts (see [execution.md](execution.md)). CI matrix runs each TFM via `scripts/ci-build-test.sh`.
- **Desktop:** FlaUI is Windows-only. Linux CI can restore/compile (`EnableWindowsTargeting`); `@Desktop` scenarios are inconclusive off Windows.

### GitHub Actions notes

- Actions are **pinned to commit SHAs** in `ci.yml` so local validators and restricted networks do not need to resolve `@v4` tags.
- IDE warnings like *"Unable to resolve action actions/checkout@v4"* are expected when the editor cannot reach GitHub; the workflow still runs on `github.com` runners.

### Azure DevOps notes

- Use **`azure-pipelines.yml`** at the repo root — `.github/workflows/` is **not** executed by Azure DevOps.
- `UseDotNet@2` with `useGlobalJson: true` reads the same SDK as GitHub Actions.
- Test TRX files are published from the agent workspace; coverage output is published from `src/QualiFrame.Tests/TestResults`.

### Local parity with CI

```bash
export BUILD_CONFIGURATION=Release
bash scripts/ci-build-test.sh
```

## Commercial-use / dependency policy

- Dependencies are chosen to avoid restrictive default licenses in enterprise settings.
- **Removed or avoided in core:** EPPlus, SixLabors.ImageSharp, iText7, NBomber, Applitools Eyes.Selenium defaults.
- **Replacements:** ClosedXML (Excel), SkiaSharp (images / visual testing), PdfPig-based PDF stack, external k6/JMeter for load in CI if needed.
- **Authoritative list:** `LICENSE-NOTICE.md` (names, versions, SPDX/license family, commercial-use notes).

## Third-party doc links (external)

- NUnit: https://docs.nunit.org/
- Reqnroll: https://docs.reqnroll.net/
- Selenium: https://www.selenium.dev/documentation/
- Appium: https://appium.io/docs/en/latest/
- FlaUI: https://github.com/FlaUI/FlaUI

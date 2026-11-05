# Archive

Historical merge and migration reports from February–March 2026. **Not required for day-to-day development.**

These documents may reference paths or CI that have since changed:

| Was | Now |
|-----|-----|
| `documents[md files]/` | `docs/guides/` |
| `.gitlab-ci.yml` only | GitHub Actions + Azure DevOps + Jenkins |
| `QualiFrame.Mocking/Examples/` | `QualiFrame.Tests/Tests/Integration/Mocking/` |
| Runner → Tests project reference | `QualiFrame.Runner.Core` (lightweight) |
| Desktop automation (March 2026 snapshot) | FlaUI in-process (`DesktopSessionFactory`). See [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md). |

Current source of truth: [CHANGELOG.md](../../CHANGELOG.md), [docs/context/](../context/README.md).

| File | Topic |
|------|--------|
| [MERGE_ANALYSIS.md](MERGE_ANALYSIS.md) | Truestage → master merge strategy |
| [MERGE_SUMMARY_REPORT.md](MERGE_SUMMARY_REPORT.md) | PDF & Mocking merge summary |
| [ENTERPRISE_UPSTREAM_MIGRATION_REPORT_20260318.md](ENTERPRISE_UPSTREAM_MIGRATION_REPORT_20260318.md) | GitHub → GitLab upstream sync |

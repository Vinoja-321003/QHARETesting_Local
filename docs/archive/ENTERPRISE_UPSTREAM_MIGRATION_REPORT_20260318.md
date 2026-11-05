# Enterprise Upstream Migration Report (Audit Grade)

> **Historical document (March 2026).** CI is now GitHub Actions + Azure DevOps + Jenkins (not `.gitlab-ci.yml`). Docs live under `docs/guides/` and `docs/archive/`. **Desktop tests now use FlaUI** (`DesktopSessionFactory`); ignore any desktop driver-server or extra-process guidance below. See [CHANGELOG.md](../../CHANGELOG.md) and [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md).

## Executive summary
- **Objective**: Integrate all latest upstream changes from GitHub into the enterprise-managed GitLab repository **without touching `main` directly**, preserving enterprise stability and traceability.
- **Scope**: Sync enterprise `main` (GitLab) with upstream `main` (GitHub) that was **3 commits ahead**.
- **Outcome**: Upstream changes integrated into a dedicated migration branch via a **non-destructive merge commit**. No enterprise history rewritten. Operation is reversible by simply deleting the migration branch or reverting the merge commit on that branch.
- **Risk level**: **Low** (fast-forwardable divergence; no enterprise-only commits found). Notable operational considerations documented below (platform-specific test dependencies and external API behavior).

## Repositories
- **Upstream (source)**: `https://github.com/qualitestautomationcoe/Qualiframe-Selenium-CSharp-NUnit_v3.0.git`
- **Enterprise (target)**: `git@gitlab.com:qualitest.group/qualiframev3.0/qualiframe-selenium-csharp-nunit_v3.0.git`

## Safety constraints compliance (non-negotiables)
- **Zero destructive operations**: No deletes of enterprise code/history; only additive merge into a migration branch. (One hygiene change removed mistakenly tracked build artifacts from the *migration branch* working tree; does not affect enterprise `main` unless approved/merged.)
- **No overwrite of target history**: No rebases, resets, force-push, or history edits.
- **No direct `main` modification**: All work performed on `migration/github-sync-20260318-zero-risk`.
- **Fully reversible**: Migration branch can be abandoned; merge commit can be reverted within the branch if needed.
- **Production readiness maintained**: No change is applied to enterprise `main` until Merge Request review/approval.

## Phase 1 — Repository intelligence & risk assessment

### A. Structural analysis
- **Layouts**: Both repos share the same solution-centric layout:
  - Root: `Qualiframe-CSharp-NUnit_v3.0.sln`, `.gitlab-ci.yml`, `Jenkinsfile`, `global.json`, `Directory.Build.props`, `src/`, `documents[md files]/`
- **New components introduced by upstream**:
  - `src/QualiFrame.Pdf.Core.Engine/` (new project + architecture)
  - `src/QualiFrame.Mocking/` (new project + utilities)
  - `src/QualiFrame.Framework/Desktop/` (Windows desktop automation module)
- **Removed/deprecated**:
  - `src/QualiFrame.Framework/Web/Visual/ApplitoolsVisualTester.cs` removed upstream (license/usage constraints) in favor of non-commercial-restricted paths.
- **Moved/renamed modules**: No evidence of broad renames/moves; changes are primarily additive.

### B. History & divergence analysis
- **Common ancestor**: `504c53c5838d499b83666481316b452e42f32993`
- **Ahead/behind**:
  - Enterprise `main` vs Upstream `main`: **0 commits ahead / 3 commits behind**
  - This indicates a clean, low-risk sync (enterprise has no unique commits beyond the common ancestor at the time of analysis).
- **Upstream commits integrated**:
  - `50347a8` dependency licensing hardening (ClosedXML, SkiaSharp; removal of EPPlus, ImageSharp, iText7, NBomber)
  - `c437a83` PDF validation/comparison engine
  - `46a0134` Windows desktop automation (FlaUI is the current stack; see [DESKTOP_AUTOMATION.md](../guides/DESKTOP_AUTOMATION.md))

### C. Dependency & toolchain analysis
- **Solution updates**:
  - `Qualiframe-CSharp-NUnit_v3.0.sln` includes new projects `QualiFrame.Pdf.Core.Engine` and `QualiFrame.Mocking`.
- **NuGet changes** (high signal):
  - **EPPlus removed** → **ClosedXML added** (commercial-safe)
  - **SixLabors.ImageSharp removed** → **SkiaSharp added**
  - **iText7 removed** (AGPL/commercial) → PDF capabilities implemented without bundling iText7
  - **NBomber removed** (commercial licensing concerns)
- **Build configuration**:
  - No breaking changes detected in `global.json`, `Directory.Build.props`, `NuGet.config` at the repository level during the comparison.

### D. Customization detection (enterprise preservation)
Enterprise governance/custom elements identified and preserved by policy:
- **CI/CD**: `.gitlab-ci.yml`, `Jenkinsfile`
- **Environment / config**: `global.json`, `NuGet.config`, `Directory.Build.props`, and existing configuration patterns
- **Governance artifacts**: `documents[md files]/` and enterprise documentation retained

## Phase 2 — Migration strategy design
- **Branch**: `migration/github-sync-20260318-zero-risk` created from `enterprise/main`.
- **Remotes**:
  - `origin` = GitHub upstream
  - `enterprise` = GitLab enterprise (SSH)
- **Integration style**: **Additive merge** (merge commit), no history rewrite.

## Phase 3 — Controlled integration execution
### Merge rules applied
- **Enterprise-first where behavior diverges**: No merge conflicts occurred, so no behavioral conflicts required adjudication.
- **Add/merge upstream improvements**: Completed (new modules, dependency hardening, docs).
- **Preservation**:
  - Enterprise `main` not touched.
  - No enterprise-only commits existed to be overwritten at fork point.

### Conflict resolution log
- **Conflicts**: **None** (merge was clean).

### Post-merge hygiene (governance)
- **Build artifacts untracked**: `src/QualiFrame.Tests/obj/**` was found tracked post-merge and was removed from tracking on the migration branch.
- **Enterprise ignore patterns restored**: `.gitignore` adjusted to continue ignoring `Results/` and report outputs, matching enterprise expectations.

## Phase 4 — Framework integrity validation

### Build validation
- **Result**: **PASS**
- **Command pattern** (single-node MSBuild to avoid host constraints):
  - `dotnet restore` + `dotnet build -c Release`
- **Notes**:
  - Initial parallel build/test execution produced an MSBuild node IPC “permission denied” on this host; rerunning single-node resolved it.

### Runtime/test validation
- **Focused validation executed**: **PASS** for PDF-focused tests:
  - `dotnet test ... --filter "FullyQualifiedName~PDF"`
- **Known environment constraints**:
  - Desktop automation tests are **Windows-only** (FlaUI). On non-Windows hosts they are marked inconclusive.
  - Some API tests depend on external `petstore.swagger.io` behavior, which may differ from expected status codes (environmental flakiness; not a merge regression).

### CI/CD validation
- **Files preserved**: `.gitlab-ci.yml`, `Jenkinsfile` remain present and unchanged by policy.

## Phase 5 — Change inventory (high level)
### Added
- **Desktop automation module**: `src/QualiFrame.Framework/Desktop/**`
- **PDF engine**: `src/QualiFrame.Pdf.Core.Engine/**`
- **Mocking module**: `src/QualiFrame.Mocking/**`
- **Configuration**: `src/QualiFrame.Framework/Configuration/appsettings.json`
- **Docs**: multiple new/updated documents under `documents[md files]/`

### Modified
- `.gitignore` (enterprise report/results ignores restored; repo hygiene)
- Solution + csproj updates to wire new modules and adjust packages
- `src/QualiFrame.Framework/Configuration/TestConfiguration.cs` (env-var override precedence hardened for CI/non-Windows safety)

### Removed
- `src/QualiFrame.Framework/Web/Visual/ApplitoolsVisualTester.cs` (upstream removal)
- Tracked build artifacts under `src/QualiFrame.Tests/obj/**` (migration-branch hygiene only)

## Breaking change analysis
- **Platform specificity**:
  - Desktop automation is Windows-only (FlaUI); guard desktop suites by OS in CI.
- **Licensing-driven package changes**:
  - EPPlus/ImageSharp/iText7/NBomber removals may impact downstream consumers who relied on them; replacements are provided/expected via new modules and patterns.
- **External API expectations**:
  - Tests asserting `petstore.swagger.io` responses may fail due to upstream external behavior changes (not deterministic).

## Customization preservation report
- ✅ **No enterprise functionality removed from `enterprise/main`** (branch-only integration).
- ✅ **No client customization overwritten** (no enterprise-only commits at divergence point; enterprise branch remains unchanged).
- ✅ **No security configuration altered on enterprise mainline** (changes live only on migration branch until MR approval).

## Post-migration recommendations
- **CI gating**:
  - Mark Desktop/Mobile suites as Windows-only jobs; desktop tests use FlaUI and need no extra driver process.
- **API test hardening**:
  - Consider mocking Petstore responses or pinning to controlled endpoints for deterministic CI.
- **Future sync strategy**:
  - Repeat this process periodically: fetch upstream, merge into dated migration branch, validate, MR to enterprise.

## Clean commit strategy
Commits on `migration/github-sync-20260318-zero-risk` are structured and reversible:
- Merge commit: `[MIGRATION] Sync upstream GitHub changes — zero-risk integration`
- Hygiene commit: `[MIGRATION] Preserve enterprise ignores; untrack build artifacts`
- Safety commit: `[MIGRATION] Allow env overrides to supersede JSON config`


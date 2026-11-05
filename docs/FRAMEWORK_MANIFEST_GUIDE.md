# Framework Manifest Guide

The manifest is the AI-readable Single Source of Truth for this repository. It answers what exists, how it should be used, and where deeper details live.

| File | Responsibility |
|------|----------------|
| `FRAMEWORK_MANIFEST.json` | Framework knowledge: identity, capabilities, modules, reusable components, lifecycle, configuration keys, execution, docs. |
| `FRAMEWORK_MANIFEST.schema.json` | Contract for that knowledge. Change only when the knowledge model cannot represent a concept. |
| `docs/FRAMEWORK_MANIFEST_GUIDE.md` | This guide. |

## Manifest vs schema

- **Routine framework change** (new module, component, capability, integration, command, doc): update `FRAMEWORK_MANIFEST.json` only. Preserve stable IDs.
- **Knowledge-model change** (new section, property, enum, or ID format): update the schema, bump `schemaVersion`, update the manifest, validate, and record the change here.

Do not silently rename schema properties.

## Schema versioning

`schemaVersion` and `manifestVersion` use semantic versioning.

| Version | Meaning |
|---------|----------|
| `schemaVersion` `1.0.0` | First formal schema. Introduced the knowledge model (replacing the previous package-inventory JSON that had no schema file). |
| `manifestVersion` `2.0.0` | First schema-compliant knowledge dump. Curated project roles, NuGet versions, and removed-dependency notes from the old inventory were preserved. |
| `manifestVersion` `2.0.1` | SQL repository, nested ParallelExecution keys, opt-in video capture, GitLab CI aligned to `ci-build-test.sh`. |
| `manifestVersion` `2.0.2` | Incremental reconcile: removed stale `healenium/` compose paths; added CalculatorSteps, GraphQL/Extent reuse, SQL/API config keys, newsroom binding gap, and additional capability traceability. Schema unchanged. |
| `manifestVersion` `2.0.3` | Newsroom bindings, GraphQL example, AppiumDriverFactory/WEBVIEW, opt-in gRPC/WebSocket/Kafka/OTel/S3/Blob wrappers, synthetic test data, ZAP availability gating. Schema unchanged. |
| `manifestVersion` `2.0.4` | Version alignment (csproj 3.1.1), Skills.md inventory refresh, leftover Applitools config marked deprecated, in-repo `healenium/` compose restored. Schema unchanged. |
| `manifestVersion` `2.0.5` | PR/merge governance: `scripts/ci-validate-manifest.sh` wired into GitHub, Azure, GitLab, and Jenkins. Skills.md marked non-authoritative for versions. Runner/Tests version split documented. Schema unchanged. |
| `manifestVersion` `2.0.6` | Removed the Newsroom sample (feature, steps, page object, environment config). Schema unchanged. |
| `manifestVersion` `2.0.7` | Evidence-based Healenium healing (local baselines, ScoreCap, step-gated promotion). Schema unchanged. |
| `manifestVersion` `2.0.8` | Desktop automation uses FlaUI via `DesktopSessionFactory`. Schema unchanged. |
| `manifestVersion` `2.0.9` | Desktop docs and knowledge-model completeness: Calculator vs login-template limitation, `Category=Calculator` filter, `config.desktop.launchMode`. Schema unchanged. |
| `manifestVersion` `2.0.10` | Desktop public API is `DesktopSessionFactory` only; leftover driver-compatibility types removed. Schema unchanged. |

Bump **patch** for additive optional fields, **minor** for compatible additions, **major** for breaking required/enum/ID changes.

## Update workflow

1. Analyze the repository (source, config, docs, CI). Do not infer from folder names alone.
2. Reconcile with the existing manifest. Preserve valid curated data and stable IDs.
3. Mark removed items `deprecated`; do not invent capabilities.
4. Use `implemented` / `partiallyImplemented` / `planned` / `deprecated` / `notSupported` / `notDetected` / `unknown` plus `high` / `medium` / `low` confidence.
5. Store repository-relative paths only. Never copy secret values.
6. Validate (below).
7. Change the schema only if the current contract cannot represent the concept.

## Validation

```bash
python3 scripts/validate-framework-manifest.py
```

The script requires the `jsonschema` package (`pip install jsonschema` in a virtualenv). It checks:

- JSON syntax
- schema compliance
- unique IDs
- internal references
- referenced source/documentation paths
- declared output folders (`artifactDirectories`) — missing dirs warn only (often gitignored until a run)
- obvious secret values

CI runs this on pull requests and default-branch pushes via `scripts/ci-validate-manifest.sh` (GitHub Actions, Azure DevOps, Jenkins) and a dedicated GitLab `validate-manifest` job. The build/test script (`scripts/ci-build-test.sh`) is unchanged.

## Stable IDs

Keep IDs stable across updates.

| Kind | Pattern | Example |
|------|---------|---------|
| Capability | `capability.*` | `capability.web.selfHealing` |
| Approach | `approach.*` | `approach.bdd` |
| Standard | `standard.*` | `standard.web.waits` |
| Module | `module.*` | `module.web.core` |
| Component | `component.*` | `component.web.click` |
| Integration | `integration.*` | `integration.healenium` |
| Documentation | `DOC-<AREA>-NNN` | `DOC-WEB-001` |
| Config | `config.*` | `config.healenium.enabled` |

Prefer references over copying source or large documentation into the JSON.

## AI / MCP consumption

1. Read the manifest and validate it.
2. Identify capability and automation approach.
3. Apply standards and anti-patterns.
4. Locate the module; **reuse existing components** before creating helpers.
5. Check lifecycle and configuration (secret references only).
6. Follow implementation, example, and documentation references.
7. Generate or modify code in the correct layer (`QualiFrame.Framework` vs `QualiFrame.Tests`).
8. Update the manifest only when knowledge changed.

Core behavior: **Understand → Validate → Locate → Reuse → Extend → Verify → Document**.

Start with `docs/context/README.md` and `docs/MEMORY_MAP.yaml` for token-efficient topic routing. Agent rules: `docs/context/agent-instructions.md`.

## Adding a capability or component

1. Verify it in source or configuration.
2. Add a catalog item with a new stable ID, status, confidence, and evidence paths that exist.
3. Link it from `traceability` when it is a major capability.
4. Index any new docs in `documentation`.
5. Validate. Do not add unsupported capabilities.

## Troubleshooting

| Symptom | What to check |
|--------|----------------|
| Schema error | Property not in schema, wrong enum, or ID pattern. That is a schema change if the concept is real. |
| Missing path | Path must be repository-relative and exist on disk. |
| Unresolved reference | Referenced ID is missing or misspelled. |
| Duplicate ID | IDs must be unique across the file. |
| Secret flagged | Remove actual secret values; keep key names and `secretReference` only. |
| Unsure if something exists | Mark `unknown` / `notDetected` / `partiallyImplemented` with evidence. Do not fabricate. |

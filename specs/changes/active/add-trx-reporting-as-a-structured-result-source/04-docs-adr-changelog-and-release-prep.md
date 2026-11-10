# 04 - Docs, ADR, changelog and release prep

Owns: docs/architecture/result-sources.md, docs/decisions/**, docs/README.md, README.md, CHANGELOG.md, skills/dotnet-test-axi/SKILL.md
Docs: docs/architecture/test-filtering.md

## Goal

Docs, ADR, README, changelog and skill describe TRX reporting, and the version is ready to bump.

## Context

Docs impact section of the proposal lists the files. CHANGELOG uses Keep a Changelog with an entry under Unreleased in Added (and Changed if the output changes). Version bump is a minor (feat:) via pwsh scripts/bump-version.ps1 minor at release time, not in this step. The generic-repo rule applies: no references to other projects or private paths.

## Work

1. Create docs/architecture/result-sources.md with docs-axi new architecture: TRX versus console parsing, artifact naming, merge and mismatch rules, per-framework TRX option, missing-package warning.
2. ADR docs/decisions/0002-prefer-trx-over-console-parsing-when-the-extension-is.md already exists as proposed; review it against what was built and set it accepted with docs-axi.
3. README: add a TRX column to the framework table and document trx:.
4. CHANGELOG: one short user-facing line under Unreleased/Added.
5. skills/dotnet-test-axi/SKILL.md: mention trx: and how to read it for detail.
6. docs-axi check passes.

## Done when

docs-axi check passes; README table matches observed behavior of the samples; CHANGELOG has the Unreleased entry; the VersionTests still pass.

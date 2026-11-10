# 08 - Update docs for per-framework filters

Owns: README.md, docs/architecture/test-filtering.md, AGENTS.md
Docs: README.md, docs/decisions/0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and.md

## Goal

Document the filter syntax, the per-framework behaviour and the architecture, and keep the decision record accurate.

## Context

README.md has a framework table saying --class/--method/--namespace do not work outside xUnit v3: that becomes true for all four. ADR-0001-use-xunit-v3-filter-semantics-as-the-tool-s-own-and is the decision record (status proposed): accept it once the change is verified. Keep the repo generic (no references to other projects).

## Work

1. Update the README: framework table, filter syntax and examples, --parallel, the frameworks config key, mixed solution behaviour and the projects table.
2. Create docs/architecture/test-filtering.md with docs-axi new (detection, adapters, run strategies, how to add an adapter).
3. Update AGENTS.md with the rule to add an adapter plus tests and fixtures for any new framework.
4. Accept the ADR with docs-axi update. Run docs-axi check.

## Done when

docs-axi check and change-axi check pass, and the README table matches the behaviour verified in the end-to-end tests.

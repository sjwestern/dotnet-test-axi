---
title: "Use xUnit v3 filter semantics as the tool's own and translate per framework"
summary: The --class, --method and --namespace flags keep xUnit v3 rules and framework adapters translate them
status: proposed
---

# Use xUnit v3 filter semantics as the tool's own and translate per framework

## Context

Only xUnit v3 accepts --filter-class, --filter-method and --filter-namespace. MSTest and NUnit take a VSTest-style --filter expression and TUnit takes --treenode-filter. A solution can hold projects in several frameworks and one command line cannot carry different filters to each. A test run sent an unsupported flag reports every other module as a failed handshake.

## Decision

The tool's --class, --method and --namespace flags use xUnit v3 semantics: fully qualified names, with * allowed only at the start or end of a value, and a method needs a qualified name or a leading *. Each project's framework is detected from its PackageReference items and an adapter translates the filter to that framework's native options. A solution whose projects share one framework runs once with the translated flags. A mixed solution is built once, then each project runs with --no-build and its own filter, in parallel capped by processor count, and the results are merged.

## Consequences

Agents learn one filter vocabulary. Detection costs about half a second per project and only happens when a filter is given. Wildcards in the middle of a value are rejected rather than approximated. Unknown frameworks need a config override or native options after --. Mixed solutions lose the single-command solution run and gain a per-project table in the output.

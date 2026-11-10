---
title: Prefer TRX over console parsing
summary: Use TRX as the structured result source for counts and failures, with console parsing for the rest
status: accepted
---

# Prefer TRX over console parsing

## Context

Console parsing breaks on output changes and truncates detail. TRX is stable and carries full detail.

## Decision

When a project can produce a TRX (xUnit v3 always, MSTest, NUnit and TUnit with the TrxReport extension) the tool writes it into the same run folder as the log and reads failures from it, with counts when the console printed no summary. Console parsing stays for build errors, crashes, rejected options and projects without TRX. A pass needs a success summary from the console or the TRX plus a zero exit code.

## Consequences

Agents get a second file for detail. A count mismatch between TRX and console yields NoSummary. A project that needs the extension and lacks it gets a warning with the install command. Targets with several projects run each project separately so each writes its own report file. Classic VSTest stays out of scope.

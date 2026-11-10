---
title: Prefer TRX over console parsing
summary: Use TRX as the structured result source for counts and failures, with console parsing for the rest
status: proposed
---

# Prefer TRX over console parsing

## Context

Console parsing breaks on output changes and truncates detail. TRX is stable and carries full detail.

## Decision

When Microsoft.Testing.Extensions.TrxReport is present the tool writes a TRX beside the log with the same stem and reads counts and failures from it. Console parsing stays for build errors, crashes, the MTP summary check and projects without the extension. TRX alone never yields a pass.

## Consequences

Agents get a second file for detail. A count mismatch between TRX and console yields NoSummary. Classic VSTest stays out of scope.

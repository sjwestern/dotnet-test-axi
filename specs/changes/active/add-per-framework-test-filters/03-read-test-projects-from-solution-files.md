# 03 - Read test projects from solution files

Owns: src/DotnetTestAxi/Solutions/, tests/DotnetTestAxi.Tests/SolutionReaderTests.cs
Docs: none

## Goal

List the project files in a .slnx, .sln or .slnf so a solution target can be expanded.

## Context

Independent of the filter work. .slnx is XML (Project Path elements, possibly inside Folder elements); .sln has Project(...) lines; .slnf is JSON listing projects relative to a parent solution. Paths are relative to the solution file; return absolute paths. Skip non-project entries and missing files with a note rather than failing.

## Work

1. Add src/DotnetTestAxi/Solutions/SolutionReader.cs with Read(string solutionPath) returning IReadOnlyList<string>.
2. Handle slnx, sln and slnf.
3. Add tests/DotnetTestAxi.Tests/SolutionReaderTests.cs using temp files for each format, including nested folders and a missing project.

## Done when

SolutionReaderTests pass for all three formats, and reading DotnetTestAxi.slnx returns its five project files.

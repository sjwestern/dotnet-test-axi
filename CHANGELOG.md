# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [SemVer](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- README install and update steps.

### Changed

- Agent skill reports a missing tool instead of falling back to `dotnet test`.

## [0.1.0]

### Added

- Compact test summary for xUnit v3, MSTest, NUnit and TUnit on Microsoft Testing Platform.
- `--class`, `--method` and `--namespace` filters for all four frameworks.
- Solution support, including mixed-framework solutions.
- Agent skill.

[Unreleased]: https://github.com/sjwestern/dotnet-test-axi/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/sjwestern/dotnet-test-axi/commits/v0.1.0

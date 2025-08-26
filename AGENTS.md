# Repository Guidelines

## Project Structure & Modules
- `src/Serilog.Extensions.Formatting/`: Library source (multi-targets: net9.0, net8.0, net6.0, netstandard2.0).
- `test/Serilog.Extensions.Formatting.Test/`: xUnit tests (`*Tests.cs`).
- `test/Serilog.Extensions.Formatting.Benchmark/`: BenchmarkDotNet harness.
- `build/version.sh`: Syncs package.json version to `.csproj` via Changesets.
- `Serilog.Extensions.sln`, `Directory.*.props`: Solution and shared settings.

## Build, Test, Run
- Restore: `dotnet restore`
- Build: `dotnet build Serilog.Extensions.sln -c Release --no-restore`
- Test: `dotnet test -c Release --no-build`
  - With coverage (local): `dotnet test -c Release --collect:"XPlat Code Coverage"`
- Pack: `dotnet pack src/Serilog.Extensions.Formatting/Serilog.Extensions.Formatting.csproj -c Release -o ./artifacts`
- Benchmarks: `dotnet run -c Release --project test/Serilog.Extensions.Formatting.Benchmark`

## Coding Style & Naming
- C# 12; warnings-as-errors; implicit usings/nullable disabled (see `Directory.Build.props`).
- Indentation: 4 spaces for C#; 2 for JSON/YAML/XML (see `.editorconfig`).
- Fields: instance `_camelCase`; static `s_camelCase`.
- Members/types: PascalCase; locals/params: camelCase.
- Keep files formatted; run IDE format-on-save.

## Testing Guidelines
- Framework: xUnit. Prefer `[Fact]` for unit tests and `[Theory]` with data for variants.
- File naming: `SomethingTests.cs`; method names describe behavior, e.g., `FormatsJson_AsExpected`.
- Run tests across TFMs where relevant (project targets multiple frameworks).
- Validate JSON with helpers when possible rather than string-fragile assertions.

## Versioning & Releases
- Create a changeset: `yarn changeset` (workspace root).
- Bump version and propagate to `.csproj`: `yarn version` (requires `jq`).
- CI packs and, on release, publishes to NuGet (see `.github/workflows/release.yml`).

## Commits & Pull Requests
- Commits: concise, imperative subject; mention touched areas in backticks, e.g., `Utf8JsonFormatter`, `Directory.Packages.props`; reference issues (`#123`) when applicable.
- PRs: include a clear description, linked issues, test coverage for changes, and a changeset file for versioned packages.
- Note: CI test workflow ignores docs-only PRs (`**.md`).

## Security & Tooling
- NuGet uses `nuget.config`; keep API keys out of source. Never commit secrets.

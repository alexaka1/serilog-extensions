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
- Create a changeset: `bun changeset` (workspace root).
- Bump version and propagate to `.csproj`: `bun run version` (requires `jq`).
- CI packs and, on release, publishes to NuGet (see `.github/workflows/release.yml`).

## Commits & Pull Requests
- Commits: concise, imperative subject; mention touched areas in backticks, e.g., `Utf8JsonFormatter`, `Directory.Packages.props`; reference issues (`#123`) when applicable.
- PRs: include a clear description, linked issues, test coverage for changes, and a changeset file for versioned packages.
- Note: CI test workflow ignores docs-only PRs (`**.md`).

## Security & Tooling
- NuGet uses `nuget.config`; keep API keys out of source. Never commit secrets.

## Cursor Cloud specific instructions

This repository is a **.NET library** (NuGet package), not a long-running service. No servers, databases, or Docker containers need to be started for development or validation.

### Prerequisites (VM image / first-time setup)

- **.NET SDKs 9.0, 8.0, and 6.0** on `PATH` (CI and local dev target all three). If missing, install via [dotnet-install.sh](https://dot.net/v1/dotnet-install.sh) into `$HOME/.dotnet` and add to `PATH`.
- **`mono-complete`** (Linux) for `net472` / `net481` test TFMs.
- **`bun`** (version in `.bun-version`) and **`jq`** for Changesets release tooling only — not required for build/test.

### Standard validation loop

From the repo root (see **Build, Test, Run** above):

```bash
dotnet restore
dotnet build Serilog.Extensions.sln -c Release --no-restore
dotnet test -c Release --no-build
```

There is no separate linter step; `Directory.Build.props` treats warnings as errors during build.

### Runnable harnesses (not production apps)

| Project | Purpose | Command |
|---------|---------|---------|
| `test/Serilog.Extensions.Formatting.Test` | xUnit tests (primary validation) | `dotnet test -c Release --no-build` |
| `test/Serilog.Extensions.Formatting.AotTest` | Smoke test for `Utf8JsonFormatter` | `dotnet run -c Release --framework net9.0 --project test/Serilog.Extensions.Formatting.AotTest` |
| `test/Serilog.Extensions.Formatting.Benchmark` | BenchmarkDotNet (slow; interactive) | `dotnet run -c Release --project test/Serilog.Extensions.Formatting.Benchmark` |

The AotTest harness exercises core library behavior (camelCase JSON logging, complex objects, exceptions) and prints `SUCCESS: All AOT compatibility tests passed` on exit code 0.

### Gotchas

- Multi-target projects require `--framework` when using `dotnet run` (e.g. `--framework net9.0` for AotTest).
- `net6.0` builds may emit NuGet TFM support warnings from transitive `9.x` packages; these are expected and do not fail the build.
- AOT native publish (`clang`, `zlib1g-dev`) is only needed for the optional `aot-tests.yml` CI workflow, not for standard `dotnet test`.

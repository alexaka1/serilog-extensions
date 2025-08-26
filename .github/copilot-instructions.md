# Copilot Instructions for Serilog.Extensions

## Repository Overview

**Purpose**: Serilog extension library providing high-performance UTF-8 JSON formatting via `Utf8JsonFormatter` using `System.Text.Json.Utf8JsonWriter`.

**Type**: .NET library NuGet package  
**Size**: Small, focused codebase (~600 lines of source code)  
**Language**: C# 12  
**Frameworks**: Multi-target (net9.0, net8.0, net6.0, netstandard2.0)  
**Dependencies**: Serilog, System.Text.Json, PolySharp  
**Package Management**: NuGet with centralized packages + Yarn for changesets  
**Runtime**: Requires .NET 6+ and Node.js for full development workflow

## Build & Validation Commands

**ALWAYS run commands in this exact order for reliable builds:**

### 1. Prerequisites
- .NET 9 SDK (primary), 8, 6 for multi-targeting
- Node.js 22.x with Yarn 4.9.4 (for versioning)
- `jq` command (for version scripts)

### 2. Restore Dependencies (~2s)
```bash
dotnet restore
```

### 3. Build Solution (~15s)
```bash
dotnet build Serilog.Extensions.sln -c Release --no-restore
```
**Expected**: Success with 13-21 warnings about .NET 9 package compatibility with net6.0 target (these are expected and safe).

### 4. Run Tests (~2-4 minutes total)
```bash
dotnet test -c Release --no-build
```
**Expected**: Takes 30+ seconds per target framework (net9.0, net8.0, net6.0, net481) due to comprehensive thread safety tests that run thousands of log iterations across multiple threads. Do NOT interrupt - this is normal behavior.

**For faster feedback during development:**
```bash
dotnet test -c Release --no-build --filter "DisplayName~Utf8JsonFormatterTests"
```

### 5. Generate NuGet Package (~3s)
```bash
dotnet pack src/Serilog.Extensions.Formatting/Serilog.Extensions.Formatting.csproj -c Release -o ./artifacts
```
**Output**: Creates `.nupkg` and `.snupkg` files in `./artifacts/`



### Development Environment Setup

**Clean Build Sequence (full validation):**
```bash
rm -rf artifacts                                                          # Clean previous artifacts
dotnet clean                                                             # Clean build outputs
dotnet restore                                                           # Restore packages (~2s)
dotnet build -c Release --no-restore                                     # Build all projects (~3s)
dotnet pack src/Serilog.Extensions.Formatting/Serilog.Extensions.Formatting.csproj -c Release -o ./artifacts  # Package (~1s)
```

### 6. Code Coverage (local development)
```bash
dotnet test -c Release --collect:"XPlat Code Coverage"
```

### Core Structure
```
src/Serilog.Extensions.Formatting/          # Main library (521 lines)
├── Utf8JsonFormatter.cs                    # Primary formatter implementation
├── JsonLogPropertyNames.cs                 # Property name constants (44 lines)
├── DefaultNamingPolicy.cs                  # Naming policy utilities
└── Serilog.Extensions.Formatting.csproj    # Multi-target project file

test/Serilog.Extensions.Formatting.Test/    # xUnit tests
├── Utf8JsonFormatterTests.cs               # Unit tests for formatter
├── SerilogJsonFormatterTests.cs            # Comparison tests
├── HostTests.cs                             # Thread safety stress tests
├── Helpers.cs                               # JSON validation utilities
└── Some.cs                                  # Test data generation

test/Serilog.Extensions.Formatting.Benchmark/ # BenchmarkDotNet performance tests
├── JsonFormatterBenchmark.cs               # Performance comparisons
└── JsonFormatterEnrichBenchmark.cs         # Enriched logging benchmarks
```

### Configuration Files
- `Directory.Build.props`: Shared build properties (C# 12, warnings-as-errors, nullable disabled)
- `Directory.Packages.props`: Centralized package management
- `.editorconfig`: Code style (4 spaces C#, 2 spaces JSON/YAML, camelCase fields with `_`/`s_` prefixes)
- `nuget.config`: NuGet sources configuration
- `package.json`: Yarn workspace root with changesets scripts

### Key Architectural Elements
- **Thread Safety**: Uses `ThreadLocal<T>` for `Utf8JsonWriter`, `StringBuilder`, `StringWriter`
- **Multi-Target Support**: Conditional compilation with `#if FEATURE_*` for framework-specific functionality
- **High Performance**: UTF-8 optimized with span-based formatting where available
- **Extensibility**: Configurable naming policies, encoders, and formatting options

## CI/CD & Validation Pipeline

### GitHub Actions Workflows
1. **tests.yml**: Multi-OS testing (Ubuntu, Windows, macOS) with security hardening
2. **release.yml**: Automated releases with NuGet publishing and attestations
3. **coverage-report.yml**: Code coverage reporting
4. **codeql.yml**: Security scanning

### Pre-commit Validation
The CI runs these checks - replicate locally:
```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-restore --logger trx --collect:"XPlat Code Coverage"
```

### Version Management
Uses changesets for semantic versioning - agents only need to create changesets:
```bash
yarn changeset              # Create a changeset (major, minor, patch) for relevant changes
```
**Note**: Version updates and releases are handled automatically by CI. Agents should only create changesets when making changes that require version bumps.

## Common Issues & Troubleshooting

### Build Warnings
- **Package compatibility warnings for net6.0**: Expected and safe - newer packages used for dependencies
- **Missing mono on Linux**: Install with `sudo apt-get install mono-complete`
- **Missing mono on macOS**: Install with `brew install mono`

### Test Issues
- **Tests taking 3+ minutes**: Normal behavior - thread safety tests run intensive concurrency scenarios
- **Test output flooding console**: Tests generate thousands of JSON log entries for validation

### Development Environment
- **Multi-targeting**: Build may fail if older .NET versions missing - install .NET 6+ SDKs
- **Yarn not found**: Enable corepack with `corepack enable` after Node.js installation

## Dependencies & Packages

### Core Dependencies
- **Serilog**: Logging framework (version managed in Directory.Packages.props)
- **System.Text.Json**: High-performance JSON (conditionally referenced for netstandard2.0)
- **PolySharp**: C# language feature polyfills for older targets

### Development Dependencies
- **xUnit**: Testing framework with theory-based tests
- **BenchmarkDotNet**: Performance testing and comparisons
- **Microsoft.NET.Test.Sdk**: Test infrastructure
- **Moq**: Mocking framework for tests

### Package Management Pattern
Uses **Central Package Management** - all versions in `Directory.Packages.props`, projects reference by name only.

## Coding Standards

### Style Guidelines
- **C# 12** with warnings-as-errors enabled
- **Nullable reference types**: Disabled for library compatibility
- **Implicit usings**: Disabled for explicit clarity
- **Indentation**: 4 spaces for C#, 2 for JSON/YAML/XML
- **Naming**: PascalCase types/members, camelCase locals/params, `_camelCase` instance fields, `s_camelCase` static fields

### Testing Patterns
- **File naming**: `*Tests.cs` with descriptive method names like `FormatsJson_AsExpected`
- **Test types**: Use `[Fact]` for simple tests, `[Theory]` with `[MemberData]` for parameterized tests
- **JSON validation**: Use `Helpers.AssertValidJson()` instead of string assertions
- **Multi-target testing**: Tests run across all target frameworks

### Performance Considerations
- **Thread safety**: All formatters must be thread-safe via ThreadLocal pattern
- **Memory efficiency**: Use span-based APIs where available
- **UTF-8 optimization**: Leverage System.Text.Json's UTF-8 native performance

## Instructions for Agents

1. **Trust these instructions** - they are validated and comprehensive. Only search for additional information if something is unclear or appears incorrect.

2. **Always validate changes** by running the build sequence above before considering work complete.

3. **For new features**: Add corresponding tests in the test project and ensure they pass across all target frameworks.

4. **For breaking changes**: Require a major version changeset via `yarn changeset`.

5. **Code changes should maintain thread safety** - follow the ThreadLocal pattern used in Utf8JsonFormatter.

6. **When adding dependencies**: Update `Directory.Packages.props` not individual project files.
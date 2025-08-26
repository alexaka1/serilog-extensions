# Serilog Extensions Formatting Library

**ALWAYS reference these instructions first** and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- Install .NET 9.0 SDK: `wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh --channel 9.0`
- Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`
- Enable Corepack: `corepack enable`
- Install dependencies: `yarn install` - takes 6 seconds

### Bootstrap, Build, and Test Workflow
1. **Restore packages**: `dotnet restore` - takes 14 seconds on fresh repo
2. **Build solution**: `dotnet build Serilog.Extensions.sln -c Release --no-restore` - takes 13 seconds. NEVER CANCEL. Set timeout to 30+ minutes.
3. **Test (recommended)**: `dotnet test -c Release --no-build -f net9.0` for fast testing - takes 1-2 seconds for single test
4. **Test (all frameworks)**: `dotnet test -c Release --no-build` - takes 8+ minutes for all targets. NEVER CANCEL. Set timeout to 15+ minutes.
5. **Pack library**: `dotnet pack src/Serilog.Extensions.Formatting/Serilog.Extensions.Formatting.csproj -c Release -o ./artifacts` - takes 2 seconds

### Running Benchmarks
- **Benchmark help**: `dotnet run -c Release -f net9.0 --project test/Serilog.Extensions.Formatting.Benchmark -- --help`
- **Run specific benchmarks**: `dotnet run -c Release -f net9.0 --project test/Serilog.Extensions.Formatting.Benchmark -- -f '*JsonFormatterBenchmark*'`
- **NEVER CANCEL**: Benchmarks can take 30+ minutes. Set timeout to 60+ minutes.

## Validation and Testing

### Always Test Functionality After Changes
Create a simple test app to validate the Utf8JsonFormatter:

```bash
cd /tmp && dotnet new console -n FormatterTest
cd FormatterTest
dotnet add reference [repo-path]/src/Serilog.Extensions.Formatting/Serilog.Extensions.Formatting.csproj
dotnet add package Serilog
```

Replace Program.cs with:
```csharp
using System;
using System.IO;
using System.Text.Json;
using Serilog;
using Serilog.Extensions.Formatting;

var stringWriter = new StringWriter();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(new TestSink(new Utf8JsonFormatter(namingPolicy: JsonNamingPolicy.CamelCase), stringWriter))
    .CreateLogger();

logger.Information("Hello {Name}!", "World");
logger.Information("Complex object {@User}", new { FirstName = "John", LastName = "Doe", Age = 30 });

string output = stringWriter.ToString();
Console.WriteLine("=== JSON OUTPUT ===");
Console.WriteLine(output);

// Validate JSON output
string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
foreach (var line in lines)
{
    if (!string.IsNullOrWhiteSpace(line))
    {
        JsonDocument.Parse(line); // Throws if invalid JSON
    }
}
Console.WriteLine("✓ All validation passed!");

public class TestSink : Serilog.Core.ILogEventSink
{
    private readonly Serilog.Formatting.ITextFormatter _formatter;
    private readonly TextWriter _writer;
    public TestSink(Serilog.Formatting.ITextFormatter formatter, TextWriter writer)
    {
        _formatter = formatter; _writer = writer;
    }
    public void Emit(Serilog.Events.LogEvent logEvent) => _formatter.Format(logEvent, _writer);
}
```

Run: `dotnet run` - should output valid JSON with camelCase properties.

### Pre-commit Validation
- Always run single test: `dotnet test -c Release --no-build -f net9.0 --filter "DisplayName~ExpressionTemplate" --logger "console;verbosity=minimal"`
- Check build warnings are acceptable (some NETSDK1210 warnings about AOT are expected)

## Project Structure

### Key Directories
```
├── .github/workflows/     # CI/CD workflows (tests.yml, release.yml)
├── src/                   # Library source code
│   └── Serilog.Extensions.Formatting/  # Main library project
├── test/                  # Test projects
│   ├── Serilog.Extensions.Formatting.Test/      # xUnit tests
│   └── Serilog.Extensions.Formatting.Benchmark/ # BenchmarkDotNet tests
├── build/version.sh       # Version sync script for Changesets
├── Directory.Build.props  # Shared MSBuild properties
├── Directory.Packages.props # Central package management
└── Serilog.Extensions.sln # Solution file
```

### Target Frameworks
- Library: `net9.0`, `net8.0`, `net6.0`, `netstandard2.0`
- Tests: `net9.0`, `net8.0`, `net6.0`, `net481`, `net472`

## Core Library Features

### Utf8JsonFormatter
- Main class: `Serilog.Extensions.Formatting.Utf8JsonFormatter`
- Uses `System.Text.Json.Utf8JsonWriter` for performance
- Supports custom `JsonNamingPolicy` (CamelCase, SnakeCase, etc.)
- Thread-safe and optimized for UTF-8 output

### Key Options
- `namingPolicy`: Convert property names (e.g., `JsonNamingPolicy.CamelCase`)
- `closingDelimiter`: Line ending (default: `Environment.NewLine`)
- `renderMessage`: Render message template (default: `false`)
- `skipValidation`: Skip JSON validation (default: `true`)

## Common Issues and Solutions

### .NET Framework Test Failures
- Tests on `net481`/`net472` may fail in Linux environments (mono dependency issues)
- Use `dotnet test -f net9.0` to test only .NET Core targets
- CI handles full framework testing in appropriate environments

### Build Warnings
- NETSDK1210 warnings about AOT compatibility are expected for netstandard2.0
- Package version warnings for net6.0 targets are non-critical

### Missing .NET 9.0
- Project requires .NET 9.0 SDK for full build
- Install with provided script or use `dotnet-install.sh --channel 9.0`

## Versioning and Release

### Creating Changes
- Add changeset: `yarn changeset`
- Bump versions: `yarn version` (requires `jq` installed)
- CI automatically handles NuGet publishing on main branch

### Manual Release Testing
- Pack: `dotnet pack src/Serilog.Extensions.Formatting/Serilog.Extensions.Formatting.csproj -c Release -o ./artifacts`
- Artifacts appear in `./artifacts/` as `.nupkg` and `.snupkg` files

## Quick Reference Commands

### Most Frequently Used
```bash
# Full build and test cycle
export PATH="$HOME/.dotnet:$PATH"
dotnet restore                                                    # 14s
dotnet build Serilog.Extensions.sln -c Release --no-restore      # 13s
dotnet test -c Release --no-build -f net9.0                      # 1-2s

# Quick functionality test
dotnet test -c Release --no-build -f net9.0 --filter "DisplayName~ExpressionTemplate"

# Package the library
dotnet pack src/Serilog.Extensions.Formatting/Serilog.Extensions.Formatting.csproj -c Release -o ./artifacts
```

### Directory Listings
```
Root: AGENTS.md, Directory.*.props, README.md, Serilog.Extensions.sln, package.json
Src:  Serilog.Extensions.Formatting/
Test: Serilog.Extensions.Formatting.Test/, Serilog.Extensions.Formatting.Benchmark/
```

## Development Guidelines

### Code Style
- C# 12 with warnings as errors
- 4 spaces for C#, 2 spaces for JSON/YAML/XML
- Instance fields: `_camelCase`, static fields: `s_camelCase`
- PascalCase for members/types, camelCase for locals/params

### Testing
- xUnit framework with `[Fact]` and `[Theory]` attributes
- File naming: `SomethingTests.cs`
- Validate JSON output using helpers, not string assertions
- Test thread safety and performance scenarios

### CRITICAL TIMING NOTES
- **NEVER CANCEL** any build or test command
- Build: 13 seconds (set 30+ minute timeout)
- Full test suite: 8+ minutes (set 15+ minute timeout)
- Benchmarks: 30+ minutes (set 60+ minute timeout)
- Single test: 1-2 seconds (set 5+ minute timeout)

## Troubleshooting

### Common Problems
1. **Multi-framework targeting error**: Install .NET 9.0 SDK
2. **Slow tests**: Use `-f net9.0` to test single framework
3. **Mono failures**: Expected on Linux for .NET Framework targets
4. **Yarn version mismatch**: Run `corepack enable`

### Success Indicators
- ✓ JSON output validates with `JsonDocument.Parse()`
- ✓ CamelCase property names when using `JsonNamingPolicy.CamelCase`
- ✓ Thread-safe behavior in concurrent scenarios
- ✓ No memory leaks in long-running applications
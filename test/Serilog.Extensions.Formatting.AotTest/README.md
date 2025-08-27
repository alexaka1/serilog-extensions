# AOT Compatibility Tests

This project provides Ahead-of-Time (AOT) compilation compatibility tests for the Serilog.Extensions.Formatting library.

## Purpose

Tests that the `Utf8JsonFormatter` works correctly when the consuming application is compiled with .NET's AOT compilation, ensuring compatibility with trimmed and AOT-published applications.

## Test Coverage

The AOT test exercises the following scenarios:

1. **Basic Logging**: Simple string interpolation with parameters
2. **Complex Object Logging**: Structured logging with nested objects, arrays, and various data types
3. **Exception Logging**: Error logging with exception details

All scenarios validate that:

- JSON output is generated correctly
- Required properties (`Timestamp`, `Level`, `MessageTemplate`) are present
- JSON is well-formed and parseable

## Usage

### Run normally (pre-flight check)

```bash
dotnet run --project test/Serilog.Extensions.Formatting.AotTest/ -c Release -f net8.0
dotnet run --project test/Serilog.Extensions.Formatting.AotTest/ -c Release -f net9.0
```

### AOT Publish and Test

```bash
# Publish with AOT
dotnet publish test/Serilog.Extensions.Formatting.AotTest/ -c Release -f net8.0 -r linux-x64 --self-contained

# Run AOT-compiled executable
./test/Serilog.Extensions.Formatting.AotTest/bin/Release/net8.0/linux-x64/publish/Serilog.Extensions.Formatting.AotTest
```

## Exit Codes

- `0`: All tests passed successfully
- `1`: One or more tests failed

## CI Integration

The AOT tests are automatically run in CI via the `.github/workflows/aot-tests.yml` workflow on:

- .NET 8 and .NET 9 (mature AOT implementations)
- Ubuntu, Windows, and macOS platforms
- Pull requests and manual dispatch

The workflow:

1. Runs pre-flight tests normally
2. Publishes with AOT for the target platform
3. Executes the AOT-compiled binary
4. Uploads executables as artifacts for debugging
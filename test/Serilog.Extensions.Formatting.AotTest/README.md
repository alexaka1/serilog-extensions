# AOT Compatibility Test for Serilog.Extensions.Formatting

This project is a console application designed to test AOT (Ahead-of-Time) compilation compatibility with the `Serilog.Extensions.Formatting` library.

## Purpose

The AOT test verifies that the library can be successfully compiled using .NET's Native AOT feature and still produce the expected functionality at runtime. This is important for applications that require:

- Fast startup times
- Reduced memory footprint  
- Self-contained deployment without .NET runtime dependency
- Compatibility with environments where JIT compilation is not available

## What It Tests

The test application exercises key functionality of the library:

- **Basic logging**: Simple log messages
- **Structured logging**: Messages with properties
- **Data type serialization**: Numbers, dates, complex objects
- **Exception logging**: Error handling with stack traces
- **Format strings**: Custom formatting for properties
- **JSON output validation**: Ensures all output is valid JSON

## Running the Tests

### Manual Testing

1. **Build the regular version**:
   ```bash
   dotnet build
   dotnet run
   ```

2. **Build and run with AOT**:
   ```bash
   dotnet publish -c Release -r linux-x64
   ./bin/Release/net8.0/linux-x64/publish/Serilog.Extensions.Formatting.AotTest
   ```

### Integration Testing

The AOT compatibility is automatically tested as part of the main test suite via `AotCompatibilityTests.cs`, which:

1. Verifies the AOT test project exists and is configured correctly
2. Runs the AOT-compiled executable (if available)
3. Validates the output contains valid JSON with expected structure

## Configuration

The project is configured for AOT with:

- `<PublishAot>true</PublishAot>` - Enables AOT compilation
- `<InvariantGlobalization>true</InvariantGlobalization>` - Reduces size and improves AOT compatibility
- `<EnableAotAnalyzer>true</EnableAotAnalyzer>` - Provides AOT-specific warnings during build
- `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` - Helps identify trimming issues

## Known Limitations

- AOT compilation requires a target runtime identifier (e.g., `linux-x64`, `win-x64`)
- Some reflection-based features may require additional configuration
- Build times are longer compared to JIT compilation
- Published output size includes the full .NET runtime

## Expected Output

A successful run should output:
```
SUCCESS: AOT test passed - valid JSON output generated
Generated X log entries
Sample output: {"Timestamp":"...","Level":"Information",...}
```

Any errors indicate potential AOT compatibility issues that need to be addressed.
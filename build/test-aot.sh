#!/usr/bin/env bash

set -e

# Parse command line arguments
TFM="${1:-net8.0}"
RID="${2:-linux-x64}"

# Determine executable extension based on platform
EXE_EXT=""
if [[ "$RID" == win-* ]]; then
    EXE_EXT=".exe"
fi

echo "Building and testing AOT compatibility for Serilog.Extensions.Formatting"
echo "Target Framework: $TFM"
echo "Runtime Identifier: $RID"
echo "======================================================================"

# Change to repository root
cd "$(dirname "$0")/.."

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build the AOT test project (will also build dependencies)
echo "Building AOT test project..."
dotnet build test/Serilog.Extensions.Formatting.AotTest/Serilog.Extensions.Formatting.AotTest.csproj -c Release --no-restore -f $TFM

# Test regular run first
echo "Testing regular run..."
dotnet run --project test/Serilog.Extensions.Formatting.AotTest/Serilog.Extensions.Formatting.AotTest.csproj -c Release --no-build -f $TFM

# Publish with AOT
echo "Publishing with AOT (this may take a while)..."
dotnet publish test/Serilog.Extensions.Formatting.AotTest/Serilog.Extensions.Formatting.AotTest.csproj -c Release -r $RID --no-restore -f $TFM

# Test AOT-compiled executable
echo "Testing AOT-compiled executable..."
EXECUTABLE_PATH="./test/Serilog.Extensions.Formatting.AotTest/bin/Release/$TFM/$RID/publish/Serilog.Extensions.Formatting.AotTest$EXE_EXT"

if [[ "$RID" == win-* ]]; then
    # On Windows, we may need to handle path differently
    "$EXECUTABLE_PATH"
else
    "$EXECUTABLE_PATH"
fi

# Build test project for integration tests
echo "Building integration tests..."
dotnet build test/Serilog.Extensions.Formatting.Test/Serilog.Extensions.Formatting.Test.csproj -c Release -f $TFM --no-restore

# Run integration tests
echo "Running integration tests..."
dotnet test test/Serilog.Extensions.Formatting.Test/Serilog.Extensions.Formatting.Test.csproj -c Release --no-build --filter "AotCompatibilityTests" --logger "console;verbosity=normal" -f $TFM

echo "======================================================================"
echo "AOT compatibility testing completed successfully!"
echo ""
echo "Executable size:"
ls -lh "$EXECUTABLE_PATH"
echo ""
echo "Total published size:"
du -sh "./test/Serilog.Extensions.Formatting.AotTest/bin/Release/$TFM/$RID/publish/"
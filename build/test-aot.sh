#!/usr/bin/env bash

set -e

echo "Building and testing AOT compatibility for Serilog.Extensions.Formatting"
echo "======================================================================"

# Change to repository root
cd "$(dirname "$0")/.."

# Restore with specific target framework to avoid .NET 9 issues
echo "Restoring packages..."
dotnet restore -p:TargetFrameworks=net8.0

# Build the AOT test project (will also build dependencies)
echo "Building AOT test project..."
dotnet build test/Serilog.Extensions.Formatting.AotTest/Serilog.Extensions.Formatting.AotTest.csproj -c Release --no-restore

# Test regular run first
echo "Testing regular run..."
dotnet run --project test/Serilog.Extensions.Formatting.AotTest/Serilog.Extensions.Formatting.AotTest.csproj -c Release --no-build

# Publish with AOT
echo "Publishing with AOT (this may take a while)..."
dotnet publish test/Serilog.Extensions.Formatting.AotTest/Serilog.Extensions.Formatting.AotTest.csproj -c Release -r linux-x64 --no-restore

# Test AOT-compiled executable
echo "Testing AOT-compiled executable..."
./test/Serilog.Extensions.Formatting.AotTest/bin/Release/net8.0/linux-x64/publish/Serilog.Extensions.Formatting.AotTest

# Build test project for integration tests
echo "Building integration tests..."
dotnet build test/Serilog.Extensions.Formatting.Test/Serilog.Extensions.Formatting.Test.csproj -c Release -f net8.0 --no-restore

# Run integration tests
echo "Running integration tests..."
dotnet test test/Serilog.Extensions.Formatting.Test/Serilog.Extensions.Formatting.Test.csproj -c Release --no-build --filter "AotCompatibilityTests" --logger "console;verbosity=normal" -f net8.0

echo "======================================================================"
echo "AOT compatibility testing completed successfully!"
echo ""
echo "Executable size:"
ls -lh ./test/Serilog.Extensions.Formatting.AotTest/bin/Release/net8.0/linux-x64/publish/Serilog.Extensions.Formatting.AotTest
echo ""
echo "Total published size:"
du -sh ./test/Serilog.Extensions.Formatting.AotTest/bin/Release/net8.0/linux-x64/publish/
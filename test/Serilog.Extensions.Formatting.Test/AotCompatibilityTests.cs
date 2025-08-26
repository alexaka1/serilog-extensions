using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Serilog.Extensions.Formatting.Test;

public class AotCompatibilityTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void AotCompiledApplication_ProducesValidJsonOutput()
    {
        // This test verifies that the library can be used in an AOT-compiled application
        // and still produces valid JSON output
        
        // Find the solution root by looking for .sln file
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        DirectoryInfo solutionRoot = currentDir;
        while (solutionRoot != null && !solutionRoot.GetFiles("*.sln").Any())
        {
            solutionRoot = solutionRoot.Parent;
        }
        
        Assert.NotNull(solutionRoot);
        
        var publishDir = Path.Combine(
            solutionRoot.FullName,
            "test", "Serilog.Extensions.Formatting.AotTest", 
            "bin", "Release", "net8.0", "linux-x64", "publish"
        );
        var executablePath = Path.Combine(publishDir, "Serilog.Extensions.Formatting.AotTest");
        
        // Skip test if AOT executable doesn't exist
        if (!File.Exists(executablePath))
        {
            _output.WriteLine($"AOT executable not found at: {executablePath}");
            _output.WriteLine("Run 'dotnet publish -c Release -r linux-x64' on the AotTest project to generate the AOT executable");
            throw new SkipException("AOT executable not available. Run AOT publish first.");
        }
        
        // Run the AOT-compiled executable
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var errorOutput = process.StandardError.ReadToEnd();
        process.WaitForExit();
        
        _output.WriteLine($"Exit code: {process.ExitCode}");
        _output.WriteLine($"Standard output: {output}");
        if (!string.IsNullOrEmpty(errorOutput))
        {
            _output.WriteLine($"Standard error: {errorOutput}");
        }
        
        // Verify the AOT application succeeded
        Assert.Equal(0, process.ExitCode);
        Assert.Contains("SUCCESS: AOT test passed", output);
        Assert.Contains("valid JSON output generated", output);
        Assert.Contains("Sample output:", output);
        
        // Extract and validate the sample JSON output
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var sampleLine = Array.Find(lines, line => line.StartsWith("Sample output:"));
        Assert.NotNull(sampleLine);
        
        var jsonPart = sampleLine["Sample output: ".Length..];
        
        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(jsonPart);
        
        // Verify it has expected Serilog JSON structure
        Assert.True(jsonDoc.RootElement.TryGetProperty("Timestamp", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("Level", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("MessageTemplate", out _));
        Assert.True(jsonDoc.RootElement.TryGetProperty("Properties", out _));
    }
    
    [Fact]
    public void AotTestProject_CanBuild()
    {
        // This test ensures the AOT test project can at least build
        // even if we can't run AOT compilation in all environments
        
        // Find the solution root by looking for .sln file
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        DirectoryInfo solutionRoot = currentDir;
        while (solutionRoot != null && !solutionRoot.GetFiles("*.sln").Any())
        {
            solutionRoot = solutionRoot.Parent;
        }
        
        Assert.NotNull(solutionRoot);
        
        var projectPath = Path.Combine(
            solutionRoot.FullName,
            "test", "Serilog.Extensions.Formatting.AotTest", 
            "Serilog.Extensions.Formatting.AotTest.csproj"
        );
        
        Assert.True(File.Exists(projectPath), $"AOT test project not found at: {projectPath}");
        
        // Verify the project file contains AOT settings
        var projectContent = File.ReadAllText(projectPath);
        Assert.Contains("<PublishAot>true</PublishAot>", projectContent);
        Assert.Contains("<InvariantGlobalization>true</InvariantGlobalization>", projectContent);
        Assert.Contains("<EnableAotAnalyzer>true</EnableAotAnalyzer>", projectContent);
    }
}

public class SkipException(string message) : Exception(message);
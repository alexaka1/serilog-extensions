using System;
using System.IO;
using System.Text.Json;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Formatting;

namespace Serilog.Extensions.Formatting.AotTest;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            // Create a temporary file for output
            var tempFile = Path.GetTempFileName();
            
            // Test Utf8JsonFormatter
            TestUtf8JsonFormatter(tempFile);
            
            // Read and validate the output
            var output = File.ReadAllText(tempFile);
            File.Delete(tempFile);
            
            // Basic validation that JSON was written
            if (string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine("ERROR: No output generated");
                return 1;
            }
            
            // Try to parse each line as JSON to verify it's valid
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                try
                {
                    JsonDocument.Parse(line.Trim());
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"ERROR: Invalid JSON output: {ex.Message}");
                    Console.WriteLine($"Line: {line}");
                    return 1;
                }
            }
            
            Console.WriteLine("SUCCESS: AOT test passed - valid JSON output generated");
            Console.WriteLine($"Generated {lines.Length} log entries");
            
            // Output first line for verification
            if (lines.Length > 0)
            {
                Console.WriteLine($"Sample output: {lines[0]}");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: AOT test failed with exception: {ex}");
            return 1;
        }
    }
    
    private static void TestUtf8JsonFormatter(string outputFile)
    {
        var formatter = new Utf8JsonFormatter();
        
        var logger = new LoggerConfiguration()
            .WriteTo.File(formatter, outputFile)
            .Enrich.WithProperty("Source", "AotTest")
            .CreateLogger();
            
        // Test various log levels and data types
        logger.Information("Simple message");
        logger.Information("Message with {Property}", "value");
        logger.Information("Message with {Number}", 42);
        logger.Information("Message with {Date}", DateTime.UtcNow);
        logger.Warning("Warning with {ComplexObject}", new { Name = "Test", Value = 123 });
        
        // Test with exception
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error occurred in {Method}", nameof(TestUtf8JsonFormatter));
        }
        
        // Test with format strings
        logger.Information("Formatted number: {Value:N2}", 123.456);
        logger.Information("Formatted date: {Date:yyyy-MM-dd}", DateTime.UtcNow);
        
        logger.Dispose();
    }
}
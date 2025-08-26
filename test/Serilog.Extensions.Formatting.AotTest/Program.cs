using System;
using System.IO;
using System.Text.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Formatting;
using Serilog.Formatting;

namespace Serilog.Extensions.Formatting.AotTest;

/// <summary>
/// AOT compatibility test program that exercises the Utf8JsonFormatter.
/// This program is designed to be published with AOT and executed to verify
/// that the library works correctly in AOT scenarios.
/// </summary>
public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            // Test basic logging with Utf8JsonFormatter
            TestBasicLogging();
            
            // Test complex object logging
            TestComplexObjectLogging();
            
            // Test exception logging
            TestExceptionLogging();
            
            Console.WriteLine("SUCCESS: All AOT compatibility tests passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILURE: AOT compatibility test failed: {ex}");
            return 1;
        }
    }
    
    private static void TestBasicLogging()
    {
        using var stringWriter = new StringWriter();
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(new TestSink(new Utf8JsonFormatter(), stringWriter))
            .CreateLogger();
            
        logger.Information("Hello, {Name}!", "World");
        logger.Debug("Debug message with number: {Number}", 42);
        logger.Warning("Warning message");
        
        var output = stringWriter.ToString();
        ValidateJsonOutput(output, "basic logging");
    }
    
    private static void TestComplexObjectLogging()
    {
        using var stringWriter = new StringWriter();
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(new TestSink(new Utf8JsonFormatter(), stringWriter))
            .CreateLogger();
            
        var complexObject = new
        {
            Name = "Test Object",
            Value = 123.45,
            Timestamp = DateTimeOffset.Now,
            Tags = new[] { "tag1", "tag2" },
            Nested = new { InnerValue = "nested" }
        };
        
        logger.Information("Complex object: {@Object}", complexObject);
        
        var output = stringWriter.ToString();
        ValidateJsonOutput(output, "complex object logging");
    }
    
    private static void TestExceptionLogging()
    {
        using var stringWriter = new StringWriter();
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(new TestSink(new Utf8JsonFormatter(), stringWriter))
            .CreateLogger();
            
        try
        {
            throw new InvalidOperationException("Test exception for AOT");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred with parameter: {Parameter}", "test-param");
        }
        
        var output = stringWriter.ToString();
        ValidateJsonOutput(output, "exception logging");
    }
    
    private static void ValidateJsonOutput(string output, string testName)
    {
        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException($"No output generated for {testName}");
            
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            try
            {
                // Validate that each line is valid JSON
                using var doc = JsonDocument.Parse(line);
                
                // Ensure basic structure exists
                if (!doc.RootElement.TryGetProperty("Timestamp", out _))
                    throw new InvalidOperationException($"Missing Timestamp property in {testName}");
                    
                if (!doc.RootElement.TryGetProperty("Level", out _))
                    throw new InvalidOperationException($"Missing Level property in {testName}");
                    
                if (!doc.RootElement.TryGetProperty("MessageTemplate", out _))
                    throw new InvalidOperationException($"Missing MessageTemplate property in {testName}");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON in {testName}: {ex.Message}\nJSON: {line}");
            }
        }
        
        Console.WriteLine($"PASS: {testName} - Generated {lines.Length} valid JSON log entries");
    }
}

/// <summary>
/// Simple test sink that writes formatted output to a TextWriter
/// </summary>
public class TestSink : ILogEventSink
{
    private readonly ITextFormatter _formatter;
    private readonly TextWriter _writer;
    
    public TestSink(ITextFormatter formatter, TextWriter writer)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }
    
    public void Emit(LogEvent logEvent)
    {
        _formatter.Format(logEvent, _writer);
    }
}
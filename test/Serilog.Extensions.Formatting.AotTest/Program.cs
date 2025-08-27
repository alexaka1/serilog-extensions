using System;
using System.IO;
using System.Text.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Extensions.Formatting.AotTest;

/// <summary>
///     Enumeration of test cases for AOT validation
/// </summary>
public enum AotTestCase
{
    BasicLogging,
    ComplexObjectLogging,
    ExceptionLogging,
}

/// <summary>
///     AOT compatibility test program that exercises the Utf8JsonFormatter.
///     This program is designed to be published with AOT and executed to verify
///     that the library works correctly in AOT scenarios.
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
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new TestSink(new Utf8JsonFormatter(namingPolicy: JsonNamingPolicy.CamelCase), stringWriter))
            .CreateLogger();

        logger.Information("Hello, {Name}!", "World");
        logger.Debug("Debug message with number: {Number}", 42);
        logger.Warning("Warning message");

        string output = stringWriter.ToString();
        ValidateJsonOutput(output, AotTestCase.BasicLogging);
    }

    private static void TestComplexObjectLogging()
    {
        using var stringWriter = new StringWriter();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new TestSink(new Utf8JsonFormatter(namingPolicy: JsonNamingPolicy.CamelCase), stringWriter))
            .CreateLogger();

        var complexObject = new
        {
            Name = "Test Object",
            Value = 123.45,
            Timestamp = DateTimeOffset.Now,
            Tags = new[] { "tag1", "tag2" },
            Nested = new { InnerValue = "nested" },
        };

        logger.Information("Complex object: {@Object}", complexObject);

        string output = stringWriter.ToString();
        ValidateJsonOutput(output, AotTestCase.ComplexObjectLogging);
    }

    private static void TestExceptionLogging()
    {
        using var stringWriter = new StringWriter();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new TestSink(new Utf8JsonFormatter(namingPolicy: JsonNamingPolicy.CamelCase), stringWriter))
            .CreateLogger();

        try
        {
            throw new InvalidOperationException("Test exception for AOT");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred with parameter: {Parameter}", "test-param");
        }

        string output = stringWriter.ToString();
        ValidateJsonOutput(output, AotTestCase.ExceptionLogging);
    }

    private static void ValidateJsonOutput(string output, AotTestCase testCase)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException($"No output generated for {testCase}");
        }

        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                // Validate that each line is valid JSON
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                // Ensure basic structure exists with camelCase property names
                if (!root.TryGetProperty("timestamp", out var timestamp))
                {
                    throw new InvalidOperationException(
                        $"Missing timestamp property (should be camelCase) in {testCase}");
                }

                if (!root.TryGetProperty("level", out var level))
                {
                    throw new InvalidOperationException($"Missing level property (should be camelCase) in {testCase}");
                }

                if (!root.TryGetProperty("messageTemplate", out var messageTemplate))
                {
                    throw new InvalidOperationException(
                        $"Missing messageTemplate property (should be camelCase) in {testCase}");
                }

                // Validate property values are correctly rendered
                string timestampStr = timestamp.GetString();
                if (string.IsNullOrEmpty(timestampStr) || !DateTimeOffset.TryParse(timestampStr, out _))
                {
                    throw new InvalidOperationException($"Invalid timestamp format in {testCase}: {timestampStr}");
                }

                string levelStr = level.GetString();
                if (string.IsNullOrEmpty(levelStr))
                {
                    throw new InvalidOperationException($"Empty level property in {testCase}");
                }

                string messageTemplateStr = messageTemplate.GetString();
                if (string.IsNullOrEmpty(messageTemplateStr))
                {
                    throw new InvalidOperationException($"Empty messageTemplate property in {testCase}");
                }

                // Check if rendered message exists and contains expected content
                if (root.TryGetProperty("renderedMessage", out var renderedMessage))
                {
                    string renderedStr = renderedMessage.GetString();
                    if (testCase == AotTestCase.BasicLogging && renderedStr.Contains("Hello, {Name}!"))
                    {
                        throw new InvalidOperationException(
                            $"Message template not properly rendered in {testCase}: {renderedStr}");
                    }
                }

                // For complex object tests, check if properties exist and are properly rendered
                if (testCase == AotTestCase.ComplexObjectLogging &&
                    root.TryGetProperty("properties", out var properties))
                {
                    if (properties.TryGetProperty("Object", out var objectProp))
                    {
                        // Verify nested object properties are camelCase
                        if (objectProp.TryGetProperty("name", out var name) && name.GetString() != "Test Object")
                        {
                            throw new InvalidOperationException(
                                $"Object.name property not properly rendered in {testCase}");
                        }

                        if (objectProp.TryGetProperty("value", out var value) &&
                            Math.Abs(value.GetDouble() - 123.45) > 0.01)
                        {
                            throw new InvalidOperationException(
                                $"Object.value property not properly rendered in {testCase}");
                        }

                        if (objectProp.TryGetProperty("tags", out var tags) && tags.GetArrayLength() != 2)
                        {
                            throw new InvalidOperationException(
                                $"Object.tags array not properly rendered in {testCase}");
                        }

                        if (objectProp.TryGetProperty("nested", out var nested) &&
                            nested.TryGetProperty("innerValue", out var innerValue) &&
                            innerValue.GetString() != "nested")
                        {
                            throw new InvalidOperationException(
                                $"Object.nested.innerValue not properly rendered in {testCase}");
                        }
                    }
                }

                // For exception tests, check if exception property exists
                if (testCase == AotTestCase.ExceptionLogging && !root.TryGetProperty("exception", out _))
                {
                    throw new InvalidOperationException($"Missing exception property in {testCase}");
                }

                // Verify that PascalCase properties don't exist (should be camelCase)
                if (root.TryGetProperty("Timestamp", out _) || root.TryGetProperty("Level", out _) ||
                    root.TryGetProperty("MessageTemplate", out _))
                {
                    throw new InvalidOperationException(
                        $"Found PascalCase properties instead of camelCase in {testCase}");
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON in {testCase}: {ex.Message}\nJSON: {line}");
            }
        }

        Console.WriteLine(
            $"PASS: {testCase} - Generated {lines.Length} valid JSON log entries with correct camelCase formatting");
    }
}

/// <summary>
///     Simple test sink that writes formatted output to a TextWriter
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

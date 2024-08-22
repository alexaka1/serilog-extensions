using System;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Serilog.Extensions.Formatting.Test
{
    public static class Helpers
    {
        public static void AssertValidJson(string actual, ITestOutputHelper output = null)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(actual).AsSpan());
            bool valid = false;
            try
            {
                valid = JsonDocument.TryParseValue(ref reader, out _);
            }
            finally
            {
                if (!valid)
                {
                    output?.WriteLine(actual);
                }
            }

            Assert.True(valid);
        }
    }
}

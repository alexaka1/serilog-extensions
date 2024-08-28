using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Debugging;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Xunit;
using Xunit.Abstractions;

namespace Serilog.Extensions.Formatting.Test
{
    public sealed class HostTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public HostTests(ITestOutputHelper output)
        {
            _output = output;
            SelfLog.Enable(_output.WriteLine);
        }

        [Theory]
        [MemberData(nameof(MemberData))]
        public async Task HostedServiceCanWriteOnManyThreads(HostParams data)
        {
            var now = DateTimeOffset.Parse("2023-01-01T12:34:56.7891111+01:00");
            data.Services.GetRequiredService<ILogger<HostTests>>()
                .LogInformation("Hello World, {CurrentTime:hh:mm:ss t z}", now);
            var service = data.Services;
            // string expected = ;

            var startSignal = new ManualResetEvent(false);

            var tasks = new Task[data.Threads];
            for (int i = 0; i < data.Threads; i++)
            {
                int taskIndex = i;
                tasks[taskIndex] = Task.Run(() =>
                {
                    // Wait until the signal is given to start
                    startSignal.WaitOne();
                    var logger = service.GetRequiredService<ILogger<HostTests>>();

                    for (int j = 0; j < data.Iterations; j++)
                    {
                        logger.LogInformation("Hello World, {CurrentTime:hh:mm:ss t z}", now);
                    }

                    // results[taskIndex] = writer.ToString();
                });
            }

            // Start all tasks at once
            startSignal.Set();

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
            Assert.True(true);
            // await Task.Delay(TimeSpan.FromSeconds(5));
            Log.CloseAndFlush();
            await data.Services.DisposeAsync();
            using (var fileStream = new FileStream(data.FilePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096,
                       FileOptions.SequentialScan))
            using (var stringReader = new StreamReader(fileStream))
            {
                // read line by line
                string actual;
                int i = 0;
                while ((actual = await stringReader.ReadLineAsync()) != null)
                {
                    i++;
                    if (string.IsNullOrWhiteSpace(actual))
                    {
                        _output.WriteLine("Empty line at {0}", i);
                        continue;
                    }

                    Helpers.AssertValidJson(actual, _output);
                    // Assert.Equal(expected, actual);
                }

                Assert.Equal(data.Iterations * data.Threads + 1, i);
            }

            File.Delete(data.FilePath);
        }

        public static TheoryData<HostParams> MemberData()
        {
            int[] threads = { 1, 10, 100 /*, 500*/ };
            int[] iterations = { 1, 100, 1000 /*, 10000*/ };
            ITextFormatter[] formatter = { new Utf8JsonFormatter("\n", true), new JsonFormatter("\n", true) };
            var data = new List<HostParams>();
            foreach (int thread in threads)
            {
                foreach (int iteration in iterations)
                {
                    foreach (var textFormatter in formatter)
                    {
                        var services = new ServiceCollection();
                        string fileName = Path.GetTempFileName();
                        services.AddLogging().AddSerilog(configuration =>
                        {
                            configuration.MinimumLevel.Verbose()
                                .WriteTo.Console(new Utf8JsonFormatter("\n", true))
                                .WriteTo.Async(a => a.Console(new Utf8JsonFormatter("\n", true)))
                                .Enrich.WithProperty("Hello",
                                    new
                                    {
                                        now = DateTimeOffset.UtcNow, Exception = new InvalidOperationException(),
                                        Url = new Uri("https://github.com/alexaka1/serilog-extensions"),
                                    })
                                // don't drop logs
                                .WriteTo.Async(a => a.File(textFormatter, fileName, buffered: false),
                                    blockWhenFull: true)
                                ;
                        });
                        data.Add(new HostParams(services.BuildServiceProvider(), iteration, thread, fileName,
                            textFormatter));
                    }
                }
            }

            return new TheoryData<HostParams>(data);
        }

        [Serializable]
        public class HostParams
        {
            [NonSerialized]
            private readonly ITextFormatter _textFormatter;

            [NonSerialized]
            public readonly ServiceProvider Services;

            public int Threads { get; set; }

            public int Iterations { get; set; }

            public string FilePath { get; set; }
            public string Name => _textFormatter.GetType().Name;

            public HostParams(ServiceProvider services, int iterations, int threads, string filePath,
                ITextFormatter textFormatter)
            {
                _textFormatter = textFormatter;
                Services = services;
                Iterations = iterations;
                Threads = threads;
                FilePath = filePath;
            }
        }

        public void Dispose()
        {
            SelfLog.Disable();
        }
    }
}

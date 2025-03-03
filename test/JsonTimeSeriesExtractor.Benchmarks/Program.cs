using JsonTimeSeriesExtractor.Benchmarks;

BenchmarkDotNet.Running.BenchmarkSwitcher
    .FromTypes([typeof(TimeSeriesExtractorBenchmarks)])
    .RunAllJoined(args: args);

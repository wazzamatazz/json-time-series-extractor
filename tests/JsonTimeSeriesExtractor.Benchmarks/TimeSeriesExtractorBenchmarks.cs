using System.Text.Json;

using BenchmarkDotNet.Attributes;

using Jaahas.Json;

namespace JsonTimeSeriesExtractor.Benchmarks;

//[ShortRunJob]
[MemoryDiagnoser]
public class TimeSeriesExtractorBenchmarks {

    private readonly TimeSeriesExtractorOptions _options;

    private readonly JsonElement _json;


    public TimeSeriesExtractorBenchmarks() {
        var deviceSample = new {
            Data = new {
                Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                SignalStrength = -75,
                DataFormat = 5,
                Temperature = 19.3,
                Humidity = 37.905,
                Pressure = 1013.35,
                Acceleration = new {
                    X = -0.872,
                    Y = 0.512,
                    Z = -0.04
                },
                BatteryVoltage = 3.085,
                TxPower = 4,
                MovementCounter = 5,
                MeasurementSequence = 34425,
                MacAddress = "AB:CD:EF:01:23:45",
                Metadata = new {
                    Labels = new[] { "Label1", "Label2" },
                    ModelName = "Model1",
                    Location = "Location1",
                    Manufacturer = new {
                        Name = "Manufacturer1",
                        Address = "Address1"
                    }
                }
            }
        };

        _json = JsonSerializer.SerializeToElement(deviceSample);

        _options = new TimeSeriesExtractorOptions {
            Recursive = true,
            TimestampProperty = "/Data/Timestamp",
        };
    }
    
    
    [Benchmark]
    public void ExtractTimeSeries() {
        foreach (var sample in TimeSeriesExtractor.GetSamples(_json, _options)) {
            // No-op
        }
    }
    
}

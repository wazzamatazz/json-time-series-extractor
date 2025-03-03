using System.Text.Json;

using BenchmarkDotNet.Attributes;

using Jaahas.Json;

namespace JsonTimeSeriesExtractor.Benchmarks;

//[ShortRunJob]
[MemoryDiagnoser]
public class TimeSeriesExtractorBenchmarks {

    private readonly TimeSeriesExtractorOptions _objectPayloadJsonOptions;

    private readonly JsonElement _objectPayloadJson;
    
    private readonly TimeSeriesExtractorOptions _arrayPayloadJsonOptions;

    private readonly JsonElement _arrayPayloadJson;


    public TimeSeriesExtractorBenchmarks() {
        var objectPayload = new {
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

        _objectPayloadJson = JsonSerializer.SerializeToElement(objectPayload);

        _objectPayloadJsonOptions = new TimeSeriesExtractorOptions {
            Recursive = true,
            TimestampProperty = "/Data/Timestamp",
        };

        var arrayPayload = new {
            Data = new [] {
                new {
                    Source = "Instrument-1",
                    Timestamp = DateTimeOffset.Parse("2024-04-13T10:01:47Z"),
                    Value = 1019D
                },
                new {
                    Source = "Instrument-2",
                    Timestamp = DateTimeOffset.Parse("2024-04-13T09:59:51Z"),
                    Value = 23.7
                },
                new {
                    Source = "Instrument-2",
                    Timestamp = DateTimeOffset.Parse("2024-04-13T10:00:32Z"),
                    Value = 23.6
                }
            }
        };
        
        _arrayPayloadJson = JsonSerializer.SerializeToElement(arrayPayload);
        
        _arrayPayloadJsonOptions = new TimeSeriesExtractorOptions {
            Recursive = true,
            AllowNestedTimestamps = true,
            TimestampProperty = "/Timestamp",
            CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                PointersToInclude = ["/Data/+/Value"],
                AllowWildcardExpressions = true
            }),
            Template = "{Source}"
        };
    }
    
    
    [Benchmark]
    public void ExtractTimeSeriesFromComplexObject() {
        foreach (var sample in TimeSeriesExtractor.GetSamples(_objectPayloadJson, _objectPayloadJsonOptions)) {
            // No-op
        }
    }
    
    
    [Benchmark]
    public void ExtractTimeSeriesFromArray() {
        foreach (var sample in TimeSeriesExtractor.GetSamples(_arrayPayloadJson, _arrayPayloadJsonOptions)) {
            // No-op
        }
    }
    
}

# About

Jaahas.Json.TimeSeriesExtractor is a library for extracting time series data from JSON documents using `System.Text.Json`. Its primary use case is separating structured JSON payloads from IoT and IIoT devices into separate time series for storage in a database or other time series data store.

More documentation is available on [GitHub](https://github.com/wazzamatazz/json-time-series-extractor).


# How to Use

Use the static `TimeSeriesExtractor.GetSamples` method to extract values from a JSON string or a `JsonElement` and return them as a collection of `TimeSeriesSample` objects: 

```csharp
const string json = @"{ ""timestamp"": ""2021-05-30T09:47:38Z"", ""temperature"": 24.7, ""pressure"": 1021.3, ""humidity"": 33.76 }";
var samples = TimeSeriesExtractor.GetSamples(json);
```

You can customise the extraction behaviour by providing a `TimeSeriesExtractorOptions` object when calling `TimeSeriesExtractor.GetSamples`:

```json
// data.json
{
  "data": [
    {
      "src": "Instrument-1",
      "ts": "2024-04-13T10:01:47Z",
      "val": 1019
    },
    {
      "src": "Instrument-2",
      "ts": "2024-04-13T09:59:51Z",
      "val": 23.7
    },
    {
      "src": "Instrument-2",
      "ts": "2024-04-13T10:00:32Z",
      "val": 23.6
    }
  ]
}
```

```csharp
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.Converters.Add(new JsonStringEnumConverter());

JsonElement json;
using (var stream = new FileStream("data.json", FileMode.Open)) {
    json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, jsonOptions);
}

var options = new TimeSeriesExtractorOptions() {
    // We want to extract samples from nested objects.
    Recursive = true,
    // The "data" property in our JSON document contains an array of samples, each with its own 
    // timestamp.
    AllowNestedTimestamps = true,
    // The timestamp is located at the "ts" property on each sample object.
    TimestampProperty = "/ts",
    // Each object in the samples array contains a "val" property with the sample value. We'll use
    // an MQTT-style match expression to only extract values from the "val" property on each 
    // object in the samples array.
    CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
        PointersToInclude = ["/data/+/val"],
        AllowWildcardExpressions = true
    }),
    // Each object in the samples array contains a "src" property with the name of the instrument
    // for the sample. We'll use this as the key when emitting a sample instead of inferring a key.
    Template = "{src}"
};

foreach (var sample in TimeSeriesExtractor.GetSamples(json, options)) {
    Console.WriteLine(JsonSerializer.Serialize(sample, jsonOptions));
}

// Output:
// {"key":"Instrument-1","timestamp":"2024-04-13T10:01:47Z","value":1019,"timestampSource":"Document"}
// {"key":"Instrument-2","timestamp":"2024-04-13T09:59:51Z","value":23.7,"timestampSource":"Document"}
// {"key":"Instrument-2","timestamp":"2024-04-13T10:00:32Z","value":23.6,"timestampSource":"Document"}
```

Please refer to the [documentation](https://github.com/wazzamatazz/json-time-series-extractor) for more information.

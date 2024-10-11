# Jaahas.Json.TimeSeriesExtractor

A C# library for extracting time series data from JSON using `System.Text.Json`. This can be useful when e.g. JSON data is received from an IoT device and the data must be separated into individual series for storage in a database.


# Getting Started

Add a NuGet package reference to [Jaahas.Json.TimeSeriesExtractor](https://www.nuget.org/packages/Jaahas.Json.TimeSeriesExtractor/) to your project.


# Usage

The [TimeSeriesExtractor](./src/JsonTimeSeriesExtractor/TimeSeriesExtractor.cs) class is the entry point for the library.

Call `TimeSeriesExtractor.GetSamples` to extract values from a JSON string or a `JsonElement`: 

```csharp
const string json = @"{ ""timestamp"": ""2021-05-30T09:47:38Z"", ""temperature"": 24.7, ""pressure"": 1021.3, ""humidity"": 33.76 }";

// GetSamples uses lazy evaluation; use the ToArray extension from 
// System.Linq to force eager evaluation.
var samples = TimeSeriesExtractor.GetSamples(json).ToArray();
```

The JSON document must represent an object or an array of objects. If the document is an array of objects, each object in the array is processed as if it were a separate JSON document. You can customise the extraction behaviour by passing a [TimeSeriesExtractorOptions](./src/JsonTimeSeriesExtractor/TimeSeriesExtractorOptions.cs) object when calling the method.


## Data Samples

Properties on the JSON objects are converted to instances of [TimeSeriesSample](./src/JsonTimeSeriesExtractor/TimeSeriesSample.cs). Although the type of the `Value` property on `TimeSeriesSample` is `object`, in practical terms the value will either be `null`, or one of the following types:

- `double`
- `string`
- `bool`


## Selecting the Timestamp

The `TimestampProperty` on `TimeSeriesExtractorOptions` defines the JSON Pointer path that is used to retrieve the timestamp for the samples extracted from the JSON object. By default, this property is set to `/time`, but can overridden if required:

```csharp
new TimeSeriesExtractorOptions() {
  TimestampProperty = "/metadata/utcSampleTime"
}
```

If the `TimestampProperty` is `null`, or does not exist in the document, the delegate assigned to the `GetDefaultTimestamp` property on the `TimeSeriesExtractorOptions` is called to request a fallback timestamp to use. If no delegate has been specified, `DateTimeOffset.UtcNow` is used.

You can use the `TimestampSource` property on a `TimeSeriesSample` instance to determine how the timestamp for the sample was obtained.

Timestamps will be automatically parsed as follows:

- If the raw JSON value is a string, the timestamp will be retrieved using `JsonElement.TryGetDateTimeOffset`.
- If an integer value can be obtained from the JSON value using `JsonElement.TryGetInt64`, it is assumed to represent milliseconds since midnight UTC on 01 January 1970.
- If a timestamp cannot be inferred from either of the above approaches, the default timestamp will be used instead.

Custom timestamp parsing is described in the next section. Additional timestamp parsing behaviour can be specified when recursive mode is enabled. See below for more details.


## Custom Timestamp Parsing

Timestamp parsing can be overridden by setting the `TimestampParser` property on the `TimeSeriesExtractorOptions`. The property is a delegate that receives a `JsonElement` representing the timestamp property and returns a `DateTimeOffset?`. The delegate should return `null` if the timestamp cannot be parsed.

For example, to parse timestamps where the timestamp property is the number of seconds since midnight UTC on 01 January 1970 instead of the number of milliseconds:

```csharp
new TimeSeriesExtractorOptions() {
  TimestampParser = element => element.ValueKind == JsonValueKind.Number
    ? DateTime.UnixEpoch.AddSeconds(element.GetInt64())
    : null
}
```


## Selecting the Properties to Handle

By default, `TimeSeriesExtractor` will create a sample for each property on the object except for the configured timestamp property. To customise if a JSON element will be processed or not, you can assign a delegate to the `CanProcessElement` property on the `TimeSeriesExtractorOptions` instance passed to the extractor. The delegate returns a Boolean value indicating if the element should be processed. For example:

```csharp
new TimeSeriesExtractorOptions() {
  CanProcessElement = (TimeSeriesExtractorContext context, JsonPonter pointer, JsonElement element) => {
    // Check if the current pointer is allowed
    switch (pointer.ToString()) {
      case "/temperature":
      case "/pressure":
      case "/humidity":
        return true;
      default:
        return false;
    }
  }
}
```

If you have a known list of properties to include or exclude, you can use one of the `TimeSeriesExtractor.CreateJsonPointerMatchDelegate` method overloads to create a compatible delegate that can be assigned to the `CanProcessElement` property. For example:

```csharp
var matcher = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() {
    PointersToInclude = ["/temperature", "/pressure", "/humidity"]
});

var options = new TimeSeriesExtractorOptions() {
  CanProcessElement = matcher
}
```

### Pattern Matching using Wildcards

`TimeSeriesExtractor.CreateJsonPointerMatchDelegate` supports using single- and multi-character wildcards (`?` and `*` respectively) in JSON pointers when the `TimeSeriesExtractorOptions.AllowWildcardExpressions` property is `true`. Pattern matching wildcard expressions do not need to be valid JSON pointers.

Example 1: process all elements that are descendents of a `data` property anywhere in the document:

```csharp
var matcher = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() {
    AllowWildcardExpressions = true,
    PointersToInclude = ["*/data/*"]
});

var options = new TimeSeriesExtractorOptions() {
  CanProcessElement = matcher
}
```

Example 2: exclude all properties named `metadata` anywhere in the document:

```csharp
var matcher = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() {
    AllowWildcardExpressions = true,
    PointersToExclude = ["*/metadata"]
});

var options = new TimeSeriesExtractorOptions() {
  CanProcessElement = matcher
}
```


### Pattern Matching using MQTT-style Match Expressions

In addition to matching using wildcard characters, `TimeSeriesExtractor.CreateJsonPointerMatchDelegate` also supports using MQTT-style match expressions in JSON pointers when the `TimeSeriesExtractorOptions.AllowWildcardExpressions` property is `true`. Note that, unlike with pattern matching expressions, MQTT-style match expressions must be valid JSON pointers.

Example 1: process all elements that are descendents of `/data/instrument-1`:

```csharp
var matcher = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() {
    AllowWildcardExpressions = true,
    PointersToInclude = ["/data/instrument-1/#"]
});

var options = new TimeSeriesExtractorOptions() {
  CanProcessElement = matcher
}
```

Example 2: include all `temperature` properties that are grandchildren of `/data`:

```csharp
var matcher = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() {
    AllowWildcardExpressions = true,
    PointersToInclude = ["/data/+/temperature"]
});

var options = new TimeSeriesExtractorOptions() {
  CanProcessElement = matcher
}
```

Note that, if the JSON pointer includes single- or multi-character wildcards, regular pattern matching will always be used instead of MQTT-style match expressions.


## Data Sample Keys

Each `TimeSeriesSample` has a `Key` property that is used to identify the JSON property that was used to generate the sample. The key is generated from the `Template` property on the `TimeSeriesExtractorOptions` class. Consider the following example template:

```
devices/{deviceId}/instruments/{$prop}
```

The parts of the template enclosed in `{` and `}` are placeholders that will be replaced at runtime; `{deviceId}` will be replaced with the value of the property named `deviceId` on the same object as the property being processed, and `{$prop}` will be replaced with the JSON Pointer path of the property itself, without the leading `/`.

Here is an example JSON document:

```json
{
  "deviceId": 7,
  "temperature": 28.9
}
```

When processing the `temperature` property on the JSON object using the template above, the key that would be generated would be `devices/7/instruments/temperature`.

The default key template if one is not specified is simply `{$prop}` (i.e. the JSON Pointer path to the property).


### Built-In Template Placeholders

The following built-in template placeholders are available:

| Placeholder | Description | Example |
|-------------|-------------| ------- |
| `{$prop}` | The JSON Pointer path to the property being processed, without the leading `/`. | `data/device-1/temperature` |
| `{$prop-local}` | The local property name of the property being processed. | `temperature` |
| `{$prop-path}` | The full JSON Pointer path to the *parent element* of the property being processed, without the leading `/`. | `data/device-1` |

All other placeholders are resolved using the JSON document, or using a default placeholder value (see below). Note that, when recursive mode is disabled, the `{$prop}` and `${prop-local}` template placeholders are functionally identical, and the `{$prop-path}` placeholder will always be replaced with an empty string.


### Default Template Placeholder Values

Default template replacement values can be provided via the `GetTemplateReplacement` property on `TimeSeriesExtractorOptions`; these will be used if a property name referenced in the template does not exist on the JSON object being processed. For example:

Template:
```
devices/{deviceId}/instruments/{$prop}
```

JSON:
```json
{
  "temperature": 97.3
}
```

Options:
```csharp
new TimeSeriesExtractorOptions() {
  GetTemplateReplacement = name => {
    switch (name) {
      case "deviceId":
        return "A-001";
      default:
        return null;
    }
  }
}
```

When processing the `temperature` property on the JSON object using the template above, the key that would be generated would be `devices/A-001/instruments/temperature`.


### Handling Unresolved Template Replacements

By default, unresolved template replacements are ignored; the final key will contain the placeholder text. To skip JSON properties when one or more template replacements cannot be fulfilled, set the `AllowUnresolvedTemplateReplacements` property on the `TimeSeriesExtractorOptions` object to `false`.


## Recursive Processing

The default behaviour of `TimeSeriesExtractor` is to process top-level properties on the specified JSON object only. For example, consider the following JSON:

```json
{
  "temperature": 28.1,
  "pressure": 1020.99,
  "acceleration": {
    "x": -0.876,
    "y": 0.516,
    "z": -0.044
  }
}
```

The default behaviour of `TimeSeriesExtractor` would be to emit 3 samples, despite the `acceleration` property defining a nested structure containing additional data:

- `temperature`: `28.1`
- `pressure`: `1020.99`
- `acceleration`: `"{ \"x\": -0.876, \"y\": 0.516, \"z\": -0.044 }"`

In this circumstance, it is desirable to recursively process the nested object and emit a sample for each of the sub-properties. This can be done by setting the `Recursive` property on `TimeSeriesExtractorOptions` to `true`. If the `PathSeparator` property on `TimeSeriesExtractorOptions` is configured to anything other than `/`, the path delimiters in the `{$prop}` substitution value will be replaced with the specified `PathSeparator`.

With recursive mode enabled, the same JSON above would result in 5 samples being emitted:

- `temperature`: `28.1`
- `pressure`: `1020.99`
- `acceleration/x`: `-0.876`
- `acceleration/y`: `0.516`
- `acceleration/z`: `-0.044`

When recursive mode is enabled, the extractor will also iterate over nested arrays. For example:

```json
{
  "temperatures": [
    37.7,
    38.1,
    37.9
  ]
}
```

The samples emitted would be:

- `temperatures/0`: `37.7`
- `temperatures/1`: `38.1`
- `temperatures/2`: `37.9`

It is also possible to omit array indexes from the sample keys when recursive mode is enabled; see below for more details.


### A Note on Recursive Mode Template Replacements

In recursive mode, template replacements other than the built-in placeholders are resolved using all objects in the hierarchy from the root to the current object, and the matches are concatenated together with the configured path separator. Consider the following JSON:

```json
{
  "location": "System A",
  "measurements": {
    "location": "Subsystem 1",
    "temperature": 57.6
  }
}
```

Given a template of `{location}/{$prop}`, the key generated for the nested `temperature` property would be `System A/Subsystem 1/measurements/temperature`.

If you want to use the local property name in the generated key instead of the full JSON Pointer path, you can specify `{$prop-local}` in the template instead of `{$prop}`.

Using the above example JSON and a template of `{location}/{$prop-local}`, the key for the nested `temperature` property would be `System A/Subsystem 1/temperature` instead of `System A/Subsystem 1/measurements/temperature`.

> Remember: when recursive mode is disabled, the `{$prop}` and `${prop-local}` template placeholders are functionally identical, and the `{$prop-path}` placeholder will always be replaced with an empty string.


### Enabling Nested Timestamps

When recursive mode is enabled, the `TimeSeriesExtractorOptions.AllowNestedTimestamps` property controls whether timestamps can be extracted from nested objects. By default, this property is set to `false`, meaning that the timestamp is resolved by evaluating the timestamp property against the root of the JSON document. If set to `true`, the timestamp pointer is evaluated against each ancestor of the property being processed, and the first successful match is used for the sample. For example, consider the following JSON:

```json
{
  "time": "2021-05-30T09:47:38Z",
  "temperature": 24.7,
  "pressure": 1021.3,
  "humidity": 33.76,
  "acceleration": {
    "time": "2021-05-30T09:47:37Z",
    "x": -0.876,
    "y": 0.516,
    "z": -0.044
  }
}
```

If the configured timestamp property is `/time` and nested timestamps and recursive mode are enabled, the timestamp for the x, y and z acceleration samples would be `2021-05-30T09:47:37Z` instead of `2021-05-30T09:47:38Z`.


### Omitting Array Indexes From Sample Keys

When recursive mode is enabled, the `TimeSeriesExtractorOptions.IncludeArrayIndexesInSampleKeys` property controls whether array indexes in the JSON Pointer path are included in the generated sample keys. By default, this property is set to `true`. If set to `false`, the array indexes will be omitted from the generated sample keys. The `TimeSeriesExtractorOptions.IncludeArrayIndexesInSampleKeys` is typically used in conjunction with the `TimeSeriesExtractorOptions.AllowNestedTimestamps` property to allow multiple samples with identical keys but different timestamps to be extracted from an array. For example, consider the following JSON:

```json
{
  "device-1": {
    "data": [
      {
        "time": "2021-05-30T09:47:38Z",
        "temperature": 24.7
      },
      {
        "time": "2021-05-30T09:47:39Z",
        "temperature": 24.8
      },
      {
        "time": "2021-05-30T09:47:40Z",
        "temperature": 24.9
      }
    ] 
  }
}
```

If array indexes are omitted from the sample keys, three samples would be extracted with a key of `device-1/data/temperature`. With array indexes included in the sample keys, three samples would be extracted with keys of `device-1/data/0/temperature`, `device-1/data/1/temperature` and `device-1/data/2/temperature`.


## Specifying a Custom Processing Start Position

By default, processing starts at the root of the JSON document. However, in some scenarios it may be desirable to customise the start position. For example, consider the following example response from the [Airthings](https://developer.airthings.com) API:

```json
{
  "data": {
    "battery": 100,
    "co2": 650.0,
    "humidity": 26.0,
    "pm1": 0.0,
    "pm25": 0.0,
    "pressure": 1028.7,
    "radonShortTermAvg": 2.0,
    "temp": 24.6,
    "time": 1686421947,
    "voc": 58.0,
    "relayDeviceType": "hub"
  }
}
```

When processing the above document, it may be preferable to start at the `data` property instead of at the root of the document. This can be achieved by setting the `TimeSeriesExtractorOptions.StartAt` property to specify a JSON pointer to the desired start position:

```csharp
new TimeSeriesExtractorOptions() {
  StartAt = "/data",
  TimestampParser = element => element.ValueKind == JsonValueKind.Number
    ? DateTime.UnixEpoch.AddSeconds(element.GetInt64())
    : null
}
```

Note that specifying a start position means that all features that use JSON pointers (including the configured timestamp property and the `{$prop}` placeholder for a given sample) become relative to the start position instead of the root of the JSON document. For example, in the above document the `{$prop}` placeholder for the humidity reading would be `humidity` instead of `data/humidity`.


# Building the Solution

The repository uses [Cake](https://cakebuild.net/) for cross-platform build automation. The build script allows for metadata such as a build counter to be specified when called by a continuous integration system such as TeamCity.

A build can be run from the command line using the [build.ps1](./build.ps1) PowerShell script. For documentation about the available build script parameters, see [build.cake](./build.cake).

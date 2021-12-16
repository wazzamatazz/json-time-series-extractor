# Jaahas.Json.TimeSeriesExtractor

A C# library for extracting time series data from JSON using `System.Text.Json`. This can be useful when e.g. JSON data is received from an IoT device and the data must be separated into individual series for storage in a database.


# Getting Started

Add a NuGet package reference to [Jaahas.Json.TimeSeriesExtractor](https://www.nuget.org/packages/Jaahas.Json.TimeSeriesExtractor/) to your project.


# Usage

The [TimeSeriesExtractor](/src/JsonTimeSeriesExtractor/TimeSeriesExtractor.cs) class is the entry point for the library.

Call `TimeSeriesExtractor.GetSamples` to extract values from a JSON string or a `JsonElement`: 

```csharp
const string json = @"{ ""timestamp"": ""2021-05-30T09:47:38Z"", ""temperature"": 24.7, ""pressure"": 1021.3, ""humidity"": 33.76 }";

// GetSamples uses lazy evaluation; use the ToArray extension from 
// System.Linq to force eager evaluation.
var samples = TimeSeriesExtractor.GetSamples(json).ToArray();
```

The JSON document must represent an object or an array of objects. You can customise the extraction behaviour by passing a [TimeSeriesExtractorOptions](/src/JsonTimeSeriesExtractor/TimeSeriesExtractorOptions.cs) object when calling the method.


## Data Samples

Properties on the JSON objects are converted to instances of [TimeSeriesSample](/src/JsonTimeSeriesExtractor/TimeSeriesSample.cs). Although the type of the `Value` property on `TimeSeriesSample` is `object`, in practical terms the value will either be `null`, or one of the following types:

- `double`
- `string`
- `bool`


## Selecting the Timestamp

By default, `TimeSeriesExtractor` will try and use a property on your object named `time` or `timestamp` to extract the timestamp for each sample. The property name check is case-insensitive. If a timestamp cannot be extracted, the value assigned to the `NowTimestamp` property on the `TimeSeriesExtractorOptions` is used.

Alternatively, you can assign a delegate to the `IsTimestampProperty` on `TimeSeriesExtractorOptions` to select the timestamp property manually:

```csharp
new TimeSeriesExtractorOptions() {
  IsTimestampProperty = prop => prop.Equals("sampleTime")
}
```


## Selecting the Properties to Handle

By default, `TimeSeriesExtractor` will create a sample for each property on the object except for the timestamp property. To customise if a property will be included or excluded, you can assign a delegate to the `IncludeProperty` property on the `TimeSeriesExtractorOptions` instance passed to the extractor. The delegate will receive a `KeyValuePair<string?, JsonElement>[]` array, representing the bottom-up list of JSON property names and elements that have been visited to reach the current property (i.e. the entry at index 0 in the array). 

The array is guaranteed to have at least two items; the final `KeyValuePair<string?, JsonElement>` in the array will always have a key of `null` and a `JsonElement` value that represents the root JSON object that is being processed.

For example:

```csharp
new TimeSeriesExtractorOptions() {
  IncludeProperty = props => {
    // Check if the current property is allowed
    switch (props[0].Key) {
      case "temperature":
      case "pressure":
      case "humidity":
        return true;
      default:
        return false;
    }
  }
}
```


## Data Sample Keys

Each `TimeSeriesSample` has a `Key` property that is used to identify the JSON property that was used to generate the sample. The key is generated from the `Template` property on the `TimeSeriesExtractorOptions` class. Consider the following example template:

```
devices/{deviceId}/instruments/{$prop}
```

The parts of the template enclosed in `{` and `}` are placeholders that will be replaced at runtime; `{deviceId}` will be replaced with the value of the property named `deviceId` on the same object as the property being processed, and `{$prop}` will be replaced with the name of the property itself.

Here is an example JSON document:

```json
{
  "deviceId": 7,
  "temperature": 28.9
}
```

When processing the `temperature` property on the JSON object using the template above, the key that would be generated would be `devices/7/instruments/temperature`.

The default key template if one is not specified is simply `{prop$}` (i.e. the property name).


### Default Placeholder Values

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

In this circumstance, it is desirable to recursively process the nested object and emit a sample for each of the sub-properties. This can be done by setting the `Recursive` property on `TimeSeriesExtractorOptions` to `true`. When recursive mode is enabled, the `{$prop}` replacement in the key template consists of a delimited list of all of the property names that were traversed from the root object to current property. The `PathSeparator` property on `TimeSeriesExtractorOptions` defines the delimeter to use (default: `/`).

With recursive mode enabled, the same JSON above would result in 5 samples being emitted:

- `temperature`: `28.1`
- `pressure`: `1020.99`
- `acceleration/x`: `-0.876`
- `acceleration/y`: `0.516`
- `acceleration/z`: `-0.044`

When recursive mode is enabled, the extractor will also iterate over nested arrays. The array index will be used as the local property name when replacing the `{$prop}` template replacement. For example:

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


### A Note on Recursive Mode Template Replacements

In recursive mode, template replacements are resolved using all objects in the hierarchy from the root to the current object, and the matches are concatenated together with the configured path separator. Consider the following JSON:

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

If you do not want to include the document path in the generated key (i.e. the `measurements/` part in the above example), you can specify `{$prop-local}` in the template instead of `{$prop}`.

Using the above example JSON and a template of `{location}/{$prop-local}`, the key for the nested `temperature` property would be `System A/Subsystem 1/temperature`.

> When recursive mode is disabled, the `{$prop}` and `${prop-local}` template placeholders are functionally identical.


# Building the Solution

The repository uses [Cake](https://cakebuild.net/) for cross-platform build automation. The build script allows for metadata such as a build counter to be specified when called by a continuous integration system such as TeamCity.

A build can be run from the command line using the [build.ps1](/build.ps1) PowerShell script. For documentation about the available build script parameters, see [build.cake](/build.cake).

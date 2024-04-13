using System.Text.Json;
using System.Text.Json.Serialization;

using Jaahas.Json;

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.Converters.Add(new JsonStringEnumConverter());

JsonElement json;
using (var stream = new FileStream("data.json", FileMode.Open)) {
    json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, jsonOptions);
}

var options = new TimeSeriesExtractorOptions() {
    // We want to extract samples from nested objects.
    Recursive = true,
    // Our JSON contains an array of samples, each with its own timestamp.
    AllowNestedTimestamps = true,
    // The timestamp is located at the "ts" property on each sample object.
    TimestampProperty = "/ts",
    // Each object in the samples array contains a "v" property with the sample value. We'll use
    // an MQTT-style match expression to extract values from the "v" property on each object in
    // the array
    CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
        PointersToInclude = ["/body/data/+/v"],
        AllowWildcardExpressions = true
    }),
    // Each object in the samples array contains a "t" property with the name of the instrument
    // for the sample. We'll use this as the key when emitting a sample.
    Template = "{t}"
};

foreach (var sample in TimeSeriesExtractor.GetSamples(json, options)) {
    Console.WriteLine(JsonSerializer.Serialize(sample, jsonOptions));
}

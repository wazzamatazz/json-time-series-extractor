using System.Text.Json;
using System.Text.Json.Serialization;

using Jaahas.Json;

using Json.Pointer;

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.Converters.Add(new JsonStringEnumConverter());

JsonElement json;
using (var stream = new FileStream("data.json", FileMode.Open)) {
    json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, jsonOptions);
}

var options = new TimeSeriesExtractorOptions() { 
    AllowNestedTimestamps = true,
    IncludeArrayIndexesInSampleKeys = false,
    IncludeProperty = p => !p.Segments.Last().Value.Equals("t"),
    MaxDepth = 5,
    Recursive = true,
    StartAt = JsonPointer.Parse("/body/data"),
    TimestampProperty = JsonPointer.Parse("/ts"),
    Template = "{t}"
};



foreach (var sample in TimeSeriesExtractor.GetSamples(json, options)) {
    Console.WriteLine(JsonSerializer.Serialize(sample, jsonOptions));
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// Utility class for extracting key-timestamp-value time series data from JSON objects.
    /// </summary>
    /// <seealso cref="TimeSeriesExtractorOptions"/>
    public sealed class TimeSeriesExtractor {

        /// <summary>
        /// Template placeholder for the full JSON Pointer path to a property.
        /// </summary>
        public const string FullPropertyNamePlaceholder = "{$prop}";

        /// <summary>
        /// Template placeholder for the local property name only.
        /// </summary>
        public const string LocalPropertyNamePlaceholder = "{$prop-local}";

        /// <summary>
        /// Matches JSON property name references in sample key templates.
        /// </summary>
        private static readonly Regex s_sampleKeyTemplateMatcher = new Regex(@"\{(?<property>[^\}]+?)\}", RegexOptions.Singleline);


        /// <summary>
        /// Parses the specified JSON string and extracts samples from the parsed object.
        /// </summary>
        /// <param name="json">
        ///   The JSON string. This must be either a JSON object, or an array of JSON objects.
        /// </param>
        /// <param name="options">
        ///   The options for the extraction.
        /// </param>
        /// <param name="serializerOptions">
        ///   The JSON serializer options to use when deserializing the <paramref name="json"/> 
        ///   string.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{Sample}"/> that will emit the parsed samples.
        /// </returns>
        public static IEnumerable<TimeSeriesSample> GetSamples(string json, TimeSeriesExtractorOptions? options = null, JsonSerializerOptions? serializerOptions = null) {
            var element = JsonSerializer.Deserialize<JsonElement>(json, serializerOptions);
            return GetSamples(element, options);
        }


        /// <summary>
        /// Extracts samples from the specified root JSON element.
        /// </summary>
        /// <param name="element">
        ///   The root JSON element.
        /// </param>
        /// <param name="options">
        ///   The options for extraction.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{Sample}"/> that will emit the parsed samples.
        /// </returns>
        /// <remarks>
        ///   <paramref name="element"/> must have a <see cref="JsonValueKind"/> of <see cref="JsonValueKind.Object"/> 
        ///   or <see cref="JsonValueKind.Array"/>.
        /// </remarks>
        /// <exception cref="ValidationException">
        ///   <paramref name="options"/> is not valid.
        /// </exception>
        /// <seealso cref="TimeSeriesExtractorOptions"/>
        public static IEnumerable<TimeSeriesSample> GetSamples(JsonElement element, TimeSeriesExtractorOptions? options = null) {
            if (options == null) {
                options = new TimeSeriesExtractorOptions();
            }
            else {
                Validator.ValidateObject(options, new ValidationContext(options), true);
            }

            if (options.StartAt != null) {
                if (!JsonPointer.TryParse(options.StartAt, out var startAt)) {
                    throw new ArgumentOutOfRangeException(nameof(options), string.Format(CultureInfo.CurrentCulture, Resources.Error_InvalidJsonPointer, options.StartAt));
                }

                var newElement = startAt!.Evaluate(element);
                if (newElement == null) {
                    yield break;
                }

                element = newElement.Value;
            }

            if (element.ValueKind == JsonValueKind.Array) {
                foreach (var item in element.EnumerateArray()) {
                    foreach (var value in GetSamples(item, options)) {
                        yield return value;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Object) {
                foreach (var value in GetSamplesCore(element, options)) {
                    yield return value;
                }
            }
        }


        /// <summary>
        /// Internal starting point for extracting samples from a JSON object.
        /// </summary>
        /// <param name="element">
        ///   The root JSON object.
        /// </param>
        /// <param name="options">
        ///   The options for extraction.
        /// </param>
        /// <returns>
        ///   The parsed tag values.
        /// </returns>
        private static IEnumerable<TimeSeriesSample> GetSamplesCore(JsonElement element, TimeSeriesExtractorOptions options) {
            var context = new TimeSeriesExtractorContext(options);

            ParsedTimestamp defaultTimestamp;

            if (context.TimestampPointer == null || !TryGetTimestamp(element, context.TimestampPointer, context.Options, out var sampleTime)) {
                var ts = options.GetDefaultTimestamp?.Invoke();
                if (ts == null) {
                    defaultTimestamp = new ParsedTimestamp(DateTimeOffset.UtcNow, TimestampSource.CurrentTime, null);
                }
                else {
                    defaultTimestamp = new ParsedTimestamp(ts.Value, TimestampSource.FallbackProvider, null);
                }
            }
            else {
                defaultTimestamp = new ParsedTimestamp(sampleTime, TimestampSource.Document, context.TimestampPointer);
            }
            context.TimestampStack.Push(defaultTimestamp);

            context.ElementStack.Push(new KeyValuePair<string?, JsonElement>(null, element));

            foreach (var prop in element.EnumerateObject()) {
                context.ElementStack.Push(new KeyValuePair<string?, JsonElement>(prop.Name, prop.Value));

                foreach (var val in GetSamplesCore(context, 1)) {
                    yield return val;
                }

                context.ElementStack.Pop();
            }
        }


        /// <summary>
        /// Extracts samples from a JSON element.
        /// </summary>
        /// <param name="context">
        ///   The extractor context.
        /// </param>
        /// <param name="currentRecursionDepth">
        ///   The recursion depth for the current iteration of the method.
        /// </param>
        /// <returns>
        ///   The extracted tag values.
        /// </returns>
        private static IEnumerable<TimeSeriesSample> GetSamplesCore(
            TimeSeriesExtractorContext context,
            int currentRecursionDepth
        ) {
            var pointer = JsonPointer.Create(context.ElementStack.Where(x => x.Key != null).Reverse().Select(x => PointerSegment.Create(x.Key!)));

            // Check if this property should be included.
            if (!context.IncludeElement.Invoke(pointer)) {
                yield break;
            }

            var currentElement = context.ElementStack.Peek();

            if (!context.Options.Recursive || (context.Options.MaxDepth > 0 && currentRecursionDepth >= context.Options.MaxDepth)) {
                // We are not using recursive mode, or we have exceeded the maximum recursion
                // depth; build a sample with the current element. When we have exceeded the
                // maximum recursion depth, the value will be the serialized JSON of the element
                // if the element is an object or an array.
                string key = null!;
                var error = false;

                try {
                    key = BuildSampleKeyFromTemplate(
                        context.Options,
                        pointer,
                        context.ElementStack,
                        context.IsDefaultSampleKeyTemplate
                    );
                }
                catch (InvalidOperationException) {
                    error = true;
                }

                if (error) {
                    yield break;
                }

                var timestamp = context.TimestampStack.Peek();
                yield return BuildSampleFromJsonValue(timestamp.Timestamp, timestamp.Source, key, currentElement.Value);
            }
            else {
                // We have doing recursive processing and have not exceeded the maximum recursion
                // depth, so continue as normal.

                switch (currentElement.Value.ValueKind) {
                    case JsonValueKind.Object:
                        var popTimestamp = false;
                        if (context.Options.AllowNestedTimestamps && context.TimestampPointer != null && TryGetTimestamp(currentElement.Value, context.TimestampPointer, context.Options, out var sampleTime)) {
                            context.TimestampStack.Push(new ParsedTimestamp(sampleTime, TimestampSource.Document, pointer.Combine(context.TimestampPointer)));
                            popTimestamp = true;
                        }

                        foreach (var item in currentElement.Value.EnumerateObject()) {
                            context.ElementStack.Push(new KeyValuePair<string?, JsonElement>(item.Name, item.Value));

                            foreach (var val in GetSamplesCore(
                                context,
                                currentRecursionDepth + 1
                            )) {
                                yield return val;
                            }

                            context.ElementStack.Pop();
                        }

                        if (popTimestamp) {
                            context.TimestampStack.Pop();
                        }
                        break;
                    case JsonValueKind.Array:
                        var index = -1;

                        foreach (var item in currentElement.Value.EnumerateArray()) {
                            ++index;

                            context.ElementStack.Push(new KeyValuePair<string?, JsonElement>(index.ToString(), item));

                            foreach (var val in GetSamplesCore(
                                context,
                                currentRecursionDepth + 1
                            )) {
                                yield return val;
                            }

                            context.ElementStack.Pop();
                        }
                        break;
                    default:
                        if (context.ElementStack.Count == 0) {
                            yield break;
                        }

                        string key;

                        try {
                            key = BuildSampleKeyFromTemplate(
                                context.Options,
                                pointer,
                                context.ElementStack,
                                context.IsDefaultSampleKeyTemplate
                            );
                        }
                        catch (InvalidOperationException) {
                            break;
                        }

                        var timestamp = context.TimestampStack.Peek();
                        yield return BuildSampleFromJsonValue(timestamp.Timestamp, timestamp.Source, key, currentElement.Value);
                        break;
                }
            }
        }


        /// <summary>
        /// Tries to parse the timestamp to use for samples extracted from the specified root object.
        /// </summary>
        /// <param name="element">
        ///   The root JSON object.
        /// </param>
        /// <param name="pointer">
        ///   The pointer to the timestamp property.
        /// </param>
        /// <param name="options">
        ///   The extraction options.
        /// </param>
        /// <param name="value">
        ///   The timestamp that was extracted.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the timestamp was successfully extracted, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        private static bool TryGetTimestamp(JsonElement element, JsonPointer pointer, TimeSeriesExtractorOptions options, out DateTimeOffset value) {
            value = default;

            if (element.ValueKind != JsonValueKind.Object || options.TimestampProperty == null) {
                return false;
            }

            var el = pointer.Evaluate(element);
            if (el == null) {
                return false;
            }

            if (options.TimestampParser != null) {
                var dt = options.TimestampParser.Invoke(el.Value);
                if (dt == null) {
                    return false;
                }

                value = dt.Value;
                return true;
            }

            if (el.Value.ValueKind == JsonValueKind.String) {
                if (el.Value.TryGetDateTimeOffset(out var dt)) {
                    value = dt;
                    return true;
                }
            }
            else if (el.Value.ValueKind == JsonValueKind.Number) {
                if (el.Value.TryGetInt64(out var ms)) {
                    value = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms);
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Builds a key for a <see cref="TimeSeriesSample"/> from the specified template.
        /// </summary>
        /// <param name="options">
        ///   The extraction options.
        /// </param>
        /// <param name="pointer">
        ///   The JSON pointer for the element that is currently being processed.
        /// </param>
        /// <param name="elementStack">
        ///   The stack of objects with the current object being processed at the top and the root 
        ///   object in the hierarchy at the bottom.
        /// </param>
        /// <param name="isDefaultTemplate">
        ///   Specifies if <paramref name="template"/> is the default sample key template.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        private static string BuildSampleKeyFromTemplate(
            TimeSeriesExtractorOptions options,
            JsonPointer pointer,
            IEnumerable<KeyValuePair<string?, JsonElement>> elementStack,
            bool isDefaultTemplate
            
        ) {
            string GetFullPropertyName(bool forceLocalName = false) {
                if (!options.Recursive || forceLocalName) {
                    return pointer.Segments.Last().Value;
                }

                if (options.IncludeArrayIndexesInSampleKeys) {
                    return string.Equals(options.PathSeparator, TimeSeriesExtractorOptions.DefaultPathSeparator, StringComparison.Ordinal)
                        ? pointer.ToString().TrimStart('/')
                        : pointer.ToString().TrimStart('/').Replace("/", options.PathSeparator);
                }

                // Remove array indexes from the path. An array index is any segment that can be
                // parsed to an integer.
                return string.Join(options.PathSeparator, pointer.Segments.Where(x => !int.TryParse(x.Value, out _)).Select(x => x.Value));
            }

            if (isDefaultTemplate) {
                // Fast path for default template.
                return GetFullPropertyName();
            }

            var closestObject = elementStack.FirstOrDefault(x => x.Value.ValueKind == JsonValueKind.Object);

            KeyValuePair<string?, JsonElement>[] elementStackInHierarchyOrder = null!;

            KeyValuePair<string?, JsonElement>[] GetElementStackInHierarchyOrder() {
                elementStackInHierarchyOrder ??= options.Recursive
                    ? options.IncludeArrayIndexesInSampleKeys
                        ? elementStack.Reverse().ToArray()
                        : elementStack.Reverse().Where(x => x.Key == null || !int.TryParse(x.Key, out _)).ToArray()
                    : Array.Empty<KeyValuePair<string?, JsonElement>>();
                return elementStackInHierarchyOrder;
            }

            return s_sampleKeyTemplateMatcher.Replace(options.Template, m => {
                var pName = m.Groups["property"].Value;

                if (string.Equals(pName, "$prop", StringComparison.Ordinal) || string.Equals(pName, "$prop-local", StringComparison.Ordinal)) {
                    return GetFullPropertyName(string.Equals(pName, "$prop-local", StringComparison.Ordinal)) ?? m.Value;
                }

                if (options.Recursive) {
                    // Recursive mode: try and get matching replacements from every object in the
                    // stack (starting from the root) and concatenate them using the path separator.

                    var propVals = new List<string>();
                    
                    foreach (var obj in GetElementStackInHierarchyOrder()) {
                        if (obj.Value.ValueKind != JsonValueKind.Object) {
                            continue;
                        }

                        if (obj.Value.TryGetProperty(pName, out var prop)) {
                            propVals.Add(prop.ValueKind == JsonValueKind.String ? prop.GetString()! : prop.GetRawText());
                        }
                    }

                    if (propVals.Count > 0) {
                        return string.Join(options.PathSeparator, propVals);
                    }
                }
                else {
                    // Non-recursive mode: use the nearest object to the item we are processing to
                    // try and get the referenced property.

                    if (closestObject.Value.ValueKind == JsonValueKind.Object && closestObject.Value.TryGetProperty(pName, out var prop)) {
                        return prop.ValueKind == JsonValueKind.String ? prop.GetString()! : prop.GetRawText();
                    }
                }

                // No match in the object stack: try and find a default replacement.
                var replacement = options.GetTemplateReplacement?.Invoke(pName);
                if (replacement == null && !options.AllowUnresolvedTemplateReplacements) {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_UnresolvedTemplateParameter, pName));
                }

                // Return the replacement if available, otherwise return the original placeholder
                // text.
                return replacement ?? m.Value;
            });
        }


        /// <summary>
        /// Builds a <see cref="TimeSeriesSample"/> from the specified JSON value.
        /// </summary>
        /// <param name="sampleTime">
        ///   The sample time for the value.
        /// </param>
        /// <param name="sampleTimeSource">
        ///   The source of the <paramref name="sampleTime"/>.
        /// </param>
        /// <param name="key">
        ///   The sample key to use.
        /// </param>
        /// <param name="value">
        ///   The JSON value for the sample.
        /// </param>
        /// <returns>
        ///   The generated <see cref="TimeSeriesSample"/>.
        /// </returns>
        private static TimeSeriesSample BuildSampleFromJsonValue(
            DateTimeOffset sampleTime,
            TimestampSource sampleTimeSource,
            string key,
            JsonElement value
        ) {
            object? val;

            switch (value.ValueKind) {
                case JsonValueKind.Number:
                    val = value.GetDouble();
                    break;
                case JsonValueKind.String:
                    val = value.GetString();
                    break;
                case JsonValueKind.True:
                    val = true;
                    break;
                case JsonValueKind.False:
                    val = false;
                    break;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    val = value.GetRawText();
                    break;
                default:
                    val = null;
                    break;
            }

            return new TimeSeriesSample(key, sampleTime, val, sampleTimeSource);
        }

    }
}

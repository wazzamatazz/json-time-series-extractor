using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jaahas.Json {

    /// <summary>
    /// Utility class for extracting key-timestamp-value time series data from JSON objects.
    /// </summary>
    /// <seealso cref="TimeSeriesExtractorOptions"/>
    public class TimeSeriesExtractor {

        /// <summary>
        /// Matches JSON property name references in sample key templates.
        /// </summary>
        private static readonly Regex s_tagNameTemplateMatcher = new Regex(@"\{(?<property>[^\}]+?)\}", RegexOptions.Singleline);


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
        /// <seealso cref="TimeSeriesExtractorOptions"/>
        public static IEnumerable<TimeSeriesSample> GetSamples(JsonElement element, TimeSeriesExtractorOptions? options = null) {
            options ??= new TimeSeriesExtractorOptions();

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
            DateTimeOffset sampleTime;

            if (!TryGetTimestamp(element, options, out var timestampPropName, out sampleTime)) {
                sampleTime = options.NowTimestamp ?? DateTimeOffset.UtcNow;
            }

            return GetSamplesCore(
                new Stack<JsonElement>(),
                sampleTime,
                element,
                null,
                new Stack<string>(),
                options.Template,
                options.TemplateReplacements,
                options.IncludeProperty == null
                    ? timestampPropName == null
                        ? null
                        : prop => !string.Equals(prop, timestampPropName, StringComparison.Ordinal)
                    : timestampPropName == null
                        ? options.IncludeProperty
                        : prop => !string.Equals(prop, timestampPropName, StringComparison.Ordinal) && options.IncludeProperty.Invoke(prop),
                options.Recursive,
                options.PathSeparator
            );
        }


        /// <summary>
        /// Extracts samples from a JSON element.
        /// </summary>
        /// <param name="objectStack">
        ///   The stack of objects with the current object being processed at the top and the root 
        ///   object in the hierarchy at the bottom.
        /// </param>
        /// <param name="sampleTime">
        ///   The timestamp to use for extracted tag values.
        /// </param>
        /// <param name="element">
        ///   The JSON element that is being processed in this iteration.
        /// </param>
        /// <param name="propertyName">
        ///   The name of the JSON property that is being processed in this iteration. Can be 
        ///   <see langword="null"/> if <paramref name="element"/> is the root object.
        /// </param>
        /// <param name="sampleKeyTemplate">
        ///   The sample key template to use for the generated values.
        /// </param>
        /// <param name="templateReplacements">
        ///   The default replacements for <paramref name="sampleKeyTemplate"/>.
        /// </param>
        /// <param name="includeProperty">
        ///   A delegate that will check if a given property name should be included.
        /// </param>
        /// <param name="recursive">
        ///   Specifies if recursive mode is enabled.
        /// </param>
        /// <param name="pathSeparator">
        ///   The recursive path separator to use.
        /// </param>
        /// <returns>
        ///   The extracted tag values.
        /// </returns>
        private static IEnumerable<TimeSeriesSample> GetSamplesCore(
            Stack<JsonElement> objectStack,
            DateTimeOffset sampleTime,
            JsonElement element,
            string? propertyName,
            Stack<string> propertyNameStack,
            string sampleKeyTemplate,
            IDictionary<string, string>? templateReplacements,
            Func<string, bool>? includeProperty,
            bool recursive,
            string? pathSeparator
        ) {
            if (propertyName != null) {
                // currentPropertyName should only be null for top-level objects!

                // Check if this property should be included.
                if (!(includeProperty?.Invoke(propertyName) ?? true)) {
                    yield break;
                }
            }

            if (recursive) {
                if (string.IsNullOrWhiteSpace(pathSeparator)) {
                    pathSeparator = "/";
                };

                switch (element.ValueKind) {
                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        if (element.ValueKind == JsonValueKind.Object) {
                            objectStack.Push(element);

                            if (propertyName != null) {
                                propertyNameStack.Push(propertyName);
                            }

                            foreach (var item in element.EnumerateObject()) {
                                foreach (var val in GetSamplesCore(
                                    objectStack,
                                    sampleTime,
                                    item.Value,
                                    item.Name,
                                    propertyNameStack,
                                    sampleKeyTemplate,
                                    templateReplacements,
                                    includeProperty,
                                    recursive,
                                    pathSeparator
                                )) {
                                    yield return val;
                                }
                            }

                            if (propertyName != null) {
                                propertyNameStack.Pop();
                            }

                            objectStack.Pop();
                        }
                        else {
                            var index = -1;
                            foreach (var item in element.EnumerateArray()) {
                                if (item.ValueKind == JsonValueKind.Object) {
                                    objectStack.Push(item);
                                }

                                ++index;

                                if (propertyName != null) {
                                    propertyNameStack.Push(propertyName);
                                }

                                foreach (var val in GetSamplesCore(
                                    objectStack,
                                    sampleTime,
                                    item,
                                    index.ToString(),
                                    propertyNameStack,
                                    sampleKeyTemplate,
                                    templateReplacements,
                                    includeProperty,
                                    recursive,
                                    pathSeparator
                                )) {
                                    yield return val;
                                }

                                if (propertyName != null) {
                                    propertyNameStack.Pop();
                                }

                                if (item.ValueKind == JsonValueKind.Object) {
                                    objectStack.Pop();
                                }
                            }
                        }

                        break;
                    default:
                        if (propertyName == null) {
                            yield break;
                        }

                        var key = BuildSampleKeyFromTemplate(
                            objectStack,
                            recursive,
                            pathSeparator!,
                            propertyName, 
                            propertyNameStack,
                            sampleKeyTemplate, 
                            templateReplacements
                        );
                        yield return BuildSampleFromJsonValue(sampleTime, key, element);
                        break;
                }
            }
            else {
                if (objectStack.Count == 0) {
                    // This is the root object.
                    objectStack.Push(element);

                    foreach (var item in element.EnumerateObject()) {
                        foreach (var val in GetSamplesCore(
                            objectStack,
                            sampleTime,
                            item.Value,
                            item.Name,
                            propertyNameStack,
                            sampleKeyTemplate,
                            templateReplacements,
                            includeProperty,
                            false,
                            pathSeparator
                        )) {
                            yield return val;
                        }
                    }

                    objectStack.Pop();
                }
                else {
                    if (propertyName == null) {
                        yield break;
                    }

                    var tagName = BuildSampleKeyFromTemplate(
                        objectStack,
                        recursive,
                        pathSeparator!,
                        propertyName,
                        propertyNameStack,
                        sampleKeyTemplate,
                        templateReplacements
                    );
                    yield return BuildSampleFromJsonValue(sampleTime, tagName, element);
                }
            }
        }


        /// <summary>
        /// Tries to extract the timestamp for samples from the specified JSON object.
        /// </summary>
        /// <param name="element">
        ///   The JSON object.
        /// </param>
        /// <param name="options">
        ///   The extraction options.
        /// </param>
        /// <param name="name">
        ///   The name of the property that was identified as the timestamp property.
        /// </param>
        /// <param name="value">
        ///   The timestamp that was extracted.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the timestamp was successfully extracted, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        private static bool TryGetTimestamp(JsonElement element, TimeSeriesExtractorOptions options, out string? name, out DateTimeOffset value) {
            name = null;
            value = default;

            if (element.ValueKind != JsonValueKind.Object) {
                return false;
            }

            if (options.IsTimestampProperty != null) {
                
                
                return false;
            }

            foreach (var prop in element.EnumerateObject()) {
                if (options.IsTimestampProperty != null) {
                    if (!options.IsTimestampProperty.Invoke(prop.Name)) {
                        continue;
                    }

                    name = prop.Name;
                    value = prop.Value.GetDateTimeOffset();
                    return true;
                }
                else {
                    if (!string.Equals(prop.Name, "time", StringComparison.OrdinalIgnoreCase) && !string.Equals(prop.Name, "timestamp", StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    if (!prop.Value.TryGetDateTimeOffset(out var dt)) {
                        continue;
                    }

                    name = prop.Name;
                    value = dt;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Builds a key for a <see cref="TimeSeriesSample"/> from the specified template.
        /// </summary>
        /// <param name="objectStack">
        ///   The stack of objects with the current object being processed at the top and the root 
        ///   object in the hierarchy at the bottom.
        /// </param>
        /// <param name="recursive">
        ///   When <see langword="true"/>, all matching replacements from all levels of the 
        ///   <paramref name="objectStack"/> will be returned, instead of just looking at the 
        ///   object on top of the stack.
        /// </param>
        /// <param name="pathSeparator">
        ///   The path separator to use when joining multiple replacements together in recursive 
        ///   mode.
        /// </param>
        /// <param name="propertyName">
        ///   The local name of the property that the sample is being generated for.
        /// </param>
        /// <param name="propertyNameStack">
        ///   The property name stack (that is, the names of all of the properties in ancestor 
        ///   objects that led to <paramref name="propertyName"/>).
        /// </param>
        /// <param name="template">
        ///   The sample key template.
        /// </param>
        /// <param name="defaultReplacements">
        ///   The default placeholder replacement values to use, if a referenced property does not 
        ///   exist on <paramref name="parentObject"/>.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        private static string BuildSampleKeyFromTemplate(
            Stack<JsonElement> objectStack, 
            bool recursive, 
            string pathSeparator, 
            string propertyName, 
            Stack<string> propertyNameStack,
            string template, 
            IDictionary<string, string>? defaultReplacements
        ) {
            var objectStackInHierarchyOrder = recursive
                ? objectStack.ToArray().Reverse().ToArray()
                : Array.Empty<JsonElement>();

            string? GetFullPropertyName() {
                if (propertyNameStack.Count == 0) {
                    return propertyName;
                }

                return string.Concat(string.Join(pathSeparator, propertyNameStack.Reverse()), pathSeparator, propertyName);
            }

            return s_tagNameTemplateMatcher.Replace(template, m => {
                var pName = m.Groups["property"].Value;

                if (string.Equals(pName, "$prop")) {
                    return GetFullPropertyName() ?? m.Value;
                }

                if (recursive) {
                    // Recursive mode: try and get matching replacements from every object in the
                    // stack (starting from the root) and concatenate them using the path separator.

                    var propVals = new List<string>();
                    
                    foreach (var obj in objectStackInHierarchyOrder) {
                        if (obj.ValueKind != JsonValueKind.Object) {
                            continue;
                        }

                        if (obj.TryGetProperty(pName, out var prop)) {
                            propVals.Add(prop.ValueKind == JsonValueKind.String ? prop.GetString()! : prop.GetRawText());
                        }
                    }

                    if (propVals.Count > 0) {
                        return string.Join(pathSeparator, propVals);
                    }
                }
                else {
                    // Non-recursive mode: peek at the object on the top of the stack (i.e. the
                    // current object being processed) and try and get the referenced property.

                    var top = objectStack.Peek();
                    if (top.ValueKind == JsonValueKind.Object && top.TryGetProperty(pName, out var prop)) {
                        return prop.ValueKind == JsonValueKind.String ? prop.GetString()! : prop.GetRawText();
                    }
                }

                // No match in the object stack: try and find a default replacement.
                if (defaultReplacements != null && defaultReplacements.TryGetValue(pName, out var replacement)) {
                    return replacement;
                }

                // No replacement available.
                return m.Value;
            });
        }


        /// <summary>
        /// Builds a <see cref="TimeSeriesSample"/> from the specified JSON value.
        /// </summary>
        /// <param name="sampleTime">
        ///   The sample time for the value.
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

            return new TimeSeriesSample(key, sampleTime, val);
        }

    }
}

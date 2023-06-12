using System;
using System.Collections.Generic;
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
    public class TimeSeriesExtractor {

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
            JsonPointer? tsPointer = null;
            DateTimeOffset sampleTime;
            TimestampSource timestampSource = TimestampSource.Unspecified;
            
            if (options.TimestampProperty == null || !JsonPointer.TryParse(options.TimestampProperty, out tsPointer, JsonPointerKind.Plain) || !TryGetTimestamp(element, tsPointer!, options, out sampleTime)) {
                var ts = options!.GetDefaultTimestamp?.Invoke();
                if (ts == null) {
                    sampleTime = DateTimeOffset.UtcNow;
                    timestampSource = TimestampSource.CurrentTime;
                }
                else {
                    sampleTime = ts.Value;
                    timestampSource = TimestampSource.FallbackProvider;
                }
            }
            else {
                timestampSource = TimestampSource.Document;
            }

            Func<JsonPointer, bool>? handleProperty = options.IncludeProperty == null
                ? tsPointer == null
                    ? null
                    : path => !path.Equals(tsPointer)
                : tsPointer == null
                    ? path => options.IncludeProperty.Invoke(path.ToString())
                    : path => !path.Equals(tsPointer) && options.IncludeProperty.Invoke(path.ToString());

            var pointerSegments = new Stack<KeyValuePair<string?, JsonElement>>();
            pointerSegments.Push(new KeyValuePair<string?, JsonElement>(null, element));

            var template = string.IsNullOrWhiteSpace(options.Template)
                ? TimeSeriesExtractorOptions.DefaultTemplate
                : options.Template;

            // We are using the default template if:
            //
            // 1. We are running in recursive mode and the template is equal to FullPropertyNamePlaceholder.
            // 2. We are running in non-recursive mode and the template is equal to FullPropertyNamePlaceholder
            //    or LocalPropertyNamePlaceholder. 
            var isDefaultTemplate = options.Recursive 
                ? string.Equals(template, FullPropertyNamePlaceholder, StringComparison.Ordinal)
                : string.Equals(template, FullPropertyNamePlaceholder, StringComparison.Ordinal) || string.Equals(template, LocalPropertyNamePlaceholder, StringComparison.Ordinal);

            foreach (var prop in element.EnumerateObject()) {
                pointerSegments.Push(new KeyValuePair<string?, JsonElement>(prop.Name, prop.Value));

                foreach (var val in GetSamplesCore(
                    pointerSegments,
                    sampleTime,
                    timestampSource,
                    template,
                    isDefaultTemplate,
                    options.GetTemplateReplacement,
                    options.AllowUnresolvedTemplateReplacements,
                    handleProperty,
                    options.Recursive,
                    options.MaxDepth,
                    1,
                    options.PathSeparator
                )) {
                    yield return val;
                }

                pointerSegments.Pop();
            }
        }


        /// <summary>
        /// Extracts samples from a JSON element.
        /// </summary>
        /// <param name="elementStack">
        ///   The stack of objects with the current object being processed at the top and the root 
        ///   object in the hierarchy at the bottom.
        /// </param>
        /// <param name="sampleTime">
        ///   The timestamp to use for extracted tag values.
        /// </param>
        /// <param name="sampleTimeSource">
        ///   The source of the <paramref name="sampleTime"/>.
        /// </param>
        /// <param name="template">
        ///   The template to use when generating the key for a given value.
        /// </param>
        /// <param name="isDefaultTemplate">
        ///   Specifies if <paramref name="template"/> is equal to <see cref="TimeSeriesExtractorOptions.DefaultTemplate"/>.
        /// </param>
        /// <param name="templateReplacements">
        ///   A callback for retrieving the default replacements for <paramref name="template"/>.
        /// </param>
        /// <param name="allowUnresolvedTemplateReplacements">
        ///   When <see langword="false"/>, failure to replace a placeholder in the <paramref name="template"/> 
        ///   for a given property will result in that property being skipped.
        /// </param>
        /// <param name="includeProperty">
        ///   A delegate that will check if a given property name should be included.
        /// </param>
        /// <param name="recursive">
        ///   Specifies if recursive mode is enabled.
        /// </param>
        /// <param name="maxRecursionDepth">
        ///   The maximum number of recursive calls that the method is allowed to make.
        /// </param>
        /// <param name="currentRecursionDepth">
        ///   The recursion depth for the current iteration of the method.
        /// </param>
        /// <param name="pathSeparator">
        ///   The recursive path separator to use.
        /// </param>
        /// <returns>
        ///   The extracted tag values.
        /// </returns>
        private static IEnumerable<TimeSeriesSample> GetSamplesCore(
            Stack<KeyValuePair<string?, JsonElement>> elementStack,
            DateTimeOffset sampleTime,
            TimestampSource sampleTimeSource,
            string template,
            bool isDefaultTemplate,
            Func<string, string?>? templateReplacements,
            bool allowUnresolvedTemplateReplacements,
            Func<JsonPointer, bool>? includeProperty,
            bool recursive,
            int maxRecursionDepth,
            int currentRecursionDepth,
            string? pathSeparator
        ) {
            var pointer = JsonPointer.Create(elementStack.Where(x => x.Key != null).Reverse().Select(x => PointerSegment.Create(x.Key!)), false);

            if (includeProperty != null) {
                // Check if this property should be included.
                if (!includeProperty.Invoke(pointer)) {
                    yield break;
                }
            }

            var currentElement = elementStack.Peek();

            if (!recursive || (maxRecursionDepth > 0 && currentRecursionDepth >= maxRecursionDepth)) {
                // We are not using recursive mode, or we have exceeded the maximum recursion
                // depth; build a sample with the current element. When we have exceeded the
                // maximum recursion depth, the value will be the serialized JSON of the element
                // if the element is an object or an array.
                string key = null!;
                var error = false;

                try {
                    key = BuildSampleKeyFromTemplate(
                        pointer.ToString(),
                        elementStack,
                        recursive,
                        pathSeparator!,
                        template,
                        isDefaultTemplate,
                        templateReplacements,
                        allowUnresolvedTemplateReplacements
                    );
                }
                catch (InvalidOperationException) {
                    error = true;
                }

                if (error) {
                    yield break;
                }

                yield return BuildSampleFromJsonValue(sampleTime, sampleTimeSource, key, currentElement.Value);
            }
            else {
                // We have doing recursive processing and have not exceeded the maximum recursion
                // depth, so continue as normal.

                if (string.IsNullOrWhiteSpace(pathSeparator)) {
                    pathSeparator = "/";
                };

                switch (currentElement.Value.ValueKind) {
                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        if (currentElement.Value.ValueKind == JsonValueKind.Object) {
                            foreach (var item in currentElement.Value.EnumerateObject()) {
                                elementStack.Push(new KeyValuePair<string?, JsonElement>(item.Name, item.Value));

                                foreach (var val in GetSamplesCore(
                                    elementStack,
                                    sampleTime,
                                    sampleTimeSource,
                                    template,
                                    isDefaultTemplate,
                                    templateReplacements,
                                    allowUnresolvedTemplateReplacements,
                                    includeProperty,
                                    recursive,
                                    maxRecursionDepth, 
                                    currentRecursionDepth + 1,
                                    pathSeparator
                                )) {
                                    yield return val;
                                }

                                elementStack.Pop();
                            }
                        }
                        else {
                            var index = -1;
                            foreach (var item in currentElement.Value.EnumerateArray()) {
                                ++index;

                                elementStack.Push(new KeyValuePair<string?, JsonElement>(index.ToString(), item));

                                foreach (var val in GetSamplesCore(
                                    elementStack,
                                    sampleTime,
                                    sampleTimeSource,
                                    template,
                                    isDefaultTemplate,
                                    templateReplacements,
                                    allowUnresolvedTemplateReplacements,
                                    includeProperty,
                                    recursive,
                                    maxRecursionDepth,
                                    currentRecursionDepth + 1,
                                    pathSeparator
                                )) {
                                    yield return val;
                                }

                                elementStack.Pop();
                            }
                        }

                        break;
                    default:
                        if (elementStack.Count == 0) {
                            yield break;
                        }

                        string key;

                        try {
                            key = BuildSampleKeyFromTemplate(
                                pointer.ToString(),
                                elementStack,
                                recursive,
                                pathSeparator!,
                                template,
                                isDefaultTemplate,
                                templateReplacements,
                                allowUnresolvedTemplateReplacements
                            );
                        }
                        catch (InvalidOperationException) {
                            break;
                        }

                        yield return BuildSampleFromJsonValue(sampleTime, sampleTimeSource, key, currentElement.Value);
                        break;
                }
            }
        }


        /// <summary>
        /// Tries to extract the timestamp for samples from the specified JSON object.
        /// </summary>
        /// <param name="element">
        ///   The JSON object.
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
        /// <param name="pointer">
        ///   The JSON pointer for the element that is currently being processed.
        /// </param>
        /// <param name="elementStack">
        ///   The stack of objects with the current object being processed at the top and the root 
        ///   object in the hierarchy at the bottom.
        /// </param>
        /// <param name="recursive">
        ///   When <see langword="true"/>, all matching replacements from all levels of the 
        ///   <paramref name="elementStack"/> will be returned, instead of just looking at the 
        ///   object on top of the stack.
        /// </param>
        /// <param name="pathSeparator">
        ///   The path separator to use when joining multiple replacements together in recursive 
        ///   mode.
        /// </param>
        /// <param name="template">
        ///   The sample key template.
        /// </param>
        /// <param name="isDefaultTemplate">
        ///   Specifies if <paramref name="template"/> is equal to <see cref="TimeSeriesExtractorOptions.DefaultTemplate"/>.
        /// </param>
        /// <param name="defaultReplacements">
        ///   The default placeholder replacement values to use, if a referenced property does not 
        ///   exist on <paramref name="parentObject"/>.
        /// </param>
        /// <param name="allowUnresolvedReplacements">
        ///   When <see langword="false"/>, failure to replace a placeholder in the <paramref name="template"/> 
        ///   will result in an <see cref="InvalidOperationException"/> being thrown.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        private static string BuildSampleKeyFromTemplate(
            string pointer,
            IEnumerable<KeyValuePair<string?, JsonElement>> elementStack,
            bool recursive,
            string pathSeparator,
            string template,
            bool isDefaultTemplate,
            Func<string, string?>? defaultReplacements,
            bool allowUnresolvedReplacements
        ) {
            string GetFullPropertyName(bool forceLocalName = false) {
                if (!recursive || forceLocalName) {
                    return pointer.Substring(pointer.LastIndexOf('/') + 1);
                }

                return string.Equals(pathSeparator, TimeSeriesExtractorOptions.DefaultPathSeparator, StringComparison.Ordinal)
                    ? pointer.TrimStart('/')
                    : pointer.TrimStart('/').Replace("/", pathSeparator);
            }

            if (isDefaultTemplate) {
                // Fast path for default template.
                return GetFullPropertyName();
            }

            var closestObject = elementStack.FirstOrDefault(x => x.Value.ValueKind == JsonValueKind.Object);

            KeyValuePair<string?, JsonElement>[] elementStackInHierarchyOrder = null!;

            KeyValuePair<string?, JsonElement>[] GetElementStackInHierarchyOrder() {
                elementStackInHierarchyOrder ??= recursive
                    ? elementStack.Reverse().ToArray()
                    : Array.Empty<KeyValuePair<string?, JsonElement>>();
                return elementStackInHierarchyOrder;
            }

            return s_tagNameTemplateMatcher.Replace(template, m => {
                var pName = m.Groups["property"].Value;

                if (string.Equals(pName, "$prop", StringComparison.Ordinal) || string.Equals(pName, "$prop-local", StringComparison.Ordinal)) {
                    return GetFullPropertyName(string.Equals(pName, "$prop-local", StringComparison.Ordinal)) ?? m.Value;
                }

                if (recursive) {
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
                        return string.Join(pathSeparator, propVals);
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
                var replacement = defaultReplacements?.Invoke(pName);
                if (replacement == null && !allowUnresolvedReplacements) {
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

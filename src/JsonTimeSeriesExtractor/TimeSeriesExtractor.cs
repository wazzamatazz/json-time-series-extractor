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
    public sealed partial class TimeSeriesExtractor {

#if NET8_0_OR_GREATER
        /// <summary>
        /// Gets a regular expression matcher for JSON property name references in sample key templates.
        /// </summary>
        /// <returns>
        ///   A <see cref="Regex"/> instance.
        /// </returns>
        [GeneratedRegex(@"\{(?<property>[^\}]+?)\}", RegexOptions.Singleline)]
        private static partial Regex GetSampleKeyTemplateMatcher();

#else
        /// <summary>
        /// Matches JSON property name references in sample key templates.
        /// </summary>
        private static readonly Regex s_sampleKeyTemplateMatcher = new Regex(@"\{(?<property>[^\}]+?)\}", RegexOptions.Singleline);

        /// <summary>
        /// Gets a regular expression matcher for JSON property name references in sample key templates.
        /// </summary>
        /// <returns>
        ///   A <see cref="Regex"/> instance.
        /// </returns>
        private static Regex GetSampleKeyTemplateMatcher() => s_sampleKeyTemplateMatcher;
#endif

        /// <summary>
        /// Single-level wildcard character in a JSON Pointer path.
        /// </summary>
        public const string SingleLevelWildcard = "+";

        /// <summary>
        /// Multi-level wildcard character in a JSON Pointer path.
        /// </summary>
        /// <remarks>
        ///   Multi-level wildcards are only valid in the final segment of a JSON Pointer path.
        /// </remarks>
        public const string MultiLevelWildcard = "#";

        /// <summary>
        /// Single-character wildcard character in a JSON Pointer path.
        /// </summary>
        public const string SingleCharacterWildcard = "?";

        /// <summary>
        /// Multi-character wildcard character in a JSON Pointer path.
        /// </summary>
        public const string MultiCharacterWildcard = "*";


        /// <summary>
        /// Creates a property matcher function compatible with <see cref="TimeSeriesExtractorOptions.IncludeProperty"/> 
        /// that includes and/or excludes properties matching the specified pointers.
        /// </summary>
        /// <param name="pointersToInclude">
        ///   The JSON pointers to properties to include. If not <see langword="null"/>, only 
        ///   properties that match an entry in this list will be included. Otherwise, properties 
        ///   will be included unless they match an entry in <paramref name="pointersToExclude"/>.
        /// </param>
        /// <param name="pointersToExclude">
        ///   The JSON pointers to properties to exclude.
        /// </param>
        /// <param name="allowWildcards">
        ///   Specifies if MQTT-style wildcard patterns are allowed in the specified JSON pointer 
        ///   paths.
        /// </param>
        /// <returns>
        ///   A function that returns <see langword="true"/> if a <see cref="TimeSeriesSample"/> 
        ///   should be generated for the specified property or <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   When wildcard patterns are enabled via the <paramref name="allowWildcards"/> parameter, 
        ///   segments in <see cref="JsonPointer"/> instances can specify either pattern match 
        ///   wildcards (i.e. <c>?</c> for a single-character wildcard, and <c>*</c> for a 
        ///   multi-character wildcard) or MQTT-style wildcard characters (i.e. <c>+</c> for a 
        ///   single-level wildcard, and <c>#</c> for a multi-level wildcard).
        /// </para>
        /// 
        /// <para>
        ///   The two matching styles are mutually exclusive; if a pointer path contains single- 
        ///   or multi-character wildcard characters the path is assumed to be a pattern match, 
        ///   and MQTT-style wildcards are treated as literal characters. For example, 
        ///   <c>/foo/+/bar</c> is treated as an MQTT-style match, but <c>/foo/+/*</c> is treated 
        ///   as a regular pattern match.
        /// </para>
        /// 
        /// <para>
        ///   In an MQTT-style match expression, the multi-level wildcard character is only valid 
        ///   in the final segment of the pointer path. For example, <c>/foo/bar/#</c> is a valid 
        ///   MQTT match expression, but <c>/foo/#/bar</c> is not.
        /// </para>
        /// 
        /// <para>
        ///   In an MQTT-style match expression, you cannot specify both wildcard and non-wildcard 
        ///   characters in the same pointer segment. For example, <c>/foo/bar+/baz</c> is not a 
        ///   valid MQTT match expression and will be interpreted as a literal JSON Pointer path.
        /// </para>
        /// 
        /// </remarks>
        public static Func<JsonPointer, bool> CreateJsonPointerMatchDelegate(IEnumerable<JsonPointer>? pointersToInclude, IEnumerable<JsonPointer>? pointersToExclude, bool allowWildcards = false) {
            Predicate<JsonPointer>? includePredicate = null;
            Predicate<JsonPointer>? excludePredicate = null;

            if (pointersToInclude != null) { 
                includePredicate = CreateJsonPointerMatchDelegate(pointersToInclude, allowWildcards);
            }

            if (pointersToExclude != null) {
                excludePredicate = CreateJsonPointerMatchDelegate(pointersToExclude, allowWildcards);
            }

            if (includePredicate == null && excludePredicate == null) {
                return _ => true;
            }

            return pointer => {
                if (excludePredicate != null && excludePredicate.Invoke(pointer)) {
                    return false;
                }

                if (includePredicate != null) {
                    return includePredicate.Invoke(pointer);
                }

                return true;
            };
        }


        /// <summary>
        /// Creates a property matcher function compatible with <see cref="TimeSeriesExtractorOptions.IncludeProperty"/> 
        /// that includes and/or excludes properties matching the specified pointers.
        /// </summary>
        /// <param name="pointersToInclude">
        ///   The JSON pointers for properties to include. If not <see langword="null"/>, only 
        ///   properties that match an entry in this list will be included. Otherwise, properties 
        ///   will be included unless they match an entry in <paramref name="pointersToExclude"/>.
        /// </param>
        /// <param name="pointersToExclude">
        ///   The JSON pointers to properties to exclude.
        /// </param>
        /// <param name="allowWildcards">
        ///   Specifies if MQTT-style wildcard patterns are allowed in the specified JSON pointer 
        ///   paths.
        /// </param>
        /// <returns>
        ///   A function that returns <see langword="true"/> if a <see cref="TimeSeriesSample"/> 
        ///   should be generated for the specified property or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Any entry in <paramref name="pointersToInclude"/> or <paramref name="pointersToExclude"/> 
        ///   is <see langword="null"/>.
        /// </exception>
        /// <exception cref="PointerParseException">
        ///   Any entry in <paramref name="pointersToInclude"/> or <paramref name="pointersToExclude"/> 
        ///   is not a valid JSON pointer.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   When wildcard patterns are enabled via the <paramref name="allowWildcards"/> parameter, 
        ///   segments in <see cref="JsonPointer"/> instances can specify either pattern match 
        ///   wildcards (i.e. <c>?</c> for a single-character wildcard, and <c>*</c> for a 
        ///   multi-character wildcard) or MQTT-style wildcard characters (i.e. <c>+</c> for a 
        ///   single-level wildcard, and <c>#</c> for a multi-level wildcard).
        /// </para>
        /// 
        /// <para>
        ///   The two matching styles are mutually exclusive; if a pointer path contains single- 
        ///   or multi-character wildcard characters the path is assumed to be a pattern match, 
        ///   and MQTT-style wildcards are treated as literal characters. For example, 
        ///   <c>/foo/+/bar</c> is treated as an MQTT-style match, but <c>/foo/+/*</c> is treated 
        ///   as a regular pattern match.
        /// </para>
        /// 
        /// <para>
        ///   In an MQTT-style match expression, the multi-level wildcard character is only valid 
        ///   in the final segment of the pointer path. For example, <c>/foo/bar/#</c> is a valid 
        ///   MQTT match expression, but <c>/foo/#/bar</c> is not.
        /// </para>
        /// 
        /// <para>
        ///   In an MQTT-style match expression, you cannot specify both wildcard and non-wildcard 
        ///   characters in the same pointer segment. For example, <c>/foo/bar+/baz</c> is not a 
        ///   valid MQTT match expression and will be interpreted as a literal JSON Pointer path.
        /// </para>
        /// 
        /// </remarks>
        public static Func<JsonPointer, bool> CreateJsonPointerMatchDelegate(IEnumerable<string>? pointersToInclude, IEnumerable<string>? pointersToExclude, bool allowWildcards = false) {
            var includes = pointersToInclude?.Select(JsonPointer.Parse)?.ToArray();
            var excludes = pointersToExclude?.Select(JsonPointer.Parse)?.ToArray();
            return CreateJsonPointerMatchDelegate(includes, excludes, allowWildcards);
        }


        /// <summary>
        /// Creates a predicate that tests if a JSON pointer matches against any of the specified JSON pointers.
        /// </summary>
        /// <param name="matchPointers">
        ///   The JSON pointers to match against.
        /// </param>
        /// <param name="allowWildcards">
        ///   Specifies if pattern match or MQTT-style match expressions are allowed in the 
        ///   <paramref name="matchPointers"/>.
        /// </param>
        /// <returns>
        ///   A predicate that returns <see langword="true"/> if the specified JSON pointer matches 
        ///   any of the <paramref name="matchPointers"/>.
        /// </returns>
        private static Predicate<JsonPointer> CreateJsonPointerMatchDelegate(IEnumerable<JsonPointer> matchPointers, bool allowWildcards) {
            if (!allowWildcards) {
                // No wildcards: return a simple predicate that checks for equality.
                return pointer => matchPointers.Any(x => x != null && x.Equals(pointer));
            }

            var pointersWithWildcardStatus = matchPointers.Select(x => {
                var containsPatternMatchWildcard = ContainsPatternMatchWildcard(x);
                return new {
                    Pointer = x,
                    ContainsPatternMatchExpression = containsPatternMatchWildcard,
                    ContainsMqttMatchExpression = !containsPatternMatchWildcard && (ContainsSingleLevelMqttWildcard(x) || ContainsMultiLevelMqttWildcard(x))
                };
            }).ToArray();

            if (pointersWithWildcardStatus.All(x => !x.ContainsPatternMatchExpression && !x.ContainsMqttMatchExpression)) {
                // No wildcards are present: return a simple predicate that checks for equality.
                return pointer => matchPointers.Any(x => x != null && x.Equals(pointer));
            }

            // Wildcards are present: return a more complex predicate that checks for wildcard matches.
            var predicates = new List<Predicate<JsonPointer>>();

            foreach (var matchPointer in pointersWithWildcardStatus) {
                if (matchPointer == null) {
                    continue;
                }

                if (!matchPointer.ContainsPatternMatchExpression && !matchPointer.ContainsMqttMatchExpression) {
                    // Pointer does not contain wildcards: add a simple predicate that checks for equality.
                    predicates.Add(pointer => matchPointer.Equals(pointer));
                    continue;
                }

                // Pointer contains wildcards: add a predicate that checks for wildcard matches.

                if (matchPointer.ContainsPatternMatchExpression) {
                    // Use pattern match wildcards.
#if NETCOREAPP
                    var pattern = Regex.Escape(matchPointer.Pointer.ToString())
                        .Replace(@"\*", ".*", StringComparison.Ordinal)
                        .Replace(@"\?", ".", StringComparison.Ordinal);
#else
                    var pattern = Regex.Escape(matchPointer.Pointer.ToString())
                        .Replace(@"\*", ".*")
                        .Replace(@"\?", ".");
#endif

                    var regex = new Regex($"^{pattern}$", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
                    predicates.Add(pointer => regex.IsMatch(pointer.ToString()));
                    continue;
                }

                // For each pointer segment, we'll check if that segment is a single-level or
                // multi-level wildcard.
                var matchSegments = matchPointer.Pointer.Segments.Reverse().Select((x, i) => new {
                    Segment = x,
                    IsSingleLevelWildcard = x.Value.Equals(SingleLevelWildcard, StringComparison.Ordinal),
                    // Multi-level wildcard is only valid in the final segment, which is at index
                    // 0 in our reversed segment list.
                    IsMultiLevelWildcard = i == 0 && x.Value.Equals(MultiLevelWildcard, StringComparison.Ordinal)
                }).Reverse().ToArray();

                predicates.Add(pointer => {
                    if (pointer.Segments.Length < matchSegments.Length) {
                        // The pointer has fewer segments than the match pattern; no match.
                        return false;
                    }

                    if (pointer.Segments.Length > matchSegments.Length) {
                        // The pointer has more segments than the match pattern; no match unless
                        // the last match segment is a multi-level wildcard.
                        return matchSegments[matchSegments.Length - 1].IsMultiLevelWildcard;
                    }

                    for (var i = 0; i < pointer.Segments.Length; i++) {
                        var pointerSegment = pointer.Segments[i];
                        var matchSegment = matchSegments[i];

                        if (matchSegment.IsSingleLevelWildcard) {
                            // Single-level wildcard: match any segment.
                            continue;
                        }

                        if (matchSegment.IsMultiLevelWildcard) {
                            // Multi-level wildcard: match all remaining pointer segments.
                            break;
                        }

                        // Check for an exact match between the current pointer segment and match
                        // segment.
                        if (!pointerSegment.Equals(matchSegment.Segment)) {
                            return false;
                        }
                    }

                    return true;
                });
            }

            return predicates.Count == 0 
                ? _ => true
                : pointer => predicates.Any(x => x.Invoke(pointer));

            bool ContainsPatternMatchWildcard(JsonPointer p) {
                var s = p.ToString();
                return s.Contains(SingleCharacterWildcard) || s.Contains(MultiCharacterWildcard);
            };

            bool ContainsSingleLevelMqttWildcard(JsonPointer p) => p.Segments.Any(x => x.Value.Equals(SingleLevelWildcard, StringComparison.Ordinal));

            bool ContainsMultiLevelMqttWildcard(JsonPointer p) => p.Segments[p.Segments.Length - 1].Value.Equals(MultiLevelWildcard, StringComparison.Ordinal);

        }


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
        ///   An <see cref="IEnumerable{TimeSeriesSample}"/> that will emit the parsed samples.
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
        ///   An <see cref="IEnumerable{TimeSeriesSample}"/> that will emit the parsed samples.
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
                var newElement = options.StartAt!.Evaluate(element);
                if (newElement == null) {
                    yield break;
                }

                element = newElement.Value;
            }

            foreach (var value in GetSamplesFromRootElement(element, options)) {
                yield return value;
            }
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
        ///   An <see cref="IEnumerable{TimeSeriesSample}"/> that will emit the parsed samples.
        /// </returns>
        /// <remarks>
        ///   <paramref name="element"/> must have a <see cref="JsonValueKind"/> of <see cref="JsonValueKind.Object"/> 
        ///   or <see cref="JsonValueKind.Array"/>.
        /// </remarks>
        /// <exception cref="ValidationException">
        ///   <paramref name="options"/> is not valid.
        /// </exception>
        /// <seealso cref="TimeSeriesExtractorOptions"/>
        private static IEnumerable<TimeSeriesSample> GetSamplesFromRootElement(JsonElement element, TimeSeriesExtractorOptions options) {
            if (element.ValueKind == JsonValueKind.Array) {
                foreach (var item in element.EnumerateArray()) {
                    foreach (var value in GetSamplesFromRootElement(item, options)) {
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

            if (context.Options.TimestampProperty == null || !TryGetTimestamp(element, context.Options.TimestampProperty, context.Options, out var sampleTime)) {
                var ts = options.GetDefaultTimestamp?.Invoke();
                if (ts == null) {
                    defaultTimestamp = new ParsedTimestamp(DateTimeOffset.UtcNow, TimestampSource.CurrentTime, null);
                }
                else {
                    defaultTimestamp = new ParsedTimestamp(ts.Value, TimestampSource.FallbackProvider, null);
                }
            }
            else {
                defaultTimestamp = new ParsedTimestamp(sampleTime, TimestampSource.Document, context.Options.TimestampProperty);
            }
            context.TimestampStack.Push(defaultTimestamp);

            context.ElementStack.Push(new ElementStackEntry(null, element, false));

            foreach (var prop in element.EnumerateObject()) {
                context.ElementStack.Push(new ElementStackEntry(prop.Name, prop.Value, false));

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

            var currentElement = context.ElementStack.Peek();

            if (!context.Options.Recursive || (context.Options.MaxDepth > 0 && currentRecursionDepth >= context.Options.MaxDepth)) {
                // We are not using recursive mode, or we have exceeded the maximum recursion
                // depth; build a sample with the current element. When we have exceeded the
                // maximum recursion depth, the value will be the serialized JSON of the element
                // if the element is an object or an array.
                var sample = BuildSample(pointer, currentElement.Element);
                if (!sample.HasValue) {
                    yield break;
                }

                yield return sample.Value;
            }
            else {
                // We are doing recursive processing and have not exceeded the maximum recursion
                // depth, so continue as normal.

                switch (currentElement.Element.ValueKind) {
                    case JsonValueKind.Object:
                        var popTimestamp = false;
                        if (context.Options.AllowNestedTimestamps && context.Options.TimestampProperty != null && TryGetTimestamp(currentElement.Element, context.Options.TimestampProperty, context.Options, out var sampleTime)) {
                            context.TimestampStack.Push(new ParsedTimestamp(sampleTime, TimestampSource.Document, pointer.Combine(context.Options.TimestampProperty)));
                            popTimestamp = true;
                        }

                        foreach (var item in currentElement.Element.EnumerateObject()) {
                            context.ElementStack.Push(new ElementStackEntry(item.Name, item.Value, false));

                            foreach (var val in GetSamplesCore(context, currentRecursionDepth + 1)) {
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

                        foreach (var item in currentElement.Element.EnumerateArray()) {
                            ++index;

                            context.ElementStack.Push(new ElementStackEntry(index.ToString(CultureInfo.InvariantCulture), item, true));

                            foreach (var val in GetSamplesCore(context, currentRecursionDepth + 1)) {
                                yield return val;
                            }

                            context.ElementStack.Pop();
                        }
                        break;
                    default:
                        if (context.ElementStack.Count == 0) {
                            yield break;
                        }

                        var sample = BuildSample(pointer, currentElement.Element);
                        if (sample.HasValue) {
                            yield return sample.Value;
                        }
                        break;
                }
            }

            TimeSeriesSample? BuildSample(JsonPointer pointer, JsonElement element) {
                // Check if this element should be included.
                if (!context.IncludeElement.Invoke(pointer)) {
                    return null;
                }

                try {
                    var key = BuildSampleKeyFromTemplate(
                        context.Options,
                        pointer,
                        context.ElementStack,
                        context.IsDefaultSampleKeyTemplate);

                    var timestamp = context.TimestampStack.Peek();
                    return BuildSampleFromJsonValue(timestamp.Timestamp, timestamp.Source, key, element);
                }
                catch (InvalidOperationException) {
                    return null;
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
            IEnumerable<ElementStackEntry> elementStack,
            bool isDefaultTemplate
            
        ) {
            ElementStackEntry[] elementStackInHierarchyOrder = null!;

            ElementStackEntry[] GetElementStackInHierarchyOrder() {
                elementStackInHierarchyOrder ??= options.Recursive
                    ? elementStack.Reverse().ToArray()
                    : Array.Empty<ElementStackEntry>();
                return elementStackInHierarchyOrder;
            }

            string GetFullPropertyName(bool forceLocalName = false) {
                if (!options.Recursive || forceLocalName) {
                    return pointer.Segments.Last().Value;
                }

                if (options.IncludeArrayIndexesInSampleKeys) {
                    return string.Equals(options.PathSeparator, TimeSeriesExtractorConstants.DefaultPathSeparator, StringComparison.Ordinal)
                        ? pointer.ToString().TrimStart('/')
                        : pointer.ToString().TrimStart('/').Replace("/", options.PathSeparator);
                }

                // Remove array indexes from the path. In order to do this, we construct the key
                // from the element stack instead of using the pointer directly. This allows us
                // to skip any elements that are array entries. This ensures that we don't
                // accidentally omit object properties that use integer values as the property name.
                return string.Join(options.PathSeparator, GetElementStackInHierarchyOrder().Where(x => x.Key != null && !x.IsArrayItem).Select(x => x.Key));
            }

            if (isDefaultTemplate) {
                // Fast path for default template.
                return GetFullPropertyName();
            }

            var closestObject = elementStack.FirstOrDefault(x => x.Element.ValueKind == JsonValueKind.Object);

            string GetElementDisplayValue(JsonElement el) => el.ValueKind == JsonValueKind.String ? el.GetString()! : el.GetRawText();

            return GetSampleKeyTemplateMatcher().Replace(options.Template, m => {
                var pName = m.Groups["property"].Value;

                if (string.Equals(pName, "$prop", StringComparison.Ordinal) || string.Equals(pName, "$prop-local", StringComparison.Ordinal)) {
                    return GetFullPropertyName(string.Equals(pName, "$prop-local", StringComparison.Ordinal)) ?? m.Value;
                }

                if (options.Recursive) {
                    // Recursive mode: try and get matching replacements from every object in the
                    // stack (starting from the root) and concatenate them using the path separator.

                    var propVals = new List<string>();
                    
                    foreach (var stackEntry in GetElementStackInHierarchyOrder()) {
                        if (stackEntry.Element.ValueKind == JsonValueKind.Object) {
                            if (!stackEntry.Element.TryGetProperty(pName, out var prop)) {
                                continue;
                            }

                            propVals.Add(GetElementDisplayValue(prop));
                        }
                    }

                    if (propVals.Count > 0) {
                        return string.Join(options.PathSeparator, propVals);
                    }
                }
                else {
                    // Non-recursive mode: use the nearest object to the item we are processing to
                    // try and get the referenced property.

                    if (closestObject.Element.ValueKind == JsonValueKind.Object && closestObject.Element.TryGetProperty(pName, out var prop)) {
                        return GetElementDisplayValue(prop);
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

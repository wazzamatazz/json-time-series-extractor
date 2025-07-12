using System;
using System.Buffers;
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
    public static partial class TimeSeriesExtractor {

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
        private static readonly Regex s_sampleKeyTemplateMatcher = new Regex(@"\{(?<property>[^\}]+?)\}", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Gets a regular expression matcher for JSON property name references in sample key templates.
        /// </summary>
        /// <returns>
        ///   A <see cref="Regex"/> instance.
        /// </returns>
        private static Regex GetSampleKeyTemplateMatcher() => s_sampleKeyTemplateMatcher;
#endif

        /// <summary>
        /// Single-level wildcard character in a JSON Pointer MQTT match expression.
        /// </summary>
        public const string SingleLevelMqttWildcard = "+";

        /// <summary>
        /// Multi-level wildcard character in a JSON Pointer MQTT match expression.
        /// </summary>
        /// <remarks>
        ///   Multi-level wildcards are only valid in the final segment of a JSON Pointer path.
        /// </remarks>
        public const string MultiLevelMqttWildcard = "#";

        /// <summary>
        /// Single-character wildcard character in a JSON Pointer pattern match expression.
        /// </summary>
        public const string SingleCharacterWildcard = "?";

        /// <summary>
        /// Multi-character wildcard character in a JSON Pointer pattern match expression.
        /// </summary>
        public const string MultiCharacterWildcard = "*";


        /// <summary>
        /// Creates a delegate compatible with <see cref="TimeSeriesExtractorOptions.CanProcessElement"/> 
        /// that includes and/or excludes JSON elements based on the provided options.
        /// </summary>
        /// <param name="options">
        ///   The options for the delegate.
        /// </param>
        /// <returns>
        ///   A function that returns <see langword="true"/> if a <see cref="JsonElement"/> should 
        ///   be processed <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Any entry in <see cref="JsonPointerMatchDelegateOptions.PointersToInclude"/> or 
        ///   <see cref="JsonPointerMatchDelegateOptions.PointersToExclude"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Any entry in <see cref="JsonPointerMatchDelegateOptions.PointersToInclude"/> or 
        ///   <see cref="JsonPointerMatchDelegateOptions.PointersToExclude"/> is not a valid JSON 
        ///   pointer or pattern wildcard expression.
        /// </exception>
        public static JsonPointerMatchDelegate CreateJsonPointerMatchDelegate(JsonPointerMatchDelegateOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            JsonPointerMatchDelegate? includePredicate = null;
            JsonPointerMatchDelegate? excludePredicate = null;

            if (options.PointersToInclude != null) {
                includePredicate = CreateJsonPointerMatchDelegate(options.PointersToInclude, options.AllowWildcardExpressions, options.UseCompiledRegularExpressions);
            }

            if (options.PointersToExclude != null) {
                excludePredicate = CreateJsonPointerMatchDelegate(options.PointersToExclude, options.AllowWildcardExpressions, options.UseCompiledRegularExpressions);
            }

            if (includePredicate == null && excludePredicate == null) {
                return (_, _, _) => true;
            }

            return (context, pointer, element) => {
                if (excludePredicate != null && excludePredicate.Invoke(context, pointer, element)) {
                    return false;
                }

                return includePredicate == null || includePredicate.Invoke(context, pointer, element);
            };
        }


        /// <summary>
        /// Creates a predicate that tests if a JSON pointer matches against any of the specified JSON pointers.
        /// </summary>
        /// <param name="matchRules">
        ///   The JSON pointers to match against.
        /// </param>
        /// <param name="allowWildcards">
        ///   Specifies if pattern match or MQTT-style match expressions are allowed in the 
        ///   <paramref name="matchRules"/>.
        /// </param>
        /// <param name="useCompiledRegularExpressions">
        ///   When <see langword="true"/>, regular expressions generated from pattern match expressions
        ///   in <paramref name="matchRules"/> will be created using <see cref="RegexOptions.Compiled"/>
        /// </param>
        /// <returns>
        ///   A predicate that returns <see langword="true"/> if the specified JSON pointer matches 
        ///   any of the <paramref name="matchRules"/>.
        /// </returns>
        private static JsonPointerMatchDelegate CreateJsonPointerMatchDelegate(IEnumerable<JsonPointerMatch> matchRules, bool allowWildcards, bool useCompiledRegularExpressions) {
            // Optimised predicate construction: process each rule only once and avoid redundant predicates.
            var matchRuleArray = matchRules as JsonPointerMatch[] ?? matchRules.ToArray();
            var nonWildcardPointers = new List<JsonPointer>();
            var wildcardPredicates = new List<JsonPointerMatchDelegate>();

            foreach (var matchRule in matchRuleArray) {
                // Ignore rules with no pointer and no raw value.
                if (matchRule.Pointer == null && string.IsNullOrWhiteSpace(matchRule.RawValue)) {
                    continue;
                }

                // Non-wildcard rules: collect for fast lookup.
                if (!allowWildcards || !matchRule.IsWildcardMatchRule) {
                    // These are exact or partial pointer matches, handled together for efficiency.
                    nonWildcardPointers.Add(matchRule.Pointer!);
                    continue;
                }

                // Pattern wildcard rules: compile regex once per rule.
                if (matchRule.IsPatternWildcardMatchRule) {
                    // Convert wildcard pattern to regex. '*' matches any sequence, '?' matches any single character.
#if NETCOREAPP
                    var pattern = Regex.Escape(matchRule.RawValue!)
                        .Replace(@"\*", ".*", StringComparison.Ordinal)
                        .Replace(@"\?", ".", StringComparison.Ordinal);
#else
                    var pattern = Regex.Escape(matchRule.RawValue!)
                        .Replace(@"\*", ".*")
                        .Replace(@"\?", ".");
#endif
                    var regex = new Regex(
                        $"^{pattern}$",
                        useCompiledRegularExpressions
                            ? RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
                            : RegexOptions.IgnoreCase | RegexOptions.Singleline,
                        TimeSpan.FromSeconds(1));
                    wildcardPredicates.Add((context, pointer, element) => {
                        // If the JSON element is an object or an array and we are running in recursive mode,
                        // always return true if we have not reached our maximum recursion depth. This is required
                        // because we perform a regex match against the entire pointer string instead of doing a
                        // segment-by-segment match like we do with MQTT-style expressions so we don't want to
                        // accidentally prune non-matching pointers too early.
                        if (context.Options.Recursive && (context.ElementStack.Count < context.MaxDepth) && (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)) {
                            return true;
                        }
                        return regex.IsMatch(pointer.ToString());
                    });
                    continue;
                }

                // MQTT-style wildcard match: build segment matcher once per rule.
                // For each pointer segment, check if that segment is a single-level or multi-level wildcard.
                // Multi-level wildcards are only valid in the final segment (index 0 in reversed segment list).
                // Avoid double Reverse and ToArray allocations by using a for-loop and Span for small arrays.
                var pointerSegments = matchRule.Pointer!;
                var matchSegments = new (string Segment, bool IsSingleLevelWildcard, bool IsMultiLevelWildcard)[pointerSegments.Count];
                for (var i = 0; i < pointerSegments.Count; i++) {
                    var segment = pointerSegments[i];
                    matchSegments[i] = (
                        Segment: segment,
                        IsSingleLevelWildcard: segment.Equals(SingleLevelMqttWildcard, StringComparison.Ordinal),
                        IsMultiLevelWildcard: i == pointerSegments.Count - 1 && segment.Equals(MultiLevelMqttWildcard, StringComparison.Ordinal)
                    );
                }
                
                wildcardPredicates.Add((context, pointer, element) => {
                    // Special handling for when the element pointer has fewer segments than the match pointer.
                    if (pointer.Count < matchSegments.Length) {
                        // We're not running in recursive mode so definitely no match.
                        if (!context.Options.Recursive) {
                            return false;
                        }
                        // The element is not an object or an array so definitely no match.
                        if (element.ValueKind != JsonValueKind.Object && element.ValueKind != JsonValueKind.Array) {
                            return false;
                        }
                        // We have reached our maximum recursion depth so definitely no match.
                        // We use > in the comparison above instead of >= because the element stack will always contain the root element.
                        if (context.Options.MaxDepth >= 1 && context.ElementStack.Count > context.Options.MaxDepth) {
                            return false;
                        }
                    }
                    var elementPointerIsLongerThanMatchPointer = pointer.Count > matchSegments.Length;
                    // The pointer has more segments than the match pattern; definitely no match unless the last match segment is a multi-level wildcard.
                    if (elementPointerIsLongerThanMatchPointer) {
                        if (!matchSegments[^1].IsMultiLevelWildcard) {
                            return false;
                        }
                    }
                    // Only ever need to test the final segment of the element pointer, as previous segments were tested in previous iterations.
                    var pointerSegmentIndex = pointer.Count - 1;
                    var matchSegment = pointerSegmentIndex >= matchSegments.Length
                        ? matchSegments[^1]
                        : matchSegments[pointerSegmentIndex];
                    
                    // Single-level wildcard: match the current segment unless the element pointer has more segments than the match pointer and we have advanced beyond the end of the match pointer.
                    if (matchSegment.IsSingleLevelWildcard) {
                        if (elementPointerIsLongerThanMatchPointer && pointerSegmentIndex >= matchSegments.Length) {
                            return false;
                        }
                        return true;
                    }
                    
                    // Multi-level wildcard: always match the current segment.
                    if (matchSegment.IsMultiLevelWildcard) {
                        return true;
                    }
                    
                    // Not a wildcard; check if the segment values match.
                    return pointer[pointerSegmentIndex].Equals(matchSegment.Segment);
                });
            }

            // Use HashSet for fast exact pointer lookup if there are many non-wildcard rules.
            HashSet<JsonPointer>? nonWildcardPointerSet = null;
            if (nonWildcardPointers.Count > 8) {
                nonWildcardPointerSet = new HashSet<JsonPointer>(nonWildcardPointers);
            }

            return (context, pointer, element) => {
                // First, check non-wildcard rules (exact or partial match).
                if (nonWildcardPointerSet != null) {
                    if (nonWildcardPointerSet.Contains(pointer)) {
                        return true;
                    }
                } else if (nonWildcardPointers.Count > 0) {
                    // For partial matches, check each pointer in the list.
                    foreach (var p in nonWildcardPointers) {
                        if (MatchExactOrPartialJsonPointer(context, p, pointer, element)) {
                            return true;
                        }
                    }
                }
                // Then, check wildcard/pattern rules.
                foreach (var pred in wildcardPredicates) {
                    if (pred(context, pointer, element)) {
                        return true;
                    }
                }
                return false;
            };
        }


        /// <summary>
        /// Tests if the JSON pointer for the specified element matches the provided match pointer.
        /// </summary>
        /// <param name="context">
        ///   The context for the extraction.
        /// </param>
        /// <param name="matchPointer">
        ///   The match pointer.
        /// </param>
        /// <param name="elementPointer">
        ///   The pointer for the element that is currently being processed.
        /// </param>
        /// <param name="element">
        ///   The element that is currently being processed.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the element pointer matches the match pointer; otherwise, 
        ///   <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   If <paramref name="element"/> is an object or an array and we are running in 
        ///   recursive mode, we will also allow partial matches i.e. if the element pointer has 
        ///   fewer segments than the match pointer, we will treat it as a match if all of the 
        ///   element pointer segments match their equivalent match pointer segments.
        /// </remarks>
        private static bool MatchExactOrPartialJsonPointer(TimeSeriesExtractorContext context, JsonPointer? matchPointer, JsonPointer elementPointer, JsonElement element) {
            if (matchPointer == null) {
                return false;
            }

            if (matchPointer.Equals(elementPointer)) {
                return true;
            }

            if (context.Options.Recursive && (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array) && elementPointer.Count < matchPointer.Count) {
                for (var i = 0; i < elementPointer.Count; i++) {
                    if (!matchPointer[i].Equals(elementPointer[i])) {
                        return false;
                    }
                }
                return true;
            }

            return false;
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

            if (options.StartAt.HasValue) {
                var newElement = options.StartAt.Value.Pointer.Evaluate(element);
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
            using var context = new TimeSeriesExtractorContext(options);

            ParsedTimestamp defaultTimestamp;

            if (!TryGetTimestamp(element, context.Options.TimestampProperty, context.Options, out var sampleTime)) {
                var ts = options.GetDefaultTimestamp?.Invoke();
                defaultTimestamp = ts == null 
                    ? new ParsedTimestamp(DateTimeOffset.UtcNow, TimestampSource.CurrentTime, null) 
                    : new ParsedTimestamp(ts.Value, TimestampSource.FallbackProvider, null);
            }
            else {
                defaultTimestamp = new ParsedTimestamp(sampleTime, TimestampSource.Document, context.Options.TimestampProperty);
            }
            context.TimestampStack.Push(defaultTimestamp);

            context.ElementStack.Push(new ElementStackEntry(null, element, false));

            foreach (var prop in element.EnumerateObject()) {
                context.ElementStack.Push(new ElementStackEntry(prop.Name, prop.Value, false));

                try {
                    foreach (var val in GetSamplesCore(context, 1, JsonPointer.Parse("/" + prop.Name))) {
                        yield return val;
                    }
                }
                finally {
                    context.ElementStack.Pop();
                }
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
        /// <param name="pointer">
        ///   The JSON pointer for the element that is currently being processed.
        /// </param>
        /// <returns>
        ///   The extracted tag values.
        /// </returns>
        private static IEnumerable<TimeSeriesSample> GetSamplesCore(
            TimeSeriesExtractorContext context,
            int currentRecursionDepth,
            JsonPointer pointer
        ) {
            var currentElement = context.ElementStack.Peek();
            if (!context.CanProcessElement(pointer, currentElement.Element)) {
                yield break;
            }

            if (!context.Options.Recursive || currentRecursionDepth >= context.MaxDepth) {
                var sample = BuildSample(context, pointer, currentElement.Element);
                if (!sample.HasValue) {
                    yield break;
                }
                yield return sample.Value;
            }
            else {
                switch (currentElement.Element.ValueKind) {
                    case JsonValueKind.Object:
                        foreach (var val in ProcessObjectElement(context, currentRecursionDepth, pointer, currentElement.Element)) {
                            yield return val;
                        }
                        break;
                    case JsonValueKind.Array:
                        foreach (var val in ProcessArrayElement(context, currentRecursionDepth, pointer, currentElement.Element)) {
                            yield return val;
                        }
                        break;
                    default:
                        if (context.ElementStack.Count == 0) {
                            yield break;
                        }
                        var sample = BuildSample(context, pointer, currentElement.Element);
                        if (sample.HasValue) {
                            yield return sample.Value;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Processes a JSON object element during sample extraction.
        /// </summary>
        private static IEnumerable<TimeSeriesSample> ProcessObjectElement(
            TimeSeriesExtractorContext context,
            int currentRecursionDepth,
            JsonPointer pointer,
            JsonElement element
        ) {
            // If nested timestamps are allowed and the current object contains a timestamp property,
            // push the new timestamp onto the stack for use by child elements.
            var popTimestamp = false;
            if (context.Options is { AllowNestedTimestamps: true, TimestampProperty: not null } && TryGetTimestamp(element, context.Options.TimestampProperty, context.Options, out var sampleTime)) {
                context.TimestampStack.Push(new ParsedTimestamp(sampleTime, TimestampSource.Document, pointer.Combine(context.Options.TimestampProperty!)));
                popTimestamp = true;
            }
            // Iterate over each property in the object and process recursively.
            foreach (var item in element.EnumerateObject()) {
                context.ElementStack.Push(new ElementStackEntry(item.Name, item.Value, false));
                foreach (var val in GetSamplesCore(context, currentRecursionDepth + 1, pointer.Combine(item.Name))) {
                    yield return val;
                }
                context.ElementStack.Pop();
            }
            // Pop the timestamp if one was pushed for this object.
            if (popTimestamp) {
                context.TimestampStack.Pop();
            }
        }

        /// <summary>
        /// Processes a JSON array element during sample extraction.
        /// </summary>
        private static IEnumerable<TimeSeriesSample> ProcessArrayElement(
            TimeSeriesExtractorContext context,
            int currentRecursionDepth,
            JsonPointer pointer,
            JsonElement element
        ) {
            // Iterate over each item in the array, tracking the index for pointer construction.
            var index = -1;
            foreach (var item in element.EnumerateArray()) {
                ++index;
                // Push the array item onto the stack, marking it as an array entry.
                context.ElementStack.Push(new ElementStackEntry(index.ToString(CultureInfo.InvariantCulture), item, true));
                foreach (var val in GetSamplesCore(context, currentRecursionDepth + 1, pointer.Combine(index))) {
                    yield return val;
                }
                context.ElementStack.Pop();
            }
        }

        /// <summary>
        /// Builds a time series sample from the specified JSON element.
        /// </summary>
        private static TimeSeriesSample? BuildSample(TimeSeriesExtractorContext context, JsonPointer ptr, JsonElement element) {
            // Only build a sample if the element passes the processing predicate.
            if (!context.CanProcessElement(ptr, element)) {
                return null;
            }
            try {
                // Build the sample key using the template and context.
                var key = BuildSampleKeyFromTemplate(context, ptr);
                // Use the current timestamp from the stack.
                var timestamp = context.TimestampStack.Peek();
                // Construct the sample from the JSON value.
                return BuildSampleFromJsonValue(timestamp.Timestamp, timestamp.Source, key, element);
            }
            catch (InvalidOperationException) {
                // If the template cannot be resolved, skip this sample.
                return null;
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
        private static bool TryGetTimestamp(JsonElement element, JsonPointer? pointer, TimeSeriesExtractorOptions options, out DateTimeOffset value) {
            value = default;

            if (pointer == null || element.ValueKind != JsonValueKind.Object || options.TimestampProperty == null) {
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
        /// Builds a key for a <see cref="TimeSeriesSample"/> from the template defined in the extractor context options.
        /// </summary>
        /// <param name="context">
        ///   The extractor context.
        /// </param>
        /// <param name="pointer">
        ///   The JSON pointer for the element that is currently being processed.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        private static string BuildSampleKeyFromTemplate(
            TimeSeriesExtractorContext context,
            JsonPointer pointer
            
        ) {
            ElementStackEntry[] elementStackInHierarchyOrder = null!;
            ElementStackEntry[] elementStackInHierarchyOrderNoArrays = null!;
            var elementStackInHierarchyOrderNoArraysCount = 0;
            
            var options = context.Options;
            
            try {
                if (context.IsDefaultSampleKeyTemplate) {
                    // Fast path for default template.
                    return GetFullPropertyName();
                }

                if (!context.SampleKeyTemplateContainsPlaceholders) {
                    // No template replacements: this would be unusual but return the template as-is.
                    return options.Template;
                }
                
                return GetSampleKeyTemplateMatcher().Replace(options.Template, m => {
                    var pName = m.Groups["property"].Value;

                    if (string.Equals(pName, "$prop", StringComparison.Ordinal) || string.Equals(pName, "$prop-local", StringComparison.Ordinal)) {
                        return GetFullPropertyName(string.Equals(pName, "$prop-local", StringComparison.Ordinal));
                    }

                    if (string.Equals(pName, "$prop-path", StringComparison.Ordinal)) {
                        return GetPropertyPath();
                    }

                    if (options.Recursive) {
                        // Recursive mode: try and get matching replacements from every object in the
                        // stack (starting from the root) and concatenate them using the path separator.

                        var hierarchy = GetElementStackInHierarchyOrder();
                        var propVals = new List<string>(hierarchy.Count);

                        foreach (var stackEntry in hierarchy) {
                            if (stackEntry.Element.ValueKind != JsonValueKind.Object) {
                                continue;
                            }

                            if (!stackEntry.Element.TryGetProperty(pName, out var prop)) {
                                continue;
                            }

                            propVals.Add(GetElementDisplayValue(prop));
                        }

                        if (propVals.Count > 0) {
                            return string.Join(options.PathSeparator, propVals);
                        }
                    }
                    else {
                        // Non-recursive mode: use the nearest object to the item we are processing to
                        // try and get the referenced property.

                        var closestObject = context.ElementStack.FirstOrDefault(x => x.Element.ValueKind == JsonValueKind.Object);
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
            finally {
                if (elementStackInHierarchyOrder != null) {
                    ArrayPool<ElementStackEntry>.Shared.Return(elementStackInHierarchyOrder);
                }
                if (elementStackInHierarchyOrderNoArrays != null) {
                    ArrayPool<ElementStackEntry>.Shared.Return(elementStackInHierarchyOrderNoArrays);
                }
            }
            
            static string GetElementDisplayValue(JsonElement el) => el.ValueKind == JsonValueKind.String 
                ? el.GetString()! 
                : el.GetRawText();
            
            ArraySegment<ElementStackEntry> GetElementStackInHierarchyOrder() {
                if (elementStackInHierarchyOrder == null) {
                    if (!options.Recursive || context.ElementStack.Count == 0) {
#if NETCOREAPP
                        return ArraySegment<ElementStackEntry>.Empty;
#else
                        return default;
#endif
                    }
                    
                    elementStackInHierarchyOrder = ArrayPool<ElementStackEntry>.Shared.Rent(context.ElementStack.Count);
                    var index = 0;
                    foreach (var item in context.ElementStack) {
                        elementStackInHierarchyOrder[index++] = item;
                    }
                }
                
                return new ArraySegment<ElementStackEntry>(elementStackInHierarchyOrder, 0, context.ElementStack.Count);
            }

            ArraySegment<ElementStackEntry> GetElementStackInHierarchyOrderNoArrays() {
                if (elementStackInHierarchyOrderNoArrays == null) {
                    if (!options.Recursive || context.ElementStack.Count == 0) {
#if NETCOREAPP
                        return ArraySegment<ElementStackEntry>.Empty;
#else
                        return default;
#endif
                    }
                    
                    var orderedStack = GetElementStackInHierarchyOrder();
                    elementStackInHierarchyOrderNoArrays = ArrayPool<ElementStackEntry>.Shared.Rent(orderedStack.Count);
                    foreach (var item in orderedStack) {
                        if (item.Key == null || item.IsArrayItem) {
                            continue;
                        }
                        
                        elementStackInHierarchyOrderNoArrays[elementStackInHierarchyOrderNoArraysCount++] = item;
                    }
                }
                
                return new ArraySegment<ElementStackEntry>(elementStackInHierarchyOrderNoArrays, 0, elementStackInHierarchyOrderNoArraysCount);
            }

            // Gets the name of the current property.
            string GetFullPropertyName(bool forceLocalName = false) {
                if (!options.Recursive || forceLocalName) {
                    return pointer.Count == 0
                        ? string.Empty
                        : pointer[^1];
                }

                if (options.IncludeArrayIndexesInSampleKeys || !GetElementStackInHierarchyOrder().Any(x => x.IsArrayItem)) {
                    return string.Equals(options.PathSeparator, TimeSeriesExtractorConstants.DefaultPathSeparator, StringComparison.Ordinal)
                        ? pointer.ToString().TrimStart('/')
                        : string.Join(options.PathSeparator, pointer);
                }

                // Remove array indexes from the path. In order to do this, we construct the key
                // from the element stack instead of using the pointer directly. This allows us
                // to skip any elements that are array entries. This ensures that we don't
                // accidentally omit object properties that use integer values as the property name.
                return string.Join(options.PathSeparator, GetElementStackInHierarchyOrderNoArrays().Select(x => x.Key));
            }

            // Gets the full path for the current property, not including the actual property name.
            string GetPropertyPath() {
                if (!options.Recursive || pointer.Count <= 1) {
                    return string.Empty;
                }
                
                var useDirectPointer = options.IncludeArrayIndexesInSampleKeys || !GetElementStackInHierarchyOrder().Any(x => x.IsArrayItem);
                if (useDirectPointer) {
                    var ancestor = pointer.GetAncestor(1);
                    if (string.Equals(options.PathSeparator, TimeSeriesExtractorConstants.DefaultPathSeparator, StringComparison.Ordinal)) {
                        var ancestorString = ancestor.ToString();
                        return ancestorString.Length > 0 && ancestorString[0] == '/' 
                            ? ancestorString.Substring(1) 
                            : ancestorString;
                    }
                    return string.Join(options.PathSeparator, ancestor);
                }

                var filtered = GetElementStackInHierarchyOrderNoArrays();
                if (filtered.Count == 0) {
                    return string.Empty;
                }
                
#if NETCOREAPP
                return string.Join(options.PathSeparator, filtered.SkipLast(1).Select(x => x.Key));
#else
                return string.Join(options.PathSeparator, filtered.Take(filtered.Count - 1).Select(x => x.Key));
#endif
            }
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
            return new TimeSeriesSample(key, sampleTime, value.ValueKind switch {
                JsonValueKind.Number => value.GetDouble(),
                JsonValueKind.String => value.GetString()!,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => value.GetRawText(),
                JsonValueKind.Array => value.GetRawText(),
                _ => null
            }, sampleTimeSource);
        }

    }
}

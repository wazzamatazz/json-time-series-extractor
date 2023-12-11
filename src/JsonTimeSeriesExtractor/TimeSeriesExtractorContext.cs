using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// The context to use when extracting samples using <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    internal sealed class TimeSeriesExtractorContext {

        /// <summary>
        /// The extractor options.
        /// </summary>
        internal TimeSeriesExtractorOptions Options { get; }

        /// <summary>
        /// The <see cref="JsonPointer"/> that corresponds to the timestamp property.
        /// </summary>
        internal JsonPointer? TimestampPointer { get; }

        /// <summary>
        /// A delegate that determines whether a document property should be extracted as a time series.
        /// </summary>
        internal Func<JsonPointer, bool> IncludeElement { get; }

        /// <summary>
        /// The stack of JSON elements that are currently being processed.
        /// </summary>
        internal Stack<KeyValuePair<string?, JsonElement>> ElementStack { get; }

        /// <summary>
        /// The stack of timestamps that are currently being processed.
        /// </summary>
        internal Stack<ParsedTimestamp> TimestampStack { get; }

        /// <summary>
        /// Specifies whether the default sample key template is being used.
        /// </summary>
        internal bool IsDefaultSampleKeyTemplate { get; }


        /// <summary>
        /// Creates a new <see cref="TimeSeriesExtractorContext"/> instance.
        /// </summary>
        /// <param name="options">
        ///   The extractor options.
        /// </param>
        internal TimeSeriesExtractorContext(TimeSeriesExtractorOptions options) {
            Options = options;

            if (options.TimestampProperty != null) {
                if (!JsonPointer.TryParse(options.TimestampProperty, out var timestampPointer)) {
                    throw new ArgumentOutOfRangeException(nameof(options), string.Format(CultureInfo.CurrentCulture, Resources.Error_InvalidJsonPointer, options.TimestampProperty));
                }
                TimestampPointer = timestampPointer;
            }
            else {
                TimestampPointer = null;
            }

            ElementStack = new Stack<KeyValuePair<string?, JsonElement>>();

            // Assign to local variable first so that it can be referenced in the lambda
            // expression below.
            var timestampStack = Options.Recursive
                ? Options.MaxDepth < 1
                    ? new Stack<ParsedTimestamp>()
                    : new Stack<ParsedTimestamp>(options.MaxDepth)
                : new Stack<ParsedTimestamp>(1);

            TimestampStack = timestampStack;

            // We are using the default sample key template if:
            //
            // 1. We are running in recursive mode and the template is equal to TimeSeriesExtractor.FullPropertyNamePlaceholder.
            // 2. We are running in non-recursive mode and the template is equal to TimeSeriesExtractor.FullPropertyNamePlaceholder
            //    or TimeSeriesExtractor.LocalPropertyNamePlaceholder.
            IsDefaultSampleKeyTemplate = Options.Recursive
                ? string.Equals(Options.Template, TimeSeriesExtractor.FullPropertyNamePlaceholder, StringComparison.Ordinal)
                : string.Equals(Options.Template, TimeSeriesExtractor.FullPropertyNamePlaceholder, StringComparison.Ordinal) || string.Equals(Options.Template, TimeSeriesExtractor.LocalPropertyNamePlaceholder, StringComparison.Ordinal);

            IncludeElement = p => {
                var ts = timestampStack.Peek();
                if (ts.Pointer != null && p.Equals(ts.Pointer)) {
                    return false;
                }

                // Lambdas declared in structs cannot reference instance members, so we need to
                // use the constructor parameter for the options instead.
                if (options.IncludeProperty != null && !options.IncludeProperty.Invoke(p)) {
                    return false;
                }

                return true;
            };
        }

    }
}

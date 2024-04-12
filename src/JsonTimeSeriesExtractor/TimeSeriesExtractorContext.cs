using System;
using System.Collections.Generic;
using System.Text.Json;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// The context to use when extracting samples using <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    public sealed class TimeSeriesExtractorContext {

        /// <summary>
        /// The extractor options.
        /// </summary>
        public TimeSeriesExtractorOptions Options { get; }

        /// <summary>
        /// The stack of JSON elements that are currently being processed.
        /// </summary>
        internal Stack<ElementStackEntry> ElementStack { get; }

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

            ElementStack = new Stack<ElementStackEntry>();

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
                ? string.Equals(Options.Template, TimeSeriesExtractorConstants.FullPropertyNamePlaceholder, StringComparison.Ordinal)
                : string.Equals(Options.Template, TimeSeriesExtractorConstants.FullPropertyNamePlaceholder, StringComparison.Ordinal) || string.Equals(Options.Template, TimeSeriesExtractorConstants.LocalPropertyNamePlaceholder, StringComparison.Ordinal);
        }


        /// <summary>
        /// Tests whether the specified JSON element can be processed.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer to the element.
        /// </param>
        /// <param name="element">
        ///   The JSON element to test.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the element can be processed; otherwise, <see langword="false"/>.
        /// </returns>
        internal bool CanProcessElement(JsonPointer pointer, JsonElement element) {
            // Never process the timestamp property for the current document level.
            var ts = TimestampStack.Peek();
            if (ts.Pointer != null && pointer.Equals(ts.Pointer)) {
                return false;
            }

            if (Options.CanProcessElement != null && !Options.CanProcessElement.Invoke(this, pointer, element)) {
                return false;
            }

            return true;
        }

    }
}

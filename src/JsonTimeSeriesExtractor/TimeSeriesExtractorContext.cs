using System;
using System.Text.Json;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// The context to use when extracting samples using <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    public sealed class TimeSeriesExtractorContext : IDisposable {

        /// <summary>
        /// Specifies whether the context has been disposed.
        /// </summary>
        private bool _disposed;
        
        /// <summary>
        /// The extractor options.
        /// </summary>
        public TimeSeriesExtractorOptions Options { get; }
        
        /// <summary>
        /// The maximum depth of the JSON document that can be processed.
        /// </summary>
        internal int MaxDepth { get; }

        /// <summary>
        /// The stack of JSON elements that are currently being processed.
        /// </summary>
        internal ElementStack ElementStack { get; }

        /// <summary>
        /// The stack of timestamps that are currently being processed.
        /// </summary>
        internal TimestampStack TimestampStack { get; }

        /// <summary>
        /// Specifies whether the default sample key template is being used.
        /// </summary>
        internal bool IsDefaultSampleKeyTemplate { get; }
        
        /// <summary>
        /// Specifies whether the sample key template contains any placeholders.
        /// </summary>
        internal bool SampleKeyTemplateContainsPlaceholders { get; }


        /// <summary>
        /// Creates a new <see cref="TimeSeriesExtractorContext"/> instance.
        /// </summary>
        /// <param name="options">
        ///   The extractor options.
        /// </param>
        internal TimeSeriesExtractorContext(TimeSeriesExtractorOptions options) {
            Options = options;

            MaxDepth = options.Recursive
                ? options.MaxDepth < 1
                    ? TimeSeriesExtractorConstants.DefaultMaxDepth
                    : options.MaxDepth
                : 1;
            
            // We need to add 1 to the maximum depth to allow for the root element.
            ElementStack = new ElementStack(MaxDepth + 1);
            TimestampStack = new TimestampStack(options is { Recursive: true, AllowNestedTimestamps: true } ? MaxDepth : 1);

            // We are using the default sample key template if:
            //
            // 1. We are running in recursive mode and the template is equal to TimeSeriesExtractor.FullPropertyNamePlaceholder.
            // 2. We are running in non-recursive mode and the template is equal to TimeSeriesExtractor.FullPropertyNamePlaceholder
            //    or TimeSeriesExtractor.LocalPropertyNamePlaceholder.
            IsDefaultSampleKeyTemplate = Options.Recursive
                ? string.Equals(Options.Template, TimeSeriesExtractorConstants.FullPropertyNamePlaceholder, StringComparison.Ordinal)
                : string.Equals(Options.Template, TimeSeriesExtractorConstants.FullPropertyNamePlaceholder, StringComparison.Ordinal) || string.Equals(Options.Template, TimeSeriesExtractorConstants.LocalPropertyNamePlaceholder, StringComparison.Ordinal);
            
            // We can take a shortcut if the sample key template does not contain any placeholders.
            SampleKeyTemplateContainsPlaceholders = IsDefaultSampleKeyTemplate || Options.Template.Contains("{");
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

            return Options.CanProcessElement == null || Options.CanProcessElement.Invoke(this, pointer, element);
        }


        public void Dispose() {
            if (_disposed) {
                return;
            }
            
            ElementStack.Dispose();
            TimestampStack.Dispose();
            
            _disposed = true;
        }

    }
}

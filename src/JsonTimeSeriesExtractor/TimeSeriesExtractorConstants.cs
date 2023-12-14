namespace Jaahas.Json {

    /// <summary>
    /// Constants for <see cref="TimeSeriesExtractorOptions"/> and <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    public static class TimeSeriesExtractorConstants {

        /// <summary>
        /// <see cref="TimeSeriesExtractorOptions.Template"/> placeholder for the full JSON 
        /// Pointer path to a property.
        /// </summary>
        public const string FullPropertyNamePlaceholder = "{$prop}";

        /// <summary>
        /// <see cref="TimeSeriesExtractorOptions.Template"/> placeholder for the local property 
        /// name only.
        /// </summary>
        public const string LocalPropertyNamePlaceholder = "{$prop-local}";

        /// <summary>
        /// Default <see cref="TimeSeriesExtractorOptions.Template"/> value.
        /// </summary>
        public const string DefaultTemplate = FullPropertyNamePlaceholder;

        /// <summary>
        /// Default <see cref="TimeSeriesExtractorOptions.TimestampProperty"/> value.
        /// </summary>
        public const string DefaultTimestampProperty = "/time";

        /// <summary>
        /// Default <see cref="TimeSeriesExtractorOptions.PathSeparator"/> value.
        /// </summary>
        public const string DefaultPathSeparator = "/";

        /// <summary>
        /// Default <see cref="TimeSeriesExtractorOptions.MaxDepth"/> value.
        /// </summary>
        public const int DefaultMaxDepth = 5;

    }
}

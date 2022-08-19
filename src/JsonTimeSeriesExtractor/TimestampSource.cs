namespace Jaahas.Json {

    /// <summary>
    /// Describes the source of the <see cref="TimeSeriesSample.Timestamp"/> property on a <see cref="TimeSeriesSample"/>.
    /// </summary>
    public enum TimestampSource {

        /// <summary>
        /// The source of the timestamp has not been specified.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The timestamp was sourced from a property in the JSON document.
        /// </summary>
        Document,

        /// <summary>
        /// The timestamp was sourced from the fallback timestamp provider specified in 
        /// <see cref="TimeSeriesExtractorOptions.GetDefaultTimestamp"/>.
        /// </summary>
        FallbackProvider,

        /// <summary>
        /// The timestamp was sourced using the current time at the moment that the <see cref="TimeSeriesSample"/> 
        /// was created.
        /// </summary>
        CurrentTime

    }
}

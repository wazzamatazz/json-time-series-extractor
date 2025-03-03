using System;

namespace Jaahas.Json {

    /// <summary>
    /// Describes a sample extracted from a JSON document using <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    public readonly struct TimeSeriesSample {

        /// <summary>
        /// The key (identifier) for the sample.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The timestamp for the sample.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// The value for the sample.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Specifies where the <see cref="Timestamp"/> for the sample was sourced from.
        /// </summary>
        public TimestampSource TimestampSource { get; }


        /// <summary>
        /// Creates a new <see cref="TimeSeriesSample"/>.
        /// </summary>
        /// <param name="key">
        ///   The key for the sample.
        /// </param>
        /// <param name="timestamp">
        ///   The sample timestamp.
        /// </param>
        /// <param name="value">
        ///   The sample value.
        /// </param>
        /// <param name="timestampSource">
        ///   A flag describing the source of the specified <paramref name="timestamp"/>.
        /// </param>
        public TimeSeriesSample(string key, DateTimeOffset timestamp, object? value, TimestampSource timestampSource = TimestampSource.Unspecified) {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Timestamp = timestamp;
            Value = value;
            TimestampSource = timestampSource;
        }

    }
}

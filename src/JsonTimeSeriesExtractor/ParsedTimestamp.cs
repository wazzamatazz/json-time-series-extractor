using System;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// A timestamp for a <see cref="TimeSeriesSample"/>.
    /// </summary>
    internal readonly struct ParsedTimestamp {

        /// <summary>
        /// The timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// The source of the timestamp.
        /// </summary>
        public TimestampSource Source { get; }

        /// <summary>
        /// The JSON pointer to the element that the timestamp was parsed from, if <see cref="Source"/> 
        /// is <see cref="TimestampSource.Document"/>.
        /// </summary>
        public JsonPointer? Pointer { get; }


        /// <summary>
        /// Creates a new <see cref="ParsedTimestamp"/> instance.
        /// </summary>
        /// <param name="timestamp">
        ///   The timestamp.
        /// </param>
        /// <param name="source">
        ///   The source of the timestamp.
        /// </param>
        /// <param name="pointer">
        ///   The JSON pointer to the element that the timestamp was parsed from, if <paramref name="source"/> 
        ///   is <see cref="TimestampSource.Document"/>.
        /// </param>
        public ParsedTimestamp(DateTimeOffset timestamp, TimestampSource source, JsonPointer? pointer) {
            Timestamp = timestamp;
            Source = source;
            Pointer = pointer;
        }

    }
}

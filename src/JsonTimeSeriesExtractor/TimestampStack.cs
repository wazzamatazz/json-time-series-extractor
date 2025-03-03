namespace Jaahas.Json;

/// <summary>
/// A stack of <see cref="ParsedTimestamp"/> items.
/// </summary>
internal sealed class TimestampStack : FixedLengthStack<ParsedTimestamp> {

    /// <inheritdoc />
    public TimestampStack(int capacity) : base(capacity) { }

}

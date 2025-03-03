namespace Jaahas.Json;

/// <summary>
/// A stack of <see cref="ElementStackEntry"/> items.
/// </summary>
internal sealed class ElementStack : FixedLengthStack<ElementStackEntry> {

    /// <inheritdoc />
    public ElementStack(int capacity) : base(capacity) { }

}

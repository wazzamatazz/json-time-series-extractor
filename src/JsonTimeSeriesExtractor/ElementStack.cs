using System;
using System.Buffers;

namespace Jaahas.Json;

/// <summary>
/// A stack of <see cref="ElementStackEntry"/> items with a fixed length that cannot be resized.
/// </summary>
/// <remarks>
/// <para>
///   <see cref="ElementStack"/> uses <see cref="ArrayPool{T}"/> to rent a backing array to
///   hold the stack items. The stack must be disposed when no longer required to allow the backing
///   array to be returned to the pool.
/// </para>
/// </remarks>
internal sealed class ElementStack : IDisposable {

    /// <summary>
    /// Specifies whether the stack has been disposed.
    /// </summary>
    private bool _disposed;
    
    /// <summary>
    /// The backing array.
    /// </summary>
    private readonly ElementStackEntry[] _buffer;

    /// <summary>
    /// The maximum number of items that can be stored in the stack.
    /// </summary>
    private readonly int _capacity;
    
    /// <summary>
    /// The current number of items in the stack.
    /// </summary>
    private int _count;
    
    /// <summary>
    /// The number of items in the stack.
    /// </summary>
    public int Count => _count;
    
    
    /// <summary>
    /// Creates a new <see cref="ElementStack"/> instance.
    /// </summary>
    /// <param name="capacity">
    ///   The maximum number of items that can be stored in the stack.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="capacity"/> is less than or equal to zero.
    /// </exception>
    public ElementStack(int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity), Resources.Error_StackCapacityTooSmall);
        }
        
        _capacity = capacity;
        _buffer = ArrayPool<ElementStackEntry>.Shared.Rent(_capacity);
    }

    
    /// <summary>
    /// Pushes an item onto the stack.
    /// </summary>
    /// <param name="entry">
    ///   The item to push.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    ///   The stack has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The stack is full.
    /// </exception>
    public void Push(ElementStackEntry entry) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(ElementStack));
        }
        if (_count >= _capacity) {
            throw new InvalidOperationException(Resources.Error_StackIsFull);
        }
        
        _buffer[_count++] = entry;
    }
    
    
    /// <summary>
    /// Pops an item from the stack.
    /// </summary>
    /// <returns>
    ///   The item popped from the stack.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///   The stack has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The stack is empty.
    /// </exception>
    public ElementStackEntry Pop() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(ElementStack));
        }
        if (_count == 0) {
            throw new InvalidOperationException(Resources.Error_StackIsEmpty);
        }
        
        return _buffer[--_count];
    }
    
    
    /// <summary>
    /// Peeks at the item on the top of the stack without removing it.
    /// </summary>
    /// <returns>
    ///   The item on the top of the stack.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///   The stack has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The stack is empty.
    /// </exception>
    public ElementStackEntry Peek() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(ElementStack));
        }
        if (_count == 0) {
            throw new InvalidOperationException(Resources.Error_StackIsEmpty);
        }
        
        return _buffer[_count - 1];
    }
    

    /// <summary>
    /// Returns the first element that matches the specified condition.
    /// </summary>
    /// <param name="predicate">
    ///   A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///   The first element that matches the condition, or the default value if no element is found.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///   The stack has been disposed.
    /// </exception>
    public ElementStackEntry FirstOrDefault(Func<ElementStackEntry, bool> predicate) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(ElementStack));
        }
        
        for (var i = 0; i < _count; i++) {
            if (predicate(_buffer[i])) {
                return _buffer[i];
            }
        }
        
        return default;
    }
    
    
    /// <summary>
    /// Gets a read-only span of the stack entries.
    /// </summary>
    /// <returns>
    ///   A read-only span of the stack entries.
    /// </returns>
    public ReadOnlySpan<ElementStackEntry> AsSpan() => _buffer.AsSpan(0, _count);
    
    
    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }
        
        // Manually clear only the used portion to optimize performance while preventing memory leaks
        if (_count > 0) {
            Array.Clear(_buffer, 0, _count);
        }
        
        ArrayPool<ElementStackEntry>.Shared.Return(_buffer, clearArray: false);
        
        _disposed = true;
    }

}

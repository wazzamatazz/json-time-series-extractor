using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace Jaahas.Json;

/// <summary>
/// <see cref="FixedLengthStack{T}"/> is a stack with a fixed length that cannot be resized.
/// </summary>
/// <typeparam name="T">
///   The item type.
/// </typeparam>
/// <remarks>
///
/// <para>
///   <see cref="FixedLengthStack{T}"/> uses <see cref="ArrayPool{T}"/> to rent a backing array to
///   hold the stack items. The stack must be disposed when no longer required to allow the backing
///   array to be returned to the pool.
/// </para>
/// 
/// </remarks>
internal abstract class FixedLengthStack<T> : IDisposable, IReadOnlyCollection<T> {

    /// <summary>
    /// Specifies whether the stack has been disposed.
    /// </summary>
    private bool _disposed;
    
    /// <summary>
    /// The backing array.
    /// </summary>
    private readonly T[] _buffer;

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
    /// Creates a new <see cref="FixedLengthStack{T}"/> instance.
    /// </summary>
    /// <param name="capacity">
    ///   The maximum number of items that can be stored in the stack.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="capacity"/> is less than or equal to zero.
    /// </exception>
    public FixedLengthStack(int capacity) {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }
        
        _capacity = capacity;
        _buffer = ArrayPool<T>.Shared.Rent(_capacity);
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
    public void Push(T entry) {
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
        if (_count >= _capacity) {
            throw new InvalidOperationException("The stack is full.");
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
    public T Pop() {
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
        if (_count == 0) {
            throw new InvalidOperationException("The stack is empty.");
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
    public T Peek() {
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }
        if (_count == 0) {
            throw new InvalidOperationException("The stack is empty.");
        }
        
        return _buffer[_count - 1];
    }


    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() {
        if (_disposed) {
            throw new ObjectDisposedException(GetType().FullName);
        }

        return new StackEnumerator(this);
    }


    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
    
    
    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }
        
        ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
        
        _disposed = true;
    }
    
    
    /// <summary>
    /// Enumerator for <see cref="FixedLengthStack{T}"/>.
    /// </summary>
    /// <remarks>
    ///   Note that the enumerator enumerates from the bottom of the stack to the top rather
    ///   than the top of the stack to the bottom.
    /// </remarks>
    private class StackEnumerator : IEnumerator<T> {

        private readonly FixedLengthStack<T> _stack;
        
        private int _index;
        
        public StackEnumerator(FixedLengthStack<T> stack) {
            _stack = stack;
            _index = -1;
        }


        /// <inheritdoc />
        public bool MoveNext() {
            if (_index >= _stack._count) {
                return false;
            }
            
            _index++;
            return true;
        }


        /// <inheritdoc />
        public void Reset() {
            _index = -1;
        }


        /// <inheritdoc />
        public T Current => _index >= 0 ? _stack._buffer[_index] : default!;
        

        /// <inheritdoc />
        object? IEnumerator.Current {
            get => Current;
        }


        /// <inheritdoc />
        public void Dispose() {
            // No-op
        }

    }

}

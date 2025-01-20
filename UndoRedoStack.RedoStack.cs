﻿using System.Diagnostics;

namespace System.Collections.Generic
{
    public partial class UndoRedoStack<T> : IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// If initialized with count == 0, <see cref="IReadOnlyList{T}"/> of <see cref="UndoRedoStack{T}"/> redo stack.
        /// Otherwise, <see cref="IReadOnlyList{T}"/> of count items popped from <see cref="UndoRedoStack{T}"/> redo stack.
        /// </summary>
        private sealed class RedoStack : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IEnumerator<T>, IEnumerator
        {
            private readonly int _length;
            private readonly UndoRedoStack<T> _this;
            private readonly int _threadId;
            private int _state;
            private T _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="UndoStack{T}"/> class.
            /// </summary>
            public RedoStack(UndoRedoStack<T> stack, int count)
            {
                Debug.Assert(stack != null);
                Debug.Assert(count >= 0);

                _length = count;
                _this = stack;
                _threadId = Environment.CurrentManagedThreadId;
                _state = -1;
                _current = default;
            }

            /// <summary>
            /// Gets the number of redos contained in the <see cref="UndoRedoStack{T}"/>.
            /// </summary>
            /// <returns>
            /// The number of redos contained in the <see cref="UndoRedoStack{T}"/>.
            /// </returns>
            public int Count => _length == 0 ? _this._redos : Math.Min(_length, _this._stack.Count - _this._redos);

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                RedoStack _enum;
                if ((_state == -1) && (_threadId == Environment.CurrentManagedThreadId))
                {
                    _state = 0;
                    _enum = this;
                }
                else
                {
                    _enum = new RedoStack(_this, _length)
                    {
                        _state = 0
                    };
                }
                return _enum;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// Gets the element at the specified index in the read-only list.
            /// </summary>
            /// <param name="index">The zero-based index of the element to get.</param>
            /// <returns>The element at the specified index in the read-only list.</returns>
            /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than zero -or- greater than or equal to <see cref="Count"/>.</exception>
            public T this[int index]
            {
                get
                {
                    if ((index < 0) || (index >= Count)) throw new IndexOutOfRangeException();
                    return _this._stack[Math.Max(_this._stack.Count - _this._redos - _length, 0) + index];
                }
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            T IEnumerator<T>.Current => _current;

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            object IEnumerator.Current => _current;

            /// <summary>
            /// Provides a mechanism for releasing unmanaged resources.
            /// </summary>
            void IDisposable.Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            bool IEnumerator.MoveNext()
            {
                if ((_state < 0) || (_state >= Count)) return false;

                _current = _this._stack[Math.Max(_this._stack.Count - _this._redos - _length, 0) + _state];
                _state++;
                return true;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            void IEnumerator.Reset()
            {
                if (_state > 0) _state = 0;
            }
        }
    }
}

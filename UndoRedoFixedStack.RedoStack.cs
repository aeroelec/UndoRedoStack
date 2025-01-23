using System.Diagnostics;

namespace System.Collections.Generic
{
    public partial class UndoRedoFixedStack<T>
    {
        /// <summary>
        /// If initialized with count == 0, <see cref="IReadOnlyList{T}"/> of <see cref="UndoRedoFixedStack{T}"/> redo stack.
        /// Otherwise, <see cref="IReadOnlyList{T}"/> of count items popped from <see cref="UndoRedoFixedStack{T}"/> redo stack.
        /// </summary>
        private sealed class RedoStack : IReadOnlyList<T>, IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable, IEnumerator<T>, IEnumerator
        {
            private readonly int _length;
            private readonly UndoRedoFixedStack<T> _this;
            private readonly int _threadId;
            private int _state;
            private T _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="RedoStack{T}"/> class.
            /// </summary>
            public RedoStack(UndoRedoFixedStack<T> stack, int count)
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
            /// Gets a value indicating whether access to the <see cref="ICollection"/> is synchronized (thread safe).
            /// </summary>
            /// <returns>
            /// true if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, false. In the default implementation of <see cref="UndoRedoFixedStack{T}"/>, this property always returns false.
            /// </returns>
            bool ICollection.IsSynchronized => ((ICollection)_this).IsSynchronized;

            /// <summary>
            /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
            /// </summary>
            /// <returns>
            /// An object that can be used to synchronize access to the <see cref="ICollection"/>. In the default implementation of <see cref="UndoRedoFixedStack{T}"/>, this property always returns the current instance.
            /// </returns>
            object ICollection.SyncRoot => ((ICollection)_this).SyncRoot;

            /// <summary>
            /// Gets the number of redos contained in the <see cref="UndoRedoFixedStack{T}"/>.
            /// </summary>
            /// <returns>
            /// The number of redos contained in the <see cref="UndoRedoFixedStack{T}"/>.
            /// </returns>
            public int Count => _length == 0 ? _this._redos : Math.Min(_length, _this._undos);

            /// <summary>
            /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
            /// <exception cref="ArgumentNullException">array is null.</exception>
            /// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than zero.</exception>
            /// <exception cref="ArgumentException">array is multidimensional. -or- array does not have zero-based indexing. -or- The number of elements in the source <see cref="ICollection"/> is greater than the available space from arrayIndex to the end of the destination array. -or- The type of the source <see cref="ICollection"/> cannot be cast automatically to the type of the destination array.</exception>
            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array == null) throw new ArgumentNullException(nameof(array), "array is null.");
                if (array.Rank != 1) throw new ArgumentException("array is multidimensional.", nameof(array));

                int _start = _this.GetIndex(Math.Max(_this._undos - _length, 0));
                int _count = Count;
                int length = _this._array.Length - _start;
                if (_count > length)
                {
                    Array.Copy(_this._array, _start, array, arrayIndex, length);
                    Array.Copy(_this._array, 0, array, arrayIndex + length, _count - length);
                }
                else
                {
                    Array.Copy(_this._array, _start, array, arrayIndex, _count);
                }
            }

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
                    return _this._array[_this.GetIndex(index + Math.Max(_this._undos - _length, 0))];
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

                _current = _this._array[_this.GetIndex(_state + Math.Max(_this._undos - _length, 0))];
                _state++;
                return true;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            void IEnumerator.Reset()
            {
                if (_state > 0)
                {
                    _state = 0;
                    _current = default;
                }
            }
        }
    }
}

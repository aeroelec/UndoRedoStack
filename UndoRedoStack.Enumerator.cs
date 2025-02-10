namespace System.Collections.Generic
{
    public partial class UndoRedoStack<T>
    {
        /// <summary>
        /// Enumerator for <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        private struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly UndoRedoStack<T> _this;
            private readonly int _version;
            private readonly int _count;
            private int _index;
            private int _ptr;
            private T _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator{T}"/> class.
            /// </summary>
            internal Enumerator(UndoRedoStack<T> stack, int start, int count, int version)
            {
                _this = stack;
                _version = version;
                _count = count;
                _index = 0;
                _ptr = start;
                _current = default;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public T Current => _current;

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
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            /// <exception cref="InvalidOperationException">version has changed.</exception>
            public bool MoveNext()
            {
                if (_version != _this._version) throw new InvalidOperationException("version has changed.");

                if (_index == _count)
                {
                    _current = default;

                    return false;
                }
                else
                {
                    _current = _this._array[_ptr];

                    if (_count < 0)
                    {
                        _index--;
                        _ptr--;
                    }
                    else
                    {
                        _index++;
                        _ptr++;
                    }

                    return true;
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">version has changed.</exception>
            public void Reset()
            {
                if (_version != _this._version) throw new InvalidOperationException("version has changed.");

                _ptr -= _index;

                _index = 0;
                _current = default;
            }
        }
    }
}

using System.Diagnostics;

namespace System.Collections.Generic
{
    public partial class UndoRedoFixedStack<T>
    {
        /// <summary>
        /// Enumerator for <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        private sealed class Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly UndoRedoFixedStack<T> _this;
            private int _index;
            private T _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator{T}"/> class.
            /// </summary>
            public Enumerator(UndoRedoFixedStack<T> stack)
            {
                Debug.Assert(stack != null);

                _this = stack;
                _index = 0;
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
            public bool MoveNext()
            {
                if (_index >= _this.Count) return false;

                _current = _this._array[_this.GetIndex(_index)];
                _index++;
                return true;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                if (_index > 0)
                {
                    _index = 0;
                    _current = default;
                }
            }
        }
    }
}

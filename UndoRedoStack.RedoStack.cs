namespace System.Collections.Generic
{
    public partial class UndoRedoStack<T>
    {
        /// <summary>
        /// If initialized with count == 0, <see cref="IReadOnlyList{T}"/> of <see cref="UndoRedoStack{T}"/> redo stack.
        /// Otherwise, <see cref="IReadOnlyList{T}"/> of count items popped from <see cref="UndoRedoStack{T}"/> redo stack.
        /// </summary>
        private sealed class RedoStack : IReadOnlyList<T>, IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable
        {
            private readonly UndoRedoStack<T> _this;
            private readonly int _version;
            private readonly int _count;

            /// <summary>
            /// Initializes a new instance of the <see cref="RedoStack{T}"/> class.
            /// </summary>
            internal RedoStack(UndoRedoStack<T> stack, int count)
            {
                _this = stack;
                _version = stack._version;
                _count = count;
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
            /// Gets the number of redos contained in the <see cref="UndoRedoStack{T}"/>.
            /// </summary>
            /// <returns>
            /// The number of redos contained in the <see cref="UndoRedoStack{T}"/>.
            /// </returns>
            /// <exception cref="InvalidOperationException">version has changed.</exception>
            public int Count
            {
                get
                {
                    if (_count > 0)
                    {
                        if (_version != _this._version) throw new InvalidOperationException("version has changed.");

                        return _count;
                    }
                    else
                    {
                        return _this._redos;
                    }
                }
            }

            /// <summary>
            /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
            /// <exception cref="ArgumentNullException">array is null.</exception>
            /// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than zero.</exception>
            /// <exception cref="ArgumentException">array is multidimensional. -or- array does not have zero-based indexing. -or- The number of elements in the source <see cref="ICollection"/> is greater than the available space from arrayIndex to the end of the destination array. -or- The type of the source <see cref="ICollection"/> cannot be cast automatically to the type of the destination array.</exception>
            /// <exception cref="InvalidOperationException">version has changed.</exception>
            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array == null) throw new ArgumentNullException(nameof(array), "array is null.");
                if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex), "arrayIndex is less than zero.");
                if (array.Rank != 1) throw new ArgumentException("array is multidimensional.", nameof(array));
                if (array.GetLowerBound(0) != 0) throw new ArgumentException("array does not have zero-based indexing.", nameof(array));

                if (_count > 0)
                {
                    if (_version != _this._version) throw new InvalidOperationException("version has changed.");

                    Array.Copy(_this._array, _this._undos - _count, array, arrayIndex, _count);
                }
                else if (_this._redos > 0)
                {
                    Array.Copy(_this._array, _this._undos, array, arrayIndex, _this._redos);
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">version has changed.</exception>
            public IEnumerator<T> GetEnumerator()
            {
                if (_count > 0)
                {
                    if (_version != _this._version) throw new InvalidOperationException("version has changed.");

                    return new Enumerator(_this, _this._undos - _count, _count, _version);
                }
                else
                {
                    return new Enumerator(_this, _this._undos, _this._redos, _this._version);
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">version has changed.</exception>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// Gets the element at the specified index in the read-only list.
            /// </summary>
            /// <param name="index">The zero-based index of the element to get.</param>
            /// <returns>The element at the specified index in the read-only list.</returns>
            /// <exception cref="InvalidOperationException">version has changed.</exception>
            /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than zero -or- greater than or equal to <see cref="Count"/>.</exception>
            public T this[int index]
            {
                get
                {
                    if (_count > 0)
                    {
                        if (_version != _this._version) throw new InvalidOperationException("version has changed.");
                        if ((index < 0) || (index >= _count)) throw new IndexOutOfRangeException();

                        index += _this._undos - _count;
                    }
                    else
                    {
                        if ((index < 0) || (index >= _this._redos)) throw new IndexOutOfRangeException();

                        index += _this._undos;
                    }

                    return _this._array[index];
                }
            }
        }
    }
}

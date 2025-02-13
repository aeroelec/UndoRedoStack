namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a variable size, undo/redo last-in-first-out (LIFO) collection of instances of the same specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the undo/redo stack.</typeparam>
    [Serializable]
    public partial class UndoRedoStack<T> : IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        private const int _defaultCapacity = 4;

        private T[] _array;
        private int _version;
        private int _undos;
        private int _redos;
        private UndoStack _undostack;
        private RedoStack _redostack;

        [NonSerialized]
        private object _syncRoot;

        private static readonly T[] _emptyArray = new T[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that is empty and has the default initial capacity.
        /// </summary>
        public UndoRedoStack()
        {
            _array = _emptyArray;
            _version = 0;
            _undos = 0;
            _redos = 0;
            _undostack = null;
            _redostack = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="UndoRedoStack{T}"/> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than zero.</exception>
        public UndoRedoStack(int capacity) : this()
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "capacity is less than zero.");

            if (capacity > 0) _array = new T[capacity];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that contains elements copied from the specified collection as undo elements and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection to copy elements from.</param>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        public UndoRedoStack(IEnumerable<T> collection) : this()
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection), "collection is null.");

            switch (collection)
            {
                case ICollection<T> c:
                    _undos = c.Count;
                    if (_undos != 0)
                    {
                        _array = new T[_undos];
                        c.CopyTo(_array, 0);
                    }
                    break;
                case IReadOnlyCollection<T> c:
                    int _count = c.Count;
                    if (_count != 0)
                    {
                        _array = new T[_count];
                        foreach (T item in collection)
                        {
                            _array[_undos++] = item;
                        }
                    }
                    break;
                default:
                    foreach (T item in collection)
                    {
                        EnsureCapacity(_undos + 1);
                        _array[_undos++] = item;
                    }
                    break;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that contains elements copied from the specified collection as undo elements with specified redo elements and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection to copy elements from.</param>
        /// <param name="redoCount">The number of elements in <paramref name="collection"/> to push to redo stack.</param>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">redoCount is less than zero.</exception>
        /// <exception cref="ArgumentException">redoCount is greater than the number of elements in collection.</exception>
        public UndoRedoStack(IEnumerable<T> collection, int redoCount) : this(collection)
        {
            if (redoCount < 0) throw new ArgumentOutOfRangeException(nameof(redoCount), "redoCount is less than zero.");
            if (redoCount > _undos) throw new ArgumentException("redoCount is greater than the number of elements in collection.", nameof(redoCount));
            _redos = redoCount;
            _undos -= redoCount;
        }

        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        /// <returns>
        /// The number of elements that the <see cref="UndoRedoStack{T}"/> can contain before resizing is required.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="UndoRedoStack{T}.Capacity"/> is set to a value that is less than <see cref="UndoRedoStack{T}.Count"/>.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available on the system.</exception>
        public int Capacity
        {
            get => _array.Length;
            set
            {
                int _size = _undos + _redos;
                if (value < _size) throw new ArgumentOutOfRangeException(nameof(Capacity), "Capacity is set to a value that is less than Count.");

                if (value != _array.Length)
                {
                    if (value > 0)
                    {
                        T[] newArray = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_array, 0, newArray, 0, _size);
                        }
                        _array = newArray;
                    }
                    else
                    {
                        _array = _emptyArray;
                    }
                }
            }
        }

        /// <summary>
        /// Ensures that the capacity of this list is at least the specified capacity.
        /// If the current capacity is less than capacity, it is successively increased to twice the current capacity until it is at least the specified capacity.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        /// <remarks>EnsureCapacity is used to ensure items can be pushed onto the undo stack. Therefore, it does not maintain redo stack if <paramref name="capacity"/> greater than <seealso cref="Capacity"/>.</remarks>
        private void EnsureCapacity(int capacity)
        {
            if (_array.Length < capacity)
            {
                int newCapacity = _array.Length == 0 ? _defaultCapacity : _array.Length * 2;
                // Allow the stack to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _array.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;
                if (newCapacity < capacity) newCapacity = capacity;

                T[] newArray = new T[newCapacity];
                if (_undos > 0)
                {
                    Array.Copy(_array, 0, newArray, 0, _undos);
                }
                _redos = 0;
                _array = newArray;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public int Count => _undos + _redos;

        /// <summary>
        /// Gets the number of undos contained in the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The number of undos contained in the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public int CountUndos => _undos;

        /// <summary>
        /// Gets the number of redos contained in the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The number of redos contained in the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public int CountRedos => _redos;

        /// <summary>
        /// Gets the undos stack contained in the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The undos stack contained in the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public IReadOnlyList<T> Undos
        {
            get
            {
                if (_undostack == null)
                {
                    _undostack = new UndoStack(this, 0);
                }
                return _undostack;
            }
        }

        /// <summary>
        /// Gets the redos stack contained in the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The redos stack contained in the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public IReadOnlyList<T> Redos
        {
            get
            {
                if (_redostack == null)
                {
                    _redostack = new RedoStack(this, 0);
                }
                return _redostack;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <returns>
        /// true if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, false. In the default implementation of <see cref="UndoRedoStack{T}"/>, this property always returns false.
        /// </returns>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
        /// </summary>
        /// <returns>
        /// An object that can be used to synchronize access to the <see cref="ICollection"/>. In the default implementation of <see cref="UndoRedoStack{T}"/>, this property always returns the current instance.
        /// </returns>
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                }

                return _syncRoot;
            }
        }

        /// <summary>
        /// Removes all objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        public void Clear()
        {
            int _count = _undos + _redos;
            if (_count > 0)
            {
                // indicate version change
                _version++;

                Array.Clear(_array, 0, _count);

                _undos = 0;
                _redos = 0;
            }
        }

        /// <summary>
        /// Removes all undo objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        public void ClearUndo()
        {
            if (_undos > 0)
            {
                // indicate version change
                _version++;

                if (_redos > 0)
                {
                    Array.Copy(_array, _undos, _array, 0, _redos);
                }
                Array.Clear(_array, _redos, _undos);

                _undos = 0;
            }
        }

        /// <summary>
        /// Removes all redo objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        public void ClearRedo()
        {
            if (_redos > 0)
            {
                // indicate version change
                _version++;

                Array.Clear(_array, _undos, _redos);

                _redos = 0;
            }
        }

        /// <summary>
        /// Removes the oldest undo object from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The undo stack is empty.</exception>
        public void RemoveUndo()
        {
            if (_undos > 0)
            {
                // indicate version change
                _version++;

                _undos--;
                int _count = _undos + _redos;

                if (_count > 0)
                {
                    Array.Copy(_array, 1, _array, 0, _count);
                }
                _array[_count] = default;
            }
            else
            {
                throw new InvalidOperationException("The undo stack is empty.");
            }
        }

        /// <summary>
        /// Removes the oldest redo object from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The redo stack is empty.</exception>
        public void RemoveRedo()
        {
            if (_redos > 0)
            {
                // indicate version change
                _version++;

                _redos--;
                _array[_undos + _redos] = default;
            }
            else
            {
                throw new InvalidOperationException("The redo stack is empty.");
            }
        }

        /// <summary>
        /// Removes the oldest count of undo objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <param name="count">number of undo objects to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">count is greater than the number of undo objects.</exception>
        public void RemoveUndo(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count is less than or equal to zero.");
            if (count > _undos) throw new ArgumentException("count is greater than the number of undo objects.", nameof(count));

            // indicate version change
            _version++;

            _undos -= count;
            int _count = _undos + _redos;

            if (_count > 0)
            {
                Array.Copy(_array, count, _array, 0, _count);
            }
            Array.Clear(_array, _count, count);
        }

        /// <summary>
        /// Removes the oldest count of redo objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <param name="count">number of redo objects to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">count is greater than the number of redo objects.</exception>
        public void RemoveRedo(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count is less than or equal to zero.");
            if (count > _redos) throw new ArgumentException("count is greater than the number of redo objects.", nameof(count));

            // indicate version change
            _version++;

            _redos -= count;
            Array.Clear(_array, _undos + _redos, count);
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="UndoRedoStack{T}"/>. The value can be null for reference types.</param>
        /// <returns>
        /// true if item is found in the <see cref="UndoRedoStack{T}"/>; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            int _count = _undos + _redos;

            if (item == null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_array[0] == null) return true;
                }
            }
            else
            {
                for (int i = 0; i < _count; i++)
                {
                    if (item.Equals(_array[i])) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the <see cref="UndoRedoStack{T}"/> to an existing one-dimensional <see cref="Array"/>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="UndoRedoStack{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source <see cref="UndoRedoStack{T}"/> is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_array, 0, array, arrayIndex, _undos + _redos);
        }

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
            Array.Copy(_array, 0, array, arrayIndex, _undos + _redos);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this, 0, _undos + _redos, _version);
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
        /// Sets the capacity to the actual number of elements in the <see cref="UndoRedoStack{T}"/>, if that number is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            int _count = _undos + _redos;
            int threshold = (int)(((double)_array.Length) * 0.9);
            if (_count < threshold)
            {
                Capacity = _count;
            }
        }

        /// <summary>
        /// Returns the object at the top of the <see cref="UndoRedoStack{T}"/> undo stack without moving it to the redo stack.
        /// </summary>
        /// <returns>
        /// The object at the top of the <see cref="UndoRedoStack{T}"/> undo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The undo stack is empty.</exception>
        public T PeekUndo()
        {
            if (_undos > 0)
            {
                return _array[_undos - 1];
            }
            else
            {
                throw new InvalidOperationException("The undo stack is empty.");
            }
        }

        /// <summary>
        /// Returns the object at the top of the <see cref="UndoRedoStack{T}"/> redo stack without moving it to the undo stack.
        /// </summary>
        /// <returns>
        /// The object at the top of the <see cref="UndoRedoStack{T}"/> redo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The redo stack is empty.</exception>
        public T PeekRedo()
        {
            if (_redos > 0)
            {
                return _array[_undos];
            }
            else
            {
                throw new InvalidOperationException("The redo stack is empty.");
            }
        }

        /// <summary>
        /// Removes and returns the object at the top of the <see cref="UndoRedoStack{T}"/> undo stack, moving it to the redo stack.
        /// </summary>
        /// <returns>
        /// The object removed from the top of the <see cref="UndoRedoStack{T}"/> undo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The undo stack is empty.</exception>
        public T PopUndo()
        {
            if (_undos > 0)
            {
                // indicate version change
                _version++;

                _undos--;
                _redos++;
                return _array[_undos];
            }
            else
            {
                throw new InvalidOperationException("The undo stack is empty.");
            }
        }

        /// <summary>
        /// Removes and returns the object at the top of the <see cref="UndoRedoStack{T}"/> redo stack, moving it to the undo stack.
        /// </summary>
        /// <returns>
        /// The object removed from the top of the <see cref="UndoRedoStack{T}"/> redo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The redo stack is empty.</exception>
        public T PopRedo()
        {
            if (_redos > 0)
            {
                // indicate version change
                _version++;

                T redo = _array[_undos];
                _undos++;
                _redos--;
                return redo;
            }
            else
            {
                throw new InvalidOperationException("The redo stack is empty.");
            }
        }

        /// <summary>
        /// Removes and returns <paramref name="count"/> objects at the top of the <see cref="UndoRedoStack{T}"/> undo stack, moving them to the redo stack.
        /// </summary>
        /// <param name="count">The number of objects to pop off the <see cref="UndoRedoStack{T}"/> undo stack.</param>
        /// <returns>A list of the objects removed from the top of the <see cref="UndoRedoStack{T}"/> undo stack.</returns>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">count is greater than the number of undo objects.</exception>
        public IReadOnlyList<T> PopUndo(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count is less than or equal to zero.");
            if (count > _undos) throw new ArgumentException("count is greater than the number of undo objects.", nameof(count));

            // indicate version change
            _version++;

            _undos -= count;
            _redos += count;
            return new UndoStack(this, count);
        }

        /// <summary>
        /// Removes and returns <paramref name="count"/> objects at the top of the <see cref="UndoRedoStack{T}"/> redo stack, moving them to the undo stack.
        /// </summary>
        /// <param name="count">The number of objects to pop off the <see cref="UndoRedoStack{T}"/> redo stack.</param>
        /// <returns>A list of the objects removed from the top of the <see cref="UndoRedoStack{T}"/> redo stack.</returns>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">count is greater than the number of redo objects.</exception>
        public IReadOnlyList<T> PopRedo(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count is less than or equal to zero.");
            if (count > _redos) throw new ArgumentException("count is greater than the number of redo objects.", nameof(count));

            // indicate version change
            _version++;

            _undos += count;
            _redos -= count;
            return new RedoStack(this, count);
        }

        /// <summary>
        /// Inserts an object at the top of the <see cref="UndoRedoStack{T}"/> undo stack, clearing the redo stack.
        /// </summary>
        /// <param name="item">The object to push onto the <see cref="UndoRedoStack{T}"/> undo stack. The value can be null for reference types.</param>
        public void Push(T item)
        {
            // indicate version change
            _version++;

            EnsureCapacity(_undos + 1);
            _array[_undos] = item;

            _undos++;
            if (_redos > 1)
            {
                Array.Clear(_array, _undos, _redos - 1);
            }
            _redos = 0;
        }

        /// <summary>
        /// Copies the <see cref="UndoRedoStack{T}"/> to a new array.
        /// </summary>
        /// <returns>
        /// A new array containing copies of the elements of the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public T[] ToArray()
        {
            int _count = _undos + _redos;
            T[] array = new T[_count];
            if (_count > 0) Array.Copy(_array, 0, array, 0, _count);
            return array;
        }
    }
}

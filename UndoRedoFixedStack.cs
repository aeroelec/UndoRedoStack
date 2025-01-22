using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a fixed size, rolling buffer, undo/redo last-in-first-out (LIFO) collection of instances of the same specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the undo/redo stack.</typeparam>
    [Serializable]
    [ComVisible(false)]
    public partial class UndoRedoFixedStack<T> : IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        private readonly T[] _array;
        private int _start;
        private int _undos;
        private int _redos;
        private UndoStack _undostack = null;
        private RedoStack _redostack = null;

        [NonSerialized]
        private object _syncRoot;

        /// <summary>
        /// Gets the total number of elements the internal data structure can hold.
        /// </summary>
        /// <returns>
        /// The number of elements that the <see cref="UndoRedoFixedStack{T}"/> can contain before discarding older elements.
        /// </returns>
        public int Capacity => _array.Length;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="UndoRedoFixedStack{T}"/>.
        /// </returns>
        public int Count => _undos + _redos;

        /// <summary>
        /// Gets the undos stack contained in the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The undos stack contained in the <see cref="UndoRedoFixedStack{T}"/>.
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
        /// Gets the redos stack contained in the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The redos stack contained in the <see cref="UndoRedoFixedStack{T}"/>.
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
        /// true if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, false. In the default implementation of <see cref="UndoRedoFixedStack{T}"/>, this property always returns false.
        /// </returns>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
        /// </summary>
        /// <returns>
        /// An object that can be used to synchronize access to the <see cref="ICollection"/>. In the default implementation of <see cref="UndoRedoFixedStack{T}"/>, this property always returns the current instance.
        /// </returns>
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Threading.Interlocked.CompareExchange<object>(ref _syncRoot, new object(), (object)null);
                }

                return _syncRoot;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoFixedStack{T}"/> class that is empty and has the specified capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the <see cref="UndoRedoFixedStack{T}"/> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than or equal to zero.</exception>
        public UndoRedoFixedStack(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "capacity is less than or equal to zero.");
            _array = new T[capacity];
            _start = 0;
            _undos = 0;
            _redos = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoFixedStack{T}"/> class that contains elements copied from the specified collection as undo elements at set capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the <see cref="UndoRedoFixedStack{T}"/> can contain.</param>
        /// <param name="collection">The collection to copy elements from.</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than or equal to zero.</exception>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        /// <exception cref="ArgumentException">size of collection is bigger than capacity.</exception>
        public UndoRedoFixedStack(int capacity, IEnumerable<T> collection) : this(capacity)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection), "collection is null.");
            foreach (T element in collection)
            {
                if (_undos == capacity) throw new ArgumentException("size of collection is bigger than capacity.", nameof(collection));
                _array[_undos] = element;
                _undos++;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoFixedStack{T}"/> class that contains elements copied from the specified collection as undo elements with specified redo elements at set capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the <see cref="UndoRedoFixedStack{T}"/> can contain.</param>
        /// <param name="collection">The collection to copy elements from.</param>
        /// <param name="redoCount">The number of elements in <paramref name="collection"/> to push to redo stack.</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than or equal to zero.</exception>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">redoCount is less than zero.</exception>
        /// <exception cref="ArgumentException">size of collection is bigger than capacity -or- redoCount is greater than the number of elements in collection.</exception>
        public UndoRedoFixedStack(int capacity, IEnumerable<T> collection, int redoCount) : this(capacity, collection)
        {
            if (redoCount < 0) throw new ArgumentOutOfRangeException(nameof(redoCount), "redoCount is less than zero.");
            if (redoCount > _undos) throw new ArgumentException("redoCount is greater than the number of elements in collection.", nameof(redoCount));
            _redos = redoCount;
            _undos -= redoCount;
        }

        /// <summary>
        /// Gets the actual index of the given index within the rolling buffer.
        /// </summary>
        /// <param name="index">Desired index.</param>
        /// <returns>The desired index within the rolling buffer.</returns>
        private int GetIndex(int index)
        {
            Debug.Assert(index >= 0);

            // convert actual index to unsigned integer to deal with overflow (_start + index < 0)
            uint _index = (uint)(_start + index);
            _index %= (uint)_array.Length;
            return (int)_index;
        }

        /// <summary>
        /// Removes all objects from the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        public void Clear()
        {
            int _count = _undos + _redos;
            if (_count > 0)
            {
                if (_count == _array.Length)
                {
                    Array.Clear(_array, 0, _count);
                }
                else
                {
                    int _length = _array.Length - _start;

                    if (_count > _length)
                    {

                        Array.Clear(_array, _start, _length);
                        Array.Clear(_array, 0, _count - _length);
                    }
                    else
                    {
                        Array.Clear(_array, _start, _count);
                    }
                }

                _start = 0;
                _undos = 0;
                _redos = 0;
            }
        }

        /// <summary>
        /// Removes all undo objects from the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        public void ClearUndo()
        {
            if (_undos > 0)
            {
                if (_undos == _array.Length)
                {
                    Array.Clear(_array, 0, _undos);

                    _start = 0;
                }
                else
                {
                    int _length = _array.Length - _start;

                    if (_undos > _length)
                    {
                        Array.Clear(_array, _start, _length);

                        _start = _undos - _length;
                        Array.Clear(_array, 0, _start);
                    }
                    else
                    {
                        Array.Clear(_array, _start, _undos);

                        _start += _undos;
                    }
                }

                _undos = 0;
            }
        }

        /// <summary>
        /// Removes all redo objects from the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        public void ClearRedo()
        {
            if (_redos > 0)
            {
                if (_redos == _array.Length)
                {
                    Array.Clear(_array, 0, _redos);

                    _start = 0;
                }
                else
                {
                    int _index = GetIndex(_undos);
                    int _length = _array.Length - _index;

                    if (_redos > _length)
                    {
                        Array.Clear(_array, _index, _length);
                        Array.Clear(_array, 0, _redos - _length);
                    }
                    else
                    {
                        Array.Clear(_array, _index, _redos);
                    }
                }

                _redos = 0;
            }
        }

        /// <summary>
        /// Removes the oldest undo object from the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The undo stack is empty.</exception>
        public void RemoveUndo()
        {
            if (_undos > 0)
            {
                _array[_start] = default;
                _undos--;
                _start = GetIndex(1);
            }
            else
            {
                throw new InvalidOperationException("The undo stack is empty.");
            }
        }

        /// <summary>
        /// Removes the oldest redo object from the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The redo stack is empty.</exception>
        public void RemoveRedo()
        {
            if (_redos > 0)
            {
                _array[GetIndex(_undos + _redos - 1)] = default;
                _redos--;
            }
            else
            {
                throw new InvalidOperationException("The redo stack is empty.");
            }
        }

        /// <summary>
        /// Removes the oldest count of undo objects from the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <param name="count">number of undo objects to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">count is greater than the number of undo objects.</exception>
        public void RemoveUndo(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count is less than or equal to zero.");
            if (count <= _undos)
            {
                if (count == _array.Length)
                {
                    Array.Clear(_array, 0, count);

                    _start = 0;
                }
                else
                {
                    int _length = _array.Length - _start;

                    if (count > _length)
                    {
                        Array.Clear(_array, _start, _length);

                        _start = count - _length;
                        Array.Clear(_array, 0, _start);
                    }
                    else
                    {
                        Array.Clear(_array, _start, count);

                        _start += count;
                    }
                }

                _undos -= count;
            }
            else
            {
                throw new ArgumentException("count is greater than the number of undo objects.", nameof(count));
            }
        }

        /// <summary>
        /// Removes the oldest count of redo objects from the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <param name="count">number of redo objects to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">count is greater than the number of redo objects.</exception>
        public void RemoveRedo(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count is less than or equal to zero.");
            if (count <= _redos)
            {
                if (count == _array.Length)
                {
                    Array.Clear(_array, 0, count);

                    _start = 0;
                    _redos = 0;
                }
                else
                {
                    _redos -= count;
                    int _index = GetIndex(_undos + _redos);
                    int _length = _array.Length - _index;

                    if (count > _length)
                    {
                        Array.Clear(_array, _index, _length);
                        Array.Clear(_array, 0, count - _length);
                    }
                    else
                    {
                        Array.Clear(_array, _index, count);
                    }
                }
            }
            else
            {
                throw new ArgumentException("count is greater than the number of redo objects.", nameof(count));
            }
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="UndoRedoFixedStack{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="UndoRedoFixedStack{T}"/>. The value can be null for reference types.</param>
        /// <returns>
        /// true if item is found in the <see cref="UndoRedoFixedStack{T}"/>; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            int j = _start;
            int _count = _undos + _redos;

            if (item == null)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_array[j] == null) return true;

                    j++;
                    if (j == _array.Length)
                    {
                        for (j = 0, i++; i < _count; i++, j++)
                        {
                            if (_array[j] == null) return true;
                        }
                        return false;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _count; i++)
                {
                    if (item.Equals(_array[j])) return true;

                    j++;
                    if (j == _array.Length)
                    {
                        for (j = 0, i++; i < _count; i++, j++)
                        {
                            if (item.Equals(_array[j])) return true;
                        }
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the <see cref="UndoRedoFixedStack{T}"/> to an existing one-dimensional <see cref="Array"/>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="UndoRedoFixedStack{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException">array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source <see cref="UndoRedoFixedStack{T}"/> is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            int _count = _undos + _redos;
            if ((_start + _count) > array.Length)
            {
                int _length = _array.Length - _start;
                Array.Copy(_array, _start, array, arrayIndex, _length);
                Array.Copy(_array, 0, array, arrayIndex + _length, _count - _length);
            }
            else
            {
                Array.Copy(_array, _start, array, arrayIndex, _count);
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
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array), "array is null.");
            if (array.Rank != 1) throw new ArgumentException("array is multidimensional.", nameof(array));

            int _count = _undos + _redos;
            if ((_start + _count) > array.Length)
            {
                int _length = _array.Length - _start;
                Array.Copy(_array, _start, array, arrayIndex, _length);
                Array.Copy(_array, 0, array, arrayIndex + _length, _count - _length);
            }
            else
            {
                Array.Copy(_array, _start, array, arrayIndex, _count);
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
            int _count = _undos + _redos;

            for (int i = 0, j = _start; i < _count; i++)
            {
                yield return _array[j];

                j++;
                if (j >= _array.Length)
                {
                    for (j = 0, i++; i < _count; i++, j++)
                    {
                        yield return _array[j];
                    }
                    yield break;
                }
            }
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
        /// Returns the object at the top of the <see cref="UndoRedoFixedStack{T}"/> undo stack without moving it to the redo stack.
        /// </summary>
        /// <returns>
        /// The object at the top of the <see cref="UndoRedoFixedStack{T}"/> undo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The undo stack is empty.</exception>
        public T PeekUndo()
        {
            if (_undos > 0)
            {
                return _array[GetIndex(_undos - 1)];
            }
            else
            {
                throw new InvalidOperationException("The undo stack is empty.");
            }
        }

        /// <summary>
        /// Returns the object at the top of the <see cref="UndoRedoFixedStack{T}"/> redo stack without moving it to the undo stack.
        /// </summary>
        /// <returns>
        /// The object at the top of the <see cref="UndoRedoFixedStack{T}"/> redo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The redo stack is empty.</exception>
        public T PeekRedo()
        {
            if (_redos > 0)
            {
                return _array[GetIndex(_undos)];
            }
            else
            {
                throw new InvalidOperationException("The redo stack is empty.");
            }
        }

        /// <summary>
        /// Removes and returns the object at the top of the <see cref="UndoRedoFixedStack{T}"/> undo stack, moving it to the redo stack.
        /// </summary>
        /// <returns>
        /// The object removed from the top of the <see cref="UndoRedoFixedStack{T}"/> undo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The undo stack is empty.</exception>
        public T PopUndo()
        {
            if (_undos > 0)
            {
                T item = _array[GetIndex(_undos - 1)];
                _undos--;
                _redos++;
                return item;
            }
            else
            {
                throw new InvalidOperationException("The undo stack is empty.");
            }
        }

        /// <summary>
        /// Removes and returns the object at the top of the <see cref="UndoRedoFixedStack{T}"/> redo stack, moving it to the undo stack.
        /// </summary>
        /// <returns>
        /// The object removed from the top of the <see cref="UndoRedoFixedStack{T}"/> redo stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">The redo stack is empty.</exception>
        public T PopRedo()
        {
            if (_redos > 0)
            {
                T item = _array[GetIndex(_undos)];
                _undos++;
                _redos--;
                return item;
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

            _undos += count;
            _redos -= count;
            return new RedoStack(this, count);
        }

        /// <summary>
        /// Inserts an object at the top of the <see cref="UndoRedoFixedStack{T}"/> undo stack, clearing the redo stack.
        /// </summary>
        /// <param name="item">The object to push onto the <see cref="UndoRedoFixedStack{T}"/> undo stack. The value can be null for reference types.</param>
        public void Push(T item)
        {
            if (_undos < _array.Length)
            {
                _array[GetIndex(_undos)] = item;
                _undos++;
                if (_redos > 0)
                {
                    _redos--;
                    ClearRedo();
                }
            }
            else
            {
                _array[_start] = item;
                _start = GetIndex(1);
            }
        }

        /// <summary>
        /// Inserts a collection at the top of the <see cref="UndoRedoStack{T}"/> undo stack, clearing the redo stack.
        /// </summary>
        /// <param name="collection">The collection to push onto the <see cref="UndoRedoStack{T}"/> undo stack.</param>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        public void Push(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection), "collection is null.");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                int _count = 0;
                while (enumerator.MoveNext())
                {
                    if (_undos < _array.Length)
                    {
                        _array[GetIndex(_undos)] = enumerator.Current;
                        _undos++;
                        _count++;
                    }
                    else
                    {
                        do
                        {
                            _array[_start] = enumerator.Current;
                            _start = GetIndex(1);
                        }
                        while (enumerator.MoveNext());

                        _redos = 0;
                        return;
                    }
                }

                if (_redos > _count)
                {
                    _redos -= _count;
                    ClearRedo();
                }
                else
                {
                    _redos = 0;
                }
            }
        }

        /// <summary>
        /// Copies the <see cref="UndoRedoFixedStack{T}"/> to a new array.
        /// </summary>
        /// <returns>
        /// A new array containing copies of the elements of the <see cref="UndoRedoFixedStack{T}"/>.
        /// </returns>
        public T[] ToArray()
        {
            int _count = _undos + _redos;
            T[] array = new T[_count];

            for (int i = 0, j = _start; i < _count; i++)
            {
                array[i] = _array[j];

                j++;
                if (j == _array.Length)
                {
                    for (j = 0, i++; i < _count; i++, j++)
                    {
                        array[i] = _array[j];
                    }
                    return array;
                }
            }

            return array;
        }
    }
}

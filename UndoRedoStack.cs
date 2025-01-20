using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a variable size, undo/redo last-in-first-out (LIFO) collection of instances of the same specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the undo/redo stack.</typeparam>
    [Serializable]
    [ComVisible(false)]
    public class UndoRedoStack<T> : IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        private readonly List<T> _stack;
        private int _redos;
        private UndoStack _undostack = null;
        private RedoStack _redostack = null;

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
            get => _stack.Capacity;
            set => _stack.Capacity = value;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public int Count => _stack.Count;

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

        private sealed class UndoStack : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IEnumerator<T>, IEnumerator
        {
            private readonly int _length;
            private readonly UndoRedoStack<T> _this;
            private readonly int _threadId;
            private int _state;
            private T _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="UndoStack{T}"/> class.
            /// </summary>
            public UndoStack(UndoRedoStack<T> stack, int count)
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
            /// Gets the number of undos contained in the <see cref="UndoRedoStack{T}"/>.
            /// </summary>
            /// <returns>
            /// The number of undos contained in the <see cref="UndoRedoStack{T}"/>.
            /// </returns>
            public int Count => _length == 0 ? _this._stack.Count - _this._redos : Math.Min(_length, _this._redos);

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                UndoStack _enum;
                if ((_state == -1) && (_threadId == Environment.CurrentManagedThreadId))
                {
                    _state = 0;
                    _enum = this;
                }
                else
                {
                    _enum = new UndoStack(_this, _length)
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
                    return _this._stack[_this._stack.Count + Math.Min(_length - _this._redos, 0) - index - 1];
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

                _state++;
                _current = _this._stack[_this._stack.Count + Math.Min(_length - _this._redos, 0) - _state];
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
        object ICollection.SyncRoot => ((ICollection)_stack).SyncRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that is empty and has the default initial capacity.
        /// </summary>
        public UndoRedoStack()
        {
            _stack = new List<T>();
            _redos = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="UndoRedoStack{T}"/> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than zero.</exception>
        public UndoRedoStack(int capacity)
        {
            _stack = new List<T>(capacity);
            _redos = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that contains elements copied from the specified collection as undo elements and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection to copy elements from.</param>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        public UndoRedoStack(IEnumerable<T> collection)
        {
            _stack = new List<T>(collection);
            _redos = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoStack{T}"/> class that contains elements copied from the specified collection as undo elements with specified redo elements and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection to copy elements from.</param>
        /// <param name="redoCount">The number of elements in <paramref name="collection"/> to push to redo stack.</param>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">redoCount is less than zero.</exception>
        /// <exception cref="ArgumentException">redoCount is greater than the number of elements in collection.</exception>
        public UndoRedoStack(IEnumerable<T> collection, int redoCount)
        {
            if (redoCount < 0) throw new ArgumentOutOfRangeException(nameof(redoCount), "redoCount is less than zero.");
            _stack = new List<T>(collection);
            if (redoCount > _stack.Count) throw new ArgumentException("redoCount is greater than the number of elements in collection.", nameof(redoCount));
            _redos = redoCount;
        }

        /// <summary>
        /// Removes all objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
            _redos = 0;
        }

        /// <summary>
        /// Removes all undo objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        public void ClearUndo()
        {
            int _count = _stack.Count - _redos;
            if (_count > 0) _stack.RemoveRange(0, _count);
        }

        /// <summary>
        /// Removes all redo objects from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        public void ClearRedo()
        {
            if (_redos > 0)
            {
                _stack.RemoveRange(_stack.Count - _redos, _redos);
                _redos = 0;
            }
        }

        /// <summary>
        /// Removes the oldest undo object from the <see cref="UndoRedoStack{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The undo stack is empty.</exception>
        public void RemoveUndo()
        {
            int _count = _stack.Count - _redos;
            if (_count > 0)
            {
                _stack.RemoveAt(0);
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
                _stack.RemoveAt(_stack.Count - 1);
                _redos--;
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
            if (count <= (_stack.Count - _redos))
            {
                _stack.RemoveRange(0, count);
            }
            else
            {
                throw new ArgumentException("count is greater than the number of undo objects.", nameof(count));
            }
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
            if (count <= _redos)
            {
                _stack.RemoveRange(_stack.Count - count, count);
                _redos -= count;
            }
            else
            {
                throw new ArgumentException("count is greater than the number of redo objects.", nameof(count));
            }
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
            return _stack.Contains(item);
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
            _stack.CopyTo(array, arrayIndex);
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
            ((ICollection)_stack).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _stack.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_stack).GetEnumerator();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the <see cref="UndoRedoStack{T}"/>, if that number is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            _stack.TrimExcess();
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
            int _count = _stack.Count - _redos;
            if (_count > 0)
            {
                return _stack[_count - 1];
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
                return _stack[_stack.Count - _redos];
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
            int _count = _stack.Count - _redos;
            if (_count > 0)
            {
                _redos++;
                return _stack[_count - 1];
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
                int index = _stack.Count - _redos;
                _redos--;
                return _stack[index];
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
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero -or- greater than <see cref="Undos"/>.Count.</exception>
        public IReadOnlyList<T> PopUndo(int count)
        {
            int _count = _stack.Count - _redos;
            if ((count <= 0) || (count > _count)) throw new ArgumentOutOfRangeException(nameof(count));

            _redos += count;
            return new UndoStack(this, count);
        }

        /// <summary>
        /// Removes and returns <paramref name="count"/> objects at the top of the <see cref="UndoRedoStack{T}"/> redo stack, moving them to the undo stack.
        /// </summary>
        /// <param name="count">The number of objects to pop off the <see cref="UndoRedoStack{T}"/> redo stack.</param>
        /// <returns>A list of the objects removed from the top of the <see cref="UndoRedoStack{T}"/> redo stack.</returns>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero -or- greater than <see cref="Redos"/>.Count.</exception>
        public IReadOnlyList<T> PopRedo(int count)
        {
            if ((count <= 0) || (count > _redos)) throw new ArgumentOutOfRangeException(nameof(count));

            _redos -= count;
            return new RedoStack(this, count);
        }

        /// <summary>
        /// Inserts an object at the top of the <see cref="UndoRedoStack{T}"/> undo stack, clearing the redo stack.
        /// </summary>
        /// <param name="item">The object to push onto the <see cref="UndoRedoStack{T}"/> undo stack. The value can be null for reference types.</param>
        public void Push(T item)
        {
            ClearRedo();
            _stack.Add(item);
        }

        /// <summary>
        /// Copies the <see cref="UndoRedoStack{T}"/> to a new array.
        /// </summary>
        /// <returns>
        /// A new array containing copies of the elements of the <see cref="UndoRedoStack{T}"/>.
        /// </returns>
        public T[] ToArray()
        {
            return _stack.ToArray();
        }
    }


    /// <summary>
    /// Represents a fixed size, rolling buffer, undo/redo last-in-first-out (LIFO) collection of instances of the same specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the undo/redo stack.</typeparam>
    [Serializable]
    [ComVisible(false)]
    public class UndoRedoFixedStack<T> : IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable
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

        private sealed class UndoStack : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IEnumerator<T>, IEnumerator
        {
            private readonly int _length;
            private readonly UndoRedoFixedStack<T> _this;
            private readonly int _threadId;
            private int _state;
            private T _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="UndoStack{T}"/> class.
            /// </summary>
            public UndoStack(UndoRedoFixedStack<T> stack, int count)
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
            /// Gets the number of undos contained in the <see cref="UndoRedoFixedStack{T}"/>.
            /// </summary>
            /// <returns>
            /// The number of undos contained in the <see cref="UndoRedoFixedStack{T}"/>.
            /// </returns>
            public int Count => _length == 0 ? _this._undos : Math.Min(_length, _this._redos);

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                UndoStack _enum;
                if ((_state == -1) && (_threadId == Environment.CurrentManagedThreadId))
                {
                    _state = 0;
                    _enum = this;
                }
                else
                {
                    _enum = new UndoStack(_this, _length)
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
                    return _this._array[(_this._start + _this._undos + Math.Min(_length, _this._redos) - index - 1) % _this._array.Length];
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

                _state++;
                _current = _this._array[(_this._start + _this._undos + Math.Min(_length, _this._redos) - _state) % _this._array.Length];
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

        private sealed class RedoStack : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IEnumerator<T>, IEnumerator
        {
            private readonly int _length;
            private readonly UndoRedoFixedStack<T> _this;
            private readonly int _threadId;
            private int _state;
            private T _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="UndoStack{T}"/> class.
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
            /// Gets the number of redos contained in the <see cref="UndoRedoFixedStack{T}"/>.
            /// </summary>
            /// <returns>
            /// The number of redos contained in the <see cref="UndoRedoFixedStack{T}"/>.
            /// </returns>
            public int Count => _length == 0 ? _this._redos : Math.Min(_length, _this._undos);

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
                    return _this._array[(_this._start + Math.Max(_this._undos - _length, 0) + index) % _this._array.Length];
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

                _current = _this._array[(_this._start + Math.Max(_this._undos - _length, 0) + _state) % _this._array.Length];
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
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than zero.</exception>
        public UndoRedoFixedStack(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "capacity is less than zero.");
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
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than zero.</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than zero.</exception>
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
                else if ((_start + _count) > _array.Length)
                {
                    int _length = _array.Length - _start;
                    Array.Clear(_array, _start, _length);
                    Array.Clear(_array, 0, _count - _length);
                }
                else
                {
                    Array.Clear(_array, _start, _count);
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
                else if ((_start + _undos) > _array.Length)
                {
                    int _length = _array.Length - _start;
                    Array.Clear(_array, _start, _length);

                    _start = _undos - _length;
                    Array.Clear(_array, 0, _start);
                }
                else
                {
                    Array.Clear(_array, _start, _undos);

                    _start += _undos;
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
                    int _index = (_start + _undos) % _array.Length;

                    if ((_index + _redos) > _array.Length)
                    {
                        int _length = _array.Length - _index;
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
                _start = (_start + 1) % _array.Length;
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
                _array[(_start + _undos + _redos - 1) % _array.Length] = default;
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
                else if ((_start + count) > _array.Length)
                {
                    int _length = _array.Length - _start;
                    Array.Clear(_array, _start, _length);

                    _start = count - _length;
                    Array.Clear(_array, 0, _start);
                }
                else
                {
                    Array.Clear(_array, _start, count);

                    _start += count;
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
                    int _index = (_start + _undos + _redos) % _array.Length;

                    if ((_index + count) > _array.Length)
                    {
                        int _length = _array.Length - _index;
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
            if (array != null && array.Rank != 1)
            {
                throw new ArgumentException("array is multidimensional.", nameof(array));
            }

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
                return _array[(_start + _undos - 1) % _array.Length];
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
                return _array[(_start + _undos) % _array.Length];
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
                T item = _array[(_start + _undos - 1) % _array.Length];
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
                T item = _array[(_start + _undos) % _array.Length];
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
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero -or- greater than <see cref="Undos"/>.Count.</exception>
        public IReadOnlyList<T> PopUndo(int count)
        {
            if ((count <= 0) || (count > _undos)) throw new ArgumentOutOfRangeException(nameof(count));

            _undos -= count;
            _redos += count;
            return new UndoStack(this, count);
        }

        /// <summary>
        /// Removes and returns <paramref name="count"/> objects at the top of the <see cref="UndoRedoStack{T}"/> redo stack, moving them to the undo stack.
        /// </summary>
        /// <param name="count">The number of objects to pop off the <see cref="UndoRedoStack{T}"/> redo stack.</param>
        /// <returns>A list of the objects removed from the top of the <see cref="UndoRedoStack{T}"/> redo stack.</returns>
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero -or- greater than <see cref="Redos"/>.Count.</exception>
        public IReadOnlyList<T> PopRedo(int count)
        {
            if ((count <= 0) || (count > _redos)) throw new ArgumentOutOfRangeException(nameof(count));

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
            _array[(_start + _undos) % _array.Length] = item;

            if (_undos < _array.Length)
            {
                _undos++;
                if (_redos > 0)
                {
                    _redos--;
                    ClearRedo();
                }
            }
            else
            {
                _start = (_start + 1) % _array.Length;
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

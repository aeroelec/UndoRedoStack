﻿using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a variable size, undo/redo last-in-first-out (LIFO) collection of instances of the same specified type.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the undo/redo stack.</typeparam>
    [Serializable]
    [ComVisible(false)]
    public partial class UndoRedoStack<T> : IReadOnlyCollection<T>, ICollection, IEnumerable<T>, IEnumerable
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
        /// <exception cref="ArgumentOutOfRangeException">count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">count is greater than the number of undo objects.</exception>
        public IReadOnlyList<T> PopUndo(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count is less than or equal to zero.");
            int _count = _stack.Count - _redos;
            if (count > _count) throw new ArgumentException("count is greater than the number of undo objects.", nameof(count));

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
        /// Inserts a collection at the top of the <see cref="UndoRedoStack{T}"/> undo stack, clearing the redo stack.
        /// </summary>
        /// <param name="collection">The collection to push onto the <see cref="UndoRedoStack{T}"/> undo stack.</param>
        /// <exception cref="ArgumentNullException">collection is null.</exception>
        public void Push(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection), "collection is null.");

            ClearRedo();
            _stack.AddRange(collection);
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
}

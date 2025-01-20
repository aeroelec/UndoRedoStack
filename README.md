# UndoRedoStack
Generic stack for storing any type of undo/redo data

In general, there are three approaches used for journaling undo/redo actions
1. Memento Pattern: Storing each state as it changes
2. Command Pattern: Storing actions taken to change state
3. State Diffing: Storing the difference between states

In all cases, two stacks are generally used - an undo stack and a redo stack. As actions are taken, they are added to the undo stack. If actions are undone, then they are moved from the undo stack to the redo stack, and vice-versa if they are redone. In all cases, if a new action is done after undoing another action, the redo stack is emptied. Therefore, the undo and redo stacks can be simplified to reduce memory usage and processing time by combining them into a single stack with a pointer to the current undo/redo position.

This repository defines two types of undo/redo stacks.
+ `UndoRedoStack<T>` is a variable-length undo/redo stack. This stack is ideal if you want to save an unknown number of undos.
+ `UndoRedoFixedStack<T>` is a fixed-length undo/redo stack. It uses a rolling buffer to automatically remove the oldest undo action when the undo stack is full. This stack is ideal when you have a fixed number of undos you want to save.

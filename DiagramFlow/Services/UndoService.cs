using System;
using System.Collections.Generic;
using System.Linq;

namespace DiagramFlow.Services
{
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();
        string Name { get; }
    }

    public class UndoService
    {
        private readonly Stack<IUndoableCommand> _undoStack = new Stack<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoStack = new Stack<IUndoableCommand>();
        private readonly int _maxHistory = 100;

        public event EventHandler StateChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Execute(IUndoableCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            TrimHistory();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddToHistory(IUndoableCommand command)
        {
            // Use this when the command has already been executed (e.g. by dragging)
            _undoStack.Push(command);
            _redoStack.Clear();
            TrimHistory();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TrimHistory()
        {
            if (_undoStack.Count > _maxHistory)
            {
                // Convert stack to list, keep only the most recent _maxHistory items
                var items = new List<IUndoableCommand>(_undoStack);
                items.Reverse(); // Reverse to get chronological order (oldest first)
                
                // Keep only the most recent _maxHistory items
                var trimmedItems = items.Skip(items.Count - _maxHistory).ToList();
                
                // Clear and rebuild the stack
                _undoStack.Clear();
                trimmedItems.Reverse(); // Reverse back to stack order (newest first)
                foreach (var item in trimmedItems)
                {
                    _undoStack.Push(item);
                }
            }
        }
    }
}
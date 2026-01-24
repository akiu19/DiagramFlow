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
                // Convert to array to preserve order, then take only the most recent _maxHistory items
                var items = _undoStack.ToArray();
                
                // Clear and rebuild the stack with only the most recent items
                _undoStack.Clear();
                
                // Push items back in reverse order (from oldest of the kept items to newest)
                // to maintain correct stack order
                for (int i = _maxHistory - 1; i >= 0; i--)
                {
                    _undoStack.Push(items[i]);
                }
            }
        }
    }
}
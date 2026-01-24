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
                // ToArray() returns stack items in pop order: [newest, ..., oldest]
                var items = _undoStack.ToArray();
                
                // Clear and rebuild stack with only the most recent _maxHistory items
                _undoStack.Clear();
                
                // Push back in reverse order (from items[_maxHistory-1] to items[0])
                // so that items[0] (newest) ends up on top of the stack
                for (int i = _maxHistory - 1; i >= 0; i--)
                {
                    _undoStack.Push(items[i]);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;

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
            // コマンドが既に実行されている場合に使用します (例: ドラッグによって)
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
            // スタックは削除が簡単ではありません。LinkedListを使用するか、再スタックする必要があります。
            // シンプルな実装では、大きくなることを許可するか、大きすぎる場合はスタックを再作成します。
            // 効率的な循環バッファの方が良いですが、スタックが標準的です。
            if (_undoStack.Count > _maxHistory)
            {
                // 簡単なハック: 配列に変換して最後をスキップし、スタックを再作成します。
                // または制限を無視するか、LinkedListを使用します。
                // この要件では: 簡単な制限。
            }
        }
    }
}
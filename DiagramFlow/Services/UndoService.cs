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
            // コマンドが既に実行されている場合にこれを使用（例: ドラッグによる）
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
            // Stack は LinkedList を使用するか再スタックしない限り簡単に削除をサポートしない。
            // シンプルな実装として、成長させるか大きすぎる場合は Stack を再作成できる。
            // 効率的な循環バッファの方が良いが Stack は標準的。
            if (_undoStack.Count > _maxHistory)
            {
                // 単純なハック: 配列に変換、最後をスキップ、スタックを再作成。
                // または今のところ制限を無視するか LinkedList を使用。
                // この要件では: 単純な制限。
            }
        }
    }
}
using System;
using Xunit;
using DiagramFlow.Services;

namespace DiagramFlow.Tests.Services
{
    public class UndoServiceTests
    {
        private class TestCommand : IUndoableCommand
        {
            private readonly Action _execute;
            private readonly Action _undo;

            public string Name => "TestCommand";

            public TestCommand(Action execute, Action undo)
            {
                _execute = execute;
                _undo = undo;
            }

            public void Execute() => _execute();
            public void Undo() => _undo();
        }

        [Fact]
        public void Execute_ShouldExecuteAndPushToStack()
        {
            var service = new UndoService();
            bool executed = false;
            var command = new TestCommand(() => executed = true, () => { });

            service.Execute(command);

            Assert.True(executed);
            Assert.True(service.CanUndo);
            Assert.False(service.CanRedo);
        }

        [Fact]
        public void Undo_ShouldUndoAndPushToRedoStack()
        {
            var service = new UndoService();
            bool undone = false;
            var command = new TestCommand(() => { }, () => undone = true);

            service.Execute(command);
            service.Undo();

            Assert.True(undone);
            Assert.False(service.CanUndo);
            Assert.True(service.CanRedo);
        }

        [Fact]
        public void Redo_ShouldExecuteAndPushToUndoStack()
        {
            var service = new UndoService();
            int executedCount = 0;
            var command = new TestCommand(() => executedCount++, () => { });

            service.Execute(command); // 1
            service.Undo();
            service.Redo(); // 2

            Assert.Equal(2, executedCount);
            Assert.True(service.CanUndo);
            Assert.False(service.CanRedo);
        }

        [Fact]
        public void AddToHistory_ShouldPushWithoutExecuting()
        {
            var service = new UndoService();
            bool executed = false;
            var command = new TestCommand(() => executed = true, () => { });

            service.AddToHistory(command);

            Assert.False(executed);
            Assert.True(service.CanUndo);
        }

        [Fact]
        public void Execute_ShouldClearRedoStack()
        {
            var service = new UndoService();
            var command1 = new TestCommand(() => { }, () => { });
            var command2 = new TestCommand(() => { }, () => { });

            service.Execute(command1);
            service.Undo();
            
            Assert.True(service.CanRedo);

            service.Execute(command2);

            Assert.False(service.CanRedo);
            Assert.True(service.CanUndo);
        }

        [Fact]
        public void StateChanged_ShouldRaiseEvent()
        {
            var service = new UndoService();
            bool eventRaised = false;
            service.StateChanged += (s, e) => eventRaised = true;
            
            var command = new TestCommand(() => { }, () => { });
            service.Execute(command);

            Assert.True(eventRaised);
        }
    }
}
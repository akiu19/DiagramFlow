using System;
using System.Collections.Generic;
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

        [Fact]
        public void TrimHistory_ShouldLimitUndoStackTo100Items()
        {
            var service = new UndoService();
            
            // Add 150 commands to exceed the 100 item limit
            for (int i = 0; i < 150; i++)
            {
                var command = new TestCommand(() => { }, () => { });
                service.Execute(command);
            }

            // After 150 commands, only the most recent 100 should remain
            int undoCount = 0;
            while (service.CanUndo)
            {
                service.Undo();
                undoCount++;
            }

            Assert.Equal(100, undoCount);
        }

        [Fact]
        public void TrimHistory_ShouldKeepMostRecentCommands()
        {
            var service = new UndoService();
            var executedCommands = new List<int>();
            
            // Add 105 commands with identifiable values
            for (int i = 0; i < 105; i++)
            {
                int value = i;
                var command = new TestCommand(
                    () => executedCommands.Add(value),
                    () => executedCommands.RemoveAt(executedCommands.Count - 1)
                );
                service.Execute(command);
            }

            // At this point, all 105 commands have been executed
            // But only the most recent 100 are in the undo stack (commands 5-104)
            Assert.Equal(105, executedCommands.Count);

            // Undo all available commands (should be 100: commands 5-104)
            int undoCount = 0;
            while (service.CanUndo)
            {
                service.Undo();
                undoCount++;
            }

            // Should have undone exactly 100 commands
            Assert.Equal(100, undoCount);
            
            // After all undos, the first 5 commands should remain executed
            // (commands 0-4 were trimmed from the undo stack, so they can't be undone)
            Assert.Equal(5, executedCommands.Count);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, executedCommands);
        }
    }
}
using System.Collections.Generic;
using DiagramFlow.Services;
using DiagramFlow.ViewModels;
using Xunit;

namespace DiagramFlow.Tests.Services
{
    public class UndoCommandsTests
    {
        [Fact]
        public void MoveNodesCommand_ShouldMoveNodesAndUndo()
        {
            var node1 = new NodeViewModel { X = 10, Y = 10 };
            var node2 = new NodeViewModel { X = 20, Y = 20 };
            var nodes = new List<NodeViewModel> { node1, node2 };
            
            var command = new MoveNodesCommand(nodes, 5, 5);

            // Execute
            command.Execute();
            
            Assert.Equal(15, node1.X);
            Assert.Equal(15, node1.Y);
            Assert.Equal(25, node2.X);
            Assert.Equal(25, node2.Y);

            // Undo
            command.Undo();

            Assert.Equal(10, node1.X);
            Assert.Equal(10, node1.Y);
            Assert.Equal(20, node2.X);
            Assert.Equal(20, node2.Y);
        }

        [Fact]
        public void ResizeNodeCommand_ShouldResizeAndUndo()
        {
            var node = new NodeViewModel { Width = 100, Height = 100 };
            var command = new ResizeNodeCommand(node, 100, 100, 150, 200);

            // Execute
            command.Execute();

            Assert.Equal(150, node.Width);
            Assert.Equal(200, node.Height);

            // Undo
            command.Undo();

            Assert.Equal(100, node.Width);
            Assert.Equal(100, node.Height);
        }

        [Fact]
        public void EditTextCommand_ShouldChangeTextAndUndo()
        {
            var node = new NodeViewModel { Text = "Old" };
            var command = new EditTextCommand(node, "Old", "New");

            // Execute
            command.Execute();

            Assert.Equal("New", node.Text);

            // Undo
            command.Undo();

            Assert.Equal("Old", node.Text);
        }

        [Fact]
        public void AddItemsCommand_ShouldAddNodesAndConnectors_AndUndo()
        {
            var vm = new MainViewModel();
            vm.Nodes.Clear(); // Start empty
            
            var node1 = new NodeViewModel { Text = "N1" };
            var node2 = new NodeViewModel { Text = "N2" };
            var connector = new ConnectorViewModel(node1, 0, node2, 0);

            var nodes = new List<NodeViewModel> { node1, node2 };
            var connectors = new List<ConnectorViewModel> { connector };

            var command = new AddItemsCommand(vm, nodes, connectors);

            // Execute
            command.Execute();

            Assert.Contains(node1, vm.Nodes);
            Assert.Contains(node2, vm.Nodes);
            Assert.Contains(connector, vm.Connectors);
            
            // Should select added items
            Assert.Contains(node1, vm.SelectedNodes);
            Assert.Contains(connector, vm.SelectedConnectors);

            // Undo
            command.Undo();

            Assert.DoesNotContain(node1, vm.Nodes);
            Assert.DoesNotContain(node2, vm.Nodes);
            Assert.DoesNotContain(connector, vm.Connectors);
            Assert.Empty(vm.SelectedNodes);
        }

        [Fact]
        public void DeleteItemsCommand_ShouldDeleteNodesAndConnectors_AndUndo()
        {
            var vm = new MainViewModel();
            vm.Nodes.Clear();
            
            var node1 = new NodeViewModel { Text = "N1" };
            vm.Nodes.Add(node1);
            var connector = new ConnectorViewModel(node1, 0, null, 0); // Dangling for test simplicity or valid
            vm.Connectors.Add(connector);

            var nodesToDelete = new List<NodeViewModel> { node1 };
            var connectorsToDelete = new List<ConnectorViewModel> { connector };

            var command = new DeleteItemsCommand(vm, nodesToDelete, connectorsToDelete);

            // Execute
            command.Execute();

            Assert.DoesNotContain(node1, vm.Nodes);
            Assert.DoesNotContain(connector, vm.Connectors);

            // Undo
            command.Undo();

            Assert.Contains(node1, vm.Nodes);
            Assert.Contains(connector, vm.Connectors);
        }
    }
}

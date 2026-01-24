using System.Linq;
using Xunit;
using DiagramFlow.ViewModels;

namespace DiagramFlow.Tests.ViewModels
{
    public class MainViewModelTests
    {
        [Fact]
        public void ZoomScale_InitialValue()
        {
            var vm = new MainViewModel();
            Assert.Equal(1.0, vm.ZoomScale);
        }

        [Fact]
        public void ZoomScale_Clamping()
        {
            var vm = new MainViewModel();
            
            // Under lower limit
            vm.ZoomScale = 0.05;
            Assert.Equal(0.1, vm.ZoomScale);

            // Over upper limit
            vm.ZoomScale = 5.0;
            Assert.Equal(4.0, vm.ZoomScale);
            
            // Valid value
            vm.ZoomScale = 2.0;
            Assert.Equal(2.0, vm.ZoomScale);
        }

        [Fact]
        public void SelectNode_ShouldSelectNodeAndClearOthers()
        {
            var vm = new MainViewModel();
            var node1 = vm.Nodes[0];
            var node2 = vm.Nodes[1];

            // Select node1
            vm.SelectNode(node1);
            
            Assert.True(node1.IsSelected);
            Assert.Single(vm.SelectedNodes);
            Assert.Contains(node1, vm.SelectedNodes);

            // Select node2 (should clear node1)
            vm.SelectNode(node2);

            Assert.False(node1.IsSelected);
            Assert.True(node2.IsSelected);
            Assert.Single(vm.SelectedNodes);
            Assert.Contains(node2, vm.SelectedNodes);
        }

        [Fact]
        public void SelectNode_AddToSelection_ShouldKeepExistingSelection()
        {
            var vm = new MainViewModel();
            var node1 = vm.Nodes[0];
            var node2 = vm.Nodes[1];

            vm.SelectNode(node1);
            vm.SelectNode(node2, addToSelection: true);

            Assert.True(node1.IsSelected);
            Assert.True(node2.IsSelected);
            Assert.Equal(2, vm.SelectedNodes.Count);
        }

        [Fact]
        public void ToggleNodeSelection_ShouldToggleState()
        {
            var vm = new MainViewModel();
            var node1 = vm.Nodes[0];

            // Toggle On
            vm.ToggleNodeSelection(node1);
            Assert.True(node1.IsSelected);
            Assert.Contains(node1, vm.SelectedNodes);

            // Toggle Off
            vm.ToggleNodeSelection(node1);
            Assert.False(node1.IsSelected);
            Assert.DoesNotContain(node1, vm.SelectedNodes);
        }

        [Fact]
        public void AddNodeCommand_ShouldAddNode()
        {
            var vm = new MainViewModel();
            int initialCount = vm.Nodes.Count;

            vm.AddNodeCommand.Execute(null);

            Assert.Equal(initialCount + 1, vm.Nodes.Count);
        }

        [Fact]
        public void AddNodeCommand_ShouldAddNodeAndSupportUndoRedo()
        {
            var vm = new MainViewModel();
            int initialCount = vm.Nodes.Count;

            // Execute Add
            vm.AddNodeCommand.Execute(null);

            Assert.Equal(initialCount + 1, vm.Nodes.Count);
            Assert.True(vm.UndoService.CanUndo);

            // Execute Undo
            vm.UndoCommand.Execute(null);
            
            Assert.Equal(initialCount, vm.Nodes.Count);
            Assert.True(vm.UndoService.CanRedo);

            // Execute Redo
            vm.RedoCommand.Execute(null);

            Assert.Equal(initialCount + 1, vm.Nodes.Count);
        }

        [Fact]
        public void DeleteSelectedCommand_ShouldRemoveNodesAndConnectors()
        {
            var vm = new MainViewModel();
            // Clear default nodes for clean test
            vm.Nodes.Clear();

            var node1 = new NodeViewModel { Text = "N1" };
            var node2 = new NodeViewModel { Text = "N2" };
            vm.Nodes.Add(node1);
            vm.Nodes.Add(node2);

            // Add connector
            var connector = new ConnectorViewModel(node1, 0, node2, 0);
            vm.Connectors.Add(connector);

            // Select node1 and delete
            vm.SelectNode(node1);
            vm.DeleteSelectedCommand.Execute(null);

            // Node1 should be gone
            Assert.DoesNotContain(node1, vm.Nodes);
            Assert.Contains(node2, vm.Nodes);

            // Connector should be gone (monitoring cascading delete)
            Assert.Empty(vm.Connectors);
        }

        [Fact]
        public void DeleteSelectedCommand_ShouldRemoveSelectedConnectorOnly()
        {
            var vm = new MainViewModel();
            vm.Nodes.Clear();

            var node1 = new NodeViewModel();
            var node2 = new NodeViewModel();
            vm.Nodes.Add(node1);
            vm.Nodes.Add(node2);

            var connector = new ConnectorViewModel(node1, 0, node2, 0);
            vm.Connectors.Add(connector);

            // Select connector only
            vm.SelectConnector(connector);
            
            // Verify selection
            Assert.Single(vm.SelectedConnectors);
            Assert.Empty(vm.SelectedNodes);

            // Execute delete
            vm.DeleteSelectedCommand.Execute(null);

            // Connectors should be empty
            Assert.Empty(vm.Connectors);
            
            // Nodes should still exist
            Assert.Equal(2, vm.Nodes.Count);
        }

        [Fact]
        public void DeleteSelectedCommand_ShouldSupportUndoRedo()
        {
            var vm = new MainViewModel();
            vm.Nodes.Clear(); // Clear default nodes

            var node1 = new NodeViewModel { Text = "N1" };
            vm.Nodes.Add(node1);
            
            // Select and delete
            vm.SelectNode(node1);
            vm.DeleteSelectedCommand.Execute(null);

            Assert.Empty(vm.Nodes);
            Assert.True(vm.UndoService.CanUndo);

            // Undo
            vm.UndoCommand.Execute(null);
            Assert.Single(vm.Nodes);
            Assert.Contains(node1, vm.Nodes);

            // Redo
            vm.RedoCommand.Execute(null);
            Assert.Empty(vm.Nodes);
        }
    }
}
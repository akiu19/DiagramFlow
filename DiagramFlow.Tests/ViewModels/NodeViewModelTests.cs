using System.Windows.Media;
using DiagramFlow.ViewModels;
using Xunit;

namespace DiagramFlow.Tests.ViewModels
{
    public class NodeViewModelTests
    {
        [Fact]
        public void Properties_ShouldNotifyChanges()
        {
            var node = new NodeViewModel();
            Assert.PropertyChanged(node, nameof(node.X), () => node.X = 100);
            Assert.PropertyChanged(node, nameof(node.Y), () => node.Y = 100);
            Assert.PropertyChanged(node, nameof(node.Width), () => node.Width = 200);
            Assert.PropertyChanged(node, nameof(node.Height), () => node.Height = 200);
            Assert.PropertyChanged(node, nameof(node.Text), () => node.Text = "New Text");
            Assert.PropertyChanged(node, nameof(node.Background), () => node.Background = Brushes.Red);
            Assert.PropertyChanged(node, nameof(node.IsSelected), () => node.IsSelected = true);
            Assert.PropertyChanged(node, nameof(node.IsEditing), () => node.IsEditing = true);
        }

        [Fact]
        public void Size_ShouldRespectMinimums()
        {
            var node = new NodeViewModel();
            
            // Try setting below min
            node.Width = 10;
            node.Height = 10;

            Assert.Equal(NodeViewModel.MinWidth, node.Width);
            Assert.Equal(NodeViewModel.MinHeight, node.Height);
        }

        [Fact]
        public void GetPortPosition_ShouldReturnCorrectCoordinates()
        {
            var node = new NodeViewModel
            {
                X = 100,
                Y = 100,
                Width = 100,
                Height = 100
            };

            // Assuming GetPortPosition logic: 
            // 0: Top (50, 0) relative -> (150, 100) abs
            // 1: Bottom (50, 100) relative -> (150, 200) abs
            // 2: Left (0, 50) relative -> (100, 150) abs
            // 3: Right (100, 50) relative -> (200, 150) abs

            var top = node.GetPortPosition(0);
            Assert.Equal(150, top.X);
            Assert.Equal(100, top.Y);

            var bottom = node.GetPortPosition(1);
            Assert.Equal(150, bottom.X);
            Assert.Equal(200, bottom.Y);
        }
    }
}

using DiagramFlow.ViewModels;
using Xunit;

namespace DiagramFlow.Tests.ViewModels
{
    public class ConnectorViewModelTests
    {
        [Fact]
        public void UpdateCoordinates_ShouldUpdateWhenSourceNodeMoves()
        {
            var source = new NodeViewModel { X = 0, Y = 0, Width = 100, Height = 100 };
            var target = new NodeViewModel { X = 200, Y = 0, Width = 100, Height = 100 };
            
            // Assume Port 0 is Top (Width/2, 0) relative to Node
            // Port positions are calculated in NodeViewModel.GetPortPosition
            // But checking ConnectorViewModel logic specifically:
            
            var connector = new ConnectorViewModel(source, 0, target, 0);
            
            double initialX1 = connector.X1;
            double initialY1 = connector.Y1;

            // Move source node
            source.X = 50;
            source.Y = 50;

            Assert.NotEqual(initialX1, connector.X1);
            Assert.NotEqual(initialY1, connector.Y1);
            
            // Allow for some delta if calculation involves doubles, but X/Y should shift by 50
            Assert.Equal(initialX1 + 50, connector.X1);
            Assert.Equal(initialY1 + 50, connector.Y1);
        }

        [Fact]
        public void UpdateCoordinates_ShouldUpdateWhenTargetNodeMoves()
        {
            var source = new NodeViewModel { X = 0, Y = 0, Width = 100, Height = 100 };
            var target = new NodeViewModel { X = 200, Y = 0, Width = 100, Height = 100 };
            
            var connector = new ConnectorViewModel(source, 0, target, 0);
            
            double initialX2 = connector.X2;
            double initialY2 = connector.Y2;

            // Move target node
            target.X = 250;
            target.Y = 50;

            Assert.Equal(initialX2 + 50, connector.X2);
            Assert.Equal(initialY2 + 50, connector.Y2);
        }
    }
}

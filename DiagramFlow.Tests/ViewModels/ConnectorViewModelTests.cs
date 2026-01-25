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
            
            // ポート0は上（ノードに対して幅/2, 0）と仮定
            // ポート位置は NodeViewModel.GetPortPosition で計算される
            // ただし、ここでは特に ConnectorViewModel ロジックを確認:
            
            var connector = new ConnectorViewModel(source, 0, target, 0);
            
            double initialX1 = connector.X1;
            double initialY1 = connector.Y1;

            // ソースノードを移動
            source.X = 50;
            source.Y = 50;

            Assert.NotEqual(initialX1, connector.X1);
            Assert.NotEqual(initialY1, connector.Y1);
            
            // 計算に double が含まれる場合は多少の差を許容するが、X/Y は50ずつシフトするべき
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

            // ターゲットノードを移動
            target.X = 250;
            target.Y = 50;

            Assert.Equal(initialX2 + 50, connector.X2);
            Assert.Equal(initialY2 + 50, connector.Y2);
        }
    }
}

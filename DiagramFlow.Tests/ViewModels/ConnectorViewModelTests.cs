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
            
            // ポート0は上 (幅/2, 0) ノードに対する相対位置と想定
            // ポート位置はNodeViewModel.GetPortPositionで計算されます
            // ただし、ConnectorViewModelのロジックを具体的に確認します:
            
            var connector = new ConnectorViewModel(source, 0, target, 0);
            
            double initialX1 = connector.X1;
            double initialY1 = connector.Y1;

            // ソースノードを移動
            source.X = 50;
            source.Y = 50;

            Assert.NotEqual(initialX1, connector.X1);
            Assert.NotEqual(initialY1, connector.Y1);
            
            // 計算にdoubleが含まれる場合はいくらかのデルタを許容しますが、X/Yは50ずつシフトするべきです
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

using System.Windows.Media;
using DiagramFlow.Helpers;
using DiagramFlow.ViewModels;
using Xunit;

namespace DiagramFlow.Tests.Helpers
{
    public class DtoHelperTests
    {
        [Fact]
        public void ToDto_NodeViewModel_MapsPropertiesCorrectly()
        {
            // 準備
            var node = new NodeViewModel
            {
                X = 10,
                Y = 20,
                Width = 100,
                Height = 80,
                Text = "Test Node",
                Background = Brushes.Red,
                ShapeType = Models.ShapeType.Ellipse
            };

            // 実行
            var dto = node.ToDto();

            // 検証
            Assert.Equal(node.Id, dto.Id);
            Assert.Equal(10, dto.X);
            Assert.Equal(20, dto.Y);
            Assert.Equal(100, dto.Width);
            Assert.Equal(80, dto.Height);
            Assert.Equal("Test Node", dto.Text);
            Assert.Equal("#FFFF0000", dto.Color); // Colors.Redは#FFFF0000です
            Assert.Equal(Models.ShapeType.Ellipse, dto.ShapeType);
        }

        [Fact]
        public void ToDto_ConnectorViewModel_MapsPropertiesCorrectly()
        {
            // 準備
            var source = new NodeViewModel();
            var target = new NodeViewModel();
            var connector = new ConnectorViewModel(source, 1, target, 3);

            // 実行
            var dto = connector.ToDto();

            // 検証
            Assert.Equal(connector.Id, dto.Id);
            Assert.Equal(source.Id, dto.SourceNodeId);
            Assert.Equal(1, dto.SourcePort);
            Assert.Equal(target.Id, dto.TargetNodeId);
            Assert.Equal(3, dto.TargetPort);
        }
    }
}

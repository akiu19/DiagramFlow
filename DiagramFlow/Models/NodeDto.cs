using System;

namespace DiagramFlow.Models
{
    public class NodeDto
    {
        public Guid Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Text { get; set; }
        public string Color { get; set; }
        public ShapeType ShapeType { get; set; } = ShapeType.Rectangle;
    }
}

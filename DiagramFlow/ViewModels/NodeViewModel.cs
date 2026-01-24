using System;
using System.ComponentModel;
using System.Windows.Media;

namespace DiagramFlow.ViewModels
{
    public class NodeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Guid Id { get; set; } = Guid.NewGuid();

        // Position
        public double X { get; set; }
        public double Y { get; set; }

        // Size
        private double _width = 100;
        public double Width
        {
            get => _width;
            set => _width = Math.Max(MinWidth, value);
        }

        private double _height = 80;
        public double Height
        {
            get => _height;
            set => _height = Math.Max(MinHeight, value);
        }

        // Content
        public string Text { get; set; } = "Node";

        // Appearance
        public Brush Background { get; set; } = Brushes.LightBlue;

        // State
        public bool IsSelected { get; set; }
        public bool IsEditing { get; set; }

        // Minimum size constants
        public const double MinWidth = 30;
        public const double MinHeight = 30;

        public NodeViewModel()
        {
        }

        // Port positions (calculated from current position and size)
        // Port enum: 0=Top, 1=Bottom, 2=Left, 3=Right
        public (double X, double Y) GetPortPosition(int port)
        {
            double centerX = X + Width / 2;
            double centerY = Y + Height / 2;

            switch (port)
            {
                case 0: // Top
                    return (centerX, Y);
                case 1: // Bottom
                    return (centerX, Y + Height);
                case 2: // Left
                    return (X, centerY);
                case 3: // Right
                    return (X + Width, centerY);
                default:
                    return (centerX, centerY);
            }
        }
    }
}

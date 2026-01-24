using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiagramFlow.ViewModels
{
    public class ConnectorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public NodeViewModel SourceNode { get; set; }
        public int SourcePort { get; set; } // 0=Top, 1=Bottom, 2=Left, 3=Right

        public NodeViewModel TargetNode { get; set; }
        public int TargetPort { get; set; }

        // Calculated line coordinates
        private double _x1;
        public double X1
        {
            get => _x1;
            set { _x1 = value; OnPropertyChanged(); }
        }

        private double _y1;
        public double Y1
        {
            get => _y1;
            set { _y1 = value; OnPropertyChanged(); }
        }

        private double _x2;
        public double X2
        {
            get => _x2;
            set { _x2 = value; OnPropertyChanged(); }
        }

        private double _y2;
        public double Y2
        {
            get => _y2;
            set { _y2 = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public ConnectorViewModel(NodeViewModel source, int sourcePort, NodeViewModel target, int targetPort)
        {
            SourceNode = source;
            SourcePort = sourcePort;
            TargetNode = target;
            TargetPort = targetPort;

            // Subscribe to position changes to update line coordinates
            if (SourceNode != null)
            {
                SourceNode.PropertyChanged += SourceNode_PropertyChanged;
            }

            if (TargetNode != null)
            {
                TargetNode.PropertyChanged += TargetNode_PropertyChanged;
            }

            UpdateCoordinates();
        }

        private void SourceNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NodeViewModel.X) ||
                e.PropertyName == nameof(NodeViewModel.Y) ||
                e.PropertyName == nameof(NodeViewModel.Width) ||
                e.PropertyName == nameof(NodeViewModel.Height))
            {
                UpdateCoordinates();
            }
        }

        private void TargetNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NodeViewModel.X) ||
                e.PropertyName == nameof(NodeViewModel.Y) ||
                e.PropertyName == nameof(NodeViewModel.Width) ||
                e.PropertyName == nameof(NodeViewModel.Height))
            {
                UpdateCoordinates();
            }
        }

        public void UpdateCoordinates()
        {
            if (SourceNode != null)
            {
                var sourcePos = SourceNode.GetPortPosition(SourcePort);
                X1 = sourcePos.X;
                Y1 = sourcePos.Y;
            }

            if (TargetNode != null)
            {
                var targetPos = TargetNode.GetPortPosition(TargetPort);
                X2 = targetPos.X;
                Y2 = targetPos.Y;
            }
        }
    }
}

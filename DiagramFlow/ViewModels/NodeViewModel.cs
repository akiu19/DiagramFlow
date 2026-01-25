using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DiagramFlow.Models;

namespace DiagramFlow.ViewModels
{
    public class NodeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        // 位置
        private double _x;
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        private double _y;
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        // サイズ
        private double _width = 100;
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, Math.Max(MinWidth, value));
        }

        private double _height = 80;
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, Math.Max(MinHeight, value));
        }

        // コンテンツ
        private string _text = "Node";
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        // 外観
        private Brush _background = Brushes.LightBlue;
        public Brush Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        private ShapeType _shapeType = ShapeType.Rectangle;
        public ShapeType ShapeType
        {
            get => _shapeType;
            set => SetProperty(ref _shapeType, value);
        }

        // 状態
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        // 最小サイズ定数
        public const double MinWidth = 30;
        public const double MinHeight = 30;

        public NodeViewModel()
        {
        }

        // ポート位置（現在の位置とサイズから計算）
        // ポート列挙: 0=上, 1=下, 2=左, 3=右
        public (double X, double Y) GetPortPosition(int port)
        {
            double centerX = X + Width / 2;
            double centerY = Y + Height / 2;

            switch (port)
            {
                case 0: // 上
                    return (centerX, Y);
                case 1: // 下
                    return (centerX, Y + Height);
                case 2: // 左
                    return (X, centerY);
                case 3: // 右
                    return (X + Width, centerY);
                default:
                    return (centerX, centerY);
            }
        }
    }
}

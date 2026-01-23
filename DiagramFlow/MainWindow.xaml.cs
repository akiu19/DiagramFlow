using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiagramFlow
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _lastMousePosition;
        private bool _isPanning;

        public MainWindow()
        {
            InitializeComponent();
            
            // Event Handlers for Zoom and Pan
            MainScrollViewer.PreviewMouseWheel += MainScrollViewer_PreviewMouseWheel;
            MainScrollViewer.PreviewMouseRightButtonDown += MainScrollViewer_PreviewMouseRightButtonDown;
            MainScrollViewer.PreviewMouseMove += MainScrollViewer_PreviewMouseMove;
            MainScrollViewer.PreviewMouseRightButtonUp += MainScrollViewer_PreviewMouseRightButtonUp;
            MainScrollViewer.MouseDoubleClick += MainScrollViewer_MouseDoubleClick;
        }

        private ViewModels.MainViewModel ViewModel => DataContext as ViewModels.MainViewModel;

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ViewModel == null) return;

                e.Handled = true;

                double oldScale = ViewModel.ZoomScale.Value;
                double delta = e.Delta > 0 ? 1.2 : 0.8333; // +20% or -~17%
                double newScale = oldScale * delta;

                // Clamp
                if (newScale < 0.1) newScale = 0.1;
                if (newScale > 4.0) newScale = 4.0;

                Point mousePos = e.GetPosition(MainScrollViewer);
                
                // Calculate relative position of mouse in content
                double horizontalOffset = MainScrollViewer.HorizontalOffset;
                double verticalOffset = MainScrollViewer.VerticalOffset;

                // Update Scale
                ViewModel.ZoomScale.Value = newScale;

                // Adjust Scroll to keep mouse centered on same content point
                // Content Coordinate = (Offset + Mouse) / OldScale
                // New Offset = Content Coordinate * NewScale - Mouse
                
                // However, since we use LayoutTransform, the ScrollViewer's Extent size changes immediately (?) 
                // or after layout update. We might need to wait for layout update or calculate carefully.
                // LayoutTransform affects the content size.
                
                MainScrollViewer.UpdateLayout(); // Force update to get new Extent

                double newHorizontalOffset = (horizontalOffset + mousePos.X) * (newScale / oldScale) - mousePos.X;
                double newVerticalOffset = (verticalOffset + mousePos.Y) * (newScale / oldScale) - mousePos.Y;

                MainScrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
                MainScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
            }
        }

        private void MainScrollViewer_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(MainScrollViewer);
            _isPanning = true;
            MainScrollViewer.CaptureMouse();
            Cursor = Cursors.Hand;
        }

        private void MainScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPos = e.GetPosition(MainScrollViewer);
                Vector delta = currentPos - _lastMousePosition;

                MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset - delta.X);
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - delta.Y);

                _lastMousePosition = currentPos;
            }
        }

        private void MainScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                MainScrollViewer.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }

        private void MainScrollViewer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Empty area check logic might be needed here, 
            // but for now, implement toggle zoom
            if (ViewModel == null) return;

            // Simple logic: if zoomed in, reset. if 1.0, zoom to fit? 
            // Spec says: "Double click empty area toggles Zoom to Click Point vs Zoom to Fit"
            
            // For now, let's just reset to 100% or 10% 
            // Implementing animation requires Storyboard which is complex in code-behind without resources.
            // Leaving simple toggle for now.
             
             if (ViewModel.ZoomScale.Value != 1.0)
                 ViewModel.ZoomScale.Value = 1.0;
             else
                 ViewModel.ZoomScale.Value = 2.0; // Zoom In
        }
    }
}

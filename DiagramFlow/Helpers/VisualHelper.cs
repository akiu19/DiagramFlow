using System.Windows;
using System.Windows.Media;

namespace DiagramFlow.Helpers
{
    public static class VisualHelper
    {
        public static T FindParentDataContext<T>(DependencyObject child) where T : class
        {
            if (child == null) return null;
            var frameworkElement = child as FrameworkElement;
            if (frameworkElement?.DataContext is T data)
            {
                return data;
            }
            
            var parent = VisualTreeHelper.GetParent(child);
            return FindParentDataContext<T>(parent);
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent) return parent;

            return FindParent<T>(parentObject);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace Swarm.Editor.Behaviors
{
    public static class ScrollViewerBehaviors
    {
        // Sensitivity factor for scrolling speed
        private const double ScrollSensitivityFactor = 30.0;

        // Define the attached property
        public static readonly AttachedProperty<bool> EnableMiddleMouseScrollProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("EnableMiddleMouseScroll", typeof(ScrollViewerBehaviors));

        public static bool GetEnableMiddleMouseScroll(ScrollViewer element)
        {
            return element.GetValue(EnableMiddleMouseScrollProperty);
        }

        public static void SetEnableMiddleMouseScroll(ScrollViewer element, bool value)
        {
            element.SetValue(EnableMiddleMouseScrollProperty, value);
        }

        // Property changed handler: Attach/detach event listeners
        static ScrollViewerBehaviors()
        {
            EnableMiddleMouseScrollProperty.Changed.AddClassHandler<ScrollViewer>(OnEnableMiddleMouseScrollChanged);
        }

        private static void OnEnableMiddleMouseScrollChanged(ScrollViewer scrollViewer, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                scrollViewer.AddHandler(Control.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
                // Consider adding DetachedFromVisualTree handler for cleanup if needed, though Tunnel might suffice
            }
            else
            {
                scrollViewer.RemoveHandler(Control.PointerWheelChangedEvent, OnPointerWheelChanged);
            }
        }

        // Event handler for PointerWheelChanged
        private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;

            // We only care about vertical scrolls to translate to horizontal
            if (Math.Abs(e.Delta.Y) < 0.01)
                return;

            // Prevent default vertical scroll
            e.Handled = true;

            var currentOffset = scrollViewer.Offset;
            var scrollAmount = e.Delta.Y * ScrollSensitivityFactor * -1; // Invert Y delta and apply sensitivity
            var newX = currentOffset.X + scrollAmount;

            // Clamp the new offset
            var maxOffsetX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
            newX = Math.Clamp(newX, 0, maxOffsetX);

            scrollViewer.Offset = new Vector(newX, currentOffset.Y);
        }
    }
} 
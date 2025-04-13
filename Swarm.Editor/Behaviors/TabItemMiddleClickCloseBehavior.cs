using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Swarm.Editor.ViewModels;
using System.Diagnostics;
using System.Windows.Input;

namespace Swarm.Editor.Behaviors
{
    /// <summary>
    /// Attached behavior for TabItem to handle middle-click closing.
    /// </summary>
    public static class TabItemMiddleClickCloseBehavior
    {
        /// <summary>
        /// Defines the IsEnabled attached property.
        /// When true, the behavior is active for the TabItem.
        /// </summary>
        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<TabItem, bool>(
                "IsEnabled",
                typeof(TabItemMiddleClickCloseBehavior),
                defaultValue: false,
                inherits: false);

        /// <summary>
        /// Gets the value of the IsEnabled property.
        /// </summary>
        public static bool GetIsEnabled(TabItem element)
        {
            return element.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets the value of the IsEnabled property.
        /// </summary>
        public static void SetIsEnabled(TabItem element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        // Static constructor to subscribe to property changes
        static TabItemMiddleClickCloseBehavior()
        {
            IsEnabledProperty.Changed.Subscribe(OnIsEnabledChanged);
        }


        /// <summary>
        /// Called when the IsEnabled property changes. Attaches or detaches the event handler.
        /// </summary>
        private static void OnIsEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> e)
        {
            if (e.Sender is TabItem tabItem)
            {
                if (e.NewValue.Value) // Use .Value for AvaloniaOptional<T>
                {
                    // Attach
                    tabItem.AddHandler(InputElement.PointerPressedEvent, TabItem_PointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                }
                else
                {
                    // Detach
                    tabItem.RemoveHandler(InputElement.PointerPressedEvent, TabItem_PointerPressed);
                }
            }
        }

        /// <summary>
        /// Handles the PointerPressed event on the TabItem.
        /// </summary>
        private static void TabItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not TabItem tabItem) return;

            var currentPoint = e.GetCurrentPoint(tabItem);
            if (!currentPoint.Properties.IsMiddleButtonPressed) return;

            // Find the DocumentTabViewModel associated with this TabItem
            if (tabItem.DataContext is not DocumentTabViewModel documentTabViewModel)
            {
                Debug.WriteLine("[WARN] TabItemMiddleClickCloseBehavior: DataContext is not a DocumentTabViewModel.");
                return;
            }

            // Find the MainWindow and its ViewModel
            var mainWindow = tabItem.FindAncestorOfType<Window>();
            if (mainWindow == null)
            {
                Debug.WriteLine("[WARN] TabItemMiddleClickCloseBehavior: Could not find MainWindow.");
                return;
            }

            if (mainWindow.DataContext is not MainWindowViewModel mainWindowViewModel)
            {
                Debug.WriteLine("[WARN] TabItemMiddleClickCloseBehavior: MainWindow DataContext is not a MainWindowViewModel.");
                return;
            }

            // Get the command and execute if possible
            ICommand? command = mainWindowViewModel.CloseTabCommand;
            if (command?.CanExecute(documentTabViewModel) == true)
            {
                command.Execute(documentTabViewModel);
                e.Handled = true; // Mark event as handled
            }
            else
            {
                Debug.WriteLine($"[WARN] TabItemMiddleClickCloseBehavior: CloseTabCommand cannot execute for {documentTabViewModel.DisplayName} or command is null.");
            }
        }
    }
} 
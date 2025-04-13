using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;
using System;
using System.Windows.Input;
using Avalonia.Markup.Xaml; // Required for AvaloniaXamlLoader

namespace Swarm.Editor.Views.CodeEditor
{
    // Define the TextChangedEventArgs for our custom event
    public class TextChangedEventArgs : EventArgs
    {
        // Potentially add more details later if needed (e.g., offset, added/removed text)
    }

    public partial class SwarmCodeEditor : UserControl
    {
        // Store reference to AvaloniaEdit's TextEditor
        private TextEditor? _editor;

        #region Text Property
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<SwarmCodeEditor, string>(nameof(Text), string.Empty, 
                inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        #endregion

        #region ShowLineNumbers Property
        public static readonly StyledProperty<bool> ShowLineNumbersProperty =
            AvaloniaProperty.Register<SwarmCodeEditor, bool>(nameof(ShowLineNumbers), true);

        public bool ShowLineNumbers
        {
            get => GetValue(ShowLineNumbersProperty);
            set => SetValue(ShowLineNumbersProperty, value);
        }
        #endregion

        #region TextViewMargin Property
        public static readonly StyledProperty<Thickness> TextViewMarginProperty =
            AvaloniaProperty.Register<SwarmCodeEditor, Thickness>(nameof(TextViewMargin), new Thickness(0));

        public Thickness TextViewMargin
        {
            get => GetValue(TextViewMarginProperty);
            set => SetValue(TextViewMarginProperty, value);
        }
        #endregion

        #region Static Constructor
        static SwarmCodeEditor()
        {
            // Register a handler to update the editor's document when the Text property changes.
            TextProperty.Changed.AddClassHandler<SwarmCodeEditor>((x, e) => x.OnTextChangedExternally(e));
            
            // Register a handler to update ShowLineNumbers when it changes
            ShowLineNumbersProperty.Changed.AddClassHandler<SwarmCodeEditor>((x, e) => x.OnShowLineNumbersChanged(e));
            
            // Register a handler to update TextViewMargin when it changes
            TextViewMarginProperty.Changed.AddClassHandler<SwarmCodeEditor>((x, e) => x.OnTextViewMarginChanged(e));
        }

        private void OnTextChangedExternally(AvaloniaPropertyChangedEventArgs e)
        {
            if (_editor != null && (string)e.NewValue! != _editor.Text)
            {
                _editor.Text = (string)e.NewValue!;
            }
        }

        private void OnShowLineNumbersChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_editor != null)
            {
                _editor.ShowLineNumbers = (bool)e.NewValue!;
            }
        }
        
        private void OnTextViewMarginChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_editor?.TextArea?.TextView != null)
            {
                _editor.TextArea.TextView.Margin = (Thickness)e.NewValue!;
            }
        }
        #endregion

        #region TextChanged Event
        public event EventHandler<TextChangedEventArgs>? TextChanged;
        #endregion

        public SwarmCodeEditor()
        {
            // Call the source-generated method
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e); 
            
            // Find the TextEditor control
            _editor = this.FindControl<TextEditor>("Editor");
            
            if (_editor != null)
            {
                // Add the markers margin
                _editor.TextArea.LeftMargins.Insert(0, new MarkersMargin());
                
                // Initialize the TextEditor text if the Text property was set before the template was applied
                if (!string.IsNullOrEmpty(Text) && Text != _editor.Text)
                {
                    _editor.Text = Text;
                }
                
                // Apply ShowLineNumbers value
                _editor.ShowLineNumbers = ShowLineNumbers;
                
                // Apply TextView margin once the TextArea is loaded
                _editor.TextArea.TextView.Margin = TextViewMargin;
                
                // Listen for changes in the TextEditor's document
                _editor.Document.TextChanged += Document_TextChanged;
                
                // Try to set C# syntax highlighting by default
                try
                {
                    var highlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(".cs");
                    if (highlightingDefinition != null)
                    {
                        _editor.SyntaxHighlighting = highlightingDefinition;
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue, as syntax highlighting is not critical
                    System.Diagnostics.Debug.WriteLine($"Failed to set syntax highlighting: {ex.Message}");
                }
            }
        }

        private void Document_TextChanged(object? sender, EventArgs e)
        { 
            if (_editor != null && Text != _editor.Text)
            {
                Text = _editor.Text;
                
                // Raise our custom TextChanged event
                TextChanged?.Invoke(this, new TextChangedEventArgs());
            }
        }

        /// <summary>
        /// Helper method to load text directly into the editor.
        /// This is often more straightforward than setting the Text property,
        /// especially on initial load.
        /// </summary>
        public void LoadText(string content)
        {
            if (_editor != null)
            {
                _editor.Text = content;
                // Update the DP as well to keep it synchronized
                SetValue(TextProperty, content);
            }
            else
            {
                // If called before template applied, set the DP so it loads later
                Text = content;
            }
        }

        // Cleanup event handler on detach
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_editor?.Document != null)
            {
                _editor.Document.TextChanged -= Document_TextChanged;
            }
            base.OnDetachedFromVisualTree(e);
        }
    }
} 
using Avalonia.Controls;
using AvaloniaEdit.Editing;
using Swarm.Editor.Views.CodeEditor;
using System.ComponentModel;
using System.Linq;
using Avalonia; // Keep for Thickness used by MarkersMargin potentially

namespace Swarm.Editor.Views
{
    public partial class DocumentView : UserControl
    {
        public DocumentView()
        {
            InitializeComponent();

            var editor = this.FindControl<AvaloniaEdit.TextEditor>("Editor");
            // Ensure TextArea exists for margin addition
            if (editor?.TextArea != null)
            {
                // Add Markers Margin first (programmatically as it's custom)
                var markersMargin = new MarkersMargin();
                if (!editor.TextArea.LeftMargins.OfType<MarkersMargin>().Any())
                {
                    editor.TextArea.LeftMargins.Insert(0, markersMargin);
                }

                // // Find LineNumberMargin and add right margin - Now handled by Style
                // var lineNumberMargin = editor.TextArea.LeftMargins.OfType<LineNumberMargin>().FirstOrDefault();
                // if (lineNumberMargin != null)
                // {
                //     lineNumberMargin.Margin = new Thickness(0, 0, 5, 0);
                // }

                // // Add left margin to the TextView - Now handled by Style
                // if (editor.TextArea.TextView != null) 
                // {
                //    editor.TextArea.TextView.Margin = new Thickness(5, 0, 0, 0);
                // }
            }
        }
    }
} 
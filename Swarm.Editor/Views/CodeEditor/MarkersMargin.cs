using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Swarm.Editor.Views.CodeEditor
{
    /// <summary>
    /// Custom margin for displaying markers like breakpoints, errors, warnings, etc.
    /// </summary>
    public class MarkersMargin : AbstractMargin
    {
        private const int MarginWidth = 24;
        
        // Sample breakpoint collection - in a real implementation, you would
        // integrate this with your debugging system
        private HashSet<int> _breakpoints = new();
        
        public MarkersMargin()
        {
            // Enable mouse handling for this margin
            this.Cursor = new Cursor(StandardCursorType.Hand);
        }

        protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            base.OnTextViewChanged(oldTextView, newTextView);
            
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= TextView_VisualLinesChanged;
            }
            
            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += TextView_VisualLinesChanged;
            }
            
            InvalidateVisual();
        }
        
        private void TextView_VisualLinesChanged(object? sender, EventArgs e)
        {
            InvalidateVisual();
        }
        
        // Toggle breakpoint at the specified line when clicked
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            
            if (TextView == null || TextView.Document == null) return;
            
            // Get the Y coordinate of the mouse position relative to the margin
            var mousePos = e.GetPosition(this);
            double yPos = mousePos.Y;
            
            // Adjust by vertical scroll offset to get document coordinates
            yPos += TextView.VerticalOffset;
            
            // Get the visual line at this position
            VisualLine? visualLine = TextView.GetVisualLineFromVisualTop(yPos);
            if (visualLine == null) return;
            
            // Get the document line number
            int lineNumber = visualLine.FirstDocumentLine.LineNumber;
            
            // Toggle breakpoint for this line
            if (_breakpoints.Contains(lineNumber))
            {
                _breakpoints.Remove(lineNumber);
            }
            else
            {
                _breakpoints.Add(lineNumber);
            }
            
            // Redraw the margin
            InvalidateVisual();
            
            e.Handled = true;
        }
        
        // Set fixed width for the margin
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(MarginWidth, 0);
        }
        
        // Render the breakpoints and other markers
        public override void Render(DrawingContext drawingContext)
        {
            if (TextView == null || TextView.Document == null) return;
            
            // Fill the margin with a light background color
            var renderSize = this.Bounds.Size;
            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(30, 30, 30)), // Slightly lighter than editor bg
                null,
                new Rect(0, 0, renderSize.Width, renderSize.Height));
            
            // Draw breakpoints
            foreach (var lineNumber in _breakpoints)
            {
                VisualLine? line = null;
                
                try 
                {
                    DocumentLine documentLine = TextView.Document.GetLineByNumber(lineNumber);
                    line = TextView.GetOrConstructVisualLine(documentLine);
                }
                catch
                {
                    // Line might not exist anymore
                    continue;
                }
                
                if (line == null) continue;
                
                // Use the technique from RoslynPad to get the Y position
                double visualYPosition = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                double yPos = visualYPosition - TextView.VerticalOffset;
                
                // Only draw if vertically visible
                if (yPos >= -10 && yPos <= renderSize.Height + 10)
                {
                    // Draw breakpoint circle
                    const double radius = 6;
                    var center = new Point(renderSize.Width / 2, yPos + 8); // Add offset to center vertically
                    
                    // Draw the breakpoint circle
                    drawingContext.DrawEllipse(
                        new SolidColorBrush(Color.FromRgb(200, 20, 20)), // Red color for breakpoints
                        null,
                        center,
                        radius,
                        radius);
                }
            }
        }
    }
} 
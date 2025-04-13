using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace Swarm.Editor.Common.Converters;

/// <summary>
/// Converts a file path string to an AvaloniaEdit IHighlightingDefinition based on the file extension.
/// </summary>
public class FilePathToSyntaxHighlightingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrEmpty(filePath) && targetType.IsAssignableTo(typeof(IHighlightingDefinition)))
        {
            // Get the HighlightingManager instance
            var highlightingManager = HighlightingManager.Instance;
            
            // Get the definition based on the file extension
            string extension = Path.GetExtension(filePath);
            var definition = highlightingManager.GetDefinitionByExtension(extension);
            
            // Return the definition (or null if not found)
            return definition;
        }
        
        // Return null for invalid input or if no definition is found
        return null; 
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Conversion back is not supported
        throw new NotSupportedException();
    }
} 
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaEdit.Document; // Required for TextDocument

namespace Swarm.Editor.Common.Converters;

/// <summary>
/// Converts a string to an AvaloniaEdit TextDocument.
/// </summary>
public class StringToTextDocumentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && targetType.IsAssignableTo(typeof(TextDocument)))
        {
            return new TextDocument(text);
        }
        // Return null or AvaloniaProperty.UnsetValue for invalid conversions
        return null; 
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Converting back from TextDocument to string might be needed 
        // if the binding were TwoWay, but typically the ViewModel handles content updates.
        if (value is TextDocument doc && targetType == typeof(string))
        {
            return doc.Text;
        }
        return null; // Or throw NotSupportedException if ConvertBack is truly unsupported
    }
} 
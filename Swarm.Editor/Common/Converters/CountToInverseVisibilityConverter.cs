using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Swarm.Editor.Common.Converters
{
    public class CountToInverseVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Show the single editor when there are 0 or 1 documents
                return count <= 1;
            }
            
            // If the count is not a valid integer, show the single editor as a fallback
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
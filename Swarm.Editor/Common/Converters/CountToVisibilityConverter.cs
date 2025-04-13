using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Swarm.Editor.Common.Converters
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Show the tab control only when there are 2 or more documents
                return count > 1;
            }
            
            // If the count is not a valid integer, don't show the tabs
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
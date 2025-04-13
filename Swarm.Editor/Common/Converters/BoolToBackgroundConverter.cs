using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Swarm.Editor.Common.Converters
{
    public class BoolToBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUserMessage)
            {
                return isUserMessage 
                    ? new SolidColorBrush(Color.Parse("#2B5278")) // User message (blue)
                    : Brushes.Transparent; // Agent message (transparent)
            }
            
            return Brushes.Transparent; // Default/fallback
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
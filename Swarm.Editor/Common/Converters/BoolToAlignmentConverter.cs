using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace Swarm.Editor.Common.Converters
{
    public class BoolToAlignmentConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser 
                    ? HorizontalAlignment.Right 
                    : HorizontalAlignment.Left;
            }
            
            return HorizontalAlignment.Left; // Default/fallback
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
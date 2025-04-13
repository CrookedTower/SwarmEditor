using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Swarm.Editor.Common.Converters
{
    public class BoolToFolderIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isDirectory)
            {
                // Using Unicode icons since Segoe MDL2 Assets may not be available on all platforms
                return isDirectory ? "📁" : "📄"; // Folder icon vs Document icon
            }
            return "📄"; // Default to document icon
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
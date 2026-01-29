using Avalonia;
using Avalonia.Markup;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AVFM.Converters
{
    public class BitmapValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string) {
                string strValue = (string)value;
                int? width = null;
                if (parameter is int)
                    width = (int)parameter;

                if (string.IsNullOrEmpty(strValue))
                    return null;

                var uri = new Uri(strValue, UriKind.RelativeOrAbsolute);
                var scheme = uri.IsAbsoluteUri ? uri.Scheme : "file";

                switch (scheme)
                {
                    case "file":
                        if (width.HasValue) {
                            using (System.IO.FileStream fs = new System.IO.FileStream(strValue, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)) {
                                return Bitmap.DecodeToWidth(fs, width.Value);
                            }
                        }
                        return new Bitmap(strValue);

                    default:
                        return new Bitmap(AssetLoader.Open(uri));
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Runia.Services.Common
{
    public interface IImageProvider
    {
        ImageSource? CreateImageSource(string localPath, int decodeWidth = 0);
    }

    public sealed class ImageProvider : IImageProvider
    {
        public ImageSource? CreateImageSource(string localPath, int decodeWidth = 0)
        {
            if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath)) return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(localPath);
                if (decodeWidth > 0) bitmap.DecodePixelWidth = decodeWidth;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}

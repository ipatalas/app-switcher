using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AppSwitcher.Utils;

public class IconExtractor
{
    private readonly Dictionary<string, ImageSource> _images = new();

    public ImageSource? GetByProcessPath(string processPath)
    {
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return null;
        }

        if (_images.TryGetValue(processPath, out var imageSource))
        {
            return imageSource;
        }

        var icon = LoadIconFromExe(processPath);
        if (icon != null)
        {
            _images[processPath] = icon;
            return icon;
        }

        return null;
    }

    public ImageSource? GetByIconPath(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        if (_images.TryGetValue(iconPath, out var imageSource))
        {
            return imageSource;
        }

        try
        {
            var image = Image.FromFile(iconPath);
            return ConvertToImageSource(image);
        }
        catch
        {
            return null;
        }
    }

    private static BitmapImage? LoadIconFromExe(string path)
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            return icon != null ? ConvertToImageSource(icon.ToBitmap()) : null;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapImage ConvertToImageSource(Image image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = stream;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
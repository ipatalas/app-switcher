using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AppSwitcher.Utils;

public class IconExtractor(AppLocator appLocator)
{
    private readonly Dictionary<string, ImageSource> _images = new();

    public ImageSource? GetByProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return null;
        }

        if (_images.TryGetValue(processName, out var imageSource))
        {
            return imageSource;
        }

        var executablePath = appLocator.FindExecutablePath(processName);
        if (executablePath == null)
        {
            return null;
        }

        var icon = LoadIcon(executablePath);
        if (icon != null)
        {
            _images[processName] = icon;
            return icon;
        }

        return null;
    }

    private static BitmapImage? LoadIcon(string path)
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            return icon != null ? ConvertToImageSource(icon) : null;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapImage ConvertToImageSource(Icon icon)
    {
        using var stream = new MemoryStream();
        icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);
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
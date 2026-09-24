using System.IO;
using System.Windows.Media.Imaging;

namespace CharacterLauncher.Services;

public static class AvatarService
{
    public static BitmapImage Load(string path)
    {
        if (new FileInfo(path).Length > 20 * 1024 * 1024)
            throw new InvalidDataException(LocalizationService.T("请选择小于 20 MB 的图片。"));
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 512;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static string Import(string source) => Import(Load(source));

    public static string Import(BitmapSource image)
    {
        var directory = Path.Combine(ConfigService.DataDirectory, "avatars");
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".png");
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(target);
            encoder.Save(stream);
            return target;
        }
        catch { if (File.Exists(target)) File.Delete(target); throw; }
    }
}

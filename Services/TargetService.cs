using System.Diagnostics;
using System.IO;

namespace CharacterLauncher.Services;

public static class TargetService
{
    public static string Resolve(string? value)
    {
        value = Environment.ExpandEnvironmentVariables((value ?? "").Trim().Trim('"'));
        if (value.Length == 0) return "";
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !uri.IsFile) return value;
        return Path.IsPathRooted(value) ? value : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value));
    }

    public static string Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("请选择本地目标，或填写完整的网页地址。");
        var target = Resolve(value);
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            if ((uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrWhiteSpace(uri.Host))
                throw new ArgumentException("网页地址须以 https:// 或 http:// 开头。");
        }
        else if (!File.Exists(target) && !Directory.Exists(target))
            throw new FileNotFoundException("找不到这个本地目标，请重新选择文件或文件夹。", target);
        return target;
    }

    public static void Open(string target, string arguments) => Process.Start(new ProcessStartInfo
    {
        FileName = Validate(target), Arguments = arguments, UseShellExecute = true
    });
}

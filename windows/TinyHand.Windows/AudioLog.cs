using System;
using System.IO;

namespace TinyHand.Windows;

internal static class AudioLog
{
    internal static void Write(string message)
    {
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "TinyHand");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "audio.log");
            if (File.Exists(path) && new FileInfo(path).Length > 256 * 1024)
                File.Move(path, path + ".old", true);
            File.AppendAllText(path, $"{DateTime.Now:O} pid={Environment.ProcessId} {message}{Environment.NewLine}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}

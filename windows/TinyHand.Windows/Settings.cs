using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TinyHand.Windows;

internal sealed class Settings
{
    public string Style { get; set; } = "lavender";
    public int PointerHeight { get; set; } = 340;
    public bool SoundEnabled { get; set; } = false;
    public string Sound { get; set; } = "thock";

    internal static Settings Load(string path)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
            settings.Normalize();
            return settings;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new Settings();
        }
    }

    internal void Normalize()
    {
        if (!PointerController.AvailableStyles().Contains(Style)) Style = "lavender";
        PointerHeight = Math.Clamp(PointerHeight, 160, 640);
        if (!TapSound.Names.Contains(Sound)) Sound = "thock";
    }

    internal bool Save(string path)
    {
        string temporary = path + ".tmp";
        try
        {
            Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

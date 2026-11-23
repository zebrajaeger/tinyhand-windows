using System;
using System.IO;
using System.Text.Json;

namespace TinyHand.Windows;

internal enum PluginInstallStatus { Unavailable, Unsupported, Indeterminate, Missing, Older, Current, Newer }
internal sealed record PluginInstallationObservation(PluginInstallStatus Status, string? Executable = null,
    Version? SoftwareVersion = null, Version? InstalledVersion = null, string? Reason = null);

internal static class PluginVersion
{
    internal static bool TryParse(string? text, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (text == null || text.Split('.').Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+\.\d+\.\d+\.\d+$") || !Version.TryParse(text, out var parsed)
            || parsed.Build < 0 || parsed.Revision < 0) return false;
        version = parsed;
        return true;
    }
}

internal sealed class PluginOfferState
{
    public int SchemaVersion { get; set; } = 1;
    public bool AutomaticOffersEnabled { get; set; } = true;
    public string? LastOfferedVersion { get; set; }
    public DateTimeOffset? DeferUntilUtc { get; set; }
    public string? LastObservedVersion { get; set; }

    internal bool Eligible(PluginInstallationObservation observation, Version bundled, DateTimeOffset now, bool packageAvailable)
    {
        if (!packageAvailable || !AutomaticOffersEnabled || observation.Status is not (PluginInstallStatus.Missing or PluginInstallStatus.Older)) return false;
        if (DeferUntilUtc.HasValue) return now >= DeferUntilUtc.Value;
        return LastOfferedVersion != bundled.ToString(4);
    }

    internal void Choose(string choice, Version bundled, DateTimeOffset now)
    {
        if (choice is not ("later" or "never" or "install")) throw new ArgumentException("Unknown offer choice.", nameof(choice));
        LastOfferedVersion = bundled.ToString(4);
        DeferUntilUtc = null;
        if (choice == "later") DeferUntilUtc = now.ToUniversalTime().AddDays(7);
        else if (choice == "never") AutomaticOffersEnabled = false;
    }

    internal void Enable(bool enabled)
    {
        AutomaticOffersEnabled = enabled;
        if (enabled) { LastOfferedVersion = null; DeferUntilUtc = null; }
    }
}

internal interface IPluginOfferRepository
{
    PluginOfferState Load();
    bool Save(PluginOfferState state);
}

internal sealed class PluginOfferRepository : IPluginOfferRepository
{
    private readonly string path;
    internal PluginOfferRepository(string path) => this.path = path;
    public PluginOfferState Load()
    {
        try
        {
            string json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("SchemaVersion", out var schema) || schema.GetInt32() != 1)
                return Quiet();
            if (!document.RootElement.TryGetProperty("AutomaticOffersEnabled", out var enabled)
                || enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Quiet();
            var state = JsonSerializer.Deserialize<PluginOfferState>(json);
            if (state == null || (state.LastOfferedVersion != null && !PluginVersion.TryParse(state.LastOfferedVersion, out _))
                || (state.LastObservedVersion != null && !PluginVersion.TryParse(state.LastObservedVersion, out _))) return Quiet();
            return state;
        }
        catch (FileNotFoundException) { return new(); }
        catch (DirectoryNotFoundException) { return new(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        { return Quiet(); }
    }
    private static PluginOfferState Quiet() => new() { AutomaticOffersEnabled = false };

    public bool Save(PluginOfferState state)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(state));
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}

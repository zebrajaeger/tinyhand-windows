using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Win32;

namespace TinyHand.Windows;

internal interface IStreamDeckDetector
{
    PluginInstallationObservation Detect(Version bundledVersion, Version minimumSoftware, CancellationToken token);
}

internal interface IStreamDeckDiscovery
{
    IEnumerable<string> Candidates(CancellationToken token);
    (Version Version, string Product)? InspectExecutable(string path);
    string? ReadPluginManifest();
}

internal sealed class StreamDeckDetector : IStreamDeckDetector
{
    internal const string PluginId = "io.github.zebrajaeger.tinyhand";
    private readonly IStreamDeckDiscovery discovery;
    internal StreamDeckDetector(IStreamDeckDiscovery discovery) => this.discovery = discovery;

    public PluginInstallationObservation Detect(Version bundledVersion, Version minimumSoftware, CancellationToken token)
    {
        try
        {
            var found = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in discovery.Candidates(token))
            {
                token.ThrowIfCancellationRequested();
                string path;
                try { path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(raw.Trim().Trim('"'))); }
                catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) { continue; }
                if (!Path.GetFileName(path).Equals("StreamDeck.exe", StringComparison.OrdinalIgnoreCase)) continue;
                var info = discovery.InspectExecutable(path);
                if (info == null) continue;
                if (!info.Value.Product.Equals("Stream Deck", StringComparison.OrdinalIgnoreCase))
                    return new(PluginInstallStatus.Indeterminate, Reason: "Die Anwendung konnte nicht sicher erkannt werden.");
                found[path] = info.Value.Version;
            }
            if (found.Count == 0) return new(PluginInstallStatus.Unavailable, Reason: "Stream Deck wurde nicht gefunden.");
            if (found.Count > 1) return new(PluginInstallStatus.Indeterminate, Reason: "Mehrere Stream-Deck-Installationen wurden gefunden.");
            var entry = found.Single();
            if (entry.Value < minimumSoftware) return new(PluginInstallStatus.Unsupported, entry.Key, entry.Value,
                Reason: $"Stream Deck {minimumSoftware} oder neuer wird benötigt.");
            string? json = discovery.ReadPluginManifest();
            if (json == null) return new(PluginInstallStatus.Missing, entry.Key, entry.Value);
            using var manifest = JsonDocument.Parse(json);
            var root = manifest.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("UUID", out var uuid) || uuid.ValueKind != JsonValueKind.String || uuid.GetString() != PluginId
                || !root.TryGetProperty("Version", out var version) || version.ValueKind != JsonValueKind.String
                || !PluginVersion.TryParse(version.GetString(), out var installed))
                return new(PluginInstallStatus.Indeterminate, entry.Key, entry.Value, Reason: "Die Plugin-Version konnte nicht sicher erkannt werden.");
            return new(installed < bundledVersion ? PluginInstallStatus.Older : installed > bundledVersion ? PluginInstallStatus.Newer : PluginInstallStatus.Current,
                entry.Key, entry.Value, installed);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or System.Security.SecurityException or ArgumentException)
        { return new(PluginInstallStatus.Indeterminate, Reason: "Stream-Deck-Informationen konnten nicht gelesen werden."); }
    }
}

internal sealed class WindowsStreamDeckDiscovery : IStreamDeckDiscovery
{
    private readonly Func<RegistryHive, RegistryView, RegistryKey> openRoot;
    private readonly string[] fallbackFolders;
    private readonly string pluginDirectory;
    internal WindowsStreamDeckDiscovery(Func<RegistryHive, RegistryView, RegistryKey>? openRoot = null,
        string[]? fallbackFolders = null, string? pluginDirectory = null)
    {
        this.openRoot = openRoot ?? RegistryKey.OpenBaseKey;
        this.fallbackFolders = fallbackFolders ?? new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs") };
        this.pluginDirectory = pluginDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Elgato", "StreamDeck", "Plugins", StreamDeckDetector.PluginId + ".sdPlugin");
    }
    public IEnumerable<string> Candidates(CancellationToken token)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                token.ThrowIfCancellationRequested();
                using var root = openRoot(hive, view);
                using var app = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\StreamDeck.exe");
                if (app?.GetValue(null) is string path) candidates.Add(path);
                using var uninstall = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall == null) continue;
                foreach (string name in uninstall.GetSubKeyNames())
                {
                    token.ThrowIfCancellationRequested();
                    using var entry = uninstall.OpenSubKey(name);
                    if (entry?.GetValue("DisplayName") is not string display || !display.Contains("Stream Deck", StringComparison.OrdinalIgnoreCase)) continue;
                    if (entry.GetValue("InstallLocation") is string location && !string.IsNullOrWhiteSpace(location))
                        candidates.Add(Path.Combine(location.Trim('"'), "StreamDeck.exe"));
                }
            }
        foreach (string folder in fallbackFolders)
            if (!string.IsNullOrWhiteSpace(folder)) candidates.Add(Path.Combine(folder, "Elgato", "StreamDeck", "StreamDeck.exe"));
        return candidates;
    }

    public (Version Version, string Product)? InspectExecutable(string path)
    {
        if (!File.Exists(path)) return null;
        var info = FileVersionInfo.GetVersionInfo(path);
        return (new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart), info.ProductName ?? "");
    }

    public string? ReadPluginManifest()
    {
        if (!Directory.Exists(pluginDirectory)) return null;
        return File.ReadAllText(Path.Combine(pluginDirectory, "manifest.json"));
    }
}

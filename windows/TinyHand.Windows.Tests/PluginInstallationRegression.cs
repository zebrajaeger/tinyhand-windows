using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using Microsoft.Win32;
using TinyHand.Windows;

internal static class PluginInstallationRegression
{
    private sealed class Discovery : IStreamDeckDiscovery
    {
        internal readonly List<string> Paths = new();
        internal readonly Dictionary<string, (Version, string)> Files = new(StringComparer.OrdinalIgnoreCase);
        internal string? Manifest;
        public IEnumerable<string> Candidates(CancellationToken token) => Paths;
        public (Version Version, string Product)? InspectExecutable(string path) => Files.TryGetValue(path, out var info) ? info : null;
        public string? ReadPluginManifest() => Manifest;
    }

    internal static void Run(Action<bool, string> check)
    {
        var bundled = new Version(0, 1, 0, 5);
        var now = new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);
        var missing = new PluginInstallationObservation(PluginInstallStatus.Missing);
        var state = new PluginOfferState();
        check(state.Eligible(missing, bundled, now, true), "Missing offer state permits initial installation");
        state.Choose("later", bundled, now);
        check(!state.Eligible(missing, new Version(0, 2, 0, 0), now.AddDays(6), true), "Deferral suppresses all package versions for seven days");
        check(state.Eligible(missing, bundled, now.AddDays(7), true), "Reminder becomes eligible at exact deadline");
        check(!state.Eligible(missing, bundled, now.AddDays(-1), true), "Clock rollback never shortens deferral");
        state.Choose("never", bundled, now);
        check(!state.Eligible(missing, new Version(1, 0, 0, 0), now.AddDays(30), true), "Opt-out suppresses later update versions");
        state.Enable(true);
        check(state.Eligible(missing, bundled, now, true), "Re-enabling clears prior dismissals and deferrals");
        state.Choose("install", bundled, now);
        check(!state.Eligible(missing, bundled, now.AddDays(30), true), "Installer handoff does not nag again for same package");
        check(state.Eligible(missing, new Version(0, 1, 0, 6), now, true), "New package is eligible once after earlier handoff");
        check(!state.Eligible(missing, bundled, now, false), "App-only build without package never offers installation");
        check(!state.Eligible(new(PluginInstallStatus.Newer), bundled, now, true), "Newer installed plugin never offers downgrade");
        check(PluginVersion.TryParse("0.1.0.10", out var ten) && ten > new Version(0, 1, 0, 9), "Version comparison is numeric");
        check(!PluginVersion.TryParse("1.2.3", out _) && !PluginVersion.TryParse("wrong", out _), "Malformed plugin versions are rejected");
        check(!PluginVersion.TryParse(" 1.2.3.4", out _) && !PluginVersion.TryParse("1.2.3.+4", out _), "Version evidence must consist of four numeric components");

        var discovery = new Discovery();
        var detector = new StreamDeckDetector(discovery);
        PluginInstallationObservation Detect() => detector.Detect(bundled, new Version(7, 1), CancellationToken.None);
        check(Detect().Status == PluginInstallStatus.Unavailable, "No software means quiet unavailable state");
        string executable = Path.GetFullPath("StreamDeck.exe");
        discovery.Paths.Add('"' + executable + '"');
        discovery.Files[executable] = (new Version(7, 6, 1, 0), "Stream Deck");
        check(Detect().Status == PluginInstallStatus.Missing, "Installed-but-closed software is recognized without a process or device");
        discovery.Manifest = "{\"UUID\":\"io.github.zebrajaeger.tinyhand\",\"Version\":\"0.1.0.4\"}";
        check(Detect().Status == PluginInstallStatus.Older, "Older installed version detected");
        discovery.Manifest = "{\"UUID\":\"io.github.zebrajaeger.tinyhand\",\"Version\":\"0.1.0.10\"}";
        check(Detect().Status == PluginInstallStatus.Newer, "Greater numeric revision cannot be downgraded");
        discovery.Manifest = "{\"UUID\":\"other\",\"Version\":\"0.1.0.5\"}";
        check(Detect().Status == PluginInstallStatus.Indeterminate, "Wrong UUID does not permit overwrite");
        discovery.Manifest = "invalid";
        check(Detect().Status == PluginInstallStatus.Indeterminate, "Malformed installed manifest fails quiet");
        discovery.Paths.Add(Path.GetFullPath(Path.Combine("other", "StreamDeck.exe")));
        discovery.Files[Path.GetFullPath(Path.Combine("other", "StreamDeck.exe"))] = (new Version(7, 6), "Stream Deck");
        check(Detect().Status == PluginInstallStatus.Indeterminate, "Conflicting installed candidates are not arbitrarily selected");
        discovery.Paths.RemoveAt(1);
        discovery.Files[executable] = (new Version(6, 9), "Stream Deck");
        check(Detect().Status == PluginInstallStatus.Unsupported, "Unsupported Stream Deck version prevents offer");
        discovery.Files[executable] = (new Version(7, 6), "Other product");
        check(Detect().Status == PluginInstallStatus.Indeterminate, "Wrong executable product identity fails safe");
        discovery.Files[executable] = (new Version(7, 6), "Stream Deck");
        discovery.Manifest = "{\"UUID\":\"io.github.zebrajaeger.tinyhand\",\"Version\":\"0.1.0.5\"}";
        check(Detect().Status == PluginInstallStatus.Current, "Matching installed version suppresses redundant install");

        string directory = Path.Combine(Path.GetTempPath(), "TinyHand-offers-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "state.json");
        try
        {
            string registryPath = @"Software\TinyHand.Tests\Discovery-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var registry = Registry.CurrentUser.CreateSubKey(registryPath))
                {
                    using var app = registry.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\StreamDeck.exe");
                    app.SetValue("", '"' + executable + '"');
                    using var uninstall = registry.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\StreamDeck");
                    uninstall.SetValue("DisplayName", "Elgato Stream Deck");
                    uninstall.SetValue("InstallLocation", "\"C:\\Fixture Stream Deck\"");
                    uninstall.SetValue("UninstallString", "malicious.exe --never-execute");
                }
                var views = new System.Collections.Generic.HashSet<(RegistryHive, RegistryView)>();
                var actual = new WindowsStreamDeckDiscovery((hive, view) =>
                {
                    views.Add((hive, view));
                    return Registry.CurrentUser.OpenSubKey(registryPath)!;
                }, new[] { directory }, Path.Combine(directory, "Plugin"));
                var candidates = actual.Candidates(CancellationToken.None).ToArray();
                check(views.Count == 4, "Discovery reads both registry views in current-user and machine hives through isolated keys");
                check(candidates.Contains('"' + executable + '"') && candidates.Contains(@"C:\Fixture Stream Deck\StreamDeck.exe"), "Registered quoted App Paths and uninstall locations are discovered");
                check(!candidates.Any(p => p.Contains("malicious")), "Uninstall commands are never used as executable candidates");
                check(candidates.Contains(Path.Combine(directory, "Elgato", "StreamDeck", "StreamDeck.exe")), "Standard-location fallback is bounded and injectable");
                check(actual.ReadPluginManifest() == null, "Missing plugin directory is distinct from unreadable manifest");
                Directory.CreateDirectory(Path.Combine(directory, "Plugin"));
                bool unreadable = false;
                try { actual.ReadPluginManifest(); } catch (FileNotFoundException) { unreadable = true; }
                check(unreadable, "Existing directory without manifest is indeterminate evidence, not missing install");
                Directory.Delete(Path.Combine(directory, "Plugin"));
            }
            finally { Registry.CurrentUser.DeleteSubKeyTree(registryPath, false); }
            var repository = new PluginOfferRepository(path);
            check(repository.Load().AutomaticOffersEnabled, "Missing persisted state enables initial offers");
            state.Choose("never", bundled, now);
            check(repository.Save(state) && !repository.Load().AutomaticOffersEnabled, "Opt-out round trips atomically");
            File.WriteAllText(path, "bad");
            check(!repository.Load().AutomaticOffersEnabled, "Corrupt state fails quiet");
            File.WriteAllText(path, "{}");
            check(!repository.Load().AutomaticOffersEnabled, "Unknown state schema fails quiet");
            File.WriteAllText(path, "{\"SchemaVersion\":1}");
            check(!repository.Load().AutomaticOffersEnabled, "Incomplete reminder state fails quiet instead of prompting repeatedly");
            check(!new PluginOfferRepository(Path.Combine(path, "invalid.json")).Save(state), "Write failure is nonfatal");
        }
        finally { File.Delete(path); Directory.Delete(directory); }
    }
}

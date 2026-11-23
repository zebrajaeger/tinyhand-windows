using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TinyHand.Windows;
using System.Diagnostics;
using System.Linq;

internal static class EmbeddedPluginRegression
{
    private sealed class Detector : IStreamDeckDetector
    {
        internal Exception? Failure;
        internal Func<CancellationToken, PluginInstallationObservation>? DetectOverride;
        internal int Calls;
        internal PluginInstallationObservation Observation = new(PluginInstallStatus.Missing, "C:\\Fake Stream Deck\\StreamDeck.exe", new Version(7, 6));
        public PluginInstallationObservation Detect(Version version, Version minimum, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            if (Failure != null) throw Failure;
            return DetectOverride?.Invoke(token) ?? Observation;
        }
    }
    private sealed class Repository : IPluginOfferRepository
    {
        internal PluginOfferState State = new();
        internal bool Writable = true;
        public PluginOfferState Load() => State;
        public bool Save(PluginOfferState state) => Writable;
    }
    private sealed class Launcher : IPluginInstallerLauncher
    {
        internal bool Fail;
        internal int Calls;
        internal Action? Launched;
        public void Open(string executable, string package) { Calls++; if (Fail) throw new IOException("Synthetic launch failure"); Launched?.Invoke(); }
    }
    private static T Await<T>(Task<T> task)
    {
        var frame = new DispatcherFrame();
        task.ContinueWith(_ => Application.Current.Dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
        Dispatcher.PushFrame(frame); return task.GetAwaiter().GetResult();
    }
    private static void Await(Task task) => Await(task.ContinueWith(_ => { task.GetAwaiter().GetResult(); return true; }, TaskScheduler.Default));

    internal static void Run(Action<bool, string> check)
    {
        byte[] bytes;
        using (var memory = new MemoryStream())
        {
            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            using (var writer = new StreamWriter(archive.CreateEntry("manifest.json").Open(), new UTF8Encoding(false)))
                writer.Write("{\"UUID\":\"io.github.zebrajaeger.tinyhand\",\"Version\":\"0.1.0.5\",\"Software\":{\"MinimumVersion\":\"7.1\"}}");
            bytes = memory.ToArray();
        }
        string metadata = JsonSerializer.Serialize(new { uuid = StreamDeckDetector.PluginId, version = "0.1.0.5", minimumSoftware = "7.1", sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() });
        var package = new EmbeddedPluginPackage(bytes, metadata);
        check(package.Version == new Version(0, 1, 0, 5), "Embedded metadata matches packaged manifest version");
        bool rejected = false;
        try { new EmbeddedPluginPackage(bytes, metadata.Replace("0.1.0.5", "0.1.0.6")); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Mismatched manifest metadata fails resource validation");
        foreach (var mismatch in new[] { metadata.Replace(StreamDeckDetector.PluginId, "other.plugin"), metadata.Replace("7.1", "7.2") })
        {
            rejected = false;
            try { new EmbeddedPluginPackage(bytes, mismatch); } catch (InvalidDataException) { rejected = true; }
            check(rejected, "Wrong embedded identity or minimum software cannot validate");
        }
        rejected = false;
        try { new EmbeddedPluginPackage(new byte[] { 1, 2, 3 }, metadata); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Modified package bytes fail checksum validation");
        bytes[0] ^= 1;
        string directory = Path.Combine(Path.GetTempPath(), "TinyHand-embedded-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string extracted = package.Extract(directory);
            check(File.Exists(extracted) && package.Extract(directory) == extracted, "Matching extracted bytes are reused at a versioned path");
            File.WriteAllText(extracted, "tampered");
            rejected = false;
            try { package.Extract(directory); } catch (InvalidDataException) { rejected = true; }
            check(rejected, "Tampered existing extraction is never launched or overwritten silently");
            File.Delete(extracted);
            string occupiedRoot = Path.Combine(directory, "occupied");
            File.WriteAllText(occupiedRoot, "not a directory");
            rejected = false;
            try { package.Extract(occupiedRoot); } catch (IOException) { rejected = true; }
            check(rejected, "Unwritable extraction root fails without redirecting elsewhere");
            File.Delete(occupiedRoot);
            string external = Path.Combine(directory, "external");
            Directory.CreateDirectory(external);
            string junction = Path.Combine(directory, "redirect");
            using (var process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junction}\" \"{external}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!)
            {
                process.WaitForExit();
                check(process.ExitCode == 0, "Isolated junction fixture can be created without elevation");
            }
            try
            {
                rejected = false;
                try { package.Extract(junction); } catch (IOException) { rejected = true; }
                check(rejected && Directory.GetFileSystemEntries(external).Length == 0, "Reparse extraction path cannot write outside managed root");
            }
            finally { Directory.Delete(junction); Directory.Delete(external); }
            using (var locked = new FileStream(extracted, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                rejected = false;
                try { package.Extract(directory); } catch (IOException) { rejected = true; }
                check(rejected, "Locked extraction target fails without modifying installed plugin");
            }
            File.Delete(extracted);
            var detector = new Detector(); var repository = new Repository(); var launcher = new Launcher();
            using var coordinator = new PluginInstallationCoordinator(package, detector, repository, launcher, directory);
            Await(coordinator.RefreshAsync());
            check(coordinator.ShouldOffer, "Supported missing plugin yields an eligible offer");
            coordinator.Choose("later");
            check(!coordinator.ShouldOffer && launcher.Calls == 0, "Later persists without launching installer");
            check(!Await(coordinator.InstallAsync(TimeSpan.Zero)) && launcher.Calls == 1
                && coordinator.Message!.Contains("noch nicht bestätigt"), "Installer launch alone is never success");
            launcher.Launched = () => detector.Observation = new(PluginInstallStatus.Current, "C:\\Fake Stream Deck\\StreamDeck.exe", new Version(7, 6), package.Version);
            check(Await(coordinator.InstallAsync(TimeSpan.FromSeconds(3))) && launcher.Calls == 2,
                "Installation success requires observing matching installed version");
            detector.Observation = new(PluginInstallStatus.Newer, "C:\\Fake Stream Deck\\StreamDeck.exe", new Version(7, 6), new Version(1, 0, 0, 0));
            check(!Await(coordinator.InstallAsync(TimeSpan.Zero)) && launcher.Calls == 2, "Manual install cannot downgrade a newer observed plugin");
            repository.Writable = false;
            coordinator.EnableOffers(true);
            check(!coordinator.ShouldOffer && coordinator.Message!.Contains("nicht gespeichert"), "Persistence failure suppresses repeated current-process offers");
            using var appOnly = new PluginInstallationCoordinator(null, detector, new Repository(), new Launcher(), directory);
            Await(appOnly.RefreshAsync());
            check(!appOnly.ShouldOffer && appOnly.Message!.Contains("kein Stream-Deck-Plugin"), "App-only build explains missing embedded package");
            var cancelledDetector = new Detector();
            using var cancelled = new PluginInstallationCoordinator(package, cancelledDetector, new Repository(), new Launcher(), directory);
            cancelled.Dispose();
            check(!Await(cancelled.InstallAsync(TimeSpan.Zero)), "Disposed installation coordinator cannot launch or publish delayed UI results");
            detector.Observation = new(PluginInstallStatus.Missing, "C:\\Fake Stream Deck\\StreamDeck.exe");
            detector.Failure = new IOException("Synthetic failure");
            Await(coordinator.RefreshAsync());
            check(coordinator.Observation.Status == PluginInstallStatus.Indeterminate && !Await(coordinator.InstallAsync(TimeSpan.Zero)) && launcher.Calls == 2,
                "Failed detection clears stale installability and cannot launch");
            detector.Failure = null;
            launcher.Fail = true;
            check(!Await(coordinator.InstallAsync(TimeSpan.Zero)) && coordinator.Message!.Contains("nicht geöffnet"), "Launcher failure reports no success and preserves app use");
            var start = StreamDeckInstallerLauncher.StartInfo(@"C:\Program Files\Elgato\StreamDeck\StreamDeck.exe", @"C:\path with spaces\plugin.streamDeckPlugin");
            check(!start.UseShellExecute && start.ArgumentList.Count == 1 && start.ArgumentList[0].Contains("path with spaces"), "Installer uses one structured absolute argument without shell injection");
            var slow = new Detector { DetectOverride = token => { token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); return new(PluginInstallStatus.Missing); } };
            using var bounded = new PluginInstallationCoordinator(package, slow, new Repository(), new Launcher(), directory);
            var watch = Stopwatch.StartNew();
            var refresh = bounded.RefreshAsync();
            var parallel = bounded.RefreshAsync();
            check(ReferenceEquals(refresh, parallel), "Concurrent refresh callers await the same detection");
            Await(refresh);
            check(watch.Elapsed < TimeSpan.FromSeconds(4) && bounded.Observation.Status == PluginInstallStatus.Indeterminate,
                "Unresponsive detection is bounded and quiet");
            var shutdownDetector = new Detector();
            var shutdownLauncher = new Launcher();
            using (var shutdown = new PluginInstallationCoordinator(package, shutdownDetector, new Repository(), shutdownLauncher, directory))
            {
                shutdownLauncher.Launched = shutdown.Dispose;
                check(!Await(shutdown.InstallAsync(TimeSpan.FromSeconds(60))) && shutdownLauncher.Calls == 1 && !shutdown.ShouldOffer,
                    "Shutdown after handoff cancels follow-up immediately without replay");
            }
            var failedExtractionLauncher = new Launcher();
            File.WriteAllText(extracted, "tampered");
            using (var extractionFailure = new PluginInstallationCoordinator(package, new Detector(), new Repository(), failedExtractionLauncher, directory))
                check(!Await(extractionFailure.InstallAsync(TimeSpan.Zero)) && failedExtractionLauncher.Calls == 0,
                    "Coordinator never hands off failed extraction");
            File.Delete(extracted);
            var delayedDetector = new Detector();
            var delayedLauncher = new Launcher();
            using (var delayed = new PluginInstallationCoordinator(package, delayedDetector, new Repository(), delayedLauncher, directory))
            {
                delayedLauncher.Launched = () => delayedDetector.DetectOverride = _ => delayedDetector.Calls >= 3
                    ? new(PluginInstallStatus.Newer, InstalledVersion: new Version(1, 0, 0, 0)) : delayedDetector.Observation;
                check(Await(delayed.InstallAsync(TimeSpan.FromSeconds(5))) && delayedLauncher.Calls == 1,
                    "Delayed matching-or-newer installed observation is success without retrying handoff");
            }

            foreach (string choice in new[] { "install", "later", "never", "close" })
            {
                string? actualChoice = null;
                var offer = new PluginInstallOfferWindow(false);
                offer.Loaded += (_, _) => offer.Dispatcher.BeginInvoke(() =>
                {
                    if (choice == "close") offer.Close();
                    else
                    {
                        var buttons = ((System.Windows.Controls.StackPanel)offer.Content).Children.OfType<System.Windows.Controls.Button>().ToArray();
                        buttons[choice == "install" ? 0 : choice == "later" ? 1 : 2].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                    }
                });
                offer.ShowDialog(); actualChoice = offer.Choice;
                check(actualChoice == (choice == "close" ? "later" : choice), "Live offer button/close maps correct policy: " + choice);
            }

            string controllerPath = Path.Combine(directory, "settings.json");
            var repoPath = Path.Combine(directory, "offers.json");
            var realRepository = new PluginOfferRepository(repoPath);
            var startupDetector = new Detector(); var startupLauncher = new Launcher();
            int offers = 0;
            Func<bool, string> later = _ => { offers++; return "later"; };
            using (var first = new PointerController(controllerPath))
            {
                first.StartPluginInstallation(new PluginInstallationCoordinator(package, startupDetector, realRepository, startupLauncher, directory, () => new DateTimeOffset(2026, 11, 23, 0, 0, 0, TimeSpan.Zero)), later, _ => { });
                Await(first.PluginStartup);
                check(offers == 1 && !first.Active && startupLauncher.Calls == 0, "Startup schedules one offer after tray creation and never activates pointer");
            }
            using (var second = new PointerController(controllerPath))
            {
                second.StartPluginInstallation(new PluginInstallationCoordinator(package, startupDetector, realRepository, startupLauncher, directory, () => new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero)), later, _ => { });
                Await(second.PluginStartup);
                check(offers == 1 && !second.Active, "Real restart honors persisted seven-day deferral");
                var automatic = second.TrayMenu.Items.OfType<System.Windows.Forms.ToolStripMenuItem>().Single(i => i.Text == "Stream-Deck-Angebote beim Start");
                automatic.PerformClick(); automatic.PerformClick();
                check(realRepository.Load().AutomaticOffersEnabled && realRepository.Load().DeferUntilUtc == null && startupLauncher.Calls == 0,
                    "Tray re-enablement clears deferral without immediate install");
            }
            using (var active = new PointerController(controllerPath))
            {
                active.StartPluginInstallation(new PluginInstallationCoordinator(package, startupDetector, new Repository(), startupLauncher, directory), later, _ => { });
                active.SetActive(true);
                Await(active.PluginStartup);
                check(offers == 1 && active.Active, "Late detection does not interrupt active pointer interaction");
            }
            File.Delete(controllerPath); File.Delete(repoPath);
            var optout = new PluginOfferState();
            optout.Choose("never", package.Version, DateTimeOffset.UtcNow);
            realRepository.Save(optout);
            using (var optedOut = new PointerController(controllerPath))
            {
                var optedCoordinator = new PluginInstallationCoordinator(package, startupDetector, realRepository, startupLauncher, directory);
                optedOut.StartPluginInstallation(optedCoordinator, later, _ => { });
                Await(optedOut.PluginStartup);
                int priorOffers = offers;
                var manual = optedOut.TrayMenu.Items.OfType<System.Windows.Forms.ToolStripMenuItem>().Single(i => i.Text!.StartsWith("Stream-Deck-Plugin"));
                manual.PerformClick();
                Await(optedCoordinator.RefreshAsync());
                check(offers == priorOffers + 1 && startupLauncher.Calls == 0 && !optedOut.Active,
                    "Persistent opt-out leaves manual tray retry available without pointer activation");
            }
            optout.Choose("never", package.Version, DateTimeOffset.UtcNow);
            realRepository.Save(optout);
            check(!realRepository.Load().Eligible(new(PluginInstallStatus.Older), new Version(0, 1, 0, 6), DateTimeOffset.UtcNow, true),
                "Persisted all-version opt-out also suppresses a newer bundle");
            File.Delete(controllerPath); File.Delete(repoPath);
        }
        finally
        {
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)) File.Delete(file);
            foreach (var child in Directory.GetDirectories(directory)) Directory.Delete(child);
            Directory.Delete(directory);
        }
    }
}

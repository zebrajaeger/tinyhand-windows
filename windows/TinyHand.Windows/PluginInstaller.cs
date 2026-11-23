using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TinyHand.Windows;

internal interface IPluginInstallerLauncher
{
    void Open(string executable, string package);
}

internal sealed class StreamDeckInstallerLauncher : IPluginInstallerLauncher
{
    internal static ProcessStartInfo StartInfo(string executable, string package)
    {
        if (!Path.IsPathFullyQualified(executable) || !Path.IsPathFullyQualified(package))
            throw new ArgumentException("Installer paths must be absolute.");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        start.ArgumentList.Add(package);
        return start;
    }
    public void Open(string executable, string package)
    {
        var info = FileVersionInfo.GetVersionInfo(executable);
        if (!Path.GetFileName(executable).Equals("StreamDeck.exe", StringComparison.OrdinalIgnoreCase)
            || info.ProductName != "Stream Deck") throw new IOException("Stream Deck executable identity changed.");
        var start = StartInfo(executable, package);
        using var process = Process.Start(start) ?? throw new IOException("Stream Deck could not be started.");
    }
}

internal sealed class PluginInstallationCoordinator : IDisposable
{
    private readonly IStreamDeckDetector detector;
    private readonly IPluginOfferRepository repository;
    private readonly IPluginInstallerLauncher launcher;
    private readonly Func<DateTimeOffset> clock;
    private readonly string extractionRoot;
    private readonly CancellationTokenSource stop = new();
    internal EmbeddedPluginPackage? Package { get; }
    internal PluginOfferState Offers { get; }
    internal PluginInstallationObservation Observation { get; private set; } = new(PluginInstallStatus.Unavailable);
    internal string? Message { get; private set; }
    internal bool PersistenceFailed { get; private set; }
    internal bool Pending { get; private set; }
    internal bool OfferedThisProcess { get; private set; }
    private Task? refreshTask;
    internal event Action? Changed;

    internal PluginInstallationCoordinator(EmbeddedPluginPackage? package, IStreamDeckDetector detector,
        IPluginOfferRepository repository, IPluginInstallerLauncher launcher, string extractionRoot, Func<DateTimeOffset>? clock = null)
    {
        Package = package; this.detector = detector; this.repository = repository; this.launcher = launcher;
        this.extractionRoot = extractionRoot; this.clock = clock ?? (() => DateTimeOffset.UtcNow);
        Offers = repository.Load();
    }

    internal Task RefreshAsync()
    {
        if (stop.IsCancellationRequested) return Task.CompletedTask;
        if (refreshTask is { IsCompleted: false }) return refreshTask;
        return refreshTask = RefreshCoreAsync();
    }

    private async Task RefreshCoreAsync()
    {
        if (stop.IsCancellationRequested) return;
        if (Package == null) { Message = "Dieser Entwicklungsbuild enthält kein Stream-Deck-Plugin."; Changed?.Invoke(); return; }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            var observation = await Task.Run(() => detector.Detect(Package.Version, Package.MinimumSoftware, deadline.Token), deadline.Token).WaitAsync(deadline.Token);
            if (stop.IsCancellationRequested) return;
            Observation = observation;
            Message = Observation.Reason;
            if (Observation.InstalledVersion != null) { Offers.LastObservedVersion = Observation.InstalledVersion.ToString(4); Save(); }
        }
        catch (OperationCanceledException)
        {
            Observation = new(PluginInstallStatus.Indeterminate);
            Message = "Stream-Deck-Erkennung konnte nicht abgeschlossen werden.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or InvalidOperationException)
        {
            Observation = new(PluginInstallStatus.Indeterminate);
            Message = "Stream-Deck-Erkennung ist nicht verfügbar.";
        }
        if (!stop.IsCancellationRequested) Changed?.Invoke();
    }

    internal bool ShouldOffer => !stop.IsCancellationRequested && !OfferedThisProcess && !Pending && Package != null
        && Offers.Eligible(Observation, Package.Version, clock(), true);

    internal void Choose(string choice)
    {
        if (Package == null) return;
        OfferedThisProcess = true;
        Offers.Choose(choice, Package.Version, clock());
        Save();
        Changed?.Invoke();
    }

    internal void EnableOffers(bool enabled)
    {
        Offers.Enable(enabled);
        // Re-enablement affects the next process, not an immediate popup.
        OfferedThisProcess = true;
        Save();
        Changed?.Invoke();
    }

    private void Save()
    {
        if (repository.Save(Offers)) { PersistenceFailed = false; return; }
        PersistenceFailed = true;
        OfferedThisProcess = true;
        Message = "Die Auswahl konnte nicht gespeichert werden; für diese Sitzung wird nicht erneut gefragt.";
    }

    internal async Task<bool> InstallAsync(TimeSpan? observationTime = null)
    {
        if (Pending || Package == null || stop.IsCancellationRequested) return false;
        Pending = true;
        Changed?.Invoke();
        try
        {
            await RefreshAsync();
            stop.Token.ThrowIfCancellationRequested();
            if (Observation.Status is not (PluginInstallStatus.Missing or PluginInstallStatus.Older) || Observation.Executable == null)
                return false;
            Choose("install");
            string path = await Task.Run(() => Package.Extract(extractionRoot), stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            launcher.Open(Observation.Executable, path);
            Message = "Installation in Stream Deck geöffnet. Bitte dort bestätigen.";
            if (PersistenceFailed) Message += " Die Auswahl konnte nicht dauerhaft gespeichert werden.";
            Changed?.Invoke();
            if (stop.IsCancellationRequested) return false;
            var until = Stopwatch.StartNew();
            var limit = observationTime ?? TimeSpan.FromSeconds(60);
            using var followup = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            followup.CancelAfter(limit > TimeSpan.Zero ? limit : TimeSpan.FromMilliseconds(1));
            while (until.Elapsed < limit)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), followup.Token);
                    await RefreshAsync().WaitAsync(followup.Token);
                }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested) { break; }
                if (Observation.Status is PluginInstallStatus.Current or PluginInstallStatus.Newer)
                { Message = "Stream-Deck-Plugin ist installiert."; return true; }
            }
            Message = "Installation noch nicht bestätigt. Status kann später erneut geprüft werden.";
            return false;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException or System.Security.SecurityException)
        { Message = "Stream-Deck-Installation konnte nicht geöffnet werden."; return false; }
        finally { Pending = false; if (!stop.IsCancellationRequested) Changed?.Invoke(); }
    }

    public void Dispose() => stop.Cancel();
}

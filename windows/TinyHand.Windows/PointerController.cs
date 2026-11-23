using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace TinyHand.Windows;

internal sealed class PointerController : IDisposable
{
    private readonly Forms.NotifyIcon tray;
    private readonly HwndSource messages;
    private readonly DispatcherTimer timer;
    private readonly List<OverlayWindow> windows = new();
    private readonly Forms.ToolStripMenuItem toggle;
    private readonly Forms.ToolStripMenuItem styles;
    private readonly Forms.ToolStripMenuItem sizes;
    private readonly Forms.ToolStripMenuItem sounds;
    private readonly Settings settings;
    private readonly string settingsPath;
    private readonly TapSound audio = new();
    private System.Drawing.Icon trayIcon;
    private bool escapeRegistered;
    private bool disposed;
    private bool escapeWasDown;
    private readonly AnnotationDocument document = new();
    private readonly Forms.ToolStripMenuItem colors = new("Zeichenfarbe");
    private Forms.ToolStripMenuItem soundToggle = null!;
    private AnnotationEditor? editor;
    private string? textColor;
    private bool beforeTextActive;
    private Point textAnchor;
    private IntPtr beforeTextForeground;
    internal string TextPhase { get; private set; } = "idle";
    internal bool Interactive => Active || TextPhase == "placing";
    internal event Action? StateChanged;
    private readonly MonitorCapture capture;
    private readonly CancellationTokenSource lifetime = new();
    private PluginInstallationCoordinator? pluginInstallation;
    private bool acquiring;
    internal ControllerState State => new(Active, PointerHeight, settings.SoundEnabled, SelectedColor, TextPhase, document.Items.Count, capture.Pending);

    internal bool Active { get; private set; }
    internal string Style { get; private set; } = "lavender";
    internal int PointerHeight => settings.PointerHeight;
    internal string TrayIconStyle => TrayHandIcon.HandStyle(Style);
    internal System.Drawing.Icon TrayIcon => trayIcon;
    internal Point Cursor { get; private set; }
    internal IReadOnlyList<StrokeAnnotation> Strokes => document.Items.OfType<StrokeAnnotation>().ToArray();
    internal IReadOnlyList<Annotation> Annotations => document.Items;
    internal string SelectedColor => settings.AnnotationColor;
    internal InkStroke? Current { get; private set; }
    internal IntPtr MessageHandle => messages.Handle;
    internal DateTime TapTime { get; private set; }
    internal Point TapPoint { get; private set; }

    internal PointerController(string? settingsPath = null, IMonitorCapturePlatform? capturePlatform = null)
    {
        capture = new MonitorCapture(capturePlatform ?? new WindowsMonitorCapturePlatform(bounds =>
        {
            var overlay = windows.FirstOrDefault(w => w.DesktopBounds == bounds);
            if (overlay != null)
            {
                var scale = System.Windows.Media.VisualTreeHelper.GetDpi(overlay);
                return (scale.DpiScaleX, scale.DpiScaleY);
            }
            return MonitorDpi.Get(bounds);
        }));
        this.settingsPath = settingsPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "TinyHand", "settings.json");
        settings = Settings.Load(this.settingsPath);
        Style = settings.Style;
        AudioLog.Write($"Startup: settings={this.settingsPath}; soundEnabled={settings.SoundEnabled}; sound={settings.Sound}; executable={Environment.ProcessPath}");
        messages = new HwndSource(new HwndSourceParameters("TinyHand hotkeys")
        {
            ParentWindow = new IntPtr(-3), WindowStyle = 0, Width = 0, Height = 0
        });
        messages.AddHook(OnMessage);
        var menu = new Forms.ContextMenuStrip();
        toggle = new Forms.ToolStripMenuItem("Einschalten (Strg+Alt+H)", null, (_, _) => SetActive(!Active));
        menu.Items.Add(toggle);
        menu.Items.Add("Alles löschen (Strg+Alt+X)", null, (_, _) => Clear());
        foreach (var color in new[] { ("Pink", "#E14164"), ("Rot", "#FF0000"), ("Gelb", "#FFFF00"), ("Grün", "#00C853"), ("Blau", "#2979FF"), ("Weiß", "#FFFFFF"), ("Schwarz", "#000000") })
        {
            var item = new Forms.ToolStripMenuItem(color.Item1) { Tag = color.Item2, Checked = SelectedColor == color.Item2 };
            item.Click += (_, _) => SetAnnotationColor((string)item.Tag);
            colors.DropDownItems.Add(item);
        }
        menu.Items.Add(colors);
        menu.Items.Add("Text platzieren", null, (_, _) => BeginTextPlacement());
        menu.Items.Add("Screenshot in Zwischenablage", null, async (_, _) =>
        {
            var result = await CaptureMonitorAsync();
            if (!disposed) tray!.ShowBalloonTip(3000, "TinyHand", result.Ok ? "Screenshot in die Zwischenablage kopiert."
                : result.Error == "busy" ? "Screenshot nicht möglich: Eingabe oder Aufnahme läuft."
                : "Screenshot konnte nicht kopiert werden.", result.Ok ? Forms.ToolTipIcon.Info : Forms.ToolTipIcon.Warning);
        });
        styles = new Forms.ToolStripMenuItem("Zeiger");
        foreach (string style in AvailableStyles())
        {
            var item = new Forms.ToolStripMenuItem(style == "laser" ? "Laser" : style);
            item.Checked = style == Style;
            item.Click += (_, _) => SelectStyle(style);
            styles.DropDownItems.Add(item);
        }
        menu.Items.Add(styles);
        sizes = new Forms.ToolStripMenuItem("Zeigergröße");
        foreach (var entry in new[] { ("Klein", 220), ("Mittel", 340), ("Groß", 480) })
        {
            var item = new Forms.ToolStripMenuItem(entry.Item1) { Tag = entry.Item2, Checked = settings.PointerHeight == entry.Item2 };
            item.Click += (_, _) => SetPointerHeight(entry.Item2);
            sizes.DropDownItems.Add(item);
        }
        menu.Items.Add(sizes);
        soundToggle = new Forms.ToolStripMenuItem("Tippgeräusch") { Checked = settings.SoundEnabled };
        soundToggle.Click += (_, _) => SetSoundEnabled(!settings.SoundEnabled);
        menu.Items.Add(soundToggle);
        sounds = new Forms.ToolStripMenuItem("Geräusch auswählen");
        foreach (string name in TapSound.Names)
        {
            var item = new Forms.ToolStripMenuItem(name) { Checked = settings.Sound == name };
            item.Click += (_, _) =>
            {
                settings.Sound = name;
                foreach (Forms.ToolStripMenuItem choice in sounds.DropDownItems) choice.Checked = choice.Text == name;
                SaveSettings();
                audio.Play(name);
            };
            sounds.DropDownItems.Add(item);
        }
        menu.Items.Add(sounds);
        menu.Items.Add(new Forms.ToolStripSeparator());
        var startup = new StartupRegistration(Environment.ProcessPath!);
        var startupToggle = new Forms.ToolStripMenuItem("Mit Windows starten") { Checked = startup.Enabled };
        startupToggle.Click += (_, _) =>
        {
            bool enabled = !startupToggle.Checked;
            if (startup.SetEnabled(enabled))
                startupToggle.Checked = startup.Enabled;
            else
                MessageBox.Show("Die Autostart-Einstellung konnte nicht geändert werden.", "TinyHand",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        menu.Opening += (_, _) => startupToggle.Checked = startup.Enabled;
        menu.Items.Add(startupToggle);
        menu.Items.Add("Bedienung", null, (_, _) => MessageBox.Show(
            "Strg+Alt+H: Zeiger ein/aus (Anmerkungen bleiben)\nZiehen: zeichnen\nUmschalt beim Loslassen: Pfeil\nStrg+Alt+X: Alles löschen\nEsc: ausschalten / Texteingabe abbrechen\n\nText platzieren: Position anklicken, tippen, mit Enter bestätigen.\nIm Zeigemodus werden Mausklicks abgefangen.\nOhne Zeiger sind Anmerkungen klickdurchlässig.", "TinyHand für Windows"));
        menu.Items.Add("Beenden", null, (_, _) => Application.Current.Shutdown());
        trayIcon = TrayHandIcon.Create(Style);
        tray = new Forms.NotifyIcon
        {
            Icon = trayIcon,
            Text = "TinyHand — Strg+Alt+H zum Einschalten",
            ContextMenuStrip = menu,
            Visible = true
        };
        tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) SetActive(!Active); };
        bool toggleOk = Native.RegisterHotKey(messages.Handle, 1, Native.Modifiers, 0x48);
        bool clearOk = Native.RegisterHotKey(messages.Handle, 2, Native.Modifiers, 0x58);
        if (!toggleOk || !clearOk)
            tray.ShowBalloonTip(6000, "TinyHand", "Ein Tastenkürzel ist bereits belegt. Verwende das Tray-Menü.", Forms.ToolTipIcon.Warning);
        else
            tray.ShowBalloonTip(4000, "TinyHand ist bereit", "Strg+Alt+H schaltet den Zeiger ein. Esc schaltet ihn aus.", Forms.ToolTipIcon.Info);
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) => Tick(), Dispatcher.CurrentDispatcher);
        timer.Stop();
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
    }

    internal static IEnumerable<string> AvailableStyles()
    {
        using var stream = typeof(PointerController).Assembly.GetManifestResourceStream("TinyHand.Windows.g.resources");
        if (stream != null)
        {
            using var reader = new ResourceReader(stream);
            foreach (string name in reader.Cast<System.Collections.DictionaryEntry>().Select(e => (string)e.Key).OrderBy(n => n))
                if (name.StartsWith("hands/hand_", StringComparison.Ordinal) && name.EndsWith(".png", StringComparison.Ordinal))
                    yield return Path.GetFileNameWithoutExtension(name)[5..];
        }
        yield return "laser";
    }

    internal void SelectStyle(string style)
    {
        if (DeferDuringCapture(() => SelectStyle(style))) return;
        if (!AvailableStyles().Contains(style)) return;
        var nextIcon = TrayHandIcon.Create(style);
        tray.Icon = nextIcon;
        var previousIcon = trayIcon;
        trayIcon = nextIcon;
        previousIcon.Dispose();
        Style = style;
        settings.Style = style;
        SaveSettings();
        foreach (Forms.ToolStripMenuItem item in styles.DropDownItems)
            item.Checked = string.Equals(item.Text, style, StringComparison.OrdinalIgnoreCase);
        foreach (var window in windows) window.LoadStyle();
    }

    internal void SetPointerHeight(int height)
    {
        if (DeferDuringCapture(() => SetPointerHeight(height))) return;
        settings.PointerHeight = Math.Clamp(height, 160, 640);
        foreach (Forms.ToolStripMenuItem item in sizes.DropDownItems) item.Checked = (int)item.Tag! == settings.PointerHeight;
        SaveSettings();
        foreach (var window in windows) window.InvalidateVisual();
        StateChanged?.Invoke();
    }

    private void SaveSettings()
    {
        PersistenceWarning = !settings.Save(settingsPath);
        if (PersistenceWarning)
            tray.ShowBalloonTip(4000, "TinyHand", "Einstellungen konnten nicht gespeichert werden. Änderungen gelten nur für diese Sitzung.", Forms.ToolTipIcon.Warning);
    }

    internal bool PersistenceWarning { get; private set; }
    internal void ResetPersistenceWarning() => PersistenceWarning = false;

    internal Task PluginStartup { get; private set; } = Task.CompletedTask;
    internal Forms.ContextMenuStrip TrayMenu => tray.ContextMenuStrip!;
    private bool pluginOfferVisible;
    private PluginInstallOfferWindow? pluginOfferWindow;

    internal void StartPluginInstallation(PluginInstallationCoordinator? coordinator = null, Func<bool, string>? chooseOffer = null,
        Action<string>? showStatus = null)
    {
        if (pluginInstallation != null) return;
        EmbeddedPluginPackage? package;
        try { package = coordinator != null ? coordinator.Package : EmbeddedPluginPackage.Load(); }
        catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException or FormatException)
        {
            tray.ContextMenuStrip!.Items.Add("Stream-Deck-Plugin: Paket ungültig", null, (_, _) =>
                MessageBox.Show("Das eingebettete Plugin konnte nicht geprüft werden.", "TinyHand"));
            return;
        }
        pluginInstallation = coordinator ?? new PluginInstallationCoordinator(package,
            new StreamDeckDetector(new WindowsStreamDeckDiscovery()),
            new PluginOfferRepository(Path.Combine(Path.GetDirectoryName(settingsPath)!, "stream-deck-installation.json")),
            new StreamDeckInstallerLauncher(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TinyHand", "StreamDeckPlugin"));
        var item = new Forms.ToolStripMenuItem("Stream-Deck-Plugin …");
        item.Click += async (_, _) =>
        {
            await pluginInstallation.RefreshAsync();
            if (disposed || pluginOfferVisible) return;
            if (pluginInstallation.Observation.Status is PluginInstallStatus.Missing or PluginInstallStatus.Older && package != null)
            {
                string choice = AskOffer();
                if (disposed) return;
                pluginInstallation.Choose(choice);
                if (choice == "install")
                {
                    await pluginInstallation.InstallAsync();
                    if (!disposed) Status(pluginInstallation.Message ?? "Installationsstatus nicht verfügbar.");
                }
            }
            else Status(pluginInstallation.Message ?? "Stream-Deck-Plugin ist bereits aktuell.");
        };
        var automatic = new Forms.ToolStripMenuItem("Stream-Deck-Angebote beim Start") { Checked = pluginInstallation.Offers.AutomaticOffersEnabled };
        automatic.Click += (_, _) => pluginInstallation.EnableOffers(!pluginInstallation.Offers.AutomaticOffersEnabled);
        pluginInstallation.Changed += () =>
        {
            if (disposed) return;
            automatic.Checked = pluginInstallation.Offers.AutomaticOffersEnabled;
            item.Enabled = !pluginInstallation.Pending;
            item.Text = pluginInstallation.Pending ? "Stream-Deck-Installation läuft …" : pluginInstallation.Observation.Status switch
            {
                PluginInstallStatus.Missing when package != null => "Stream-Deck-Plugin installieren …",
                PluginInstallStatus.Older => "Stream-Deck-Plugin aktualisieren …",
                PluginInstallStatus.Current or PluginInstallStatus.Newer => "Stream-Deck-Plugin: bereits aktuell",
                _ => "Stream-Deck-Plugin: Status prüfen …"
            };
            if (pluginInstallation.Message != null && !pluginInstallation.Pending)
                item.ToolTipText = pluginInstallation.Message;
        };
        tray.ContextMenuStrip!.Items.Add(item);
        tray.ContextMenuStrip.Items.Add(automatic);
        PluginStartup = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await pluginInstallation.RefreshAsync();
            if (disposed || Active || TextPhase != "idle" || !pluginInstallation.ShouldOffer) return;
            if (pluginOfferVisible) return;
            string choice = AskOffer();
            if (disposed) return;
            pluginInstallation.Choose(choice);
            if (choice == "install")
            {
                await pluginInstallation.InstallAsync();
                if (!disposed) tray.ShowBalloonTip(4000, "TinyHand", pluginInstallation.Message ?? "Installationsstatus nicht verfügbar.", Forms.ToolTipIcon.Info);
            }
        }).Task.Unwrap();

        string AskOffer()
        {
            pluginOfferVisible = true;
            try
            {
                bool update = pluginInstallation.Observation.Status == PluginInstallStatus.Older;
                if (chooseOffer != null) return chooseOffer(update);
                pluginOfferWindow = new PluginInstallOfferWindow(update);
                pluginOfferWindow.ShowDialog();
                return pluginOfferWindow.Choice;
            }
            finally { pluginOfferVisible = false; pluginOfferWindow = null; }
        }
        void Status(string message)
        {
            if (showStatus != null) showStatus(message);
            else MessageBox.Show(message, "TinyHand");
        }
    }

    internal void SetActive(bool active)
    {
        if (DeferDuringCapture(() => SetActive(active))) return;
        if (TextPhase != "idle") FinishText(null, active);
        if (Active == active) return;
        Active = active;
        audio.SetKeepAlive(active && settings.SoundEnabled);
        if (active)
        {
            escapeRegistered = Native.RegisterHotKey(messages.Handle, 3, 0x4000, 0x1B);
            escapeWasDown = false;
            if (windows.Count == 0) CreateOverlays();
            timer.Start();
        }
        else
        {
            timer.Stop();
            if (escapeRegistered) Native.UnregisterHotKey(messages.Handle, 3);
            escapeRegistered = false;
            Current = null;
            if (document.Items.Count == 0) CloseOverlays();
        }
        foreach (var window in windows) { window.UpdateInteraction(); window.InvalidateVisual(); }
        toggle.Text = active ? "Ausschalten (Strg+Alt+H)" : "Einschalten (Strg+Alt+H)";
        tray.Text = active ? "TinyHand aktiv — Esc zum Ausschalten" : "TinyHand — Strg+Alt+H zum Einschalten";
        StateChanged?.Invoke();
    }

    internal bool BeginTextPlacement()
    {
        if (capture.Pending) return false;
        if (TextPhase != "idle" || Current != null) return false;
        beforeTextActive = Active;
        beforeTextForeground = Native.GetForegroundWindow();
        textColor = SelectedColor;
        TextPhase = "placing";
        if (windows.Count == 0) CreateOverlays();
        foreach (var window in windows) { window.UpdateInteraction(); window.InvalidateVisual(); }
        if (!Active)
        {
            escapeRegistered = Native.RegisterHotKey(messages.Handle, 3, 0x4000, 0x1B);
            escapeWasDown = false;
            timer.Start();
        }
        StateChanged?.Invoke();
        return true;
    }

    internal void PlaceText(Point point)
    {
        if (TextPhase != "placing") return;
        textAnchor = point;
        TextPhase = "editing";
        // The focused editor owns Esc now, not the global hotkey/tick fallback.
        if (escapeRegistered) Native.UnregisterHotKey(messages.Handle, 3);
        escapeRegistered = false;
        foreach (var window in windows) { window.UpdateInteraction(); window.InvalidateVisual(); }
        editor = new AnnotationEditor(point, textColor!, (text, restoreFocus) => FinishText(text, allowFocusRestore: restoreFocus));
        editor.Show();
        editor.Activate();
        StateChanged?.Invoke();
    }

    internal void FinishText(string? text, bool? requestedActive = null, bool allowFocusRestore = true)
    {
        if (TextPhase == "idle") return;
        bool restoreFocus = allowFocusRestore && editor != null && Native.GetForegroundWindow() == new WindowInteropHelper(editor).Handle;
        text = text == null ? null : AnnotationText.Normalize(text);
        if (!string.IsNullOrWhiteSpace(text)) document.Commit(new TextAnnotation(textAnchor, text, textColor!));
        TextPhase = "idle";
        var oldEditor = editor;
        editor = null;
        oldEditor?.Dismiss();
        if (escapeRegistered) Native.UnregisterHotKey(messages.Handle, 3);
        escapeRegistered = false;
        bool active = requestedActive ?? beforeTextActive;
        if (Active != active) SetActive(active);
        if (active)
        {
            escapeRegistered = Native.RegisterHotKey(messages.Handle, 3, 0x4000, 0x1B);
            escapeWasDown = Native.GetAsyncKeyState(0x1B) < 0;
        }
        else
        {
            timer.Stop();
            if (document.Items.Count == 0) CloseOverlays();
        }
        foreach (var window in windows) { window.UpdateInteraction(); window.InvalidateVisual(); }
        if (restoreFocus && Native.IsWindow(beforeTextForeground)) Native.SetForegroundWindow(beforeTextForeground);
        StateChanged?.Invoke();
    }

    private void Escape()
    {
        if (TextPhase != "idle") FinishText(null);
        else SetActive(false);
    }

    private void CloseOverlays()
    {
        foreach (var window in windows) window.Close();
        windows.Clear();
    }

    internal void SetSoundEnabled(bool enabled)
    {
        if (DeferDuringCapture(() => SetSoundEnabled(enabled))) return;
        settings.SoundEnabled = enabled;
        soundToggle.Checked = enabled;
        audio.SetKeepAlive(Active && enabled);
        SaveSettings();
        StateChanged?.Invoke();
    }

    internal bool SetAnnotationColor(string color)
    {
        if (!AnnotationColor.IsValid(color)) return false;
        if (DeferDuringCapture(() => SetAnnotationColor(color))) return true;
        settings.AnnotationColor = AnnotationColor.Normalize(color);
        foreach (Forms.ToolStripMenuItem item in colors.DropDownItems) item.Checked = (string)item.Tag! == SelectedColor;
        SaveSettings();
        StateChanged?.Invoke();
        return true;
    }

    private void CreateOverlays()
    {
        if (Native.GetCursorPos(out var p)) Cursor = new Point(p.X, p.Y);
        foreach (var screen in Forms.Screen.AllScreens)
        {
            var overlay = new OverlayWindow(this, screen.Bounds);
            windows.Add(overlay);
            overlay.Show();
        }
    }

    private void Tick()
    {
        // Fallback when another program owns the plain Escape hotkey.
        bool escape = Native.GetAsyncKeyState(0x1B) < 0;
        if (TextPhase != "editing" && escape && !escapeWasDown) { escapeWasDown = true; Escape(); return; }
        escapeWasDown = escape;
        if (Native.GetCursorPos(out var p)) Cursor = new Point(p.X, p.Y);
        foreach (var window in windows)
        {
            window.UpdatePose();
            window.InvalidateVisual();
        }
    }

    internal void Begin(Point point)
    {
        if (acquiring) return;
        if (!Active || TextPhase != "idle") return;
        Current = new InkStroke(SelectedColor);
        Current.Add(point);
        TapPoint = point;
        TapTime = DateTime.UtcNow;
        AudioLog.Write($"Pointer click: soundEnabled={settings.SoundEnabled}; sound={settings.Sound}");
        if (settings.SoundEnabled) audio.Play(settings.Sound);
    }

    internal void Move(Point point) => Current?.Add(point);
    internal void End(Point point, bool arrow)
    {
        if (Current == null) return;
        Current.Add(point);
        Current.Arrow = arrow;
        if (Current.Points.Count >= 2) document.Commit(Current.Snapshot());
        Current = null;
        StateChanged?.Invoke();
    }

    internal void Clear()
    {
        if (DeferDuringCapture(Clear)) return;
        if (TextPhase != "idle") FinishText(null);
        Current = null;
        document.Clear();
        if (!Active) CloseOverlays();
        foreach (var window in windows) window.InvalidateVisual();
        StateChanged?.Invoke();
    }

    private IntPtr OnMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != Native.WmHotkey) return IntPtr.Zero;
        switch (wParam.ToInt32())
        {
            case 1: SetActive(!Active); break;
            case 2: Clear(); break;
            case 3: Escape(); break;
        }
        handled = true;
        return IntPtr.Zero;
    }

    private void DisplayChanged(object? sender, EventArgs e) => Application.Current.Dispatcher.BeginInvoke(() =>
    {
        if (disposed) return;
        if (DeferDuringCapture(() => DisplayChanged(sender, e))) return;
        bool active = Active;
        if (TextPhase != "idle") FinishText(null);
        Current = null;
        document.Clear();
        CloseOverlays();
        if (active) CreateOverlays();
        StateChanged?.Invoke();
    });

    private readonly Queue<Action> deferredCaptureCommands = new();
    private bool DeferDuringCapture(Action command)
    {
        if (!acquiring) return false;
        deferredCaptureCommands.Enqueue(command);
        return true;
    }

    internal async Task<CaptureResult> CaptureMonitorAsync()
    {
        if (disposed) return new(false, "cancelled");
        var task = capture.CaptureAsync(document.Snapshot(), Current != null || TextPhase != "idle", (target, suppress) =>
        {
            acquiring = suppress;
            foreach (var window in windows.Where(w => w.DesktopBounds == target.Bounds))
            {
                window.CaptureSuppressed = suppress;
                window.InvalidateVisual();
                if (suppress) window.UpdateLayout();
            }
            if (!suppress)
            {
                while (deferredCaptureCommands.TryDequeue(out var command))
                    if (!disposed) command();
            }
        }, lifetime.Token);
        StateChanged?.Invoke();
        var result = await task;
        if (!disposed) StateChanged?.Invoke();
        return result;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        pluginInstallation?.Dispose();
        pluginOfferWindow?.Close();
        acquiring = false;
        deferredCaptureCommands.Clear();
        SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        SetActive(false);
        document.Clear();
        CloseOverlays();
        timer.Stop();
        Native.UnregisterHotKey(messages.Handle, 1);
        Native.UnregisterHotKey(messages.Handle, 2);
        messages.Dispose();
        tray.Visible = false;
        tray.ContextMenuStrip?.Dispose();
        tray.Dispose();
        trayIcon.Dispose();
        audio.Dispose();
    }
}

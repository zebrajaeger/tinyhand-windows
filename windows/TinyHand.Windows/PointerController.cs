using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
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

    internal bool Active { get; private set; }
    internal string Style { get; private set; } = "lavender";
    internal int PointerHeight => settings.PointerHeight;
    internal string TrayIconStyle => TrayHandIcon.HandStyle(Style);
    internal System.Drawing.Icon TrayIcon => trayIcon;
    internal Point Cursor { get; private set; }
    internal List<InkStroke> Strokes { get; } = new();
    internal InkStroke? Current { get; private set; }
    internal IntPtr MessageHandle => messages.Handle;
    internal DateTime TapTime { get; private set; }
    internal Point TapPoint { get; private set; }

    internal PointerController(string? settingsPath = null)
    {
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
        menu.Items.Add("Zeichnung löschen (Strg+Alt+X)", null, (_, _) => Clear());
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
        var soundToggle = new Forms.ToolStripMenuItem("Tippgeräusch") { Checked = settings.SoundEnabled, CheckOnClick = true };
        soundToggle.CheckedChanged += (_, _) =>
        {
            settings.SoundEnabled = soundToggle.Checked;
            audio.SetKeepAlive(Active && settings.SoundEnabled);
            SaveSettings();
        };
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
            "Strg+Alt+H: Zeiger ein/aus\nZiehen: zeichnen\nUmschalt beim Loslassen: Pfeil\nStrg+Alt+X: Zeichnung löschen\nEsc: löschen und ausschalten\n\nIm Zeigemodus werden Mausklicks abgefangen.\nZum Bedienen anderer Apps den Zeiger ausschalten.\nRechtsklick auf das Tray-Symbol: Menü.", "TinyHand für Windows"));
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
        settings.PointerHeight = Math.Clamp(height, 160, 640);
        foreach (Forms.ToolStripMenuItem item in sizes.DropDownItems) item.Checked = (int)item.Tag! == settings.PointerHeight;
        SaveSettings();
        foreach (var window in windows) window.InvalidateVisual();
    }

    private void SaveSettings()
    {
        if (!settings.Save(settingsPath))
            tray.ShowBalloonTip(4000, "TinyHand", "Einstellungen konnten nicht gespeichert werden. Änderungen gelten nur für diese Sitzung.", Forms.ToolTipIcon.Warning);
    }

    internal void SetActive(bool active)
    {
        if (Active == active) return;
        Active = active;
        audio.SetKeepAlive(active && settings.SoundEnabled);
        if (active)
        {
            Strokes.Clear();
            escapeRegistered = Native.RegisterHotKey(messages.Handle, 3, 0x4000, 0x1B);
            escapeWasDown = false;
            CreateOverlays();
            timer.Start();
        }
        else
        {
            timer.Stop();
            if (escapeRegistered) Native.UnregisterHotKey(messages.Handle, 3);
            escapeRegistered = false;
            Current = null;
            foreach (var window in windows) window.Close();
            windows.Clear();
            Strokes.Clear();
        }
        toggle.Text = active ? "Ausschalten (Strg+Alt+H)" : "Einschalten (Strg+Alt+H)";
        tray.Text = active ? "TinyHand aktiv — Esc zum Ausschalten" : "TinyHand — Strg+Alt+H zum Einschalten";
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
        if (escape && !escapeWasDown) { SetActive(false); return; }
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
        Current = new InkStroke();
        Current.Add(point);
        Strokes.Add(Current);
        // Bound memory in unusually long presentations.
        if (Strokes.Count > 500) Strokes.RemoveAt(0);
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
        Current = null;
    }

    internal void Clear()
    {
        Current = null;
        Strokes.Clear();
        foreach (var window in windows) window.InvalidateVisual();
    }

    private IntPtr OnMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != Native.WmHotkey) return IntPtr.Zero;
        switch (wParam.ToInt32())
        {
            case 1: SetActive(!Active); break;
            case 2: Clear(); break;
            case 3: SetActive(false); break;
        }
        handled = true;
        return IntPtr.Zero;
    }

    private void DisplayChanged(object? sender, EventArgs e) => Application.Current.Dispatcher.BeginInvoke(() =>
    {
        if (!Active || disposed) return;
        SetActive(false);
        SetActive(true);
    });

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        SetActive(false);
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

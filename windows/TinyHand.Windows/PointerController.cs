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
    private bool escapeRegistered;
    private bool disposed;
    private bool escapeWasDown;

    internal bool Active { get; private set; }
    internal string Style { get; private set; } = "lavender";
    internal Point Cursor { get; private set; }
    internal List<InkStroke> Strokes { get; } = new();
    internal InkStroke? Current { get; private set; }
    internal IntPtr MessageHandle => messages.Handle;
    internal DateTime TapTime { get; private set; }
    internal Point TapPoint { get; private set; }

    internal PointerController()
    {
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
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Bedienung", null, (_, _) => MessageBox.Show(
            "Strg+Alt+H: Zeiger ein/aus\nZiehen: zeichnen\nUmschalt beim Loslassen: Pfeil\nStrg+Alt+X: Zeichnung löschen\nEsc: löschen und ausschalten\n\nIm Zeigemodus werden Mausklicks abgefangen.\nZum Bedienen anderer Apps den Zeiger ausschalten.\nRechtsklick auf das Tray-Symbol: Menü.", "TinyHand für Windows"));
        menu.Items.Add("Beenden", null, (_, _) => Application.Current.Shutdown());
        tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
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

    private void SelectStyle(string style)
    {
        Style = style;
        foreach (Forms.ToolStripMenuItem item in styles.DropDownItems)
            item.Checked = string.Equals(item.Text, style, StringComparison.OrdinalIgnoreCase);
        foreach (var window in windows) window.LoadStyle();
    }

    internal void SetActive(bool active)
    {
        if (Active == active) return;
        Active = active;
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
        foreach (var window in windows) window.InvalidateVisual();
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
    }
}

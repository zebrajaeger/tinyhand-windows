using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using TinyHand.Windows;

internal static class AnnotationInputRegression
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenPoint { internal int X; internal int Y; }
    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(ScreenPoint point);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    internal static void Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "TinyHand-input-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var controller = new PointerController(Path.Combine(directory, "settings.json"));
            using var target = new System.Windows.Forms.Form { Text = "TinyHand isolated input target", StartPosition = System.Windows.Forms.FormStartPosition.Manual,
                TopMost = true, Left = 120, Top = 120, Width = 400, Height = 250 };
            target.Show();
            Native.SetForegroundWindow(target.Handle);
            Pump();
            var anchor = new Point(target.Left + 70, target.Top + 90);
            controller.SetActive(true);
            controller.Begin(anchor);
            controller.End(new Point(anchor.X + 100, anchor.Y), false);
            Pump();
            var probe = new ScreenPoint { X = (int)anchor.X, Y = (int)anchor.Y };
            check(WindowFromPoint(probe) != target.Handle, "Active overlay owns desktop hit testing over isolated target");
            controller.SetActive(false);
            Pump();
            check(WindowFromPoint(probe) == target.Handle, "Passive overlay actually passes native hit testing to underlying window");

            controller.BeginTextPlacement();
            controller.PlaceText(anchor);
            Pump();
            var editor = Application.Current.Windows.OfType<AnnotationEditor>().Single();
            var input = (TextBox)editor.Content;
            check(input.IsKeyboardFocused, "Explicit editor receives keyboard focus");
            input.Text = "one\r\ntwo 😀";
            check(input.Text == "one two 😀", "Real editor normalizes multiline input");
            input.Text = string.Concat(Enumerable.Repeat("😀", 501));
            check(input.Text.EnumerateRunes().Count() == 500, "Real editor enforces Unicode scalar input limit");
            input.Text = "Committed Ä😀";
            var source = PresentationSource.FromVisual(input)!;
            var composition = new TextComposition(InputManager.Current, input, "あ");
            input.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition)
                { RoutedEvent = TextCompositionManager.PreviewTextInputStartEvent });
            var acceptingComposition = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter)
                { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            input.RaiseEvent(acceptingComposition);
            check(controller.TextPhase == "editing", "IME acceptance Enter does not commit the annotation");
            input.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition)
                { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
            Pump();
            input.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Pump();
            check(controller.TextPhase == "idle" && controller.Annotations.OfType<TextAnnotation>().Single().Text == "Committed Ä😀",
                "Editor Enter event commits Unicode text and closes editor");
            check(!controller.Active, "Editor commit restores passive state");

            controller.SetActive(true);
            controller.BeginTextPlacement();
            controller.PlaceText(anchor);
            Pump();
            editor = Application.Current.Windows.OfType<AnnotationEditor>().Single();
            input = (TextBox)editor.Content;
            input.Text = "discard";
            input.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(input)!, Environment.TickCount, Key.Escape)
                { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Pump();
            check(controller.Active && controller.TextPhase == "idle" && controller.Annotations.Count == 2,
                "Editor Esc cancels only draft and restores active state");

            controller.SetActive(false);
            controller.BeginTextPlacement();
            controller.PlaceText(anchor);
            Pump();
            Native.SetForegroundWindow(target.Handle);
            Pump();
            check(controller.TextPhase == "idle" && !controller.Active && controller.Annotations.Count == 2,
                "Real focus loss cancels draft without changing committed content");
            IntPtr foreground = Native.GetForegroundWindow();
            GetWindowThreadProcessId(foreground, out uint foregroundProcess);
            check(foreground == target.Handle || (foreground != IntPtr.Zero && foregroundProcess != Environment.ProcessId),
                "Focus loss leaves the target or externally selected application foreground, never a TinyHand window");

            controller.BeginTextPlacement();
            controller.PlaceText(anchor);
            Pump();
            controller.SetActive(false);
            check(controller.TextPhase == "idle" && !Application.Current.Windows.OfType<AnnotationEditor>().Any(),
                "Explicit pointer-off closes a focused editor without committing");

            foreach (bool active in new[] { false, true })
            {
                controller.SetActive(active);
                controller.BeginTextPlacement();
                controller.Clear();
                check(controller.Active == active && controller.TextPhase == "idle" && controller.Annotations.Count == 0,
                    "Clear during placement preserves prior activation and cancels all content");
                controller.Begin(anchor);
                controller.End(new Point(anchor.X + 100, anchor.Y), false);
                typeof(PointerController).GetMethod("DisplayChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .Invoke(controller, new object?[] { null, EventArgs.Empty });
                Pump();
                check(controller.Active == active && controller.Annotations.Count == 0, "Display reset preserves activation and empties document");
            }
            controller.SetActive(false);
            controller.BeginTextPlacement();
            controller.PlaceText(anchor);
            Pump();
            controller.Dispose();
            check(!Application.Current.Windows.OfType<AnnotationEditor>().Any() && !Application.Current.Windows.OfType<OverlayWindow>().Any(),
                "Disposal cancels focused editor and closes every overlay");
            target.Close();
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
}

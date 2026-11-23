using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace TinyHand.Windows;

internal sealed class AnnotationEditor : Window
{
    private readonly TextBox input;
    private readonly Action<string?, bool> finish;
    private bool finished;
    private bool composing;

    internal AnnotationEditor(Point anchor, string color, Action<string?, bool> finish)
    {
        this.finish = finish;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        SizeToContent = SizeToContent.WidthAndHeight;
        input = new TextBox { MinWidth = 100, MaxWidth = 700, FontFamily = new FontFamily("Segoe UI"), FontSize = 28,
            Foreground = new SolidColorBrush(AnnotationColor.Parse(color)), Background = Brushes.White,
            AcceptsReturn = false, AcceptsTab = false, Padding = new Thickness(2) };
        Content = input;
        SourceInitialized += (_, _) =>
        {
            // Position at the physical anchor; WPF converts layout for this monitor afterward.
            Native.SetWindowPos(new WindowInteropHelper(this).Handle, new IntPtr(-1), (int)anchor.X, (int)anchor.Y, 0, 0, 0x0001);
        };
        input.TextChanged += (_, _) =>
        {
            if (composing) return;
            string normalized = AnnotationText.Normalize(input.Text);
            if (input.Text == normalized) return;
            int caret = input.CaretIndex;
            input.Text = normalized;
            input.CaretIndex = Math.Min(caret, normalized.Length);
        };
        TextCompositionManager.AddPreviewTextInputStartHandler(input, (_, _) => composing = true);
        TextCompositionManager.AddPreviewTextInputHandler(input, (_, _) =>
        {
            // Keep composition active until the key event accepting it has completed.
            Dispatcher.BeginInvoke(() =>
            {
                composing = false;
                string normalized = AnnotationText.Normalize(input.Text);
                if (input.Text != normalized)
                {
                    int caret = input.CaretIndex;
                    input.Text = normalized;
                    input.CaretIndex = Math.Min(caret, normalized.Length);
                }
            });
        });
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { e.Handled = true; Complete(null); }
            else if (e.Key == Key.Enter && !composing) { e.Handled = true; Complete(AnnotationText.Normalize(input.Text)); }
        };
        // Closing synchronously during WM_ACTIVATE can interrupt activation of the target window.
        Deactivated += (_, _) => Dispatcher.BeginInvoke(() => Complete(null, false));
        Loaded += (_, _) => input.Focus();
    }

    private void Complete(string? text, bool allowFocusRestore = true)
    {
        if (finished) return;
        finished = true;
        finish(text, allowFocusRestore);
    }

    internal void Dismiss()
    {
        finished = true;
        Close();
    }
}

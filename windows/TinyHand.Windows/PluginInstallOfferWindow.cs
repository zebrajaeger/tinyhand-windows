using System;
using System.Windows;
using System.Windows.Controls;

namespace TinyHand.Windows;

internal sealed class PluginInstallOfferWindow : Window
{
    internal string Choice { get; private set; } = "later";
    internal PluginInstallOfferWindow(bool update)
    {
        Title = "TinyHand – Stream Deck";
        Width = 460; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen; ShowInTaskbar = false;
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = update ? "Ein Update für das TinyHand-Plugin ist verfügbar." : "Stream Deck wurde gefunden. TinyHand-Plugin installieren?",
            TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.Bold });
        panel.Children.Add(new TextBlock { Text = "Tasten für Zeiger, Ton, Texte und Screenshots. Stream Deck fragt anschließend nach Bestätigung. Der Zeiger bleibt ausgeschaltet.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) });
        foreach (var entry in new[] { (update ? "Aktualisieren" : "Installieren", "install"), ("Später (in 7 Tagen)", "later"), ("Nicht mehr fragen", "never") })
        {
            var button = new Button { Content = entry.Item1, Margin = new Thickness(0, 4, 0, 4), Padding = new Thickness(8), IsCancel = entry.Item2 == "later" };
            button.Click += (_, _) => { Choice = entry.Item2; DialogResult = true; };
            panel.Children.Add(button);
        }
        Content = panel;
    }
}

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TinyHand.Windows;

internal sealed record CommandResult(bool Ok, string? Error = null, string? Warning = null);

internal sealed class LocalCommands
{
    private readonly PointerController controller;
    internal LocalCommands(PointerController controller) => this.controller = controller;

    internal async Task<CommandResult> ExecuteAsync(string command, JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object) return new(false, "invalidArgument");
        bool Shape(params string[] fields) => arguments.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(fields.Order());
        bool Boolean(string name, out bool value)
        {
            value = false;
            if (!Shape(name) || !arguments.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
            value = item.GetBoolean(); return true;
        }
        if (controller.State.CapturePending) return new(false, "busy");
        controller.ResetPersistenceWarning();
        switch (command)
        {
            case "pointer.toggle" when Shape(): controller.SetActive(!controller.Active); break;
            case "pointer.setActive" when Boolean("active", out bool active): controller.SetActive(active); break;
            case "pointer.setSize" when Shape("height") && arguments.GetProperty("height").ValueKind == JsonValueKind.Number
                && arguments.GetProperty("height").TryGetInt32(out int height) && height is 220 or 340 or 480:
                controller.SetPointerHeight(height); break;
            case "pointer.cycleSize" when Shape():
                controller.SetPointerHeight(new[] { 220, 340, 480 }.FirstOrDefault(h => h > controller.PointerHeight, 220)); break;
            case "sound.toggle" when Shape(): controller.SetSoundEnabled(!controller.State.SoundEnabled); break;
            case "sound.setEnabled" when Boolean("enabled", out bool enabled): controller.SetSoundEnabled(enabled); break;
            case "annotation.setColor" when Shape("color") && arguments.GetProperty("color").ValueKind == JsonValueKind.String:
                if (!controller.SetAnnotationColor(arguments.GetProperty("color").GetString()!)) return new(false, "invalidArgument");
                break;
            case "annotation.beginText" when Shape():
                if (!controller.BeginTextPlacement()) return new(false, "busy");
                break;
            case "annotation.clear" when Shape(): controller.Clear(); break;
            case "capture.monitor" when Shape():
                var result = await controller.CaptureMonitorAsync();
                return new(result.Ok, result.Error);
            default:
                return new(false, command is "pointer.toggle" or "pointer.setActive" or "pointer.setSize" or "pointer.cycleSize"
                    or "sound.toggle" or "sound.setEnabled" or "annotation.setColor" or "annotation.beginText" or "annotation.clear" or "capture.monitor"
                    ? "invalidArgument" : "unsupportedCommand");
        }
        return new(true, Warning: controller.PersistenceWarning ? "persistenceFailed" : null);
    }
}

namespace TinyHand.Windows;

internal sealed record ControllerState(bool Active, int PointerHeight, bool SoundEnabled,
    string AnnotationColor, string TextPhase, int AnnotationCount, bool CapturePending);

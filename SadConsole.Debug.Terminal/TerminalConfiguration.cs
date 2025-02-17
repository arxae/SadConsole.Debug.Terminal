using System.Numerics;

namespace SadConsole.Debug;

public class TerminalConfiguration
{
    public bool ScrollToBottom = false;
    public bool AutoScroll = true;
    public bool ShowFilterBar = true;
    public bool DisplayBackingFields = false;
    public string TimestampFormat = @"hh\:mm\:ss\:fff";
    public float MessageSpacing = 0f;
    public string TerminalAttachmentPoint = ID_RIGHT_PANEL;

    public const string ID_LEFT_PANEL = "Scene##LeftPanel";
    public const string ID_RIGHT_PANEL = "Previews##RightPanel";
    public const string ID_CENTER_PANEL = "Extras##CenterPanel";

    public Dictionary<string, Vector4> ColorPalette { get; set; } = new()
    {
        { LogLevel.Verbose.ToString(), new(0.5f, 0.5f, 0.5f, 1f) },
        { LogLevel.Debug.ToString(), new(0.035f, 0.4f, 1f, 1f) },
        { LogLevel.Information.ToString(), new(1f, 1f, 1f, 1f) },
        { LogLevel.Warning.ToString(), new(1.0f, 0.87f, 0.37f, 1f) },
        { LogLevel.Error.ToString(), new(1f, 0.365f, 0.365f, 1f) },
        { LogLevel.Fatal.ToString(), new(1f, 0, 0f, 1f) },
        {"inspectorBackingField", new(0.5f, 0.5f, 0.5f, 1f)},
        {"inspectorCollection", new(0.035f, 0.4f, 1f, 1f)},
    };

    public Dictionary<LogLevel, bool> EnabledInspectors { get; set; } = new()
    {
        { LogLevel.Verbose, false },
        { LogLevel.Debug, false },
        { LogLevel.Information, true },
        { LogLevel.Warning, false },
        { LogLevel.Error, true },
        { LogLevel.Fatal, true },
    };
}
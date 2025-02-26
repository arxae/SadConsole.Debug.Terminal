using System.Numerics;
using System.Text.Json;

namespace SadConsole.Debug;

public class TerminalConfiguration
{
    public bool ScrollToBottom = false;
    public bool AutoScroll = true;
    public bool ShowFilterBar = true;
    public bool DisplayBackingFields = false;
    public bool DumpFiltered = false;
    public bool ShowPrivateFields = false;
    public bool ShowStaticFields = false;
    public string TimestampFormat = @"hh\:mm\:ss\:fff";
    public string TerminalAttachmentPoint = ID_RIGHT_PANEL;
    public float MessageSpacing = 0f;

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
        {"inspectorPrivateField", new(0.990f, 0.422f, 0.0693f, 1f)},
        {"inspectorStaticField", new(0.0693f, 0.990f, 0.652f, 1f)}
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

    readonly JsonSerializerOptions SerializerOptions = new() 
    { 
        WriteIndented = true,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        Converters = { new DebugTerminal.Vector4JsonConverter() }
    };

    public void SaveConfiguration()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sadterminal.json");
        if (File.Exists(path)) File.Delete(path);
        File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
    }

    public void LoadConfiguration()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sadterminal.json");
        if (File.Exists(path))
        {
            string contents = File.ReadAllText(path);
            if(string.IsNullOrWhiteSpace(contents)) return;

            TerminalConfiguration config;

            try
            {
                config = JsonSerializer.Deserialize<TerminalConfiguration>(contents);
            }
            catch
            {
                // If it fails, just ignore and keep defaults
                return;
            }

            if (config != null)
            {
                ScrollToBottom = config.ScrollToBottom;
                AutoScroll = config.AutoScroll;
                ShowFilterBar = config.ShowFilterBar;
                DisplayBackingFields = config.DisplayBackingFields;
                TimestampFormat = config.TimestampFormat;
                MessageSpacing = config.MessageSpacing;
                TerminalAttachmentPoint = config.TerminalAttachmentPoint;
                DumpFiltered = config.DumpFiltered;
                ShowPrivateFields = config.ShowPrivateFields;
                ShowStaticFields = config.ShowStaticFields;
                ColorPalette = config.ColorPalette;
                EnabledInspectors = config.EnabledInspectors;
            }
        }
    }
}
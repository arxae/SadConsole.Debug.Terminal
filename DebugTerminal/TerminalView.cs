using SadConsole.ImGuiSystem;
using Hexa.NET.ImGui;

namespace SadConsole.Debug.DebugTerminal;

partial class TerminalView : ImGuiObjectBase
{
    bool wasPrevFrameTabCompletion = false;
    readonly List<TerminalMessage> messageCache = [];
    readonly List<string> cmdSuggestions = [];

    ImGuiTextFilter textFilter = new();

    public override void BuildUI(ImGuiRenderer renderer)
    {
        ImGui.Begin(Terminal.Configuration.TerminalAttachmentPoint, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar);

        MenuBar();
        LogWindow();
        ImGui.Separator();
        InputBar();

        ImGui.End();
    }
}

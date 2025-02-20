using SadConsole.ImGuiSystem;
using Hexa.NET.ImGui;

namespace SadConsole.Debug.DebugTerminal;

partial class TerminalView : ImGuiObjectBase
{
    void InputBar()
    {
        bool reclaimFocus = false;

        string buffer = string.Empty;

        ImGui.PushItemWidth(-ImGui.GetStyle().ItemSpacing.X * 7);

        if (ImGui.InputText("Input", ref buffer, 2048, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            ValidateInput(buffer.Trim());
            Terminal.Configuration.ScrollToBottom = true;

            // Keep focus
            reclaimFocus = true;
        }
        ImGui.PopItemWidth();

        if (ImGui.IsItemEdited() && wasPrevFrameTabCompletion == false)
        {
            cmdSuggestions.Clear();
        }
        wasPrevFrameTabCompletion = false;

        ImGui.SetItemDefaultFocus();
        if (reclaimFocus)
        {
            ImGui.SetKeyboardFocusHere(-1);
        }
    }

    void ValidateInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return;

        string[] parts = input.Split(' ');
        string command = parts[0].ToLower();
        string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        TerminalCommand? cmd = Terminal.Commands.Find(c => c.Name == command) ?? null;

        if (cmd == null)
        {
            Terminal.Error($"Command not found: {command}");
            return;
        }

        if (cmd.GetType() == typeof(TerminalCommand))
        {
            cmd.Action();
        }
        else
        {
            ((TerminalCommandArgs)cmd).Action(args);
        }
    }
}
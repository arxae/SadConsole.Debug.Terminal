using SadConsole.ImGuiSystem;
using Hexa.NET.ImGui;
using System.Numerics;

namespace SadConsole.Debug.DebugTerminal;

partial class TerminalView : ImGuiObjectBase
{
    void MenuBar()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Settings"))
            {
                ImGui.Checkbox("Auto Scroll", ref Terminal.Configuration.AutoScroll);
                CreateMenuHelp("Automatically scroll to the bottom of the log window");

                ImGui.Checkbox("Filter Bar", ref Terminal.Configuration.ShowFilterBar);
                CreateMenuHelp("Show the filter bar");

                if (ImGui.BeginMenu("Colors"))
                {
                    foreach (LogLevel foo in Enum.GetValues(typeof(LogLevel)))
                    {
                        Vector4 color = Terminal.Configuration.ColorPalette[foo.ToString()];
                        ImGui.ColorEdit4(foo.ToString(), ref color);
                        Terminal.Configuration.ColorPalette[foo.ToString()] = color;
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Inspectors"))
                {
                    foreach (KeyValuePair<LogLevel, bool> inspector in Terminal.Configuration.EnabledInspectors)
                    {
                        bool isEnabled = Terminal.Configuration.EnabledInspectors[inspector.Key];
                        if (ImGui.Checkbox(inspector.Key.ToString(), ref isEnabled))
                        {
                            Terminal.Configuration.EnabledInspectors[inspector.Key] = isEnabled;
                        }
                    }

                    ImGui.Separator();

                    ImGui.Checkbox("Display Private Fields", ref Terminal.Configuration.ShowPrivateFields);
                    CreateMenuHelp("Display private fields in the context object tree");

                    ImGui.Checkbox("Display Backing Fields", ref Terminal.Configuration.DisplayBackingFields);
                    CreateMenuHelp("Display backing fields in the context object tree");

                    ImGui.Checkbox("Display Static Fields", ref Terminal.Configuration.ShowStaticFields);
                    CreateMenuHelp("Display static fields in the context object tree");

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Log"))
            {
                if (ImGui.MenuItem("Dump to file"))
                {
                    string path = $"terminal_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                    Terminal.DumpLogToFile(path, onlyFiltered: Terminal.Configuration.DumpFiltered);
                }
                CreateMenuHelp("Dump the current log to a file");

                if (ImGui.MenuItem("Dump to file with context (if available)"))
                {
                    string path = $"terminal_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                    Terminal.DumpLogToFile(path, messagesOnly: false, onlyFiltered: Terminal.Configuration.DumpFiltered);
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Clear")) { Terminal.Messages.Clear(); }
                CreateMenuHelp("Clear the log window");

                ImGui.Separator();

                ImGui.Checkbox("Dump Filtered", ref Terminal.Configuration.DumpFiltered);
                CreateMenuHelp("Dump only the filtered messages to a file");

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Commands"))
            {
                foreach (TerminalCommand cmd in Terminal.Commands.Where(c => c.GetType() == typeof(TerminalCommand)))
                {
                    if (ImGui.MenuItem(cmd.Name))
                    {
                        cmd.Action();
                    }
                    CreateMenuHelp(cmd.Description);
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }
    }
    
    void CreateMenuHelp(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
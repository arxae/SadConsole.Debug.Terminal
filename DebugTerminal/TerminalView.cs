using SadConsole.ImGuiSystem;
using Hexa.NET.ImGui;
using System.Numerics;
using Newtonsoft.Json;
using System.Reflection;

namespace SadConsole.Debug.DebugTerminal;

class TerminalView : ImGuiObjectBase
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

            ImGui.EndMenuBar();
        }
    }

    void LogWindow()
    {
        ImGui.SetNextWindowBgAlpha(1f);
        if (Terminal.Configuration.ShowFilterBar)
        {
            textFilter.Draw("Filter", ImGui.GetWindowWidth() * .25f);
            ImGui.Separator();
        }

        float footerHeightToReserver = ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing();
        if (ImGui.BeginChild("ScrollingTextRegion##", new Vector2(0f, -footerHeightToReserver), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            float timestampWidth = ImGui.CalcTextSize("00:00:00.000").X;

            ImGui.PushTextWrapPos();

            messageCache.Clear();
            foreach (TerminalMessage item in Terminal.Messages)
            {
                if (textFilter.PassFilter(item.Message + item.Timestamp.ToString() + item.Level.ToString()) == false) continue;
                messageCache.Add(item);
            }

            int idCounter = 0;
            foreach (TerminalMessage item in messageCache)
            {
                ImGui.PushTextWrapPos(ImGui.GetColumnWidth() - timestampWidth);

                // If the inspector for specified level is disabled, or the flag for it is set. Display the message as text only
                if (Terminal.Configuration.EnabledInspectors[item.Level] == false || item.DisplayFlat)
                {
                    ImGui.TextColored(Terminal.Configuration.ColorPalette[item.Level.ToString()], item.Message);
                    ImGui.PopTextWrapPos();
                    ImGui.SameLine(ImGui.GetColumnWidth(-1) - timestampWidth);
                    ImGui.TextColored(Terminal.Configuration.ColorPalette[item.Level.ToString()], item.Timestamp.ToString(Terminal.Configuration.TimestampFormat));
                    continue;
                }

                ImGui.PushStyleColor(ImGuiCol.Text, Terminal.Configuration.ColorPalette[item.Level.ToString()]);
                if (ImGui.CollapsingHeader($"{item.Message}##{idCounter}"))
                {
                    ImGui.PopStyleColor();
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f));

                    // Copy message to clipboard
                    if (ImGui.Button($"Copy message##{idCounter}")) { ImGui.SetClipboardText(item.Message); }

                    // Copy the context object to clipboard
                    if (item.Context != null)
                    {
                        ImGui.SameLine();
                        if (ImGui.Button($"Copy object JSON##{idCounter}"))
                        {
                            string json = JsonConvert.SerializeObject(item.Context, Formatting.Indented);
                            ImGui.SetClipboardText(json);
                        }
                    }

                    // Dump everything to a file
                    ImGui.SameLine();
                    if (ImGui.Button($"Dump to file##{idCounter}"))
                    {
                        string json = string.Empty;
                        if (item.Context != null)
                        {
                            json = JsonConvert.SerializeObject(item.Context, Formatting.Indented, 
                                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        }

                        using StreamWriter file = new($"logmessage_{idCounter}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json");
                        file.WriteLine("Message: " + item.Message);
                        file.WriteLine("Timestamp: " + item.Timestamp);
                        file.WriteLine("Level: " + item.Level);
                        file.WriteLine();
                        file.Write(json);
                    }

                    if (ImGui.BeginTabBar($"detailsTabBar##{idCounter}"))
                    {
                        if (ImGui.BeginTabItem($"Message##{idCounter}"))
                        {
                            ImGui.Text($"Timestamp: {item.Timestamp}");
                            ImGui.Text("Level: ");
                            ImGui.SameLine();
                            ImGui.TextColored(Terminal.Configuration.ColorPalette[item.Level.ToString()], item.Level.ToString());
                            ImGui.EndTabItem();
                        }

                        if (item.Context != null)
                        {
                            if (ImGui.BeginTabItem($"Context##{idCounter}"))
                            {
                                string objName = item.Context.ToString() ?? "";
                                if (ImGui.TreeNode($"{objName} Properties##{idCounter}"))
                                {
                                    DisplayObjectTree(item.Context);
                                    ImGui.TreePop();
                                }
                                ImGui.EndTabItem();
                            }
                        }

                        if (item.Exception != null)
                        {
                            if (ImGui.BeginTabItem($"Exception##{idCounter}"))
                            {
                                DisplayException(item.Exception);
                                ImGui.EndTabItem();
                            }
                        }

                        ImGui.EndTabBar();
                    }
                    ImGui.Spacing();
                }
                ImGui.PopStyleColor();

                // Timestamps
                ImGui.PopTextWrapPos();
                ImGui.SameLine(ImGui.GetColumnWidth(-1) - timestampWidth);
                ImGui.TextColored(Terminal.Configuration.ColorPalette[item.Level.ToString()], item.Timestamp.ToString(Terminal.Configuration.TimestampFormat));

                idCounter++;
            }

            ImGui.PopTextWrapPos();

            if (Terminal.Configuration.ScrollToBottom && (ImGui.GetScrollY() >= ImGui.GetScrollMaxY() || Terminal.Configuration.AutoScroll))
            {
                ImGui.SetScrollHereY(1f);
            }
            Terminal.Configuration.ScrollToBottom = false;

            ImGui.EndChild();
        }
    }

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

    void DisplayObjectTree(object? obj)
    {
        if (obj == null) return;

        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
            object value;
            if (property.GetIndexParameters().Length == 0)
            {
                value = property.GetValue(obj, null) ?? "null";
            }
            else
            {
                value = "Indexed Property";
            }
            string propertyName = property.Name;
            string propertyType = GetFriendlyTypeName(property.PropertyType);

            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                if (value == null)
                {
                    ImGui.TextColored(Terminal.Configuration.ColorPalette["inspectorCollection"], $"{propertyName} ({propertyType}) (Null)");
                }
                else
                {
                    bool isEmpty = !enumerable.GetEnumerator().MoveNext();
                    string suffix = isEmpty ? "(Empty)" : string.Empty;
                    ImGui.PushStyleColor(ImGuiCol.Text, Terminal.Configuration.ColorPalette["inspectorCollection"]);
                    if (ImGui.TreeNode($"{propertyName} ({propertyType}) {suffix}##{propertyName}"))
                    {
                        ImGui.PopStyleColor();
                        if (!isEmpty)
                        {
                            int index = 0;
                            foreach (object element in enumerable)
                            {
                                if (element is string)
                                {
                                    ImGui.Text($"[{index}]: {element}");
                                }
                                else
                                {
                                    string elementLabel = element?.ToString() ?? (element != null ? element.GetType().Name : "null");
                                    if (ImGui.TreeNode($"{propertyName}[{index}] ({elementLabel})##{propertyName}[{index}]"))
                                    {
                                        DisplayObjectTree(element);
                                        ImGui.TreePop();
                                    }
                                }
                                index++;
                            }
                        }
                        ImGui.TreePop();
                    }
                    else
                    {
                        ImGui.PopStyleColor();
                    }
                }
            }
            else if (value.GetType().IsPrimitive || value.GetType() is string)
            {
                ImGui.Text($"{propertyName} ({propertyType}): {value}");
            }
            else if (ImGui.TreeNode($"{propertyName} ({propertyType})"))
            {
                DisplayObjectTree(value);
                ImGui.TreePop();
            }
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        if (Terminal.Configuration.ShowPrivateFields) flags |= BindingFlags.NonPublic;
        if (Terminal.Configuration.ShowStaticFields) flags |= BindingFlags.Static;

        foreach (FieldInfo field in obj.GetType().GetFields(flags))
        {
            if (field.Name.Contains("k__BackingField") && Terminal.Configuration.DisplayBackingFields == false) continue; // Exclude automatic backing fields

            object value = field.GetValue(obj) ?? "null";
            string fieldName = field.Name;
            string fieldType = GetFriendlyTypeName(field.FieldType);

            ImGui.TextColored(Terminal.Configuration.ColorPalette["inspectorBackingField"], $"{fieldName} ({fieldType}): {value}");
        }
    }

    string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            int backtickIndex = type.Name.IndexOf('`');
            string typeName = backtickIndex >= 0 ? type.Name.Substring(0, backtickIndex) : type.Name;
            string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
            return $"{typeName}<{genericArgs}>";
        }
        return type.Name;
    }

    void DisplayException(Exception exception)
    {
        ImGui.TextWrapped($"Exception: {exception.Message}");
        ImGui.TextWrapped($"Source: {exception.Source}");
        ImGui.TextWrapped($"Stack Trace: {exception.StackTrace}");

        if (exception.InnerException != null)
        {
            if (ImGui.TreeNode("Inner Exception"))
            {
                DisplayException(exception.InnerException);
                ImGui.TreePop();
            }
        }
    }
}

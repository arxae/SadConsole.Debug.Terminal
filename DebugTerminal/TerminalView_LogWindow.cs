using SadConsole.ImGuiSystem;
using Hexa.NET.ImGui;
using System.Numerics;
using Newtonsoft.Json;
using System.Reflection;

namespace SadConsole.Debug.DebugTerminal;

partial class TerminalView : ImGuiObjectBase
{
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
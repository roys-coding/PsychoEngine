using Hexa.NET.ImGui;
using Vector2 = System.Numerics.Vector2;

namespace PsychoEngine;

public static partial class PyConsole
{
    private static string _command = "";
    
    private static bool _displayCategories;
    private static bool _autoScroll = true;
    private static bool _colored = true;
    private static int  _previousMessageCount;
    
    private static void InitializeImGui()
    {
        PyGame.Instance.ImGuiManager.OnLayout += ImGuiLayout;
    }

    private static void ImGuiLayout(object? sender, EventArgs e)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.SquareChevronRight} Console");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }

        const ImGuiChildFlags childFlags = ImGuiChildFlags.FrameStyle;
        const ImGuiTableFlags tableFlags = ImGuiTableFlags.BordersInner;

        float regionHeight = ImGui.GetContentRegionAvail().Y;
        float frameHeight  = ImGui.GetFrameHeight();
        float itemSpacingY = ImGui.GetStyle().ItemSpacing.Y;
        
        ImGui.SetNextWindowSize(new Vector2(0, regionHeight - frameHeight - itemSpacingY));
        
        if (ImGui.BeginChild("##log", childFlags))
        {
            if (ImGui.BeginPopupContextWindow())
            {
                ImGui.MenuItem("Auto-scroll", "", ref _autoScroll);
                
                if (ImGui.BeginMenu("Show"))
                {
                    ImGui.MenuItem("Category", "", ref _displayCategories);
                    ImGui.MenuItem("Severity colors", "", ref _colored);
                
                    ImGui.EndMenu();
                }
                
                ImGui.Separator();

                bool clearPressed = ImGui.Selectable("Clear");

                if (clearPressed)
                {
                    Clear();
                }
            
                ImGui.EndPopup();
            }
            
            if (ImGui.BeginTable("##messages", 2, tableFlags))
            {
                ImGui.TableSetupColumn("Category",
                                       _displayCategories
                                           ? ImGuiTableColumnFlags.WidthFixed
                                           : ImGuiTableColumnFlags.Disabled);

                ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);

                foreach (LogMessage msg in LoggedMessages)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(msg.Category);

                    ImGui.TableNextColumn();

                    if (_colored)
                    {
                        // Color text according to it's severity.
                        switch (msg.Severity)
                        {
                            case LogSeverity.Debug:
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
                                break;

                            case LogSeverity.Info:
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text));
                                break;

                            case LogSeverity.Success:
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF20FF20);
                                break;

                            case LogSeverity.Warning:
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00DFFF);
                                break;

                            case LogSeverity.Error:
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF2020FF);
                                break;

                            case LogSeverity.Fatal:
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xAA0000FF);
                                break;

                            default: throw new NotSupportedException($"Severity '{msg.Severity}' not supported.");
                        }
                    }

                    string[] msgLines = msg.Message.Split("\\n");

                    foreach (string msgLine in msgLines)
                    {
                        ImGui.Text(msgLine);
                    }

                    if (_colored)
                    {
                        ImGui.PopStyleColor();
                    }
                }

                // Auto scroll when a new message appears.
                if (_autoScroll && LoggedMessages.Count != _previousMessageCount)
                {
                    ImGui.SetScrollHereY();
                }
            }

            ImGui.EndTable();
        }

        ImGui.EndChild();

        // Command input text.
        float spaceWidth = ImGui.GetContentRegionAvail().X;
        
        ImGui.SetNextItemWidth(spaceWidth);
        ImGui.InputTextWithHint("##command", "Command", ref _command, 256);
        
        ImGui.End();
                
        _previousMessageCount = LoggedMessages.Count;
    }
}
using Hexa.NET.ImGui;

namespace PsychoEngine;

public static partial class PyConsole
{
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
        }

        const ImGuiChildFlags ChildFlags = ImGuiChildFlags.FrameStyle | ImGuiChildFlags.ResizeY;

        if (ImGui.BeginChild("##log", ChildFlags))
        {
            foreach (LogMessage msg in LoggedMessages)
            {
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
                    default:
                        throw new NotSupportedException($"Severity '{msg.Severity}' not supported.");
                }
                
                ImGui.Text(msg);
                ImGui.PopStyleColor();
            }
        }
        
        ImGui.EndChild();

        ImGui.End();
    }
}
using Hexa.NET.ImGui;

namespace PsychoEngine.Graphics;

public static partial class PyWindow
{
    private static readonly int[] ScreenSize =
    [
        0, 0,
    ];

    private static readonly string[] WindowModeNames = Enum.GetNames<WindowMode>();

    private static          bool         _editingScreenSize;
    private static readonly List<string> Logs = new();

    private static bool _logSizeChangeEvent;

    private static void ImGuiLog(string message)
    {
        Logs.Add(message);
    }

    private static void ImGuiOnLayout(object? sender, EventArgs e)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.AppWindowMac} Window");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }

        string windowTitle  = Title;
        bool   titleChanged = ImGui.InputText("Title", ref windowTitle, 255);
        if (titleChanged)
        {
            Title = windowTitle;
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Size & mode"))
        {
            int  windowMode        = (int)Mode;
            bool windowModeChanged = ImGui.Combo("Mode", ref windowMode, WindowModeNames, WindowModeNames.Length);
            if (windowModeChanged)
            {
                SetMode((WindowMode)windowMode);
            }

            ImGui.Spacing();
            ImGui.SeparatorText("Size");
            ImGui.Spacing();
            
            if (!_editingScreenSize)
            {
                ScreenSize[0] = Width;
                ScreenSize[1] = Height;
            }

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.55f);
            bool screenSizeChanged = ImGui.DragInt2("##size", ref ScreenSize[0]);

            if (screenSizeChanged)
            {
                _editingScreenSize = true;
            }

            if (!_editingScreenSize)
            {
                ImGui.BeginDisabled();
            }

            ImGui.SameLine();
            bool applySizePressed = ImGui.Button("Apply");
            ImGui.SameLine();
            bool cancelSizePressed = ImGui.Button("X");

            if (!_editingScreenSize)
            {
                ImGui.EndDisabled();
            }

            if (applySizePressed)
            {
                SetSize(ScreenSize[0], ScreenSize[1]);
                _editingScreenSize = false;
            }

            if (cancelSizePressed)
            {
                _editingScreenSize = false;
            }
        }

        if (ImGui.CollapsingHeader("Settings"))
        {
            bool mouseVisible                       = IsMouseVisible;
            bool mouseVisibleChanged                = ImGui.Checkbox("Is Mouse Visible", ref mouseVisible);
            if (mouseVisibleChanged)
            {
                IsMouseVisible = mouseVisible;
            }

            bool isResizable                    = IsResizable;
            bool isResizableChanged             = ImGui.Checkbox("Is Resizable", ref isResizable);
            if (isResizableChanged)
            {
                IsResizable = isResizable;
            }
        }

        if (ImGui.CollapsingHeader("Events log"))
        {
            ImGui.TreePush("Events");

            if (ImGui.CollapsingHeader("Events"))
            {
                ImGui.Checkbox("OnSizeChanged", ref _logSizeChangeEvent);
            }

            ImGui.TreePop();

            bool clearLogs = ImGui.Button("Clear");
            if (clearLogs)
            {
                Logs.Clear();
            }

            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar;

            if (ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle, windowFlags))
            {
                foreach (string message in Logs)
                {
                    if (message == "separator")
                    {
                        ImGui.Separator();
                    }
                    else
                    {
                        ImGui.Text(message);
                    }
                }

                ImGui.SetScrollHereY();
            }

            ImGui.EndChild();
        }

        ImGui.End();
    }

    private static void OnOnSizeChanged(object? sender, EventArgs e)
    {
        ImGuiLog("-OnSizeChanged");
    }
}
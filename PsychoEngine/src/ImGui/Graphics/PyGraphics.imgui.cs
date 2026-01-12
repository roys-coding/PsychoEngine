using Hexa.NET.ImGui;

namespace PsychoEngine.Graphics;

public static partial class PyGraphics
{
    private static readonly string[] ExpandNames  = Enum.GetNames<CanvasResizingPolicy>();
    private static readonly string[] ScalingNames = Enum.GetNames<CanvasScalingPolicy>();

    private static bool _showSupportedResolutions;
    
    private static readonly int[] Resolution = new int[2];
    private static          bool  _editingResolution;

    private static void ImGuiOnLayout(object? sender, EventArgs e)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.Gpu} Graphics");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }
        
        ImGui.Checkbox("Show Supported Resolutions", ref _showSupportedResolutions);

        if (ImGui.CollapsingHeader("Canvas"))
        {
            ImGui.SeparatorText("Policies");
            
            int  expandMode    = (int)CanvasResizingPolicy;
            bool expandChanged = ImGui.Combo("Resizing", ref expandMode, ExpandNames, ExpandNames.Length);
            if (expandChanged)
            {
                SetExpandMode((CanvasResizingPolicy)expandMode);
            }

            int  scalingMode    = (int)CanvasScalingPolicy;
            bool scalingChanged = ImGui.Combo("Scaling", ref scalingMode, ScalingNames, ScalingNames.Length);
            if (scalingChanged)
            {
                SetScalingMode((CanvasScalingPolicy)scalingMode);
            }

            // ImGui.Spacing();
            // ImGui.SeparatorText("Resolution");
            // ImGui.Spacing();
            //
            if (!_editingResolution)
            {
                Resolution[0] = ActiveResolution.Width;
                Resolution[1] = ActiveResolution.Height;
            }
            //
            // ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.55f);
            bool resolutionChanged                    = ImGui.DragInt2("##resolution", ref Resolution[0]);
            // if (resolutionChanged)
            // {
            //     _editingResolution = true;
            // }
            //
            // if (!_editingResolution)
            // {
            //     ImGui.BeginDisabled();
            // }

            // ImGui.SameLine();
            // bool applyResPressed = ImGui.Button("Apply");
            // ImGui.SameLine();
            // bool cancelResPressed = ImGui.Button("X");
            //
            // if (!_editingResolution)
            // {
            //     ImGui.EndDisabled();
            // }
            //
            // if (applyResPressed)
            // {
            //     ActiveResolution   = new GraphicsResolution(Resolution[0], Resolution[1]);
            //     _editingResolution = false;
            // }

            // if (cancelResPressed)
            // {
            //     _editingResolution = false;
            // }
        }

        if (ImGui.CollapsingHeader("Settings"))
        {
            bool vsync        = VerticalSync;
            bool vsyncChanged = ImGui.Checkbox("Vertical Sync", ref vsync);
            if (vsyncChanged)
            {
                SetVerticalSync(vsync);
            }

            bool fixedStep        = FixedTimeStep;
            bool fixedStepChanged = ImGui.Checkbox("FixedTimeStep", ref fixedStep);
            if (fixedStepChanged)
            {
                SetFixedTimeStep(fixedStep);
            }
        }

        ImGui.End();
    }
}
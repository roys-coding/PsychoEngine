using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;

namespace PsychoEngine;

public partial class PyGame
{
    private void InitializeImGui()
    {
        ImGuiManager.OnLayout += ImGuiOnLayout;
    }

    private void ImGuiOnLayout(object? sender, EventArgs eventArgs)
    {
        const ImGuiDockNodeFlags dockFlags =
            ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode;

        ImGui.DockSpaceOverViewport(dockFlags);

        ImGui.ShowDemoWindow();
        ImPlot.ShowDemoWindow();

        if (ImGui.IsKeyDown(ImGuiKey.F) && ImGui.IsKeyDown(ImGuiKey.ModShift))
        {
            // 60 fps.
            TargetElapsedTime = TimeSpan.FromTicks(166667);
        }

        if (ImGui.IsKeyDown(ImGuiKey.Escape))
        {
            Exit();
        }

        bool gameWindow = ImGui.Begin("Game");

        if (!gameWindow)
        {
            ImGui.End();
            return;
        }

        bool exitPressed = ImGui.Button("Exit");
        ImGui.SameLine();
        ImGui.TextDisabled("(ESC)");

        if (exitPressed)
        {
            Exit();
        }

        bool timesHeader = ImGui.CollapsingHeader("Times");

        if (timesHeader)
        {
            float targetElapsedMs = (float)TargetElapsedTime.TotalMilliseconds;
            bool  elapsedChanged  = ImGui.SliderFloat("TargetMs", ref targetElapsedMs, 1f, 500.0f);

            if (elapsedChanged)
            {
                TargetElapsedTime = TimeSpan.FromMilliseconds(targetElapsedMs);
            }

            float fps        = 1000f / targetElapsedMs;
            bool  fpsChanged = ImGui.SliderFloat("TargetFPS", ref fps, 2f, 1000f);

            if (fpsChanged)
            {
                TargetElapsedTime = TimeSpan.FromMilliseconds(1000f / fps);
            }

            float sleepMs        = (float)InactiveSleepTime.TotalMilliseconds;
            bool  sleepMsChanged = ImGui.SliderFloat("InactiveMs", ref sleepMs, 0f, 1000f);

            if (sleepMsChanged)
            {
                InactiveSleepTime = TimeSpan.FromMilliseconds(sleepMs);
            }

            ImGui.TextDisabled("Press Shift+F to reset to 60 FPS!");

            bool isFixed        = IsFixedTimeStep;
            bool isFixedChanged = ImGui.Checkbox("FixedTime", ref isFixed);

            if (isFixedChanged)
            {
                IsFixedTimeStep = isFixed;
            }
        }

        ImGui.End();
    }
}
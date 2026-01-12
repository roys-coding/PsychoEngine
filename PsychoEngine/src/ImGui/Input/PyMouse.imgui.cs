using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using PsychoEngine.Utilities;
using NVector2 = System.Numerics.Vector2;

namespace PsychoEngine.Input;

public static partial class PyMouse
{
    private const  int  LogCapacity = 100;
    private static bool _logMovedEvent;
    private static bool _logScrollEvent = true;
    private static bool _logDownEvent;
    private static bool _logPressEvent     = true;
    private static bool _logReleaseEvent   = true;
    private static bool _logDragStartEvent = true;
    private static bool _logDragEvent;
    private static bool _logDragEndEvent    = true;
    private static bool _logMultiClickEvent = true;

    private static readonly string[] FocusLostNames = Enum.GetNames<FocusLostInputBehaviour>();

    private static readonly List<string> EventLog = new(LogCapacity);

    private static void InitializeImGui()
    {
        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        OnMoved += (_, args) =>
        {
            if (!_logMovedEvent)
            {
                return;
            }

            ImGuiLog("OnMoved");
            ImGuiLog($"     -PrevPos: {args.PreviousPosition}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog($"     -PosDelta: {args.PositionDelta}");
            ImGuiLog("separator");
        };

        OnScrolled += (_, args) =>
        {
            if (!_logScrollEvent)
            {
                return;
            }

            ImGuiLog("OnScrolled");
            ImGuiLog($"     -ScrollValue: {args.ScrollValue}");
            ImGuiLog($"     -ScrollDelta: {args.ScrollDelta}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };

        OnButtonDown += (_, args) =>
        {
            if (!_logDownEvent)
            {
                return;
            }

            ImGuiLog("OnButtonDown");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };

        OnButtonPressed += (_, args) =>
        {
            if (!_logPressEvent)
            {
                return;
            }

            ImGuiLog("OnButtonPressed");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };

        OnButtonReleased += (_, args) =>
        {
            if (!_logReleaseEvent)
            {
                return;
            }

            ImGuiLog("OnButtonReleased");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };

        OnDragStarted += (_, args) =>
        {
            if (!_logDragStartEvent)
            {
                return;
            }

            ImGuiLog("OnDragStarted");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog($"     -StartPos: {args.DragStartPosition}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };

        OnDragging += (_, args) =>
        {
            if (!_logDragEvent)
            {
                return;
            }

            ImGuiLog("OnDragging");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog($"     -StartPos: {args.DragStartPosition}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };

        OnDragReleased += (_, args) =>
        {
            if (!_logDragEndEvent)
            {
                return;
            }

            ImGuiLog("OnDragReleased");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog($"     -StartPos: {args.DragStartPosition}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };

        OnMultiClick += (_, args) =>
        {
            if (!_logMultiClickEvent)
            {
                return;
            }

            ImGuiLog("OnMultiClick");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog($"     -Clicks: {args.ClickCount}");
            ImGuiLog($"     -Position: {args.Position}");
            ImGuiLog("separator");
        };
    }

    private static void ImGuiLog(string message)
    {
        EventLog.Add(message);

        if (EventLog.Count >= LogCapacity)
        {
            EventLog.RemoveAt(0);
        }
    }

    private static void ImGuiOnLayout(object? sender, EventArgs args)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.Mouse} Mouse");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        if (ImGui.CollapsingHeader("Config"))
        {
            int focusLost = (int)_focusLostInputBehaviour;

            bool focusLostChanged =
                ImGui.Combo("FocusLost Behaviour", ref focusLost, FocusLostNames, FocusLostNames.Length);

            if (focusLostChanged)
            {
                _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
            }
        }

        if (ImGui.CollapsingHeader("Time stamps"))
        {
            ImGui.Text($"Last Movement: {LastMoveTime}");
            ImGui.Text($"Last Input: {LastInputTime}");
        }

        if (ImGui.CollapsingHeader("Movement"))
        {
            bool moved = Moved;

            ImGui.Text($"Position: {Position}");
            ImGui.Text($"Previous Position: {PreviousPosition}");
            ImGui.Text($"Position Delta: {PositionDelta}");
            ImGui.Checkbox("Moved", ref moved);

            ImGui.Spacing();
            bool resetViewPressed = ImGui.Button("Reset view");

            const ImPlotFlags plotFlags = ImPlotFlags.NoTitle | ImPlotFlags.NoMenus;

            const ImPlotAxisFlags axesFlags =
                ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks;

            NVector2 displaySize = ImGui.GetIO().DisplaySize;
            float    ratio       = displaySize.X / displaySize.Y;
            NVector2 plotSize    = new(400);
            plotSize.Y /= ratio;

            if (ImPlot.BeginPlot("Movement##plot", plotSize, plotFlags))
            {
                ImPlot.SetupAxes("x", "y", axesFlags, axesFlags);

                ImPlot.SetupAxesLimits(0,
                                       displaySize.X,
                                       0,
                                       -displaySize.Y,
                                       resetViewPressed ? ImPlotCond.Always : ImPlotCond.Once);

                ImDrawListPtr drawList    = ImPlot.GetPlotDrawList();
                uint          rectColor   = ImGui.GetColorU32(ImGuiCol.Separator);
                uint          circleColor = ImGui.GetColorU32(ImGuiCol.Text);
                NVector2      rectMin     = ImPlot.PlotToPixels(new ImPlotPoint(0f));
                NVector2      rectMax     = ImPlot.PlotToPixels(new ImPlotPoint(displaySize.X, -displaySize.Y));
                NVector2      mousePos    = ImPlot.PlotToPixels(new ImPlotPoint(Position.X, -Position.Y));
                Vector2       mouseDir    = PositionDelta.ToVector();
                mouseDir.Normalize();
                NVector2 mouseDirN        = mouseDir.ToNumerics();
                NVector2 directionLineEnd = mousePos + mouseDirN * 20;

                ImPlot.PushPlotClipRect();

                drawList.AddRect(rectMin, rectMax, rectColor);

                if (Moved)
                {
                    drawList.AddCircleFilled(mousePos, 4f, circleColor, 20);
                }
                else
                {
                    drawList.AddCircle(mousePos, 4f, circleColor, 20);
                }

                drawList.AddLine(mousePos, directionLineEnd, circleColor);

                ImPlot.PopPlotClipRect();

                ImPlot.EndPlot();
            }
        }

        if (ImGui.CollapsingHeader("Scroll"))
        {
            bool scrolled = Scrolled;

            ImGui.Text($"Scroll: {ScrollValue}");
            ImGui.Text($"Previous Scroll: {PreviousScrollValue}");
            ImGui.Text($"Scroll Delta: {ScrollDelta}");
            ImGui.Checkbox("Scrolled", ref scrolled);
        }

        if (ImGui.CollapsingHeader("Buttons"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;

            if (ImGui.BeginTable("Buttons", 4, flags))
            {
                ImGui.TableSetupColumn("Button");
                ImGui.TableSetupColumn("State");
                ImGui.TableSetupColumn("Pressed");
                ImGui.TableSetupColumn("Released");
                ImGui.TableHeadersRow();

                foreach (MouseButton button in AllButtons)
                {
                    bool pressed  = WasButtonPressed(button);
                    bool released = WasButtonReleased(button);

                    if (!IsButtonDown(button))
                    {
                        ImGui.BeginDisabled();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(IsButtonDown(button) ? "Down" : "Up")}");

                    if (!IsButtonDown(button))
                    {
                        ImGui.EndDisabled();
                    }

                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}pressed", ref pressed);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}released", ref released);
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Dragging"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;

            if (ImGui.BeginTable("Dragging", 5, flags))
            {
                ImGui.TableSetupColumn("Button");
                ImGui.TableSetupColumn("Start Pos");
                ImGui.TableSetupColumn("Dragging");
                ImGui.TableSetupColumn("Started");
                ImGui.TableSetupColumn("Released");
                ImGui.TableHeadersRow();

                foreach (MouseButton button in AllButtons)
                {
                    Point startPos    = GetDragStartPosition(button);
                    bool  dragging    = IsDragging(button);
                    bool  startedDrag = WasDragStarted(button);
                    bool  releasedDrag = WasDragReleased(button);

                    if (!dragging)
                    {
                        ImGui.BeginDisabled();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(startPos.ToString());

                    if (!dragging)
                    {
                        ImGui.EndDisabled();
                    }

                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}dragging", ref dragging);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}started_drag", ref startedDrag);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}released_drag", ref releasedDrag);
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Multi clicking"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;

            if (ImGui.BeginTable("Multi clicking", 4, flags))
            {
                ImGui.TableSetupColumn("Button");
                ImGui.TableSetupColumn("Last Press");
                ImGui.TableSetupColumn("Clicks");
                ImGui.TableSetupColumn("Multi clicked");
                ImGui.TableHeadersRow();

                foreach (MouseButton button in AllButtons)
                {
                    MouseButtonState state           = GetButtonStateInternal(button);
                    int              clicks          = GetConsecutiveClicks(button);
                    TimeSpan         lastReleaseTime = state.LastPressTime;
                    bool             multiClicked    = WasButtonMultiClicked(button);

                    if (!multiClicked)
                    {
                        ImGui.BeginDisabled();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(lastReleaseTime.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(clicks.ToString());

                    if (!multiClicked)
                    {
                        ImGui.EndDisabled();
                    }

                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}multi click", ref multiClicked);
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Events log"))
        {
            ImGui.TreePush("Events");

            if (ImGui.CollapsingHeader("Events"))
            {
                ImGui.Checkbox("MovedEvent", ref _logMovedEvent);
                ImGui.Checkbox("ScrollEvent", ref _logScrollEvent);
                ImGui.Checkbox("DownEvent", ref _logDownEvent);
                ImGui.Checkbox("PressEvent", ref _logPressEvent);
                ImGui.Checkbox("ReleaseEvent", ref _logReleaseEvent);
                ImGui.Checkbox("DragStartEvent", ref _logDragStartEvent);
                ImGui.Checkbox("DragEvent", ref _logDragEvent);
                ImGui.Checkbox("DragEndEvent", ref _logDragEndEvent);
                ImGui.Checkbox("MultiClickEvent", ref _logMultiClickEvent);
            }

            ImGui.TreePop();

            bool clearLogs = ImGui.Button("Clear");

            if (clearLogs)
            {
                EventLog.Clear();
            }

            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar;

            if (ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle, windowFlags))
            {
                foreach (string message in EventLog)
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
}
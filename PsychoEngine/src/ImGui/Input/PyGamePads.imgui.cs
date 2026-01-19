using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using NVector2 = System.Numerics.Vector2;
using NVector4 = System.Numerics.Vector4;

namespace PsychoEngine.Input;

public static partial class PyGamePads
{
    private const  int  LogCapacity         = 100;
    private static bool _logConnectionEvent = true;
    private static bool _logDownEvent;
    private static bool _logPressEvent      = true;
    private static bool _logReleaseEvent    = true;
    private static bool _logTriggerEvent    = true;
    private static bool _logThumbstickEvent = true;

    private static readonly string[] PlayerNames    = Enum.GetNames<PlayerIndex>();
    private static readonly string[] FocusLostNames = Enum.GetNames<FocusLostInputBehaviour>();

    private static readonly List<string> EventLog = new(LogCapacity);

    private static int  _playerIndex;
    private static bool _activeButtonsOnly = true;

    private static void InitializeImGui()
    {
        PyGame.Instance.ImGuiManager.OnLayout += ImGuiLayout;

        OnPlayerConnected += (_, args) =>
        {
            if (!_logConnectionEvent)
            {
                return;
            }

            ImGuiLog("OnPlayerConnected");
            ImGuiLog($"     -Player: {args.PlayerIndex}");
            ImGuiLog("separator");
        };

        OnPlayerDisconnected += (_, args) =>
        {
            if (!_logConnectionEvent)
            {
                return;
            }

            ImGuiLog("OnPlayerDisconnected");
            ImGuiLog($"     -Player: {args.PlayerIndex}");
            ImGuiLog("separator");
        };

        OnButtonDown += (_, args) =>
        {
            if (!_logDownEvent)
            {
                return;
            }

            ImGuiLog("OnButtonDown");
            ImGuiLog($"     -Player: {args.PlayerIndex}");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog("separator");
        };

        OnButtonPressed += (_, args) =>
        {
            if (!_logPressEvent)
            {
                return;
            }

            ImGuiLog("OnButtonPressed");
            ImGuiLog($"     -Player: {args.PlayerIndex}");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog("separator");
        };

        OnButtonReleased += (_, args) =>
        {
            if (!_logReleaseEvent)
            {
                return;
            }

            ImGuiLog("OnButtonReleased");
            ImGuiLog($"     -Player: {args.PlayerIndex}");
            ImGuiLog($"     -Button: {args.Button}");
            ImGuiLog("separator");
        };

        OnTriggerMoved += (_, args) =>
        {
            if (!_logTriggerEvent)
            {
                return;
            }

            ImGuiLog("OnTriggerMoved");
            ImGuiLog($"     -Player: {args.PlayerIndex}");
            ImGuiLog($"     -Trigger: {args.Trigger}");
            ImGuiLog($"     -Value: {args.TriggerValue}");
            ImGuiLog($"     -Delta: {args.TriggerDelta}");
            ImGuiLog("separator");
        };

        OnThumbstickMoved += (_, args) =>
        {
            if (!_logThumbstickEvent)
            {
                return;
            }

            ImGuiLog("OnThumbstickMoved");
            ImGuiLog($"     -Player: {args.PlayerIndex}");
            ImGuiLog($"     -Thumbstick: {args.Thumbstick}");
            ImGuiLog($"     -Value: {args.ThumbstickValue}");
            ImGuiLog($"     -Delta: {args.ThumbstickDelta}");
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

    private static void ImGuiLayout(object? sender, EventArgs eventArgs)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.Gamepad2} GamePads");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }

        ImGui.SeparatorText("Global");

        if (ImGui.CollapsingHeader("Config"))
        {
            int focusLost = (int)FocusLostInputBehaviour;

            bool focusLostChanged =
                ImGui.Combo("FocusLost Behaviour", ref focusLost, FocusLostNames, FocusLostNames.Length);

            if (focusLostChanged)
            {
                FocusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
            }
        }

        if (ImGui.CollapsingHeader("Time stamps##global"))
        {
            ImGui.Text($"Last Input: {LastInputTime}");
            ImGui.Text($"Last Input Player: {LastInputResponsiblePlayer}");
        }

        if (ImGui.CollapsingHeader("Connection##global"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;

            if (ImGui.BeginTable("Connection##table", 2, flags))
            {
                ImGui.TableSetupColumn("Player");
                ImGui.TableSetupColumn("Connected");
                ImGui.TableHeadersRow();

                foreach (PlayerIndex playerI in PlayersEnum)
                {
                    PyGamePad playerN   = GetPlayer(playerI);
                    bool      connected = playerN.IsConnected;

                    ImGui.TableNextColumn();
                    ImGui.Text(PlayerNames[(int)playerI]);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##connected{playerI}", ref connected);
                    ImGui.TableNextRow();
                }

                ImGui.EndTable();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.Combo("Player", ref _playerIndex, PlayerNames, PlayerNames.Length);
        PyGamePad player = GetPlayer((PlayerIndex)_playerIndex);

        ImGui.SeparatorText($"Player {PlayerNames[_playerIndex]}");

        if (ImGui.CollapsingHeader("Connection##player"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;

            if (ImGui.BeginTable("Connection##table", 3, flags))
            {
                ImGui.TableSetupColumn("Status");
                ImGui.TableSetupColumn("WasConnected");
                ImGui.TableSetupColumn("WasDisconnected");
                ImGui.TableHeadersRow();

                bool connected    = player.WasConnected();
                bool disconnected = player.WasDisconnected();

                ImGui.TableNextColumn();

                if (player.IsConnected)
                {
                    ImGui.Text("Connected");
                }
                else
                {
                    ImGui.TextDisabled("Disconnected");
                }

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{player}connected", ref connected);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{player}disconnected", ref disconnected);

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Time stamps##player"))
        {
            ImGui.Text($"Last Input: {player.LastInputTime}");
        }

        if (ImGui.CollapsingHeader("Buttons"))
        {
            ImGui.Checkbox("Only active keys", ref _activeButtonsOnly);

            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;

            if (ImGui.BeginTable("Buttons", 4, flags))
            {
                ImGui.TableSetupColumn("Button");
                ImGui.TableSetupColumn("State");
                ImGui.TableSetupColumn("Pressed");
                ImGui.TableSetupColumn("Released");
                ImGui.TableHeadersRow();

                foreach (GamePadButton button in ButtonsEnum)
                {
                    if (_activeButtonsOnly && player.GetButtonState(button) == InputStates.Up)
                    {
                        continue;
                    }

                    bool pressed  = player.WasButtonPressed(button);
                    bool released = player.WasButtonReleased(button);

                    if (!player.IsButtonDown(button))
                    {
                        ImGui.BeginDisabled();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(player.IsButtonDown(button) ? "Down" : "Up")}");

                    if (!player.IsButtonDown(button))
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

        if (ImGui.CollapsingHeader("Thumbsticks"))
        {
            const ImPlotSubplotFlags subplotFlags = ImPlotSubplotFlags.NoLegend |
                                                    ImPlotSubplotFlags.NoMenus |
                                                    ImPlotSubplotFlags.NoTitle |
                                                    ImPlotSubplotFlags.NoResize |
                                                    ImPlotSubplotFlags.LinkAllX |
                                                    ImPlotSubplotFlags.LinkAllY;

            NVector2 plotPadding = ImPlot.GetStyle().PlotPadding;
            NVector2 plotSize    = new(400, 200);
            plotSize.X += plotPadding.X * 3;
            plotSize.Y += plotPadding.Y * 2;

            if (ImPlot.BeginSubplots("Thumbsticks", 1, 2, plotSize, subplotFlags))
            {
                DrawThumbPlot("Left", GamePadThumbstick.Left, 0);
                DrawThumbPlot("Right", GamePadThumbstick.Right, 1);

                ImPlot.EndSubplots();
            }
        }

        if (ImGui.CollapsingHeader("Triggers"))
        {
            const ImPlotFlags plotFlags = ImPlotFlags.NoTitle | ImPlotFlags.NoInputs;

            const ImPlotAxisFlags axesFlags =
                ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks;

            const ImPlotLegendFlags legendFlags =
                ImPlotLegendFlags.NoMenus | ImPlotLegendFlags.Horizontal | ImPlotLegendFlags.NoButtons;

            const ImPlotBarsFlags barsFlags = ImPlotBarsFlags.None;

            NVector2 plotSize = new(400, 200);

            if (ImPlot.BeginPlot("Triggers##plot", plotSize, plotFlags))
            {
                ImPlot.SetupAxes("Value", "Trigger", axesFlags, axesFlags);
                ImPlot.SetupAxesLimits(-0.5f, 1.5f, 0f, 1f, ImPlotCond.Always);

                ImPlot.SetupLegend(ImPlotLocation.North, legendFlags);

                unsafe
                {
                    NVector2 legendPadding = ImPlot.GetStyle().LegendPadding;

                    float[] leftTrigger = [player.GetTrigger(GamePadTrigger.Left)];

                    fixed (float* leftTriggerPtr = leftTrigger)
                    {
                        ImPlot.PlotBars("Left", leftTriggerPtr, leftTrigger.Length, barsFlags);
                    }

                    float[] rightTrigger = [0f, player.GetTrigger(GamePadTrigger.Right)];

                    fixed (float* rightTriggerPtr = rightTrigger)
                    {
                        ImPlot.PlotBars("Right", rightTriggerPtr, rightTrigger.Length, barsFlags);
                    }

                    string   text         = $"{leftTrigger[0]}, {rightTrigger[1]}";
                    NVector2 textPosition = ImPlot.PlotToPixels(new ImPlotPoint(0.5f));
                    NVector2 textSize     = ImGui.CalcTextSize(text);
                    textPosition.Y -= textSize.Y / 2f;
                    textPosition.Y -= legendPadding.Y;

                    ImPlotPoint textPlotPosition = ImPlot.PixelsToPlot(textPosition);

                    ImPlot.PlotText(text, textPlotPosition.X, textPlotPosition.Y);
                }

                ImPlot.EndPlot();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Events log"))
        {
            ImGui.TreePush("Events");

            if (ImGui.CollapsingHeader("Events"))
            {
                ImGui.Checkbox("Connection events", ref _logConnectionEvent);
                ImGui.Checkbox("DownEvent", ref _logDownEvent);
                ImGui.Checkbox("PressEvent", ref _logPressEvent);
                ImGui.Checkbox("ReleaseEvent", ref _logReleaseEvent);
                ImGui.Checkbox("Trigger events", ref _logTriggerEvent);
                ImGui.Checkbox("thumbstick events", ref _logThumbstickEvent);
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

        return;

        unsafe void DrawThumbPlot(string name, GamePadThumbstick thumbstick, int index)
        {
            const ImPlotFlags plotFlags = ImPlotFlags.NoTitle | ImPlotFlags.NoInputs;

            const ImPlotLegendFlags legendFlags = ImPlotLegendFlags.NoButtons;

            const ImPlotAxisFlags axesFlags =
                ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks;

            NVector2 legendPadding = ImPlot.GetStyle().LegendPadding;

            if (!ImPlot.BeginPlot(name, new NVector2(200, 200), plotFlags))
            {
                return;
            }

            ImPlot.SetupAxes("X", "Y", axesFlags, axesFlags);
            ImPlot.SetupAxesLimits(-1f, 1f, -1f, 1f, ImPlotCond.Always);
            ImPlot.SetupLegend(ImPlotLocation.North, legendFlags);

            Vector2  thumbstickValue = player.GetThumbstick(thumbstick);
            float    magnitude       = thumbstickValue.Length();
            NVector4 color           = ImPlot.GetColormapColor(index);

            float[] xs = [0, thumbstickValue.X / magnitude];

            float[] ys = [0, thumbstickValue.Y / magnitude];

            fixed (float* xsPtr = xs)
            fixed (float* ysPtr = ys)
            {
                NVector4 markerFillColor;

                if (player.IsThumbstickButtonDown(thumbstick))
                {
                    markerFillColor = color;
                }
                else
                {
                    markerFillColor   = ImPlot.GetStyleColorVec4(ImPlotCol.Bg);
                    markerFillColor.W = 1f;
                }

                ImPlot.SetNextMarkerStyle(ImPlotMarker.Circle, 10f, markerFillColor, 0.5f);
                ImPlot.SetNextLineStyle(color);

                string   text         = $"({thumbstickValue.X}, {thumbstickValue.Y})";
                NVector2 textPosition = ImPlot.PlotToPixels(new ImPlotPoint(0f, -1f));
                NVector2 textSize     = ImGui.CalcTextSize(text);
                textPosition.Y -= textSize.Y / 2f;
                textPosition.Y -= legendPadding.Y;

                ImPlotPoint textPlotPosition = ImPlot.PixelsToPlot(textPosition);

                ImPlot.PlotText(text, textPlotPosition.X, textPlotPosition.Y);

                ImPlot.PlotLine(name, xsPtr, ysPtr, xs.Length);

                uint     circleColor = ImGui.GetColorU32(ImGuiCol.Separator);
                NVector2 center      = ImPlot.PlotToPixels(new ImPlotPoint(0f));
                NVector2 right       = ImPlot.PlotToPixels(new ImPlotPoint(1f));
                ImPlot.PushPlotClipRect();
                ImPlot.GetPlotDrawList().AddCircle(center, right.X - center.X, circleColor, 60);
                ImPlot.PopPlotClipRect();
            }

            ImPlot.EndPlot();
        }
    }
}
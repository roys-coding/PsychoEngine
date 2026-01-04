using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using NVector2 = System.Numerics.Vector2;
using NVector4 = System.Numerics.Vector4;

namespace PsychoEngine.Input;

public static class PyGamePads
{
    // Constants.
    public static readonly int SupportedPlayersCount;

    private static readonly PlayerIndex[]    PlayersEnum;
    private static readonly GamePadButtons[] ButtonsEnum;

    // GamePad states.
    private static readonly IDictionary<PlayerIndex, PyGamePad> GamePads;
    
    // Config.
    internal static FocusLostInputBehaviour FocusLostInputBehaviour = FocusLostInputBehaviour.ClearState;

    // GamePad states.
    public static bool        IsAnyConnected       { get; private set; }
    
    // Time stamps. 
    public static TimeSpan    LastInputTime        { get; private set; }
    public static PlayerIndex LastInputPlayerIndex { get; private set; }

    static PyGamePads()
    {
        PlayersEnum            = Enum.GetValues<PlayerIndex>();
        ButtonsEnum            = Enum.GetValues<GamePadButtons>();
        SupportedPlayersCount = PlayersEnum.Length;
        GamePads              = new Dictionary<PlayerIndex, PyGamePad>(SupportedPlayersCount);

        foreach (PlayerIndex player in PlayersEnum)
        {
            GamePads.Add(player, new PyGamePad(player));
        }

        #region ImGui

        PyGame.Instance.ImGuiManager.OnLayout += ImGuiLayout;

        #endregion
    }

    #region ImGui
    private static readonly string[]         PlayerNames    = Enum.GetNames<PlayerIndex>();
    private static readonly string[]         FocusLostNames = Enum.GetNames<FocusLostInputBehaviour>();

    private static int _playerIndex;
    private static bool _activeButtonsOnly = true;

    private static void ImGuiLayout(object? sender, EventArgs eventArgs)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.Gamepad2} GamePads");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }

        ImGui.Combo("Player", ref _playerIndex, PlayerNames, PlayerNames.Length);
        PyGamePad player = GetPlayer((PlayerIndex)_playerIndex);

        ImGui.Text($"Connected: {player.IsConnected}");

        if (ImGui.CollapsingHeader("Config"))
        {
            int focusLost = (int)FocusLostInputBehaviour;

            bool focusLostChanged =
                ImGui.Combo("FocusLost Behaviour", ref focusLost, FocusLostNames, FocusLostNames.Length);

            if (focusLostChanged) FocusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
        }

        if (ImGui.CollapsingHeader("Time stamps"))
        {
            ImGui.Text($"Last Input Any: {LastInputTime}");
            ImGui.Text($"Last Input Player: {LastInputPlayerIndex}");
            ImGui.Text($"Last Input Player {_playerIndex}: {player.LastInputTime}");
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

                foreach (GamePadButtons button in ButtonsEnum)
                {
                    if (_activeButtonsOnly && player.GetButton((button)) == InputStates.Up) continue;
                    
                    bool pressed  = player.WasButtonPressed(button);
                    bool released = player.WasButtonReleased(button);

                    if (!player.IsButtonDown(button)) ImGui.BeginDisabled();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(player.IsButtonDown(button) ? "Down" : "Up")}");

                    if (!player.IsButtonDown(button)) ImGui.EndDisabled();

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
                                                    ImPlotSubplotFlags.NoMenus  |
                                                    ImPlotSubplotFlags.NoTitle  |
                                                    ImPlotSubplotFlags.NoResize |
                                                    ImPlotSubplotFlags.LinkAllX |
                                                    ImPlotSubplotFlags.LinkAllY;

            NVector2 plotPadding = ImPlot.GetStyle().PlotPadding;
            NVector2 plotSize    = new(400, 200);
            plotSize.X += plotPadding.X * 3;
            plotSize.Y += plotPadding.Y * 2;

            if (ImPlot.BeginSubplots("Thumbsticks", 1, 2, plotSize, subplotFlags))
            {
                DrawThumbPlot("Left",  GamePadThumbsticks.Left,  0);
                DrawThumbPlot("Right", GamePadThumbsticks.Right, 1);

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

                    float[] leftTrigger =
                    [
                        player.GetTrigger(GamePadTriggers.Left),
                    ];

                    fixed (float* leftTriggerPtr = leftTrigger)
                    {
                        ImPlot.PlotBars("Left", leftTriggerPtr, leftTrigger.Length, barsFlags);
                    }

                    float[] rightTrigger =
                    [
                        0f, player.GetTrigger(GamePadTriggers.Right),
                    ];

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

        ImGui.End();

        return;

        unsafe void DrawThumbPlot(string name, GamePadThumbsticks thumbstick, int index)
        {
            const ImPlotFlags plotFlags = ImPlotFlags.NoTitle | ImPlotFlags.NoInputs;

            const ImPlotLegendFlags legendFlags = ImPlotLegendFlags.NoButtons;

            const ImPlotAxisFlags axesFlags =
                ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks;

            NVector2 legendPadding = ImPlot.GetStyle().LegendPadding;

            if (!ImPlot.BeginPlot(name, new NVector2(200, 200), plotFlags)) return;

            ImPlot.SetupAxes("X", "Y", axesFlags, axesFlags);
            ImPlot.SetupAxesLimits(-1f, 1f, -1f, 1f, ImPlotCond.Always);
            ImPlot.SetupLegend(ImPlotLocation.North, legendFlags);

            Vector2  thumbstickValue = player.GetThumbstick(thumbstick);
            float    magnitude       = thumbstickValue.Length();
            NVector4 color           = ImPlot.GetColormapColor(index);

            float[] xs =
            [
                0, thumbstickValue.X / magnitude,
            ];

            float[] ys =
            [
                0, thumbstickValue.Y / magnitude,
            ];

            fixed (float* xsPtr = xs)
            fixed (float* ysPtr = ys)
            {
                NVector4 markerFillColor;

                if (player.IsThumbstickDown(thumbstick))
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

                uint     borderColor = ImGui.GetColorU32(ImGuiCol.Separator);
                NVector2 center      = ImPlot.PlotToPixels(new ImPlotPoint(0f));
                NVector2 right       = ImPlot.PlotToPixels(new ImPlotPoint(1f));
                ImPlot.PushPlotClipRect();
                ImPlot.GetPlotDrawList().AddCircle(center, right.X - center.X, borderColor, 60);
                ImPlot.PopPlotClipRect();
            }

            ImPlot.EndPlot();
        }
    }

    #endregion

    public static PyGamePad GetPlayer(PlayerIndex playerIndex)
    {
        PyGamePad? player;
        bool       playerFound = GamePads.TryGetValue(playerIndex, out player);

        if (!playerFound || player == null)
        {
            throw new InvalidOperationException($"Player '{playerIndex}' not supported.");
        }

        return player;
    }

    public static bool IsPlayerConnected(PlayerIndex playerIndex)
    {
        return GetPlayer(playerIndex).IsConnected;
    }

    #region Internal methods

    internal static void Update(Game game)
    {
        foreach (PyGamePad gamePad in GamePads.Values)
        {
            gamePad.Update(game);

            if (gamePad.LastInputTime > LastInputTime)
            {
                LastInputTime = gamePad.LastInputTime;
            }
        }
    }

    #endregion
}
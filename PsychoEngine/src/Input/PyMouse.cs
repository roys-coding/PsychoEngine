using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Utilities;
using NVector2 = System.Numerics.Vector2;

namespace PsychoEngine.Input;

public static class PyMouse
{
    // TODO: Implement FNA Click ext.
    // TODO: Mouse set position.
    // TODO: Implement mouse cursors for MonoGame.
    // TODO: Make input configurable.
    // TODO: Touch input.

    #region Subclasses

    private struct MouseButtonState
    {
        // State.
        public InputStates InputState { get; set; }

        // Dragging.
        public Point DragStartPosition  { get; set; }
        public bool  PreviousIsDragging { get; set; }

        // ReSharper disable MemberHidesStaticFromOuterClass
        public bool IsDragging { get; set; }
        // ReSharper restore MemberHidesStaticFromOuterClass

        // Multi clicking.
        public TimeSpan LastPressTime     { get; set; }
        public int      ConsecutiveClicks { get; set; }
    }

    #endregion

    #region Events

    // Movement & scrolling.
    public static event EventHandler<MouseMovedEventArgs>?    OnMoved;
    public static event EventHandler<MouseScrolledEventArgs>? OnScrolled;

    // Buttons.
    public static event EventHandler<MouseButtonEventArgs>? OnButtonDown;
    public static event EventHandler<MouseButtonEventArgs>? OnButtonPressed;
    public static event EventHandler<MouseButtonEventArgs>? OnButtonReleased;

    // Dragging.
    public static event EventHandler<MouseDraggedEventArgs>? OnDragStarted;
    public static event EventHandler<MouseDraggedEventArgs>? OnDragging;
    public static event EventHandler<MouseDraggedEventArgs>? OnDragReleased;

    // Multi-clicks.
    public static event EventHandler<MouseMultiClickEventArgs>? OnMultiClick;

    #endregion

    #region Constants

    private const            float         WheelDeltaUnit = 120f;
    internal static readonly MouseButton[] AllButtons;

    #endregion

    #region Fields

    // Config.
    private const  int                     DragThreshold                    = 5;
    private const  double                  ConsecutiveClickThresholdSeconds = 0.5;
    private static FocusLostInputBehaviour _focusLostInputBehaviour         = FocusLostInputBehaviour.ClearStates;

    // States.
    private static MouseState _previousState;
    private static MouseState _currentState;

    // Button states.
    private static MouseButtonState _leftButton;
    private static MouseButtonState _middleButton;
    private static MouseButtonState _rightButton;
    private static MouseButtonState _x1Button;
    private static MouseButtonState _x2Button;

    #endregion

    #region Properties

    // Time stamps.
    public static TimeSpan LastMoveTime  { get; private set; }
    public static TimeSpan LastInputTime { get; private set; }

    // Position.
    public static Point PreviousPosition { get; private set; }
    public static Point Position         { get; private set; }
    public static Point PositionDelta    { get; private set; }
    public static bool  Moved            { get; private set; }

    // Scroll wheel.
    public static float PreviousScrollValue { get; private set; }
    public static float ScrollValue         { get; private set; }
    public static float ScrollDelta         { get; private set; }
    public static bool  Scrolled            { get; private set; }

    #endregion

    static PyMouse()
    {
        AllButtons = Enum.GetValues<MouseButton>();

        #region ImGui

        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        OnMoved += (_, args) =>
                   {
                       if (!_logMovedEvent) return;
                       ImGuiLog("OnMoved");
                       ImGuiLog($"     -PrevPos: {args.PreviousPosition}");
                       ImGuiLog($"     -Position: {args.Position}");
                       ImGuiLog($"     -PosDelta: {args.PositionDelta}");
                       ImGuiLog("separator");
                   };

        OnScrolled += (_, args) =>
                      {
                          if (!_logScrollEvent) return;
                          ImGuiLog("OnScrolled");
                          ImGuiLog($"     -ScrollValue: {args.ScrollValue}");
                          ImGuiLog($"     -ScrollDelta: {args.ScrollDelta}");
                          ImGuiLog($"     -Position: {args.Position}");
                          ImGuiLog("separator");
                      };

        OnButtonDown += (_, args) =>
                        {
                            if (!_logDownEvent) return;
                            ImGuiLog("OnButtonDown");
                            ImGuiLog($"     -Button: {args.Button}");
                            ImGuiLog($"     -Position: {args.Position}");
                            ImGuiLog("separator");
                        };

        OnButtonPressed += (_, args) =>
                           {
                               if (!_logPressEvent) return;
                               ImGuiLog("OnButtonPressed");
                               ImGuiLog($"     -Button: {args.Button}");
                               ImGuiLog($"     -Position: {args.Position}");
                               ImGuiLog("separator");
                           };

        OnButtonReleased += (_, args) =>
                            {
                                if (!_logReleaseEvent) return;
                                ImGuiLog("OnButtonReleased");
                                ImGuiLog($"     -Button: {args.Button}");
                                ImGuiLog($"     -Position: {args.Position}");
                                ImGuiLog("separator");
                            };

        OnDragStarted += (_, args) =>
                         {
                             if (!_logDragStartEvent) return;
                             ImGuiLog("OnDragStarted");
                             ImGuiLog($"     -Button: {args.Button}");
                             ImGuiLog($"     -StartPos: {args.DragStartPosition}");
                             ImGuiLog($"     -Position: {args.Position}");
                             ImGuiLog("separator");
                         };

        OnDragging += (_, args) =>
                      {
                          if (!_logDragEvent) return;
                          ImGuiLog("OnDragging");
                          ImGuiLog($"     -Button: {args.Button}");
                          ImGuiLog($"     -StartPos: {args.DragStartPosition}");
                          ImGuiLog($"     -Position: {args.Position}");
                          ImGuiLog("separator");
                      };

        OnDragReleased += (_, args) =>
                          {
                              if (!_logDragEndEvent) return;
                              ImGuiLog("OnDragReleased");
                              ImGuiLog($"     -Button: {args.Button}");
                              ImGuiLog($"     -StartPos: {args.DragStartPosition}");
                              ImGuiLog($"     -Position: {args.Position}");
                              ImGuiLog("separator");
                          };

        OnMultiClick += (_, args) =>
                        {
                            if (!_logMulticlickEvent) return;
                            ImGuiLog("OnMultiClick");
                            ImGuiLog($"     -Button: {args.Button}");
                            ImGuiLog($"     -Clicks: {args.ClickCount}");
                            ImGuiLog($"     -Position: {args.Position}");
                            ImGuiLog("separator");
                        };

        #endregion
    }

    #region ImGui

    #region ImGui fields

    private const  int  LogCapacity = 100;
    private static bool _logMovedEvent;
    private static bool _logScrollEvent = true;
    private static bool _logDownEvent;
    private static bool _logPressEvent     = true;
    private static bool _logReleaseEvent   = true;
    private static bool _logDragStartEvent = true;
    private static bool _logDragEvent;
    private static bool _logDragEndEvent    = true;
    private static bool _logMulticlickEvent = true;

    private static readonly string[] FocusLostNames = Enum.GetNames<FocusLostInputBehaviour>();

    private static readonly List<string> EventLog = new(LogCapacity);
    private static          bool         _logHeader;

    #endregion

    private static void ImGuiLog(string message)
    {
        if (!_logHeader) return;

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

            if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
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
                NVector2      mousePos    = ImPlot.PlotToPixels(new ImPlotPoint(Position.X,    -Position.Y));
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

                    if (!IsButtonDown(button)) ImGui.BeginDisabled();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(IsButtonDown(button) ? "Down" : "Up")}");

                    if (!IsButtonDown(button)) ImGui.EndDisabled();

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
                    bool  stoppedDrag = WasDragReleased(button);

                    if (!dragging) ImGui.BeginDisabled();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(startPos.ToString());

                    if (!dragging) ImGui.EndDisabled();

                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}dragging", ref dragging);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}starteddrag", ref startedDrag);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}releaseddrag", ref stoppedDrag);
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

                    if (!multiClicked) ImGui.BeginDisabled();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(button.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(lastReleaseTime.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(clicks.ToString());

                    if (!multiClicked) ImGui.EndDisabled();

                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##{button}multi click", ref multiClicked);
                }

                ImGui.EndTable();
            }
        }

        _logHeader = ImGui.CollapsingHeader("Events log");

        if (_logHeader)
        {
            ImGui.TreePush("Events");

            if (ImGui.CollapsingHeader("Events"))
            {
                ImGui.Checkbox("MovedEvent",      ref _logMovedEvent);
                ImGui.Checkbox("ScrollEvent",     ref _logScrollEvent);
                ImGui.Checkbox("DownEvent",       ref _logDownEvent);
                ImGui.Checkbox("PressEvent",      ref _logPressEvent);
                ImGui.Checkbox("ReleaseEvent",    ref _logReleaseEvent);
                ImGui.Checkbox("DragStartEvent",  ref _logDragStartEvent);
                ImGui.Checkbox("DragEvent",       ref _logDragEvent);
                ImGui.Checkbox("DragEndEvent",    ref _logDragEndEvent);
                ImGui.Checkbox("MultiClickEvent", ref _logMulticlickEvent);
            }

            ImGui.TreePop();

            bool clearLogs = ImGui.Button("Clear");
            if (clearLogs) EventLog.Clear();

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

                ImGui.EndChild();
            }
        }

        ImGui.End();
    }

    #endregion

    #region Public interface

    // Buttons.
    public static InputStates GetButtonState(MouseButton button)
    {
        return GetButtonStateInternal(button).InputState;
    }

    public static bool IsButtonUp(MouseButton button)
    {
        return GetButtonState(button).HasFlag(InputStates.Up);
    }

    public static bool IsButtonDown(MouseButton button)
    {
        return GetButtonState(button).HasFlag(InputStates.Down);
    }

    public static bool WasButtonPressed(MouseButton button)
    {
        return GetButtonState(button).HasFlag(InputStates.Pressed);
    }

    public static bool WasButtonReleased(MouseButton button)
    {
        return GetButtonState(button).HasFlag(InputStates.Released);
    }

    // Dragging.
    public static bool WasDragStarted(MouseButton button)
    {
        MouseButtonState state = GetButtonStateInternal(button);

        return state is
               {
                   IsDragging        : true,
                   PreviousIsDragging: false,
               };
    }

    public static bool IsDragging(MouseButton button)
    {
        return GetButtonStateInternal(button).IsDragging;
    }

    public static bool WasDragReleased(MouseButton button)
    {
        MouseButtonState state = GetButtonStateInternal(button);

        return state is
               {
                   IsDragging        : false,
                   PreviousIsDragging: true,
               };
    }

    public static Point GetDragStartPosition(MouseButton button)
    {
        return GetButtonStateInternal(button).DragStartPosition;
    }

    // Multi-clicks.
    public static bool WasButtonMultiClicked(MouseButton button)
    {
        return WasButtonPressed(button) && GetButtonStateInternal(button).ConsecutiveClicks > 0;
    }

    public static bool WasButtonMultiClicked(MouseButton button, int multiClicks)
    {
        MouseButtonState buttonState = GetButtonStateInternal(button);

        return WasButtonPressed(button)                        &&
               buttonState.ConsecutiveClicks               > 0 &&
               buttonState.ConsecutiveClicks % multiClicks == 0;
    }

    public static int GetConsecutiveClicks(MouseButton button)
    {
        return GetButtonStateInternal(button).ConsecutiveClicks;
    }

    #endregion

    #region Non public methods

    internal static void Update(Game game)
    {
        UpdateMouseStates(game);

        // Position.
        PreviousPosition = new Point(_previousState.X, _previousState.Y);
        Position         = new Point(_currentState.X,  _currentState.Y);
        PositionDelta    = Position - PreviousPosition;
        Moved            = PositionDelta != Point.Zero;

        if (Moved)
        {
            OnMoved?.Invoke(null,
                            new MouseMovedEventArgs(PreviousPosition,
                                                    Position,
                                                    PositionDelta,
                                                    PyKeyboard.ModifierKeysDown));

            LastMoveTime = PyGameTimes.Update.TotalGameTime;
        }

        // Scroll.
        PreviousScrollValue = _previousState.ScrollWheelValue / WheelDeltaUnit;
        ScrollValue         = _currentState.ScrollWheelValue  / WheelDeltaUnit;
        ScrollDelta         = ScrollValue - PreviousScrollValue;
        Scrolled            = ScrollDelta != 0f;

        if (Scrolled)
        {
            OnScrolled?.Invoke(null,
                               new MouseScrolledEventArgs(ScrollValue,
                                                          ScrollDelta,
                                                          Position,
                                                          PyKeyboard.ModifierKeysDown));

            LastInputTime = PyGameTimes.Update.TotalGameTime;
        }

        // Input state and dragging.
        foreach (MouseButton button in AllButtons)
        {
            MouseButtonState state = GetButtonStateInternal(button);

            UpdateButtonInputState(button, ref state);
            UpdateButtonDragging(button, ref state);
            UpdateButtonConsecutiveClicking(button, ref state);

            SetButtonStateInternal(button, state);
        }
    }

    private static void UpdateMouseStates(Game game)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureMouse)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = Mouse.GetState();

            return;
        }

        switch (_focusLostInputBehaviour)
        {
            case FocusLostInputBehaviour.ClearStates:
                // Pass an empty state, releasing all buttons.
                // We only retain the last position and scroll value.
                _previousState = _currentState;

                _currentState = new MouseState(_previousState.X,
                                               _previousState.Y,
                                               _previousState.ScrollWheelValue,
                                               ButtonState.Released,
                                               ButtonState.Released,
                                               ButtonState.Released,
                                               ButtonState.Released,
                                               ButtonState.Released);

                break;

            case FocusLostInputBehaviour.FreezeStates:
                // Maintain previous state, not releasing nor pressing any more buttons.
                _previousState = _currentState;
                break;

            case FocusLostInputBehaviour.KeepUpdating:
                // Update input state normally.
                _previousState = _currentState;
                _currentState  = Mouse.GetState();
                break;

            default:
                throw new
                    NotSupportedException($"FocusLostInputBehaviour '{_focusLostInputBehaviour}' not supported.");
        }
    }

    private static void UpdateButtonInputState(MouseButton button, ref MouseButtonState state)
    {
        ButtonState previousState = _previousState.GetButton(button);
        ButtonState currentState  = _currentState.GetButton(button);

        InputStates inputState       = InputStates.None;
        bool        receivedAnyInput = false;

        switch (currentState)
        {
            case ButtonState.Pressed:
            {
                if (previousState == ButtonState.Released)
                {
                    inputState |= InputStates.Pressed;

                    OnButtonPressed?.Invoke(null,
                                            new MouseButtonEventArgs(button, Position, PyKeyboard.ModifierKeysDown));
                }

                inputState |= InputStates.Down;
                OnButtonDown?.Invoke(null, new MouseButtonEventArgs(button, Position, PyKeyboard.ModifierKeysDown));

                receivedAnyInput = true;
                break;
            }

            case ButtonState.Released:
            {
                inputState |= InputStates.Up;

                if (previousState == ButtonState.Pressed)
                {
                    inputState |= InputStates.Released;

                    OnButtonReleased?.Invoke(null,
                                             new MouseButtonEventArgs(button, Position, PyKeyboard.ModifierKeysDown));

                    receivedAnyInput = true;
                }

                break;
            }

            default: throw new NotSupportedException($"Mouse state '{currentState}' not supported.");
        }

        state.InputState = inputState;

        if (receivedAnyInput) LastInputTime = PyGameTimes.Update.TotalGameTime;
    }

    private static void UpdateButtonDragging(MouseButton button, ref MouseButtonState state)
    {
        state.PreviousIsDragging = state.IsDragging;

        if (WasButtonPressed(button))
        {
            state.DragStartPosition = Position;
        }

        if (WasButtonReleased(button))
        {
            if (state.IsDragging)
            {
                // Button stopped dragging.
                state.IsDragging = false;

                OnDragReleased?.Invoke(null,
                                       new MouseDraggedEventArgs(button,
                                                                 state.DragStartPosition,
                                                                 Position,
                                                                 PyKeyboard.ModifierKeysDown));
            }

            state.DragStartPosition = Point.Zero;
        }

        if (IsButtonDown(button) && !state.IsDragging)
        {
            float distance = state.DragStartPosition.Distance(Position);

            if (distance > DragThreshold)
            {
                // Button began dragging.
                state.IsDragging = true;

                OnDragStarted?.Invoke(null,
                                      new MouseDraggedEventArgs(button,
                                                                state.DragStartPosition,
                                                                Position,
                                                                PyKeyboard.ModifierKeysDown));
            }
        }

        if (state.IsDragging)
        {
            OnDragging?.Invoke(null,
                               new MouseDraggedEventArgs(button,
                                                         state.DragStartPosition,
                                                         Position,
                                                         PyKeyboard.ModifierKeysDown));
        }
    }

    private static void UpdateButtonConsecutiveClicking(MouseButton button, ref MouseButtonState state)
    {
        if (WasButtonPressed(button))
        {
            TimeSpan timeSinceLastClick = PyGameTimes.Update.TotalGameTime - state.LastPressTime;

            if (timeSinceLastClick.TotalSeconds <= ConsecutiveClickThresholdSeconds)
            {
                state.ConsecutiveClicks++;

                OnMultiClick?.Invoke(null,
                                     new MouseMultiClickEventArgs(button,
                                                                  state.ConsecutiveClicks,
                                                                  Position,
                                                                  PyKeyboard.ModifierKeysDown));
            }
            else
            {
                state.ConsecutiveClicks = 0;
            }

            state.LastPressTime = PyGameTimes.Update.TotalGameTime;
        }
    }

    private static MouseButtonState GetButtonStateInternal(MouseButton button)
    {
        return button switch
               {
                   MouseButton.None   => default(MouseButtonState),
                   MouseButton.Left   => _leftButton,
                   MouseButton.Middle => _middleButton,
                   MouseButton.Right  => _rightButton,
                   MouseButton.X1     => _x1Button,
                   MouseButton.X2     => _x2Button,
                   _                  => throw new NotSupportedException($"MouseButton '{button}' not supported."),
               };
    }

    private static void SetButtonStateInternal(MouseButton button, MouseButtonState state)
    {
        switch (button)
        {
            case MouseButton.None: break;

            case MouseButton.Left:
                _leftButton = state;
                break;

            case MouseButton.Middle:
                _middleButton = state;
                break;

            case MouseButton.Right:
                _rightButton = state;
                break;

            case MouseButton.X1:
                _x1Button = state;
                break;

            case MouseButton.X2:
                _x2Button = state;
                break;

            default: throw new NotSupportedException($"MouseButton '{button}' not supported.");
        }
    }

    #endregion
}
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Utils;

namespace PsychoEngine.Input;

public static class GameMouse
{
    // TODO: Implement FNA Click ext.

    // Constants.
    private const float WheelDeltaUnit = 120f;

    // Events.
    public static event EventHandler<MouseMovedEventArgs>?    OnMoved;
    public static event EventHandler<MouseScrolledEventArgs>? OnScrolled;

    public static event EventHandler<MouseButtonEventArgs>? OnButtonDown;
    public static event EventHandler<MouseButtonEventArgs>? OnButtonPressed;
    public static event EventHandler<MouseButtonEventArgs>? OnButtonReleased;

    public static event EventHandler<MouseDraggedEventArgs>? OnDragStarted;
    public static event EventHandler<MouseDraggedEventArgs>? OnDragging;
    public static event EventHandler<MouseDraggedEventArgs>? OnDragReleased;

    // Constants.
    private static readonly MouseButtons[] AllButtons;

    // Config.
    private const  int                     DragThreshold            = 5;
    private static FocusLostInputBehaviour _focusLostInputBehaviour = FocusLostInputBehaviour.ClearState;

    // Mouse states.
    private static MouseState _previousState;
    private static MouseState _currentState;

    // Button states.
    private static ButtonDragState _leftButtonDrag;
    private static ButtonDragState _middleButtonDrag;
    private static ButtonDragState _rightButtonDrag;
    private static ButtonDragState _x1ButtonDrag;
    private static ButtonDragState _x2ButtonDrag;

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

    static GameMouse()
    {
        AllButtons = Enum.GetValues<MouseButtons>();

        #region ImGui

        CoreEngine.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        OnButtonPressed  += (_, args) => ImGuiLog($"{_frame}: Pressed {args.Button}");
        OnButtonReleased += (_, args) => ImGuiLog($"{_frame}: Released {args.Button}");
        OnScrolled       += (_, args) => ImGuiLog($"{_frame}: Scrolled {args.ScrollDelta}");
        OnDragStarted    += (_, args) => ImGuiLog($"{_frame}: Drag started {args.Button}, {args.DragStartPosition}");
        OnDragReleased   += (_, args) => ImGuiLog($"{_frame}: Drag released {args.Button}, {args.DragStartPosition}");

        OnMoved += (_, args) =>
                   {
                       if (_ignoreMovedEvent) return;
                       ImGuiLog($"{_frame}: Moved {args.MouseState.Position} Delta {args.MouseState.PositionDelta}");
                   };

        OnDragging += (_, args) =>
                      {
                          if (_ignoreDraggingEvent) return;
                          ImGuiLog($"{_frame}: Dragging {args.Button}, {args.DragStartPosition}");
                      };

        OnButtonDown += (_, args) =>
                        {
                            if (_ignoreDownEvent) return;
                            ImGuiLog($"{_frame}: Down {args.Button}");
                        };

        #endregion
    }

    #region ImGui

    private const           int          LogCapacity = 1000;
    private static          int          _frame;
    private static          bool         _ignoreDownEvent;
    private static          bool         _ignoreMovedEvent;
    private static          bool         _ignoreDraggingEvent;
    private static readonly List<string> EventLog = new(LogCapacity);
    private static          bool         _logHeader;

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
        _frame++;
        bool windowOpen = ImGui.Begin($"{Fonts.Lucide.Mouse} Mouse");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        int  focusLost                                 = (int)_focusLostInputBehaviour;
        bool focusLostChanged                          = ImGui.DragInt("FocusLost", ref focusLost, 0, 2);
        if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;

        bool movementHeader = ImGui.CollapsingHeader("Movement");

        if (movementHeader)
        {
            bool hasMoved = Moved;

            ImGui.Text($"Position: {Position}");
            ImGui.Text($"PrevPos: {PreviousPosition}");
            ImGui.Text($"PosDelta: {PositionDelta}");
            ImGui.Checkbox("HasMoved", ref hasMoved);
        }

        bool scrollHeader = ImGui.CollapsingHeader("Scroll");

        if (scrollHeader)
        {
            bool hasScrolled = Scrolled;

            ImGui.Text($"Scroll: {ScrollValue}");
            ImGui.Text($"PrevScroll: {PreviousScrollValue}");
            ImGui.Text($"ScrollDelta: {ScrollDelta}");
            ImGui.Checkbox("HasScrolled", ref hasScrolled);
        }

        bool buttonsHeader = ImGui.CollapsingHeader("Buttons");

        if (buttonsHeader)
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Buttons", 4, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("State");
            ImGui.TableSetupColumn("Pressed");
            ImGui.TableSetupColumn("Released");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                bool pressed  = WasButtonPressed(button);
                bool released = WasButtonReleased(button);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (IsButtonDown(button))
                {
                    ImGui.Text(button.ToString());
                }
                else
                {
                    ImGui.TextDisabled(button.ToString());
                }

                ImGui.TableNextColumn();

                if (IsButtonDown(button))
                {
                    ImGui.Text("Down");
                }
                else
                {
                    ImGui.TextDisabled("Up");
                }

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}pressed", ref pressed);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}released", ref released);
            }

            ImGui.EndTable();
        }

        bool dragHeader = ImGui.CollapsingHeader("Drag");

        if (dragHeader)
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Drag", 5, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("LastPress");
            ImGui.TableSetupColumn("Dragging");
            ImGui.TableSetupColumn("Started");
            ImGui.TableSetupColumn("Stopped");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                Point           lastPress = GetDragStartPosition(button);
                bool            dragging  = IsDragging(button);
                bool            startedDrag  = WasDragStarted(button);
                bool            stoppedDrag  = WasDragReleased(button);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (dragging)
                {
                    ImGui.Text(button.ToString());
                }
                else
                {
                    ImGui.TextDisabled(button.ToString());
                }

                ImGui.TableNextColumn();

                if (dragging)
                {
                    ImGui.Text(lastPress.ToString());
                }
                else
                {
                    ImGui.TextDisabled(lastPress.ToString());
                }

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}dragging", ref dragging);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}starteddrag", ref startedDrag);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}stoppeddrag", ref stoppedDrag);
            }

            ImGui.EndTable();
        }

        _logHeader = ImGui.CollapsingHeader("Events log");

        if (_logHeader)
        {
            ImGui.Checkbox("Ignore down event",     ref _ignoreDownEvent);
            ImGui.Checkbox("Ignore move event",     ref _ignoreMovedEvent);
            ImGui.Checkbox("Ignore dragging event", ref _ignoreDraggingEvent);

            bool clearPressed = ImGui.Button("Clear");
            if (clearPressed) EventLog.Clear();

            ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle);

            foreach (string message in EventLog)
            {
                ImGui.Text(message);
            }

            ImGui.SetScrollHereY();

            ImGui.EndChild();
        }

        ImGui.End();
    }

    #endregion

    public static InputStates GetButton(MouseButtons button)
    {
        InputStates inputState = InputStates.None;

        if (IsButtonUp(button)) inputState        |= InputStates.Up;
        if (IsButtonDown(button)) inputState      |= InputStates.Down;
        if (WasButtonPressed(button)) inputState  |= InputStates.Pressed;
        if (WasButtonReleased(button)) inputState |= InputStates.Released;

        return inputState;
    }

    public static bool IsButtonUp(MouseButtons button)
    {
        return _currentState.GetButton(button) == ButtonState.Released;
    }

    public static bool IsButtonDown(MouseButtons button)
    {
        return _currentState.GetButton(button) == ButtonState.Pressed;
    }

    public static bool WasButtonPressed(MouseButtons button)
    {
        return _currentState.GetButton(button)  == ButtonState.Pressed &&
               _previousState.GetButton(button) == ButtonState.Released;
    }

    public static bool WasButtonReleased(MouseButtons button)
    {
        return _currentState.GetButton(button)  == ButtonState.Released &&
               _previousState.GetButton(button) == ButtonState.Pressed;
    }

    public static bool IsDragging(MouseButtons button)
    {
        return GetButtonDragState(button).IsDragging;
    }

    public static bool WasDragStarted(MouseButtons button)
    {
        ButtonDragState dragState = GetButtonDragState(button);
        return dragState is
               {
                   IsDragging        : true,
                   PreviousIsDragging: false,
               };
    }

    public static bool WasDragReleased(MouseButtons button)
    {
        ButtonDragState dragState = GetButtonDragState(button);
        return dragState is
               {
                   IsDragging        : false,
                   PreviousIsDragging: true,
               };
    }

    public static Point GetDragStartPosition(MouseButtons button)
    {
        return GetButtonDragState(button).StartPosition;
    }

    #region Internal methods

    internal static void Update(Game game, GameTime gameTime)
    {
        UpdateMouseStates(game);

        // TODO: Last input time detection
        // TODO: Multi click detection.

        // Position.
        PreviousPosition = new Point(_previousState.X, _previousState.Y);
        Position         = new Point(_currentState.X,  _currentState.Y);
        PositionDelta    = Position - PreviousPosition;
        Moved            = PositionDelta != Point.Zero;

        if (Moved)
        {
            OnMoved?.Invoke(null,
                            new MouseMovedEventArgs(PreviousPosition, Position, PositionDelta, GetSnapshot()));
        }

        // Scroll.
        PreviousScrollValue = _previousState.ScrollWheelValue / WheelDeltaUnit;
        ScrollValue         = _currentState.ScrollWheelValue  / WheelDeltaUnit;
        ScrollDelta         = ScrollValue - PreviousScrollValue;
        Scrolled            = ScrollDelta != 0f;

        if (Scrolled)
        {
            OnScrolled?.Invoke(null, new MouseScrolledEventArgs(ScrollValue, ScrollDelta, GetSnapshot()));
        }

        // Input state and dragging.
        foreach (MouseButtons button in AllButtons)
        {
            if (IsButtonDown(button))
            {
                OnButtonDown?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }

            if (WasButtonPressed(button))
            {
                OnButtonPressed?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }

            if (WasButtonReleased(button))
            {
                OnButtonReleased?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }

            UpdateButtonDragging(button);
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
            case FocusLostInputBehaviour.ClearState:
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

            case FocusLostInputBehaviour.MaintainState:
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
                    InvalidOperationException($"FocusLostInputBehaviour '{_focusLostInputBehaviour}' not supported.");
        }
    }

    private static void UpdateButtonDragging(MouseButtons button)
    {
        ButtonDragState buttonDragState = GetButtonDragState(button);

        Point dragStartPosition    = buttonDragState.StartPosition;
        bool  dragging             = buttonDragState.IsDragging;
        bool  previousDragging     = dragging;

        if (WasButtonPressed(button))
        {
            dragStartPosition = Position;
        }

        if (WasButtonReleased(button))
        {
            if (dragging)
            {
                // Button stopped dragging.
                dragging = false;
                OnDragReleased?.Invoke(null, new MouseDraggedEventArgs(button, dragStartPosition, GetSnapshot()));
            }
            
            dragStartPosition = Point.Zero;
        }

        if (IsButtonDown(button) && !dragging)
        {
            float distance = dragStartPosition.Distance(Position);

            if (distance > DragThreshold)
            {
                // Button began dragging.
                dragging = true;
                OnDragStarted?.Invoke(null, new MouseDraggedEventArgs(button, dragStartPosition, GetSnapshot()));
            }
        }

        if (dragging)
        {
            OnDragging?.Invoke(null, new MouseDraggedEventArgs(button, dragStartPosition, GetSnapshot()));
        }

        SetButtonDragState(button, new ButtonDragState(dragStartPosition, previousDragging, dragging));
    }

    private static ButtonDragState GetButtonDragState(MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => default(ButtonDragState),
                   MouseButtons.Left   => _leftButtonDrag,
                   MouseButtons.Middle => _middleButtonDrag,
                   MouseButtons.Right  => _rightButtonDrag,
                   MouseButtons.X1     => _x1ButtonDrag,
                   MouseButtons.X2     => _x2ButtonDrag,
                   _                   => throw new InvalidOperationException($"MouseButton '{button}' not supported."),
               };
    }

    private static void SetButtonDragState(MouseButtons button, ButtonDragState state)
    {
        switch (button)
        {
            case MouseButtons.None: break;

            case MouseButtons.Left:
                _leftButtonDrag = state;
                break;

            case MouseButtons.Middle:
                _middleButtonDrag = state;
                break;

            case MouseButtons.Right:
                _rightButtonDrag = state;
                break;

            case MouseButtons.X1:
                _x1ButtonDrag = state;
                break;

            case MouseButtons.X2:
                _x2ButtonDrag = state;
                break;

            default: throw new InvalidOperationException($"MouseButton '{button}' not supported.");
        }
    }

    private static MouseSnapshot GetSnapshot()
    {
        return default(MouseSnapshot);
    }

    #endregion
}
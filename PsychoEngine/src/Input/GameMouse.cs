using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Utils;

namespace PsychoEngine.Input;

public static class GameMouse
{ 
    // Constants.
    private const float WheelDeltaUnit = 120f;

    // Events.
    public delegate void ButtonEventHandler(object?   sender, MouseButtonEventArgs   args);
    public delegate void MovedEventHandler(object?    sender, MouseMovedEventArgs    args);
    public delegate void ScrolledEventHandler(object? sender, MouseScrolledEventArgs args);
    public delegate void DraggedEventHandler(object?  sender, MouseDraggedEventArgs  args);

    public static event ButtonEventHandler? OnButtonDown;
    public static event ButtonEventHandler? OnButtonPressed;
    public static event ButtonEventHandler? OnButtonReleased;

    public static event MovedEventHandler?    OnMoved;
    public static event ScrolledEventHandler? OnScrolled;

    public static event DraggedEventHandler? OnDragStarted;
    public static event DraggedEventHandler? OnDragging;
    public static event DraggedEventHandler? OnDragReleased;

    // Constants.
    public static readonly MouseButtons[] AllButtons;

    // Config.
    private const  int                     DragThreshold            = 5;
    private static FocusLostInputBehaviour _focusLostInputBehaviour = FocusLostInputBehaviour.ClearState;

    // Mouse states.
    private static MouseState _previousState;
    private static MouseState _currentState;

    // Button states.
    private static InputStates _leftButton;
    private static InputStates _middleButton;
    private static InputStates _rightButton;
    private static InputStates _x1Button;
    private static InputStates _x2Button;

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
    }

    #region ImGui

    private const  int          LogCapacity = 1000;
    private static int          _frame;
    private static bool         _ignoreDownEvent;
    private static bool         _ignoreMovedEvent;
    private static bool         _ignoreDraggingEvent;
    private static List<string> _eventLog = new(LogCapacity);

    private static void ImGuiLog(string message)
    {
        _eventLog.Add(message);

        if (_eventLog.Count >= LogCapacity)
        {
            _eventLog.RemoveAt(0);
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
            ImGui.BeginTable("Drag", 3, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("LastPress");
            ImGui.TableSetupColumn("Dragging");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                ButtonDragState dragState = GetButtonDragState(button);
                Point           lastPress = dragState.LastPressPosition;
                bool            dragging  = dragState.Dragging;

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
            }

            ImGui.EndTable();
        }

        bool eventLogHeader = ImGui.CollapsingHeader("Events log");

        if (eventLogHeader)
        {
            ImGui.Checkbox("Ignore down event",     ref _ignoreDownEvent);
            ImGui.Checkbox("Ignore move event",     ref _ignoreMovedEvent);
            ImGui.Checkbox("Ignore dragging event", ref _ignoreDraggingEvent);

            bool clearPressed = ImGui.Button("Clear");
            if (clearPressed) _eventLog.Clear();

            ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle);

            foreach (string message in _eventLog)
            {
                ImGui.Text(message);
            }

            ImGui.SetScrollHereY();

            ImGui.EndChild();
        }

        ImGui.End();
    }

    #endregion

    public static void Update(Game game)
    {
        UpdateMouseStates(game);

        /* TODO: Last input time detection */

        // Position.
        PreviousPosition = new Point(_previousState.X, _previousState.Y);
        Position         = new Point(_currentState.X,  _currentState.Y);
        PositionDelta    = Position - PreviousPosition;
        Moved            = PositionDelta != Point.Zero;

        // Scroll.
        PreviousScrollValue = _previousState.ScrollWheelValue / WheelDeltaUnit;
        ScrollValue         = _currentState.ScrollWheelValue  / WheelDeltaUnit;
        ScrollDelta         = ScrollValue - PreviousScrollValue;
        Scrolled            = ScrollDelta != 0f;

        // Dragging.
        foreach (MouseButtons button in AllButtons)
        {
            UpdateButtonInputState(button);
            UpdateButtonDragState(button);
        }

        if (Moved)
        {
            OnMoved?.Invoke(null,
                            new MouseMovedEventArgs(PreviousPosition, Position, PositionDelta, GetSnapshot()));
        }

        if (Scrolled)
        {
            OnScrolled?.Invoke(null, new MouseScrolledEventArgs(ScrollValue, ScrollDelta, GetSnapshot()));
        }
    }

    public static InputStates GetButton(MouseButtons button)
    {
        return GetButtonState(button);
    }

    public static bool IsButtonUp(MouseButtons button)
    {
        return GetButtonState(button).HasFlag(InputStates.Up);
    }

    public static bool IsButtonDown(MouseButtons button)
    {
        return GetButtonState(button).HasFlag(InputStates.Down);
    }

    public static bool WasButtonPressed(MouseButtons button)
    {
        return GetButtonState(button).HasFlag(InputStates.Pressed);
    }

    public static bool WasButtonReleased(MouseButtons button)
    {
        return GetButtonState(button).HasFlag(InputStates.Released);
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

    private static void UpdateButtonInputState(MouseButtons button)
    {
        ButtonState previousState = _previousState.GetButton(button);
        ButtonState currentState  = _currentState.GetButton(button);

        InputStates inputState = InputStates.None;

        switch (currentState)
        {
            case ButtonState.Pressed:
            {
                // Button is currently pressed.
                inputState |= InputStates.Down;
                OnButtonDown?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));

                if (previousState == ButtonState.Released)
                {
                    // Button was just pressed.
                    inputState |= InputStates.Pressed;
                    OnButtonPressed?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
                }

                break;
            }

            case ButtonState.Released:
                // Button is currently not pressed.
                inputState |= InputStates.Up;

                if (previousState == ButtonState.Pressed)
                {
                    // Button was just released.
                    inputState |= InputStates.Released;
                    OnButtonReleased?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
                }

                break;

            default: throw new InvalidOperationException($"InputState '{currentState}' not supported.");
        }

        SetButtonStateInternal(button, inputState);
    }

    private static void UpdateButtonDragState(MouseButtons button)
    {
        ButtonDragState buttonDragState = GetButtonDragState(button);

        Point lastPressPosition = buttonDragState.LastPressPosition;
        bool  dragging          = buttonDragState.Dragging;

        if (WasButtonPressed(button))
        {
            lastPressPosition = Position;
        }

        if (WasButtonReleased(button) && dragging)
        {
            // Button stopped dragging.
            dragging = false;
            OnDragReleased?.Invoke(null, new MouseDraggedEventArgs(button, lastPressPosition, GetSnapshot()));
        }

        if (IsButtonDown(button) && !dragging)
        {
            float distance = lastPressPosition.Distance(Position);

            if (distance > DragThreshold)
            {
                // Button began dragging.
                dragging = true;
                OnDragStarted?.Invoke(null, new MouseDraggedEventArgs(button, lastPressPosition, GetSnapshot()));
            }
        }

        if (dragging)
        {
            OnDragging?.Invoke(null, new MouseDraggedEventArgs(button, lastPressPosition, GetSnapshot()));
        }

        SetButtonDragState(button, new ButtonDragState(lastPressPosition, dragging));
    }

    private static InputStates GetButtonState(MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => InputStates.None,
                   MouseButtons.Left   => _leftButton,
                   MouseButtons.Middle => _middleButton,
                   MouseButtons.Right  => _rightButton,
                   MouseButtons.X1     => _x1Button,
                   MouseButtons.X2     => _x2Button,
                   _                   => throw new InvalidOperationException($"MouseButton '{button}' not supported."),
               };
    }

    private static void SetButtonStateInternal(MouseButtons button, InputStates state)
    {
        switch (button)
        {
            case MouseButtons.None: break;

            case MouseButtons.Left:
                _leftButton = state;
                break;

            case MouseButtons.Middle:
                _middleButton = state;
                break;

            case MouseButtons.Right:
                _rightButton = state;
                break;

            case MouseButtons.X1:
                _x1Button = state;
                break;

            case MouseButtons.X2:
                _x2Button = state;
                break;

            default: throw new InvalidOperationException($"MouseButton '{button}' not supported.");
        }
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
}
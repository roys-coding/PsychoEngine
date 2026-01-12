using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Utilities;

namespace PsychoEngine.Input;

public static partial class PyMouse
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

        InitializeImGui();
    }

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
            IsDragging : true, PreviousIsDragging: false,
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
            IsDragging : false, PreviousIsDragging: true,
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

        return WasButtonPressed(button) &&
               buttonState.ConsecutiveClicks > 0 &&
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
        Position         = new Point(_currentState.X, _currentState.Y);
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
        ScrollValue         = _currentState.ScrollWheelValue / WheelDeltaUnit;
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
                throw new NotSupportedException($"FocusLostInputBehaviour '{_focusLostInputBehaviour}' not supported.");
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

        if (receivedAnyInput)
        {
            LastInputTime = PyGameTimes.Update.TotalGameTime;
        }
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
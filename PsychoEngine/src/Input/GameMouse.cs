using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static class GameMouse
{
    // Events.
    public delegate void MouseButtonEventHandler(object?   sender, MouseButtonEventArgs args);
    public delegate void MouseMovedEventHandler(object?    sender, MouseEventArgs       args);
    public delegate void MouseScrolledEventHandler(object? sender, MouseEventArgs       args);

    public static event MouseButtonEventHandler? OnMouseButtonDown;
    public static event MouseButtonEventHandler? OnMouseButtonPressed;
    public static event MouseButtonEventHandler? OnMouseButtonReleased;

    public static event MouseMovedEventHandler?    OnMouseMoved;
    public static event MouseScrolledEventHandler? OnMouseScrolled;

    // Constants.
    private const          float          WheelDeltaUnit = 120f;
    public static readonly MouseButtons[] AllButtons;

    // Config.
    private static FocusLostInputBehaviour _focusLostInputBehaviour = FocusLostInputBehaviour.ClearState;

    // Input states.
    private static MouseState _previousState;
    private static MouseState _currentState;

    // Button checks.
    public static ButtonState LeftButton => GetButton(MouseButtons.Left);

    public static ButtonState MiddleButton => GetButton(MouseButtons.Middle);

    public static ButtonState RightButton => GetButton(MouseButtons.Right);

    public static ButtonState X1Button => GetButton(MouseButtons.X1);

    public static ButtonState X2Button => GetButton(MouseButtons.X2);

    // Position.
    public static Point PositionDelta { get; private set; }
    public static Point Position      => new(_currentState.X, _currentState.Y);
    public static bool  HasMoved      => PositionDelta != Point.Zero;

    // Scroll wheel.
    public static float ScrollDelta { get; private set; }
    public static float ScrollValue => _currentState.ScrollWheelValue / WheelDeltaUnit;
    public static bool  HasScrolled => ScrollDelta != 0f;

    static GameMouse()
    {
        AllButtons = Enum.GetValues<MouseButtons>();

        CoreEngine.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;
    }

    private static void ImGuiOnLayout(object? sender, EventArgs args)
    {
        bool windowOpen = ImGui.Begin($"{Fonts.Lucide.Mouse} Mouse");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        int  focusLost                                 = (int)_focusLostInputBehaviour;
        bool focusLostChanged                          = ImGui.DragInt("FocusLost", ref focusLost, 0, 2);
        if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;

        ImGui.Text($"Pos: {Position}");
        ImGui.Text($"PosDelta: {PositionDelta}");
        ImGui.Text($"HasMoved: {HasMoved}");
        ImGui.Separator();
        ImGui.Text($"Scroll: {ScrollValue}");
        ImGui.Text($"ScrollDelta: {ScrollDelta}");
        ImGui.Text($"HasScrolled: {HasScrolled}");
        ImGui.Separator();

        foreach (MouseButtons button in AllButtons)
        {
            ButtonState state        = GetButtonInternal(_currentState, button);
            string      buttonString = $"{button}: {state}";

            if (WasButtonPressed(button)) buttonString  += " Pressed";
            if (WasButtonReleased(button)) buttonString += " Released";

            switch (state)
            {
                case ButtonState.Pressed:
                    ImGui.Text(buttonString);
                    break;

                case ButtonState.Released:
                    ImGui.TextDisabled(buttonString);
                    break;
            }
        }

        ImGui.End();
    }

    public static void Update(Game game, GameTime gameTime)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureMouse)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = Mouse.GetState();
        }
        else
        {
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

        // Handle position.
        Point previousPosition = new(_previousState.X, _previousState.Y);
        Point currentPosition  = new(_currentState.X, _currentState.Y);
        PositionDelta = currentPosition - previousPosition;

        if (HasMoved)
        {
            OnMouseMoved?.Invoke(null, new MouseEventArgs(GetSnapshot()));
        }

        // Handle scroll wheel.
        int previousScroll = _previousState.ScrollWheelValue;
        int currentScroll  = _currentState.ScrollWheelValue;
        ScrollDelta = (currentScroll - previousScroll) / WheelDeltaUnit;

        if (HasScrolled)
        {
            OnMouseScrolled?.Invoke(null, new MouseEventArgs(GetSnapshot()));
        }

        // Handle buttons.
        foreach (MouseButtons button in AllButtons)
        {
            if (IsButtonDown(button))
            {
                OnMouseButtonDown?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }

            if (WasButtonPressed(button))
            {
                OnMouseButtonPressed?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }

            if (WasButtonReleased(button))
            {
                OnMouseButtonReleased?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }
        }
    }

    public static GameMouseState GetSnapshot()
    {
        return new GameMouseState(LeftButton,
                                  MiddleButton,
                                  RightButton,
                                  X1Button,
                                  X2Button,
                                  WasButtonPressed(MouseButtons.Left),
                                  WasButtonPressed(MouseButtons.Middle),
                                  WasButtonPressed(MouseButtons.Right),
                                  WasButtonPressed(MouseButtons.X1),
                                  WasButtonPressed(MouseButtons.X2),
                                  WasButtonReleased(MouseButtons.Left),
                                  WasButtonReleased(MouseButtons.Middle),
                                  WasButtonReleased(MouseButtons.Right),
                                  WasButtonReleased(MouseButtons.X1),
                                  WasButtonReleased(MouseButtons.X2),
                                  Position,
                                  PositionDelta,
                                  HasMoved,
                                  ScrollValue,
                                  ScrollDelta,
                                  HasScrolled);
    }

    public static ButtonState GetButton(MouseButtons button)
    {
        return GetButtonInternal(_currentState, button);
    }

    public static bool IsButtonUp(MouseButtons button)
    {
        return GetButtonInternal(_currentState, button) == ButtonState.Released;
    }

    public static bool IsButtonDown(MouseButtons button)
    {
        return GetButtonInternal(_currentState, button) == ButtonState.Pressed;
    }

    public static bool WasButtonPressed(MouseButtons button)
    {
        ButtonState previousState = GetButtonInternal(_previousState, button);
        ButtonState currentState  = GetButtonInternal(_currentState,  button);

        return previousState == ButtonState.Released && currentState == ButtonState.Pressed;
    }

    public static bool WasButtonReleased(MouseButtons button)
    {
        ButtonState previousState = GetButtonInternal(_previousState, button);
        ButtonState currentState  = GetButtonInternal(_currentState,  button);

        return previousState == ButtonState.Pressed && currentState == ButtonState.Released;
    }

    private static ButtonState GetButtonInternal(MouseState state, MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => ButtonState.Released,
                   MouseButtons.Left   => state.LeftButton,
                   MouseButtons.Middle => state.MiddleButton,
                   MouseButtons.Right  => state.RightButton,
                   MouseButtons.X1     => state.XButton1,
                   MouseButtons.X2     => state.XButton2,
                   _                   => throw new InvalidOperationException($"MouseButton '{button}' not supported."),
               };
    }
}
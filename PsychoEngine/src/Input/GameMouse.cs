using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static class GameMouse
{
    // Events.
    public delegate void MouseButtonEventHandler(object? sender, MouseButtonEventArgs args);
    public delegate void MouseEventHandler(object?       sender, MouseEventArgs       args);

    public static event MouseButtonEventHandler? OnMouseButtonDown;
    public static event MouseButtonEventHandler? OnMouseButtonPressed;
    public static event MouseButtonEventHandler? OnMouseButtonReleased;

    // TODO: Add specific event arguments for each event.
    
    public static event MouseEventHandler?    OnMouseMoved;
    public static event MouseEventHandler? OnMouseScrolled;

    // Constants.
    public static readonly MouseButtons[] AllButtons;

    // Config.
    private static FocusLostInputBehaviour _focusLostInputBehaviour = FocusLostInputBehaviour.ClearState;

    // Input states.
    private static MouseState    _previousState;
    private static MouseState    _currentState;
    private static MouseSnapshot _currentSnapshot;

    // Button checks.
    public static ButtonState LeftButton   => _currentSnapshot.GetButton(MouseButtons.Left);
    public static ButtonState MiddleButton => _currentSnapshot.GetButton(MouseButtons.Middle);
    public static ButtonState RightButton  => _currentSnapshot.GetButton(MouseButtons.Right);
    public static ButtonState X1Button     => _currentSnapshot.GetButton(MouseButtons.X1);
    public static ButtonState X2Button     => _currentSnapshot.GetButton(MouseButtons.X2);

    // Position.
    public static Point PreviousPosition => _currentSnapshot.PreviousPosition;
    public static Point Position         => _currentSnapshot.Position;
    public static Point PositionDelta    => _currentSnapshot.PositionDelta;
    public static bool  HasMoved         => _currentSnapshot.HasMoved;

    // Scroll wheel.
    public static float PreviousScrollValue => _currentSnapshot.PreviousScrollValue;
    public static float ScrollValue         => _currentSnapshot.ScrollValue;
    public static float ScrollDelta         => _currentSnapshot.ScrollDelta;
    public static bool  HasScrolled         => _currentSnapshot.HasScrolled;

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

        bool movementHeader = ImGui.CollapsingHeader("Movement");

        if (movementHeader)
        {
            ImGui.Text($"Position: {Position}");
            ImGui.Text($"PrevPos: {PreviousPosition}");
            ImGui.Text($"PosDelta: {PositionDelta}");
            ImGui.Text($"HasMoved: {HasMoved}");
        }
        
        bool scrollHeader = ImGui.CollapsingHeader("Scroll");

        if (scrollHeader)
        {
            ImGui.Text($"Scroll: {ScrollValue}");
            ImGui.Text($"PrevScroll: {PreviousScrollValue}");
            ImGui.Text($"ScrollDelta: {ScrollDelta}");
            ImGui.Text($"HasScrolled: {HasScrolled}");
        }

        bool buttonsHeader = ImGui.CollapsingHeader("Buttons");

        if (buttonsHeader)
        {
            ImGui.BeginTable("Buttons", 4);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("State");
            ImGui.TableSetupColumn("Pressed");
            ImGui.TableSetupColumn("Released");
            ImGui.TableHeadersRow();
            
            foreach (MouseButtons button in AllButtons)
            {
                ButtonState state = GetButton(button);
                bool pressed      = WasButtonPressed(button);
                bool released     = WasButtonReleased(button);
            
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (state == ButtonState.Pressed)
                {
                    ImGui.Text(button.ToString());
                }
                else
                {
                    ImGui.TextDisabled(button.ToString());
                }
                
                ImGui.TableNextColumn();
                
                if (state == ButtonState.Pressed)
                {
                    ImGui.Text(state.ToString());
                }
                else
                {
                    ImGui.TextDisabled(state.ToString());
                }
                
                ImGui.TableNextColumn();
                
                if (pressed)
                {
                    ImGui.Text(pressed.ToString());
                }
                else
                {
                    ImGui.TextDisabled(pressed.ToString());
                }

                ImGui.TableNextColumn();

                if (released)
                {
                    ImGui.Text(released.ToString());
                }
                else
                {
                    ImGui.TextDisabled(released.ToString());
                }
            }
            ImGui.EndTable();
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

        _currentSnapshot = new MouseSnapshot(_previousState, _currentState);

        /* TODO: Last input time detection */

        if (HasMoved)
        {
            OnMouseMoved?.Invoke(null, new MouseEventArgs(_currentSnapshot));
        }

        if (HasScrolled)
        {
            OnMouseScrolled?.Invoke(null, new MouseEventArgs(_currentSnapshot));
        }

        // Handle buttons.
        foreach (MouseButtons button in AllButtons)
        {
            if (IsButtonDown(button))
            {
                OnMouseButtonDown?.Invoke(null, new MouseButtonEventArgs(button, _currentSnapshot));
            }

            if (WasButtonPressed(button))
            {
                OnMouseButtonPressed?.Invoke(null, new MouseButtonEventArgs(button, _currentSnapshot));
            }

            if (WasButtonReleased(button))
            {
                OnMouseButtonReleased?.Invoke(null, new MouseButtonEventArgs(button, _currentSnapshot));
            }
        }
    }

    public static ButtonState GetButton(MouseButtons button)
    {
        return _currentSnapshot.GetButton(button);
    }

    public static bool IsButtonUp(MouseButtons button)
    {
        return _currentSnapshot.IsButtonUp(button);
    }

    public static bool IsButtonDown(MouseButtons button)
    {
        return _currentSnapshot.IsButtonDown(button);
    }

    public static bool WasButtonPressed(MouseButtons button)
    {
        return _currentSnapshot.WasButtonPressed(button);
    }

    public static bool WasButtonReleased(MouseButtons button)
    {
        return _currentSnapshot.WasButtonReleased(button);
    }
}
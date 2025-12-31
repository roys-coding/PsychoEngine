using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static class GameMouse
{
    public class MouseEventArgs(MouseButtons button) : EventArgs
    {
        public MouseButtons Button { get; }
        public Vector2 Position { get; }
    }
    
    public delegate void MouseEventHandler(object? sender, MouseEventArgs args);
    
    public static event MouseEventHandler? OnMouseButtonDown;
    public static event MouseEventHandler? OnMouseButtonPressed;
    public static event MouseEventHandler? OnMouseButtonReleased;
    
    public static readonly MouseButtons[] AllButtons; 
    
    private static MouseState _previousState;
    private static MouseState _currentState;
    
    public static bool LeftButton
    {
        get => IsButtonDown(MouseButtons.Left);
    }

    public static bool MiddleButton
    {
        get => IsButtonDown(MouseButtons.Middle);
    }

    public static bool RightButton
    {
        get => IsButtonDown(MouseButtons.Right);
    }

    public static bool X1Button
    {
        get => IsButtonDown(MouseButtons.X1);
    }

    public static bool X2Button
    {
        get => IsButtonDown(MouseButtons.X2);
    }

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

        // int  focusLost                                 = (int)_focusLostInputBehaviour;
        // bool focusLostChanged                          = ImGui.DragInt("FocusLost", ref focusLost, 0, 1);
        // if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;

        foreach (MouseButtons button in AllButtons)
        {
            ButtonState state     = GetButtonInternal(_currentState, button);
            string   buttonString = $"{button}: {state}";

            if (WasButtonPressed(button)) buttonString  += " Pressed";
            if (WasButtonReleased(button)) buttonString += " Released";

            switch (state)
            {
                case ButtonState.Pressed: ImGui.Text(buttonString); break;
                case ButtonState.Released:   ImGui.TextDisabled(buttonString); break;
            }
        }

        ImGui.End();
    }

    public static void Update(Game game, GameTime gameTime)
    {
        _previousState = _currentState;
        _currentState = Mouse.GetState();

        foreach (MouseButtons button in AllButtons)
        {
            if (IsButtonDown(button)) OnMouseButtonDown?.Invoke(null, new MouseEventArgs(button));
            if (WasButtonPressed(button)) OnMouseButtonPressed?.Invoke(null, new MouseEventArgs(button));
            if (WasButtonReleased(button)) OnMouseButtonReleased?.Invoke(null, new MouseEventArgs(button));
        }
    }

    private static ButtonState GetButton(MouseButtons button)
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
        ButtonState currentState = GetButtonInternal(_currentState, button);
        
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
                   _                   => throw new InvalidOperationException($"MouseButton '{button}' not supported.")
               };
    }
}
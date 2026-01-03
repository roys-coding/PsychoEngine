using Microsoft.Xna.Framework.Input;
using PsychoEngine.Input;

namespace PsychoEngine.Utilities;

public static class XnaExtensionMethods
{
    extension(MouseState mouseState)
    {
        public ButtonState GetButton(MouseButtons button)
        {
            return button switch
                   {
                       MouseButtons.None => ButtonState.Released,
                       MouseButtons.Left => mouseState.LeftButton,
                       MouseButtons.Middle => mouseState.MiddleButton,
                       MouseButtons.Right => mouseState.RightButton,
                       MouseButtons.X1 => mouseState.XButton1,
                       MouseButtons.X2 => mouseState.XButton2,
                       _ => throw new InvalidOperationException($"MouseButton '{button}' not supported."),
                   };
        }
    }
}
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Input;

namespace PsychoEngine.Utilities;

public static class XnaExtensionMethods
{
    extension(MouseState mouseState)
    {
        public ButtonState GetButton(MouseButton button)
        {
            return button switch
                   {
                       MouseButton.None => ButtonState.Released,
                       MouseButton.Left => mouseState.LeftButton,
                       MouseButton.Middle => mouseState.MiddleButton,
                       MouseButton.Right => mouseState.RightButton,
                       MouseButton.X1 => mouseState.XButton1,
                       MouseButton.X2 => mouseState.XButton2,
                       _ => throw new NotSupportedException($"MouseButton '{button}' not supported."),
                   };
        }
    }
}
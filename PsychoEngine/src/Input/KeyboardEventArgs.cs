using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public class KeyboardEventArgs : EventArgs
{
    public Keys Key { get; }

    public KeyboardEventArgs(Keys key)
    {
        Key = key;
    }
}
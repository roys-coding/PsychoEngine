using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public class KeyboardEventArgs : EventArgs
{
    public Keys    Key          { get; }
    public ModKeys ModifierKeys { get; }

    public KeyboardEventArgs(Keys key, ModKeys modifierKeys)
    {
        Key          = key;
        ModifierKeys = modifierKeys;
    }
}
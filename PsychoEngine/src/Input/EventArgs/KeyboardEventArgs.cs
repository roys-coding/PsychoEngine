using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public class KeyboardEventArgs : EventArgs
{
    public Keys         Key          { get; }
    public ModifierKeys ModifierKeys { get; }

    public KeyboardEventArgs(Keys key, ModifierKeys modifierKeys)
    {
        Key          = key;
        ModifierKeys = modifierKeys;
    }
}
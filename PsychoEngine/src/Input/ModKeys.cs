namespace PsychoEngine.Input;

[Flags]
public enum ModKeys
{
    None = 0,
    Control = 1 << 0,
    Shift = 1 << 1,
    Alt = 1 << 2,
    Super = 1 << 3,
}
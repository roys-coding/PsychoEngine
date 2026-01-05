namespace PsychoEngine.Input;

[Flags]
public enum InputStates
{
    None     = 0,
    Up       = 1 << 0,
    Down     = 1 << 1,
    Pressed  = 1 << 2,
    Released = 1 << 3,
}
using Microsoft.Xna.Framework.Graphics;

namespace ImGuiXNA;

public sealed class TextureInfo
{
    public Texture2D Texture   { get; internal set; }
    public bool      IsManaged { get; internal init; }
}
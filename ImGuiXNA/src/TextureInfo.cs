/*
 * File provided by AristurtleDev -> https://github.com/HexaEngine/Hexa.NET.ImGui/tree/4120bcd2cf211ea136e38382916a7ad7764510f4/Examples/ExampleMonoGame
 * Modified by Roy Soriano
 */

using Microsoft.Xna.Framework.Graphics;

namespace ImGuiXNA;

internal sealed class TextureInfo
{
    public Texture2D Texture   { get; internal set; }
    public bool      IsManaged { get; internal init; }
}
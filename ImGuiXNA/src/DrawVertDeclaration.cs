/*
 * File provided by AristurtleDev -> https://github.com/HexaEngine/Hexa.NET.ImGui/tree/4120bcd2cf211ea136e38382916a7ad7764510f4/Examples/ExampleMonoGame
 * Modified by Roy Soriano
 */

#region

using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace ImGuiXNA;

internal static class DrawVertDeclaration
{
    public static readonly VertexDeclaration Declaration;
    public static readonly int               Size;

    static unsafe DrawVertDeclaration()
    {
        Size = sizeof(ImDrawVert);

        VertexElement position = new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0);
        VertexElement uv       = new(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0);
        VertexElement color    = new(16, VertexElementFormat.Color, VertexElementUsage.Color, 0);

        Declaration = new VertexDeclaration(Size, position, uv, color);
    }
}
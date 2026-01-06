#define FNA

using System.Runtime.InteropServices;
using Hexa.NET.Utilities;
using Microsoft.Xna.Framework.Graphics;

namespace ImGuiXNA;

public sealed class ImGuiXnaRenderer : IDisposable
{
    private static class DrawVertDeclaration
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

    private struct TextureInfo
    {
        public Texture2D Texture   { get; internal set; }
        public bool      IsManaged { get; internal init; }
    }

    // Xna game.
    private readonly Game           _game;
    private          GraphicsDevice _graphicsDevice;

    // Graphics resources.
    private readonly RasterizerState _rasterizerState;
    private          BasicEffect     _effect;

    // Vertex buffer.
    private byte[]       _vertexData;
    private VertexBuffer _vertexBuffer;
    private int          _vertexBufferSize;

    // Index buffer.
    private byte[]      _indexData;
    private IndexBuffer _indexBuffer;
    private int         _indexBufferSize;

    // Textures.
    private readonly Dictionary<ImTextureID, TextureInfo> _textures;
    private          int                                  _nextTexId;

    #region Life cycle

    public ImGuiXnaRenderer(Game game)
    {
        _game      = game;
        _textures  = new Dictionary<ImTextureID, TextureInfo>();
        _nextTexId = 1;

        _rasterizerState = new RasterizerState
                           {
                               CullMode             = CullMode.None,
                               DepthBias            = 0,
                               FillMode             = FillMode.Solid,
                               MultiSampleAntiAlias = false,
                               ScissorTestEnable    = true,
                               SlopeScaleDepthBias  = 0,
                           };
    }

    public void Dispose()
    {
        // Dispose of graphics resources.
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _effect?.Dispose();

        // Clean up managed textures.
        foreach (TextureInfo textureInfo in _textures.Values)
        {
            if (textureInfo.IsManaged)
            {
                textureInfo.Texture?.Dispose();
            }
        }
    }

    public unsafe void Initialize(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;

        ImGuiIOPtr io = ImGui.GetIO();

#if FNA
        io.BackendRendererName = "imgui_impl_fna".ToUTF8Ptr();
#endif
#if MONOGAME
        io.BackendRendererName = "imgui_impl_monogame".ToUTF8Ptr();
#endif

        // Create basic effect used to render ImGui.
        _effect = new BasicEffect(_graphicsDevice)
                  {
                      World = Matrix.Identity, View = Matrix.Identity, TextureEnabled = true, VertexColorEnabled = true,
                  };
    }

    #endregion

    #region Rendering

    public void Render()
    {
        ImGui.Render();

        ImDrawDataPtr drawData = ImGui.GetDrawData();
        ProcessTextureUpdates(drawData);
        RenderDrawData(drawData);
    }

    #endregion

    #region Texture binding

    public unsafe ImTextureRef BindTexture(Texture2D texture)
    {
        IntPtr texId = new(_nextTexId++);

        TextureInfo textureInfo = new()
                                  {
                                      Texture = texture, IsManaged = false,
                                  };

        _textures[texId] = textureInfo;

        return new ImTextureRef(null, texId);
    }

    public void UnbindTexture(ImTextureRef textureRef)
    {
        TextureInfo textureInfo;

        if (!_textures.TryGetValue(textureRef.TexID, out textureInfo)) return;

        if (textureInfo.IsManaged)
        {
            textureInfo.Texture?.Dispose();
        }

        _textures.Remove(textureRef.TexID);
    }

    #endregion

    #region Textures

    private void UpdateTexture(ImTextureDataPtr textureData)
    {
        switch (textureData.Status)
        {
            case ImTextureStatus.WantCreate:
                CreateTexture(textureData);
                break;

            case ImTextureStatus.WantUpdates:
                UpdateTextureData(textureData);
                break;

            case ImTextureStatus.WantDestroy:
                if (textureData.UnusedFrames > 0)
                {
                    DestroyTexture(textureData);
                }

                break;

            case ImTextureStatus.Destroyed:
            case ImTextureStatus.Ok:
            default:
                // Do nothing.
                break;
        }

        if (textureData.WantDestroyNextFrame)
        {
            DestroyTexture(textureData);
        }
    }

    private unsafe void CreateTexture(ImTextureDataPtr textureData)
    {
        SurfaceFormat format = textureData.Format switch
                               {
                                   ImTextureFormat.Rgba32 => SurfaceFormat.Color,
                                   ImTextureFormat.Alpha8 => SurfaceFormat.Alpha8,
                                   _ => throw new NotSupportedException("Texture format not supported."),
                               };

        Texture2D texture = new(_graphicsDevice, textureData.Width, textureData.Height, false, format);
        texture.Tag = "ImGui_Texture";

        texture.Name =
            $"Size: {textureData.Width}x{textureData.Height}, Format: {format}, UUID: {textureData.UniqueID}";

        if (textureData.Pixels != null)
        {
            int dataSize = textureData.GetSizeInBytes();

            byte[] managedData = new byte[dataSize];
            Marshal.Copy(new IntPtr(textureData.Pixels), managedData, 0, dataSize);
            texture.SetData(managedData);
        }

        TextureInfo textureInfo = new()
                                  {
                                      Texture = texture, IsManaged = true,
                                  };

        _textures[textureData.TexID] = textureInfo;
        textureData.SetStatus(ImTextureStatus.Ok);
    }

    private void DestroyTexture(ImTextureDataPtr textureData)
    {
        IntPtr      texId = textureData.GetTexID();
        TextureInfo textureInfo;

        if (!_textures.TryGetValue(texId, out textureInfo)) return;

        Console.WriteLine("Destroyed ImGui texture: " + textureInfo.Texture.Name);

        if (textureInfo.IsManaged)
        {
            textureInfo.Texture?.Dispose();
        }

        textureData.SetTexID(ImTextureID.Null);
        textureData.SetStatus(ImTextureStatus.Destroyed);
    }

    private unsafe void UpdateTextureData(ImTextureDataPtr textureData)
    {
        IntPtr texId = textureData.GetTexID();

        TextureInfo textureInfo;

        if (!_textures.TryGetValue(texId, out textureInfo)) return;

        Texture2D texture = textureInfo.Texture;

        // Check if the texture's dimensions or format have changed.
        SurfaceFormat newFormat =
            textureData.Format == ImTextureFormat.Rgba32 ? SurfaceFormat.Color : SurfaceFormat.Alpha8;

        if (texture.Width != textureData.Width || texture.Height != textureData.Height || texture.Format != newFormat)
        {
            texture.Dispose();
            texture = new Texture2D(_graphicsDevice, textureData.Width, textureData.Height, false, newFormat);
            textureInfo.Texture = texture;
        }

        // TODO: Look into doing only partial updates with textureData.Updates
        //       for now, just doing a full copy
        if (textureData.Pixels != null)
        {
            int pixelCount    = textureData.Width * textureData.Height;
            int bytesPerPixel = textureData.Format == ImTextureFormat.Rgba32 ? 4 : 1;
            int dataSize      = pixelCount * bytesPerPixel;

            byte[] managedData = new byte[dataSize];
            Marshal.Copy(new IntPtr(textureData.Pixels), managedData, 0, dataSize);
            texture.SetData(managedData);
        }

        textureData.SetStatus(ImTextureStatus.Ok);
    }

    #endregion

    #region Rendering internals

    private unsafe void ProcessTextureUpdates(ImDrawDataPtr drawData)
    {
        if (drawData.Textures.Data == null) return;

        for (int i = 0; i < drawData.Textures.Size; i++)
        {
            ImTextureDataPtr textureData = drawData.Textures.Data[i];
            UpdateTexture(textureData);
        }
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        // Cache states so they can be restored when we're done.
        Viewport          lastViewport     = _graphicsDevice.Viewport;
        Rectangle         lastScissorBox   = _graphicsDevice.ScissorRectangle;
        RasterizerState   lastRasterizer   = _graphicsDevice.RasterizerState;
        DepthStencilState lastDepthStencil = _graphicsDevice.DepthStencilState;
        Color             lastBlendFactor  = _graphicsDevice.BlendFactor;
        BlendState        lastBlendState   = _graphicsDevice.BlendState;
        SamplerState      lastSamplerState = _graphicsDevice.SamplerStates[0];

        // Setup render state:
        // - alpha-blending enabled.
        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState  = BlendState.NonPremultiplied;

        // - No face culling.
        // - Scissor testing enabled.
        _graphicsDevice.RasterizerState = _rasterizerState;

        // - Depth read-only (testing enabled, writes disabled).
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        // - Linear filtering for textures.
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays).
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        // Setup projection
        _graphicsDevice.Viewport = new Viewport(0,
                                                0,
                                                _graphicsDevice.PresentationParameters.BackBufferWidth,
                                                _graphicsDevice.PresentationParameters.BackBufferHeight);

        ImGuiIOPtr io = ImGui.GetIO();

        // Set up effect projection.
        _effect.Projection = Matrix.CreateOrthographicOffCenter(0.0f,
                                                                io.DisplaySize.X,
                                                                io.DisplaySize.Y,
                                                                0.0f,
                                                                -1.0f,
                                                                1.0f);

        UpdateBuffers(drawData);
        RenderCommandLists(drawData);

        // Restore modified state.
        _graphicsDevice.Viewport          = lastViewport;
        _graphicsDevice.ScissorRectangle  = lastScissorBox;
        _graphicsDevice.RasterizerState   = lastRasterizer;
        _graphicsDevice.DepthStencilState = lastDepthStencil;
        _graphicsDevice.BlendState        = lastBlendState;
        _graphicsDevice.BlendFactor       = lastBlendFactor;
        _graphicsDevice.SamplerStates[0]  = lastSamplerState;
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0) return;

        // Expand buffers if we need more room.
        if (drawData.TotalVtxCount > _vertexBufferSize)
        {
            _vertexBuffer?.Dispose();

            _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);

            _vertexBuffer = new VertexBuffer(_graphicsDevice,
                                             DrawVertDeclaration.Declaration,
                                             _vertexBufferSize,
                                             BufferUsage.None);

            _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
        }

        if (drawData.TotalIdxCount > _indexBufferSize)
        {
            _indexBuffer?.Dispose();

            _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);

            _indexBuffer = new IndexBuffer(_graphicsDevice,
                                           IndexElementSize.SixteenBits,
                                           _indexBufferSize,
                                           BufferUsage.None);

            _indexData = new byte[_indexBufferSize * sizeof(ushort)];
        }

        // Copy ImGui's vertices and indices to a set of managed byte arrays.
        int vertexOffset = 0;
        int indexOffset  = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists.Data[n];

            fixed (void* vtxDstPtr = &_vertexData[vertexOffset * DrawVertDeclaration.Size])
            fixed (void* idxDstPtr = &_indexData[indexOffset * sizeof(ushort)])
            {
                Buffer.MemoryCopy(cmdList.VtxBuffer.Data,
                                  vtxDstPtr,
                                  _vertexData.Length,
                                  cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);

                Buffer.MemoryCopy(cmdList.IdxBuffer.Data,
                                  idxDstPtr,
                                  _indexData.Length,
                                  cmdList.IdxBuffer.Size * sizeof(ushort));
            }

            vertexOffset += cmdList.VtxBuffer.Size;
            indexOffset  += cmdList.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the gpu vertex- and index buffers.
        _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
        _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount   * sizeof(ushort));
    }

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        int vertexOffset = 0;
        int indexOffset  = 0;

        for (int listIndex = 0; listIndex < drawData.CmdListsCount; listIndex++)
        {
            ImDrawListPtr commandList = drawData.CmdLists.Data[listIndex];

            for (int commandIndex = 0; commandIndex < commandList.CmdBuffer.Size; commandIndex++)
            {
                ImDrawCmdPtr drawCommand = &commandList.CmdBuffer.Data[commandIndex];

                if (drawCommand.ElemCount == 0) continue;

                // In v1.92, we need to handle ImTextureRef instead of ImTextureID
                ImTextureRef textureRef = drawCommand.TexRef;
                ImTextureID  textureId  = textureRef.GetTexID();

                TextureInfo textureInfo;

                if (!_textures.TryGetValue(textureId, out textureInfo))
                {
                    throw new
                        InvalidOperationException($"Could not find a texture with id '{textureId}', please check your bindings!");
                }

                _graphicsDevice.ScissorRectangle = new Rectangle((int)drawCommand.ClipRect.X,
                                                                 (int)drawCommand.ClipRect.Y,
                                                                 (int)(drawCommand.ClipRect.Z - drawCommand.ClipRect.X),
                                                                 (int)(drawCommand.ClipRect.W -
                                                                       drawCommand.ClipRect.Y));

                _effect.Texture = textureInfo.Texture;

                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                          (int)drawCommand.VtxOffset + vertexOffset,
                                                          0,
                                                          commandList.VtxBuffer.Size,
                                                          (int)drawCommand.IdxOffset + indexOffset,
                                                          (int)drawCommand.ElemCount / 3);
                }
            }

            vertexOffset += commandList.VtxBuffer.Size;
            indexOffset  += commandList.IdxBuffer.Size;
        }
    }

    #endregion
}
/*
 * File provided by AristurtleDev . https://github.com/HexaEngine/Hexa.NET.ImGui/tree/4120bcd2cf211ea136e38382916a7ad7764510f4/Examples/ExampleMonoGame
 * Modified by Roy Soriano
 */

#region

using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

#endregion

namespace ImGuiXNA;

public sealed class ImGuiRenderer
{
    private const float WheelDelta = 120;

    private readonly Game  _game;
    private          float _lastDeltaTime;

    // Graphics resources.
    private readonly GraphicsDevice  _graphicsDevice;
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
    private          int                                  _nextTexId = 1;

    // Input.
    private readonly Keys[] _allKeys = Enum.GetValues<Keys>();
#if MONOGAME
    private int _scrollWheelValueX;
#endif
    private int _scrollWheelValueY;

    public ImGuiRenderer(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        _game           = game;
        _graphicsDevice = game.GraphicsDevice;
        _textures       = new Dictionary<ImTextureID, TextureInfo>();

        _rasterizerState = new RasterizerState
                           {
                               CullMode             = CullMode.None,
                               DepthBias            = 0,
                               FillMode             = FillMode.Solid,
                               MultiSampleAntiAlias = false,
                               ScissorTestEnable    = true,
                               SlopeScaleDepthBias  = 0,
                           };

        _effect = CreateEffect(_graphicsDevice);
    }

    #region Life cycle

    public void Initialize()
    {
        ImGuiContextPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        SetupBackend();
        SetupInput();

        ImGui.GetIO().AddFocusEvent(_game.IsActive);

        // Register focus events.
        _game.Activated += (s, a) =>
                           {
                               ImGui.GetIO().AddFocusEvent(true);
                           };

        _game.Deactivated += (s, a) =>
                             {
                                 ImGui.GetIO().AddFocusEvent(false);
                             };

        _game.Window.ClientSizeChanged += (s, a) =>
                                          {
                                              ImGuiIOPtr io = ImGui.GetIO();

                                              _effect.Projection =
                                                  Matrix.CreateOrthographicOffCenter(0.0f,
                                                      io.DisplaySize.X,
                                                      io.DisplaySize.Y,
                                                      0.0f,
                                                      -1.0f,
                                                      1.0f);
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
            if (!textureInfo.IsManaged) continue;

            textureInfo.Texture?.Dispose();
        }

        _textures.Clear();

        ImGui.DestroyContext();
    }

    #endregion

    #region Rendering & updating

    public void NewFrame(GameTime gameTime)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Delta time must be positive.
        if (deltaTime <= 0f) deltaTime = _lastDeltaTime;

        _lastDeltaTime = deltaTime;
        io.DeltaTime   = deltaTime;

        // Update display size and scale.
        PresentationParameters parameters       = _graphicsDevice.PresentationParameters;
        int                    backBufferWidth  = _graphicsDevice.PresentationParameters.BackBufferWidth;
        int                    backBufferHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;
        io.DisplaySize             = new Vector2(backBufferWidth, backBufferHeight);
        io.DisplayFramebufferScale = Vector2.One;

        UpdateInput();

        ImGui.NewFrame();
    }

    public void Render()
    {
        ImGui.Render();

        if (_effect == null || _effect.IsDisposed)
        {
            _effect = CreateEffect(_graphicsDevice);
        }

        ImDrawDataPtr drawData = ImGui.GetDrawData();
        ProcessTextureUpdates(drawData);
        RenderDrawData(drawData);
    }

    private void UpdateInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        if (io.AppFocusLost)
        {
            return;
        }

        MouseState    mouse    = Mouse.GetState();
        KeyboardState keyboard = Keyboard.GetState();

        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.LeftButton   == ButtonState.Pressed);
        io.AddMouseButtonEvent(1, mouse.RightButton  == ButtonState.Pressed);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(3, mouse.XButton1     == ButtonState.Pressed);
        io.AddMouseButtonEvent(4, mouse.XButton2     == ButtonState.Pressed);

#if MONOGAME
        float mouseWheelX = (mouse.HorizontalScrollWheelValue - _scrollWheelValueX) / WheelDelta;
        float mouseWheelY = (mouse.ScrollWheelValue           - _scrollWheelValueY) / WheelDelta;
        io.AddMouseWheelEvent(mouseWheelX, mouseWheelY);
#endif

#if FNA
        float mouseWheelY = (mouse.ScrollWheelValue - _scrollWheelValueY) / WheelDelta;
        io.AddMouseWheelEvent(0f, mouseWheelY);
#endif

        _scrollWheelValueY = mouse.ScrollWheelValue;

        foreach (Keys key in _allKeys)
        {
            ImGuiKey imguiKey;

            if (!TryMapKeys(key, out imguiKey)) continue;

            io.AddKeyEvent(imguiKey, keyboard.IsKeyDown(key));
        }
    }

    private static bool TryMapKeys(Keys key, out ImGuiKey imguiKey)
    {
        // Special case not handled in the switch..
        // If the actual key we put in is "None", return none and true;
        // otherwise, return none and false.
        if (key == Keys.None)
        {
            imguiKey = ImGuiKey.None;

            return true;
        }

        imguiKey = key switch
                   {
                       Keys.Back                           => ImGuiKey.Backspace,
                       Keys.Tab                            => ImGuiKey.Tab,
                       Keys.Enter                          => ImGuiKey.Enter,
                       Keys.CapsLock                       => ImGuiKey.CapsLock,
                       Keys.Escape                         => ImGuiKey.Escape,
                       Keys.Space                          => ImGuiKey.Space,
                       Keys.PageUp                         => ImGuiKey.PageUp,
                       Keys.PageDown                       => ImGuiKey.PageDown,
                       Keys.End                            => ImGuiKey.End,
                       Keys.Home                           => ImGuiKey.Home,
                       Keys.Left                           => ImGuiKey.LeftArrow,
                       Keys.Right                          => ImGuiKey.RightArrow,
                       Keys.Up                             => ImGuiKey.UpArrow,
                       Keys.Down                           => ImGuiKey.DownArrow,
                       Keys.PrintScreen                    => ImGuiKey.PrintScreen,
                       Keys.Insert                         => ImGuiKey.Insert,
                       Keys.Delete                         => ImGuiKey.Delete,
                       >= Keys.D0 and <= Keys.D9           => ImGuiKey.Key0    + (key - Keys.D0),
                       >= Keys.A and <= Keys.Z             => ImGuiKey.A       + (key - Keys.A),
                       >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
                       Keys.Multiply                       => ImGuiKey.KeypadMultiply,
                       Keys.Add                            => ImGuiKey.KeypadAdd,
                       Keys.Subtract                       => ImGuiKey.KeypadSubtract,
                       Keys.Decimal                        => ImGuiKey.KeypadDecimal,
                       Keys.Divide                         => ImGuiKey.KeypadDivide,
                       >= Keys.F1 and <= Keys.F24          => ImGuiKey.F1 + (key - Keys.F1),
                       Keys.NumLock                        => ImGuiKey.NumLock,
                       Keys.Scroll                         => ImGuiKey.ScrollLock,
                       Keys.LeftShift                      => ImGuiKey.ModShift,
                       Keys.LeftControl                    => ImGuiKey.ModCtrl,
                       Keys.LeftAlt                        => ImGuiKey.ModAlt,
                       Keys.OemSemicolon                   => ImGuiKey.Semicolon,
                       Keys.OemPlus                        => ImGuiKey.Equal,
                       Keys.OemComma                       => ImGuiKey.Comma,
                       Keys.OemMinus                       => ImGuiKey.Minus,
                       Keys.OemPeriod                      => ImGuiKey.Period,
                       Keys.OemQuestion                    => ImGuiKey.Slash,
                       Keys.OemTilde                       => ImGuiKey.GraveAccent,
                       Keys.OemOpenBrackets                => ImGuiKey.LeftBracket,
                       Keys.OemCloseBrackets               => ImGuiKey.RightBracket,
                       Keys.OemPipe                        => ImGuiKey.Backslash,
                       Keys.OemQuotes                      => ImGuiKey.Apostrophe,
                       Keys.BrowserBack                    => ImGuiKey.AppBack,
                       Keys.BrowserForward                 => ImGuiKey.AppForward,
                       _                                   => ImGuiKey.None,
                   };

        return imguiKey != ImGuiKey.None;
    }

    #endregion

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

    #region Setup

    private unsafe void SetupBackend()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.BackendPlatformName =  "imgui_impl_fna".ToUTF8Ptr();
        io.BackendRendererName =  "imgui_impl_fna".ToUTF8Ptr();
        io.BackendFlags        |= ImGuiBackendFlags.RendererHasTextures;
        io.BackendFlags        |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags        |= ImGuiBackendFlags.HasGamepad;
        io.BackendFlags        |= ImGuiBackendFlags.RendererHasVtxOffset;

        // Set up platform IO for texture management.
        ImGuiPlatformIOPtr platformIo = ImGui.GetPlatformIO();

        /* TODO: Implement further platform features, such as
        clipboard, etc.*/

        if (_graphicsDevice.GraphicsProfile == GraphicsProfile.Reach)
        {
            platformIo.RendererTextureMaxWidth  = 2048;
            platformIo.RendererTextureMaxHeight = 2048;
        }
        else
        {
            platformIo.RendererTextureMaxWidth  = 4096;
            platformIo.RendererTextureMaxHeight = 4096;
        }
    }

    private void SetupInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();

#if MONOGAME
        _game.Window.TextInput += (s, a) =>
                                  {
                                      if (a.Character == '\t')
                                      {
                                          return;
                                      }

                                      io.AddInputCharacter(a.Character);
                                  };
#endif

#if FNA
        TextInputEXT.TextInput += c =>
                                  {
                                      if (c == '\t')
                                      {
                                          return;
                                      }

                                      io.AddInputCharacter(c);
                                  };
#endif
    }

    #endregion

    #region Textures

    private void UpdateTexture(ImTextureDataPtr textureData)
    {
        switch (textureData.Status)
        {
            case ImTextureStatus.WantCreate: CreateTexture(textureData); break;

            case ImTextureStatus.WantUpdates: UpdateTextureData(textureData); break;

            case ImTextureStatus.WantDestroy: DestroyTexture(textureData); break;

            case ImTextureStatus.Ok:
            case ImTextureStatus.Destroyed:
            default:
                // Do nothing.
                break;
        }
    }

    private unsafe void CreateTexture(ImTextureDataPtr textureData)
    {
        SurfaceFormat format =
            textureData.Format == ImTextureFormat.Rgba32 ? SurfaceFormat.Color : SurfaceFormat.Alpha8;

        Texture2D texture = new(_graphicsDevice, textureData.Width, textureData.Height, false, format);

        if (textureData.Pixels != null)
        {
            int pixelCount    = textureData.Width * textureData.Height;
            int bytesPerPixel = textureData.Format == ImTextureFormat.Rgba32 ? 4 : 1;
            int dataSize      = pixelCount * bytesPerPixel;

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

        if (textureInfo.IsManaged)
        {
            textureInfo.Texture?.Dispose();
        }

        _textures.Remove(texId);
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

    private BasicEffect CreateEffect(GraphicsDevice graphicsDevice)
    {
        return new BasicEffect(graphicsDevice)
               {
                   World              = Matrix.Identity,
                   View               = Matrix.Identity,
                   TextureEnabled     = true,
                   VertexColorEnabled = true,
               };
    }

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
        // - alpha-blending enabled
        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState  = BlendState.NonPremultiplied;

        // - No face culling  
        // - Scissor testing enabled
        _graphicsDevice.RasterizerState = _rasterizerState;

        // - Depth read-only (testing enabled, writes disabled)
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        // - Point filtering for textures (no interpolation)
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        // Setup projection
        _graphicsDevice.Viewport = new Viewport(0,
                                                0,
                                                _graphicsDevice.PresentationParameters.BackBufferWidth,
                                                _graphicsDevice.PresentationParameters.BackBufferHeight);

        UpdateBuffers(drawData);
        RenderCommandLists(drawData);

        // Restore modified state
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
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        // Expand buffers if we need more room
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

        // Copy ImGui's vertices and indices to a set of managed byte arrays
        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists.Data[n];

            fixed (void* vtxDstPtr = &_vertexData[vtxOffset * DrawVertDeclaration.Size])
            {
                fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
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
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the gpu vertex- and index buffers
        _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
        _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount   * sizeof(ushort));
    }

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists.Data[n];

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                ImDrawCmdPtr drawCmd = &cmdList.CmdBuffer.Data[cmdi];

                if (drawCmd.ElemCount == 0)
                {
                    continue;
                }

                // In v1.92, we need to handle ImTextureRef instead of ImTextureID
                ImTextureRef textureRef = drawCmd.TexRef;
                ImTextureID  texId      = textureRef.GetTexID();

                if (!_textures.TryGetValue(texId, out TextureInfo textureInfo))
                {
                    throw new
                        InvalidOperationException($"Could not find a texture with id '{texId}', please check your bindings");
                }

                _graphicsDevice.ScissorRectangle = new Rectangle((int)drawCmd.ClipRect.X,
                                                                 (int)drawCmd.ClipRect.Y,
                                                                 (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                                                                 (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y));

                _effect.Texture = textureInfo.Texture;

                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

#pragma warning disable CS0618 // // FNA does not expose an alternative method.
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                          (int)drawCmd.VtxOffset + vtxOffset,
                                                          0,
                                                          cmdList.VtxBuffer.Size,
                                                          (int)drawCmd.IdxOffset + idxOffset,
                                                          (int)drawCmd.ElemCount / 3);
#pragma warning restore CS0618
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    #endregion
}
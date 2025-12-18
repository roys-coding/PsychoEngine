using Hexa.NET.ImGui;

using ImGuiXNA;

using Microsoft.Xna.Framework.Graphics;

namespace PsychoEngine.Core;

public class CoreEngine : Game
{
    private ImGuiRenderer _imGuiRenderer;

    private Texture2D    _xnaTexture;
    private ImTextureRef _imGuiTexture;
    
    public CoreEngine(string windowTitle, int windowWidth, int windowHeight)
    {
        GraphicsDeviceManager deviceManager = new(this);
        deviceManager.PreferredBackBufferWidth  = windowWidth;
        deviceManager.PreferredBackBufferHeight = windowHeight;
        IsMouseVisible                          = true;

        Window.Title = windowTitle;
    }

    protected override void Initialize()
    {
        _imGuiRenderer = new ImGuiRenderer(this);

        base.Initialize();
    }

    override protected void LoadContent()
    {
        // Texture loading example

        // First, load the texture as a Texture2D (can also be done using the content pipeline)
        _xnaTexture = CreateTexture(GraphicsDevice, 300, 150, pixel =>
                                                              {
                                                                  int red = (pixel % 300) / 2;
                                                                  return new Color(red, 1, 1);
                                                              });

        // Then, bind it to an ImGui-friendly pointer that we can use during regular ImGui.** calls.
        _imGuiTexture = _imGuiRenderer.BindTexture(_xnaTexture);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _imGuiRenderer.NewFrame(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _imGuiRenderer.BeforeLayout(gameTime);
        
        ImGui.ShowDemoWindow();
        
        _imGuiRenderer.AfterLayout();
        base.Draw(gameTime);
    }

    public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
    {
        //initialize a texture
        Texture2D texture = new Texture2D(device, width, height);

        //the array holds the color for each pixel in the texture
        Color[] data = new Color[width * height];
        for (int pixel = 0; pixel < data.Length; pixel++)
        {
            //the function applies the color according to the specified pixel
            data[pixel] = paint(pixel);
        }

        //set the color
        texture.SetData(data);

        return texture;
    }

    protected override void UnloadContent()
    {
        // Clean up ImGui resources
        if (_imGuiTexture.TexID != ImTextureID.Null)
        {
            _imGuiRenderer.UnbindTexture(_imGuiTexture);
        }

        _imGuiRenderer?.Dispose();

        base.UnloadContent();
    }
}
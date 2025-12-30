using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Utilities;
using ImGuiXNA;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace PsychoEngine;

public class ImGuiManager
{
    private const    string           FontsPath     = @"Content\Fonts\";
    private const    int              FontSize      = 15;
    private const    int              IconsFontSize = 19;
    private readonly ImGuiXnaRenderer _renderer;
    private readonly ImGuiXnaPlatform _platform;

    public event EventHandler? OnLayout;

    public ImGuiManager(Game game)
    {
        _renderer = new ImGuiXnaRenderer(game);
        _platform = new ImGuiXnaPlatform(game);
    }

    public unsafe void Initialize(GraphicsDevice graphicsDevice)
    {
        // Set up ImGui context.
        ImGuiContextPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        _platform.Initialize(graphicsDevice);
        _renderer.Initialize(graphicsDevice);

        ImGuiIOPtr     io    = ImGui.GetIO();
        ImGuiStylePtr  style = ImGui.GetStyle();
        ImFontAtlasPtr fonts = ImGui.GetIO().Fonts;

        #region ImGui config

        io.ConfigFlags     |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags     |= ImGuiConfigFlags.NavEnableGamepad;
        io.ConfigFlags     |= ImGuiConfigFlags.NavEnableKeyboard;
        io.MouseDrawCursor =  true;

        #endregion

        #region ImGui colors

        Vector4 black      = new(0f, 0f, 0f, 1.0f);
        Vector4 white      = new(1f, 1f, 1f, 1.0f);
        Vector4 dark0      = new(15  / 255f, 15  / 255f, 15  / 255f, 1.0f);
        Vector4 dark1      = new(26  / 255f, 26  / 255f, 26  / 255f, 1.0f);
        Vector4 dark2      = new(38  / 255f, 38  / 255f, 38  / 255f, 1.0f);
        Vector4 medium0    = new(53  / 255f, 53  / 255f, 53  / 255f, 1.0f);
        Vector4 medium1    = new(64  / 255f, 64  / 255f, 64  / 255f, 1.0f);
        Vector4 medium2    = new(77  / 255f, 77  / 255f, 77  / 255f, 1.0f);
        Vector4 light0     = new(89  / 255f, 89  / 255f, 89  / 255f, 1.0f);
        Vector4 light1     = new(102 / 255f, 102 / 255f, 102 / 255f, 1.0f);
        Vector4 highlight1 = new(166 / 255f, 98  / 255f, 255 / 255f, 1.0f);

        // Unused colors.
        // Vector4 light2 = new Vector4(115 / 255f, 115 / 255f, 115 / 255f, 1.0f);
        // Vector4 lighter = new Vector4(128 / 255f, 128 / 255f, 128 / 255f, 1.0f);
        // Vector4 lightest = new Vector4(153 / 255f, 153 / 255f, 153 / 255f, 1.0f);
        // Vector4 highlight0 = new Vector4(151 / 255f, 71 / 255f, 255 / 255f, 1.0f);
        // Vector4 highlight2 = new Vector4(183 / 255f, 128 / 255f, 255 / 255f, 1.0f);

        Span<Vector4> colors = style.Colors;
        colors[(int)ImGuiCol.Text] = white;

        colors[(int)ImGuiCol.TextDisabled] = white with
                                             {
                                                 W = 0.6f,
                                             };

        colors[(int)ImGuiCol.WindowBg] = dark2 with
                                         {
                                             W = 0.9f,
                                         };

        colors[(int)ImGuiCol.ChildBg] = dark1 with
                                        {
                                            W = 0.9f,
                                        };

        colors[(int)ImGuiCol.PopupBg] = dark1 with
                                        {
                                            W = 0.9f,
                                        };

        colors[(int)ImGuiCol.Border]       = medium0;
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f);

        colors[(int)ImGuiCol.FrameBg] = dark1 with
                                        {
                                            W = 0.8f,
                                        };

        colors[(int)ImGuiCol.FrameBgHovered] = dark2 with
                                               {
                                                   W = 0.8f,
                                               };

        colors[(int)ImGuiCol.FrameBgActive] = medium0 with
                                              {
                                                  W = 0.8f,
                                              };

        colors[(int)ImGuiCol.TitleBg] = black with
                                        {
                                            W = 0.9f,
                                        };

        colors[(int)ImGuiCol.TitleBgActive] = dark1;

        colors[(int)ImGuiCol.TitleBgCollapsed] = dark0 with
                                                 {
                                                     W = 0.5f,
                                                 };

        colors[(int)ImGuiCol.MenuBarBg]            = dark1;
        colors[(int)ImGuiCol.ScrollbarBg]          = dark1;
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = medium0;
        colors[(int)ImGuiCol.ScrollbarGrab]        = dark2;
        colors[(int)ImGuiCol.ScrollbarGrabActive]  = medium2;
        colors[(int)ImGuiCol.CheckMark]            = medium2;
        colors[(int)ImGuiCol.SliderGrab]           = medium1;
        colors[(int)ImGuiCol.SliderGrabActive]     = light0;

        colors[(int)ImGuiCol.Button] = medium1 with
                                       {
                                           W = 0.8f,
                                       };

        colors[(int)ImGuiCol.ButtonHovered] = medium2 with
                                              {
                                                  W = 0.8f,
                                              };

        colors[(int)ImGuiCol.ButtonActive] = light1 with
                                             {
                                                 W = 0.8f,
                                             };

        colors[(int)ImGuiCol.Header] = medium1 with
                                       {
                                           W = 0.7f,
                                       };

        colors[(int)ImGuiCol.HeaderHovered] = medium2 with
                                              {
                                                  W = 0.7f,
                                              };

        colors[(int)ImGuiCol.HeaderActive] = light0 with
                                             {
                                                 W = 0.7f,
                                             };

        colors[(int)ImGuiCol.Separator]                 = new Vector4(1f, 1f, 1f, 0.2f);
        colors[(int)ImGuiCol.SeparatorHovered]          = new Vector4(1f, 1f, 1f, 0.25f);
        colors[(int)ImGuiCol.SeparatorActive]           = new Vector4(1f, 1f, 1f, 0.35f);
        colors[(int)ImGuiCol.Tab]                       = dark0;
        colors[(int)ImGuiCol.TabHovered]                = dark1;
        colors[(int)ImGuiCol.TabSelected]               = dark2;
        colors[(int)ImGuiCol.TabSelectedOverline]       = highlight1;
        colors[(int)ImGuiCol.TabDimmed]                 = black;
        colors[(int)ImGuiCol.TabDimmedSelected]         = dark0;
        colors[(int)ImGuiCol.TabDimmedSelectedOverline] = dark2;
        colors[(int)ImGuiCol.DockingPreview]            = medium2;
        colors[(int)ImGuiCol.DockingEmptyBg]            = medium2;
        colors[(int)ImGuiCol.TableHeaderBg]             = medium1;
        colors[(int)ImGuiCol.TableBorderStrong]         = medium2;
        colors[(int)ImGuiCol.TableBorderLight]          = medium1;
        colors[(int)ImGuiCol.ResizeGrip]                = medium1;
        colors[(int)ImGuiCol.ResizeGripHovered]         = medium2;
        colors[(int)ImGuiCol.ResizeGripActive]          = light1;
        colors[(int)ImGuiCol.PlotLines]                 = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.PlotLinesHovered]          = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogram]             = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogramHovered]      = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.TableRowBg]                = new Vector4(0.00f);
        colors[(int)ImGuiCol.TableRowBgAlt]             = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        colors[(int)ImGuiCol.TextLink]                  = new Vector4(0.53f, 0.53f, 0.87f, 0.80f);
        colors[(int)ImGuiCol.TextSelectedBg]            = new Vector4(0.00f, 0.00f, 1.00f, 0.35f);
        colors[(int)ImGuiCol.DragDropTarget]            = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
        colors[(int)ImGuiCol.NavCursor]                 = new Vector4(0.45f, 0.45f, 0.90f, 0.80f);
        colors[(int)ImGuiCol.NavWindowingHighlight]     = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        colors[(int)ImGuiCol.NavWindowingDimBg]         = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        colors[(int)ImGuiCol.ModalWindowDimBg]          = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);

        #endregion

        #region ImGui style

        // Padding.
        style.WindowPadding        = new Vector2(12f, 10f);
        style.FramePadding         = new Vector2(10f, 6f);
        style.CellPadding          = new Vector2(6f,  6f);
        style.SeparatorTextPadding = new Vector2(12f, 6f);
        style.TouchExtraPadding    = new Vector2(4f,  4f);

        // Trees.
        style.TreeLinesFlags = ImGuiTreeNodeFlags.DrawLinesNone;
        style.TreeLinesSize  = 1f;

        // Spacing.
        style.ItemSpacing      = new Vector2(4f, 4f);
        style.ItemInnerSpacing = new Vector2(4f, 4f);
        style.IndentSpacing    = 8f;

        // Align.
        style.WindowTitleAlign         = new Vector2(0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Left;

        // Sizes.
        style.WindowBorderSize        = 1f;
        style.ChildBorderSize         = 1f;
        style.PopupBorderSize         = 1f;
        style.FrameBorderSize         = 1f;
        style.TabBarBorderSize        = 0f;
        style.TabBarOverlineSize      = 2f;
        style.SeparatorTextBorderSize = 1;
        style.DockingSeparatorSize    = 1;
        style.ScrollbarSize           = 12f;
        style.GrabMinSize             = 12f;

        // Rounding.
        style.WindowRounding    = 5f;
        style.ChildRounding     = 5f;
        style.ScrollbarRounding = 3f;
        style.GrabRounding      = 3f;
        style.TreeLinesRounding = 0f;

        // These look like rounding with value "5"
        style.FrameRounding = 3f;
        style.PopupRounding = 2f;
        // .

        style.TabRounding = 0f;

        // Other options.
        style.AntiAliasedLinesUseTex = true;
        style.AntiAliasedLines       = true;
        style.AntiAliasedFill        = true;
        style.DisplayWindowPadding   = new Vector2(30f);
        style.DisplaySafeAreaPadding = new Vector2(30f);

        #endregion

        #region ImGui fonts

        GlyphRanges iconsFontRanges = new(Fonts.Lucide.IconMin, Fonts.Lucide.IconMax, 0);

        // Base font config.
        ImFontConfigPtr fontConfig = ImGui.ImFontConfig();
        fontConfig.SizePixels         = FontSize;
        fontConfig.GlyphExcludeRanges = iconsFontRanges.GetRanges();

        // Icons font config.
        ImFontConfigPtr iconsFontConfig = ImGui.ImFontConfig();
        iconsFontConfig.MergeMode        = true;
        iconsFontConfig.GlyphMinAdvanceX = IconsFontSize;
        iconsFontConfig.SizePixels       = IconsFontSize;
        iconsFontConfig.GlyphOffset      = new Vector2(0f, 3f);

        // Load fonts.
        fonts.AddFontFromFileTTF($"{FontsPath}{Fonts.Bfont.FileName}",            fontConfig);
        fonts.AddFontFromFileTTF($"{FontsPath}{Fonts.Lucide.FontIconFileNameLC}", iconsFontConfig);

        #endregion
    }

    public void Draw(GameTime gameTime)
    {
        _platform.NewFrame(gameTime);
        OnLayout?.Invoke(this, EventArgs.Empty);
        _renderer.Render();
    }

    public void Terminate()
    {
        _renderer.Dispose();
        ImGui.DestroyContext();
    }
}
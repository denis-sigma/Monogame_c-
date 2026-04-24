using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MyGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D? _menuBackground;
    private Texture2D _pixel = null!;

    private Rectangle _playBtn;
    private Rectangle _restartBtn;
    private Rectangle _settingsBtn;
    private Rectangle _menuPanel;
    private MouseState _prevMouse;
    private string _statusText = "Город в опасности — готовься спасать заложников";

    private Texture2D? _titleTexture;
    private Texture2D? _subtitleTexture;
    private Texture2D? _missionTexture;
    private Texture2D? _playTextTexture;
    private Texture2D? _restartTextTexture;
    private Texture2D? _settingsTextTexture;
    private Texture2D? _footerTexture;
    private Texture2D? _playingTexture;
    private Texture2D? _statusTexture;
    private Texture2D? _hintTexture;

    private GameLevel? _gameLevel;

    private enum AppState
    {
        Menu,
        Playing
    }

    private AppState _state = AppState.Menu;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        Window.Title = "Человек-Паук: День в Урфу";
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        var bgPath = Path.Combine(System.AppContext.BaseDirectory, "Content", "background.jpeg");
        if (File.Exists(bgPath))
        {
            using var stream = File.OpenRead(bgPath);
            _menuBackground = Texture2D.FromStream(GraphicsDevice, stream);
        }
        else
        {
            _menuBackground = _pixel;
        }

        _gameLevel = new GameLevel();
        _gameLevel.LoadContent(GraphicsDevice);

        BuildTextTextures();
        UpdateLayout();
    }

    private void BuildTextTextures()
    {
        _titleTexture = CreateTextTexture("Человек-Паук: День в Урфу", 38, System.Drawing.Color.White, System.Drawing.FontStyle.Bold);
        _subtitleTexture = CreateTextTexture("Пятеро заложников. Пять боссов. Спасай город.", 20, System.Drawing.Color.FromArgb(220, 235, 255), System.Drawing.FontStyle.Regular);
        _missionTexture = CreateTextTexture("Пройти уровни, победить босса и освободить пленника", 18, System.Drawing.Color.FromArgb(190, 220, 255), System.Drawing.FontStyle.Regular);
        _playTextTexture = CreateTextTexture("Играть", 28, System.Drawing.Color.White, System.Drawing.FontStyle.Bold);
        _restartTextTexture = CreateTextTexture("Начать заново", 28, System.Drawing.Color.White, System.Drawing.FontStyle.Bold);
        _settingsTextTexture = CreateTextTexture("Настройки", 28, System.Drawing.Color.White, System.Drawing.FontStyle.Bold);
        _footerTexture = CreateTextTexture("Esc — выход из игры", 18, System.Drawing.Color.FromArgb(205, 215, 230), System.Drawing.FontStyle.Regular);
        _playingTexture = CreateTextTexture("Игра запущена", 30, System.Drawing.Color.White, System.Drawing.FontStyle.Bold);
        RebuildStatusTextures();
    }

    private void RebuildStatusTextures()
    {
        _statusTexture?.Dispose();
        _hintTexture?.Dispose();

        _statusTexture = CreateTextTexture(_statusText, 22, System.Drawing.Color.FromArgb(230, 238, 255), System.Drawing.FontStyle.Regular);
        _hintTexture = CreateTextTexture("Нажми Esc, чтобы вернуться в меню", 18, System.Drawing.Color.FromArgb(205, 215, 230), System.Drawing.FontStyle.Regular);
    }

    private Texture2D CreateTextTexture(string text, float size, System.Drawing.Color color, System.Drawing.FontStyle style)
    {
        using var font = new Font("Arial", size, style, GraphicsUnit.Pixel);
        using var measureBitmap = new Bitmap(1, 1);
        using var measureGraphics = Graphics.FromImage(measureBitmap);

        measureGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var measured = measureGraphics.MeasureString(text, font);
        var width = System.Math.Max(2, (int)System.Math.Ceiling(measured.Width) + 12);
        var height = System.Math.Max(2, (int)System.Math.Ceiling(measured.Height) + 12);

        using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(System.Drawing.Color.Transparent);

        using var shadowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(140, 0, 0, 0));
        using var textBrush = new System.Drawing.SolidBrush(color);

        graphics.DrawString(text, font, shadowBrush, 7, 7);
        graphics.DrawString(text, font, textBrush, 5, 5);

        var data = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                data[y * width + x] = new Color(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }

        var texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(data);
        return texture;
    }

    private void UpdateLayout()
    {
        var viewport = GraphicsDevice.Viewport;
        var w = viewport.Width;
        var h = viewport.Height;

        var panelWidth = (int)(w * 0.34f);
        var panelHeight = (int)(h * 0.46f);
        var panelX = (int)(w * 0.08f);
        var panelY = (int)(h * 0.20f);

        _menuPanel = new Rectangle(panelX, panelY, panelWidth, panelHeight);

        var buttonWidth = panelWidth - 60;
        var buttonHeight = 68;
        var buttonX = panelX + 30;
        var firstButtonY = panelY + 150;
        var gap = 18;

        _playBtn = new Rectangle(buttonX, firstButtonY, buttonWidth, buttonHeight);
        _restartBtn = new Rectangle(buttonX, firstButtonY + buttonHeight + gap, buttonWidth, buttonHeight);
        _settingsBtn = new Rectangle(buttonX, firstButtonY + (buttonHeight + gap) * 2, buttonWidth, buttonHeight);
    }

    protected override void Update(GameTime gameTime)
    {
        var ks = Keyboard.GetState();
        if (ks.IsKeyDown(Keys.Escape))
        {
            if (_state == AppState.Playing)
            {
                _state = AppState.Menu;
                _statusText = "Возврат в главное меню";
                RebuildStatusTextures();
            }
            else
            {
                Exit();
            }
        }

        if (_state == AppState.Playing && _gameLevel != null)
        {
            _gameLevel.Update(gameTime);
        }

        var ms = Mouse.GetState();
        var leftClick = ms.LeftButton == ButtonState.Pressed &&
                        _prevMouse.LeftButton == ButtonState.Released;

        if (_state == AppState.Menu && leftClick)
        {
            if (_playBtn.Contains(ms.Position))
            {
                _state = AppState.Playing;
                _statusText = "Матч начался";
                RebuildStatusTextures();
            }
            else if (_restartBtn.Contains(ms.Position))
            {
                _state = AppState.Playing;
                _statusText = "Игра начата заново";
                RebuildStatusTextures();
            }
            else if (_settingsBtn.Contains(ms.Position))
            {
                _statusText = "Настройки пока в разработке";
                RebuildStatusTextures();
            }
        }

        _prevMouse = ms;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);

        if (_state == AppState.Menu)
        {
            DrawBackground();
            DrawMenu();
        }
        else
        {
            _gameLevel?.Draw(_spriteBatch, GraphicsDevice);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawBackground()
    {
        var bounds = GraphicsDevice.Viewport.Bounds;
        if (_state == AppState.Menu)
        {
            _spriteBatch.Draw(_menuBackground ?? _pixel, bounds, Color.White);
            _spriteBatch.Draw(_pixel, bounds, new Color(8, 12, 24) * 0.18f);
        }
        else
        {
            _spriteBatch.Draw(_pixel, bounds, new Color(8, 12, 24) * 0.86f);
        }
    }

    private void DrawMenu()
    {
        DrawShadowedBox(_menuPanel, new Color(9, 19, 42) * 0.76f, new Color(120, 190, 255));

        DrawCenteredTexture(_titleTexture, new Vector2(_menuPanel.Center.X, _menuPanel.Y + 50));
        DrawCenteredTexture(_subtitleTexture, new Vector2(_menuPanel.Center.X, _menuPanel.Y + 98));
        DrawCenteredTexture(_missionTexture, new Vector2(_menuPanel.Center.X, _menuPanel.Y + 134));

        DrawButton(_playBtn, _playTextTexture, new Color(208, 33, 49));
        DrawButton(_restartBtn, _restartTextTexture, new Color(33, 87, 168));
        DrawButton(_settingsBtn, _settingsTextTexture, new Color(65, 65, 78));

        var footerRect = new Rectangle(_menuPanel.X + 24, _menuPanel.Bottom - 54, _menuPanel.Width - 48, 28);
        _spriteBatch.Draw(_pixel, footerRect, Color.White * 0.06f);
        DrawCenteredTexture(_footerTexture, new Vector2(_menuPanel.Center.X, footerRect.Center.Y));
    }

    private void DrawButton(Rectangle rect, Texture2D? textTexture, Color baseColor)
    {
        var hover = rect.Contains(Mouse.GetState().Position);
        var shadowRect = new Rectangle(rect.X + 6, rect.Y + 6, rect.Width, rect.Height);
        var innerRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);

        _spriteBatch.Draw(_pixel, shadowRect, Color.Black * 0.35f);
        _spriteBatch.Draw(_pixel, rect, hover ? Color.White * 0.18f : Color.White * 0.08f);
        _spriteBatch.Draw(_pixel, innerRect, hover ? Lighten(baseColor, 0.12f) : baseColor);

        var border = hover ? Color.White : new Color(215, 225, 245);
        DrawBorder(rect, border, 2);

        DrawCenteredTexture(textTexture, new Vector2(rect.Center.X, rect.Center.Y));
    }

    private void DrawShadowedBox(Rectangle rect, Color fillColor, Color borderColor)
    {
        var shadow = new Rectangle(rect.X + 10, rect.Y + 10, rect.Width, rect.Height);
        _spriteBatch.Draw(_pixel, shadow, Color.Black * 0.30f);
        _spriteBatch.Draw(_pixel, rect, fillColor);
        DrawBorder(rect, borderColor, 3);
    }

    private void DrawBorder(Rectangle rect, Color color, int thickness)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void DrawCenteredTexture(Texture2D? texture, Vector2 center)
    {
        if (texture == null)
        {
            return;
        }

        var position = new Vector2(center.X - texture.Width / 2f, center.Y - texture.Height / 2f);
        _spriteBatch.Draw(texture, position, Color.White);
    }

    private static Color Lighten(Color color, float amount)
    {
        var lerped = Vector3.Lerp(color.ToVector3(), Vector3.One, amount);
        return new Color(lerped);
    }

    protected override void UnloadContent()
    {
        _titleTexture?.Dispose();
        _subtitleTexture?.Dispose();
        _missionTexture?.Dispose();
        _playTextTexture?.Dispose();
        _restartTextTexture?.Dispose();
        _settingsTextTexture?.Dispose();
        _footerTexture?.Dispose();
        _playingTexture?.Dispose();
        _statusTexture?.Dispose();
        _hintTexture?.Dispose();
        _menuBackground?.Dispose();
        _pixel?.Dispose();
        _gameLevel?.Unload();

        base.UnloadContent();
    }
}

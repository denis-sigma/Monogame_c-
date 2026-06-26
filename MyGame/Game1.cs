using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MyGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;
    private GameLevel? _gameLevel;
    private Texture2D? _menuBackground;
    private Texture2D? _menuTitleTexture;
    private Texture2D? _menuPlayTexture;
    private Texture2D? _menuPixel;
    private Texture2D? _menuTransitionTexture;
    private Song? _themeSong;
    private ExternalMp3Player? _themeMp3Player;
    private bool _isMainMenuOpen = true;
    private bool _isThemeMusicStarted;
    private bool _isMciThemeOpen;
    private bool _isStartTransitionActive;
    private float _startTransitionTimer;
    private Rectangle _playButtonBounds;
    private const float MenuBackgroundVerticalFocus = 0.18f;
    private const float StartTransitionDuration = 1.1f;
    private const string ThemeSongPath = @"c:\Users\user\Downloads\Paul_Francis_Webster_Bob_Harris_-_Spider-Man_1967_Original_Cartoon_Theme_Song_66859982.mp3";
    private static readonly string[] MenuBackgroundPaths =
    {
        @"c:\Users\user\Downloads\ab8507438af11f1ae48022aff9566b5_1.jpeg",
        @"c:\Users\user\Downloads\spider-menu.png",
        @"c:\Users\user\Downloads\spider-menu.jpeg",
        @"c:\Users\user\Downloads\spider-menu.jpg",
        @"c:\Users\user\Downloads\spiderman-menu.png",
        @"c:\Users\user\Downloads\spiderman-menu.jpeg",
        @"c:\Users\user\Downloads\spiderman-menu.jpg",
        @"c:\Users\user\Downloads\menu-background.png",
        @"c:\Users\user\Downloads\menu-background.jpeg",
        @"c:\Users\user\Downloads\menu-background.jpg"
    };
    private static readonly string[] MenuTransitionPaths =
    {
        @"c:\Users\user\Downloads\переход-ezremove.png",
        @"c:\Users\user\Downloads\transition.png",
        @"c:\Users\user\Downloads\transition.jpg",
        @"c:\Users\user\Downloads\transition.jpeg"
    };

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.HardwareModeSwitch = false;
        _graphics.IsFullScreen = true;
    }

    protected override void Initialize()
    {
        Window.Title = "Человек-Паук: День в Урфу";
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _menuPixel = new Texture2D(GraphicsDevice, 1, 1);
        _menuPixel.SetData(new[] { Color.White });
        _menuBackground = LoadFirstExistingTexture(GraphicsDevice, MenuBackgroundPaths);
        _menuTransitionTexture = LoadFirstExistingTexture(GraphicsDevice, MenuTransitionPaths);
        _menuTitleTexture = CreateTextTexture(
            GraphicsDevice,
            "\u0427\u0435\u043b\u043e\u0432\u0435\u043a \u043f\u0430\u0443\u043a: \u0434\u0435\u043d\u044c \u0432 \u0443\u0440\u0444\u0443",
            980,
            110,
            54f);
        _menuPlayTexture = CreateTextTexture(GraphicsDevice, "\u0418\u0433\u0440\u0430\u0442\u044c", 300, 78, 40f);
        LoadThemeSong();

        _gameLevel = new GameLevel();
        _gameLevel.LoadContent(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.F11) && !_prevKeyboard.IsKeyDown(Keys.F11))
        {
            _graphics.ToggleFullScreen();
        }

        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (_isMainMenuOpen)
        {
            UpdateMainMenu(gameTime, keyboard);
            _prevKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        _gameLevel?.Update(gameTime);
        if (_gameLevel?.IsPlayerDead == true || _gameLevel?.IsLizardDefeated == true)
        {
            StopThemeMusic();
        }

        _prevKeyboard = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        if (_isMainMenuOpen)
        {
            DrawMainMenu();
        }
        else
        {
            _gameLevel?.Draw(_spriteBatch, GraphicsDevice);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _menuBackground?.Dispose();
        _menuTitleTexture?.Dispose();
        _menuPlayTexture?.Dispose();
        _menuPixel?.Dispose();
        _menuTransitionTexture?.Dispose();
        StopThemeMusic();
        _themeSong?.Dispose();
        _gameLevel?.Unload();
        base.UnloadContent();
    }

    private void UpdateMainMenu(GameTime gameTime, KeyboardState keyboard)
    {
        if (_isStartTransitionActive)
        {
            _startTransitionTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_startTransitionTimer >= StartTransitionDuration)
            {
                _isStartTransitionActive = false;
                _isMainMenuOpen = false;
            }

            return;
        }

        var mouse = Mouse.GetState();
        var clickedPlay = mouse.LeftButton == ButtonState.Pressed
            && _prevMouse.LeftButton == ButtonState.Released
            && _playButtonBounds.Contains(mouse.Position);
        var pressedEnter = keyboard.IsKeyDown(Keys.Enter) && !_prevKeyboard.IsKeyDown(Keys.Enter);
        var pressedSpace = keyboard.IsKeyDown(Keys.Space) && !_prevKeyboard.IsKeyDown(Keys.Space);
        if (clickedPlay || pressedEnter || pressedSpace)
        {
            StartGameFromMenu();
        }

        _prevMouse = mouse;
    }

    private void StartGameFromMenu()
    {
        StartThemeMusic();
        _isStartTransitionActive = true;
        _startTransitionTimer = 0f;
    }

    private void StartThemeMusic()
    {
        if (_isThemeMusicStarted)
        {
            return;
        }

        if (StartThemeMusicWithExternalPlayer())
        {
            _isThemeMusicStarted = true;
            return;
        }

        if (StartThemeMusicWithMci())
        {
            _isThemeMusicStarted = true;
            return;
        }

        if (_themeSong == null)
        {
            return;
        }

        try
        {
            MediaPlayer.Stop();
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.35f;
            MediaPlayer.Play(_themeSong);
            _isThemeMusicStarted = true;
        }
        catch
        {
            _isThemeMusicStarted = true;
        }
    }

    private bool StartThemeMusicWithExternalPlayer()
    {
        if (!File.Exists(ThemeSongPath))
        {
            return false;
        }

        StopThemeMusic();
        _themeMp3Player = new ExternalMp3Player();
        if (!_themeMp3Player.Load(ThemeSongPath, 35, repeat: true))
        {
            _themeMp3Player.Dispose();
            _themeMp3Player = null;
            return false;
        }

        _themeMp3Player.PlayFromStart();
        return true;
    }

    private bool StartThemeMusicWithMci()
    {
        if (!File.Exists(ThemeSongPath))
        {
            return false;
        }

        StopThemeMusic();
        var safePath = ThemeSongPath.Replace("\"", string.Empty);
        var openResult = mciSendString($"open \"{safePath}\" type mpegvideo alias SpiderTheme", IntPtr.Zero, 0, IntPtr.Zero);
        if (openResult != 0)
        {
            return false;
        }

        _isMciThemeOpen = true;
        mciSendString("setaudio SpiderTheme volume to 350", IntPtr.Zero, 0, IntPtr.Zero);
        var playResult = mciSendString("play SpiderTheme repeat", IntPtr.Zero, 0, IntPtr.Zero);
        if (playResult == 0)
        {
            return true;
        }

        StopThemeMusic();
        return false;
    }

    private void StopThemeMusic()
    {
        _themeMp3Player?.Dispose();
        _themeMp3Player = null;

        try
        {
            MediaPlayer.Stop();
        }
        catch
        {
        }

        if (_isMciThemeOpen)
        {
            mciSendString("stop SpiderTheme", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString("close SpiderTheme", IntPtr.Zero, 0, IntPtr.Zero);
            _isMciThemeOpen = false;
        }

        _isThemeMusicStarted = false;
    }

    private void DrawMainMenu()
    {
        var viewport = GraphicsDevice.Viewport;
        var screen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        if (_menuBackground != null)
        {
            _spriteBatch.Draw(_menuBackground, screen, GetMenuBackgroundSourceRectangle(_menuBackground, viewport.Width, viewport.Height), Color.White);
        }
        else if (_menuPixel != null)
        {
            _spriteBatch.Draw(_menuPixel, screen, new Color(18, 25, 38));
        }

        if (_menuPixel != null)
        {
            _spriteBatch.Draw(_menuPixel, screen, Color.Black * 0.42f);
        }

        if (_menuTitleTexture != null)
        {
            var titlePosition = new Vector2(
                (viewport.Width - _menuTitleTexture.Width) / 2f,
                Math.Max(42f, viewport.Height * 0.08f));
            _spriteBatch.Draw(_menuTitleTexture, titlePosition, Color.White);
        }

        var buttonWidth = 360;
        var buttonHeight = 92;
        _playButtonBounds = new Rectangle(
            (viewport.Width - buttonWidth) / 2,
            (int)(viewport.Height * 0.72f),
            buttonWidth,
            buttonHeight);

        var mouse = Mouse.GetState();
        var isHover = _playButtonBounds.Contains(mouse.Position);
        if (_menuPixel != null)
        {
            var buttonColor = isHover ? new Color(215, 35, 38) : new Color(155, 18, 24);
            _spriteBatch.Draw(_menuPixel, _playButtonBounds, buttonColor * 0.92f);
            _spriteBatch.Draw(_menuPixel, new Rectangle(_playButtonBounds.X, _playButtonBounds.Y, _playButtonBounds.Width, 4), Color.White * 0.95f);
            _spriteBatch.Draw(_menuPixel, new Rectangle(_playButtonBounds.X, _playButtonBounds.Bottom - 4, _playButtonBounds.Width, 4), Color.White * 0.95f);
            _spriteBatch.Draw(_menuPixel, new Rectangle(_playButtonBounds.X, _playButtonBounds.Y, 4, _playButtonBounds.Height), Color.White * 0.95f);
            _spriteBatch.Draw(_menuPixel, new Rectangle(_playButtonBounds.Right - 4, _playButtonBounds.Y, 4, _playButtonBounds.Height), Color.White * 0.95f);
        }

        if (_menuPlayTexture != null)
        {
            var textPosition = new Vector2(
                _playButtonBounds.Center.X - _menuPlayTexture.Width / 2f,
                _playButtonBounds.Center.Y - _menuPlayTexture.Height / 2f);
            _spriteBatch.Draw(_menuPlayTexture, textPosition, Color.White);
        }

        DrawStartTransitionOverlay(screen);
    }

    private void DrawStartTransitionOverlay(Rectangle screen)
    {
        if (!_isStartTransitionActive)
        {
            return;
        }

        var alpha = MathHelper.Clamp(_startTransitionTimer / StartTransitionDuration, 0f, 1f);
        if (_menuTransitionTexture != null)
        {
            _spriteBatch.Draw(_menuTransitionTexture, screen, Color.White * alpha);
            return;
        }

        if (_menuPixel != null)
        {
            _spriteBatch.Draw(_menuPixel, screen, Color.Black * alpha);
        }
    }

    private void LoadThemeSong()
    {
        if (!File.Exists(ThemeSongPath))
        {
            return;
        }

        try
        {
            _themeSong = Song.FromUri("Spider-Man Theme", new Uri(ThemeSongPath));
        }
        catch
        {
            _themeSong = null;
        }
    }

    private static Texture2D? LoadFirstExistingTexture(GraphicsDevice graphicsDevice, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var stream = File.OpenRead(path);
                return Texture2D.FromStream(graphicsDevice, stream);
            }
            catch
            {

            }
        }

        return null;
    }

    private static Rectangle GetCoverSourceRectangle(Texture2D texture, int targetWidth, int targetHeight)
    {
        var sourceAspect = texture.Width / (float)texture.Height;
        var targetAspect = targetWidth / (float)targetHeight;
        if (sourceAspect > targetAspect)
        {
            var width = (int)(texture.Height * targetAspect);
            return new Rectangle((texture.Width - width) / 2, 0, width, texture.Height);
        }

        var height = (int)(texture.Width / targetAspect);
        return new Rectangle(0, (texture.Height - height) / 2, texture.Width, height);
    }

    private static Rectangle GetMenuBackgroundSourceRectangle(Texture2D texture, int targetWidth, int targetHeight)
    {
        var source = GetCoverSourceRectangle(texture, targetWidth, targetHeight);
        if (texture.Height <= source.Height)
        {
            return source;
        }

        var maxY = texture.Height - source.Height;
        source.Y = (int)MathF.Round(maxY * MenuBackgroundVerticalFocus);
        return source;
    }

    private static Texture2D? CreateTextTexture(GraphicsDevice graphicsDevice, string text, int width, int height, float fontSize)
    {
        using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(System.Drawing.Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new System.Drawing.Font("Segoe UI", fontSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
        var rect = new System.Drawing.RectangleF(0, 0, width, height);
        using var format = new System.Drawing.StringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center
        };
        g.DrawString(text, font, brush, rect, format);

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return Texture2D.FromStream(graphicsDevice, stream);
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, IntPtr returnString, int returnLength, IntPtr hwndCallback);
}

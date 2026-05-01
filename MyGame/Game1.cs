using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private KeyboardState _prevKeyboard;
    private GameLevel? _gameLevel;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.HardwareModeSwitch = false;
        _graphics.IsFullScreen = true;
    }

    protected override void Initialize()
    {
        Window.Title = "Человек-Паук: День в Урфу [BUILD 2026-04-29-FIX4]";
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

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

        _gameLevel?.Update(gameTime);

        _prevKeyboard = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _gameLevel?.Draw(_spriteBatch, GraphicsDevice);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _gameLevel?.Unload();
        base.UnloadContent();
    }
}

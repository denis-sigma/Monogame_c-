using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class GameLevel
{
    private enum TransitionState
    {
        None,
        FadeIn,
        FadeOut
    }

    private Texture2D? _currentMapTexture;
    private Texture2D? _newYorkTexture;
    private Texture2D? _cityChurchTexture;
    private Texture2D? _transitionTexture;
    private Player? _player;
    private Bandit? _secondMapBandit;
    private Vector2 _cameraPosition = Vector2.Zero;
    private Rectangle _levelBounds;

    private TransitionState _transitionState = TransitionState.None;
    private float _transitionAlpha;
    private bool _isSecondMapLoaded;
    private float _pathY;

    private const float TransitionDuration = 0.9f;
    private const float RightEdgeTriggerPadding = 36f;
    private const float SpawnPaddingOnNextMap = 60f;
    private const float PathYRatio = 0.70f;
    private const float BanditSpawnOffsetX = 520f;
    private const string SpriteFolderPath = @"c:\Users\user\Desktop\спрайт";

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _newYorkTexture = LoadTextureWithFallback(
            graphicsDevice,
            "new-york.png",
            Path.Combine(SpriteFolderPath, "new-york.png"));

        _cityChurchTexture = LoadTextureWithFallback(
            graphicsDevice,
            "переход-город-церковь.png",
            Path.Combine(SpriteFolderPath, "переход-город-церковь.png"));

        _transitionTexture = LoadTextureWithFallback(
            graphicsDevice,
            "переход.png",
            Path.Combine(SpriteFolderPath, "переход.png"));

        _currentMapTexture = _newYorkTexture ?? LoadTextureWithFallback(graphicsDevice, "level1.png", null);
        if (_currentMapTexture == null)
        {
            return;
        }

        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player = new Player(new Vector2(120f, _pathY));
        _player.LoadContent(graphicsDevice);
        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);

        _secondMapBandit = new Bandit(new Vector2(SpawnPaddingOnNextMap + BanditSpawnOffsetX, _pathY));
        _secondMapBandit.LoadContent(graphicsDevice);
        _secondMapBandit.SetWorld(_levelBounds, _pathY);
    }

    private static Texture2D? LoadTextureWithFallback(GraphicsDevice graphicsDevice, string contentFileName, string? externalSourcePath)
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Content", contentFileName);
        SyncFromExternalSource(contentPath, externalSourcePath);

        if (File.Exists(contentPath))
        {
            using var contentStream = File.OpenRead(contentPath);
            return Texture2D.FromStream(graphicsDevice, contentStream);
        }

        if (!string.IsNullOrWhiteSpace(externalSourcePath) && File.Exists(externalSourcePath))
        {
            using var externalStream = File.OpenRead(externalSourcePath);
            return Texture2D.FromStream(graphicsDevice, externalStream);
        }

        return null;
    }

    private static void SyncFromExternalSource(string destinationPath, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        var destinationDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static float ComputePathY(Texture2D mapTexture)
    {
        return mapTexture.Height * PathYRatio;
    }

    public void Update(GameTime gameTime)
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _player.Update(gameTime, Keyboard.GetState());

        if (!_isSecondMapLoaded && _transitionState == TransitionState.None)
        {
            var triggerX = _levelBounds.Right - RightEdgeTriggerPadding;
            if (_player.Position.X >= triggerX)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
        }

        UpdateTransition(delta);

        if (_isSecondMapLoaded && _secondMapBandit != null)
        {
            _secondMapBandit.Update(gameTime, _player);
        }

        UpdateCamera();
    }

    private void UpdateTransition(float delta)
    {
        if (_transitionState == TransitionState.None)
        {
            return;
        }

        if (_transitionState == TransitionState.FadeIn)
        {
            _transitionAlpha += delta / TransitionDuration;
            if (_transitionAlpha >= 1f)
            {
                _transitionAlpha = 1f;
                SwapToSecondMap();
                _transitionState = TransitionState.FadeOut;
            }

            return;
        }

        _transitionAlpha -= delta / TransitionDuration;
        if (_transitionAlpha <= 0f)
        {
            _transitionAlpha = 0f;
            _transitionState = TransitionState.None;
        }
    }

    private void SwapToSecondMap()
    {
        if (_cityChurchTexture == null || _player == null || _isSecondMapLoaded)
        {
            return;
        }

        _currentMapTexture = _cityChurchTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));

        _secondMapBandit?.SetWorld(_levelBounds, _pathY);

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
    }

    private void UpdateCamera()
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        var viewport = new Rectangle(0, 0, 1280, 720);
        _cameraPosition.X = _player.Position.X - viewport.Width / 2f;
        _cameraPosition.Y = _player.Position.Y - viewport.Height / 2f;

        var maxCameraX = Math.Max(0, _currentMapTexture.Width - viewport.Width);
        var maxCameraY = Math.Max(0, _currentMapTexture.Height - viewport.Height);

        _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, maxCameraX);
        _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, maxCameraY);
    }

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        var viewport = graphicsDevice.Viewport;
        var sourceRect = new Rectangle(
            (int)_cameraPosition.X,
            (int)_cameraPosition.Y,
            Math.Min(viewport.Width, _currentMapTexture.Width),
            Math.Min(viewport.Height, _currentMapTexture.Height));

        if (sourceRect.Right > _currentMapTexture.Width)
        {
            sourceRect.Width = _currentMapTexture.Width - sourceRect.X;
        }

        if (sourceRect.Bottom > _currentMapTexture.Height)
        {
            sourceRect.Height = _currentMapTexture.Height - sourceRect.Y;
        }

        var destinationRect = new Rectangle(0, 0, sourceRect.Width, sourceRect.Height);
        spriteBatch.Draw(_currentMapTexture, destinationRect, sourceRect, Color.White);

        if (_isSecondMapLoaded && _secondMapBandit != null)
        {
            _secondMapBandit.Draw(spriteBatch, _cameraPosition);
        }

        _player.Draw(spriteBatch, _cameraPosition);

        if (_transitionAlpha > 0f)
        {
            var overlayRect = new Rectangle(0, 0, viewport.Width, viewport.Height);
            if (_transitionTexture != null)
            {
                spriteBatch.Draw(_transitionTexture, overlayRect, Color.White * _transitionAlpha);
            }
            else
            {
                spriteBatch.Draw(_currentMapTexture, overlayRect, sourceRect, Color.Black * _transitionAlpha);
            }
        }
    }

    public void Unload()
    {
        _newYorkTexture?.Dispose();
        _cityChurchTexture?.Dispose();
        _transitionTexture?.Dispose();
        _player?.Unload();
        _secondMapBandit?.Unload();
    }
}

using System;
using System.Collections.Generic;
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
    private Texture2D? _thirdMapTexture;
    private Texture2D? _transitionTexture;
    private Texture2D? _uiPixel;
    private Player? _player;
    private Venom? _venom;
    private Hostage? _hostage;
    private readonly List<Bandit> _secondMapBandits = new();
    private Vector2 _cameraPosition = Vector2.Zero;
    private Rectangle _levelBounds;
    private int _viewWidth = 1280;
    private int _viewHeight = 720;

    private TransitionState _transitionState = TransitionState.None;
    private float _transitionAlpha;
    private bool _isSecondMapLoaded;
    private bool _isThirdMapLoaded;
    private KeyboardState _previousKeyboardState;
    private float _pathY;

    private const float TransitionDuration = 0.9f;
    private const float RightEdgeTriggerPadding = -320f;
    private const float SpawnPaddingOnNextMap = 60f;
    private const float PathYRatio = 0.94f;
    private const float BottomGroundOffset = 72f;
    private const float CharacterDropOffset = 340f;
    private const float CharacterVisualDropOffset = 190f;
    private const float BanditSpawnOffsetX = 420f;
    private const int SecondMapBanditCount = 5;
    private const float BanditSpawnGapX = 190f;
    private const float VenomSpawnOffsetX = 760f;
    private const float HostageSpawnPaddingRight = -28f;
    private const float RescueHoldDuration = 1.2f;
    private const float RescueVisualOffset = 130f;
    private const string SpriteFolderPath = @"c:\Users\user\Desktop\спрайт";
    private const float MaxPlayerHealth = 100f;
    private const float BanditBulletDamage = MaxPlayerHealth / 20f;
    private float _playerHealth = MaxPlayerHealth;
    private float _rescueHoldTimer;
    private bool _showRescuePrompt;
    private Texture2D? _rescuePromptTexture;
    private Texture2D? _rescueKeyTexture;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _newYorkTexture = LoadTextureWithFallback(
            graphicsDevice,
            "new-york.jpeg",
            @"c:\Users\user\Downloads\new-york.jpeg");

        _cityChurchTexture = LoadTextureWithFallback(
            graphicsDevice,
            "переход-город-церковь.jpeg",
            @"c:\Users\user\Downloads\переход-город-церковь.jpeg");

        _thirdMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "third-map.jpeg",
            @"c:\Users\user\Downloads\церковь.jpeg");

        _transitionTexture = LoadTextureWithFallback(
            graphicsDevice,
            "переход-ezremove.png",
            @"c:\Users\user\Downloads\переход-ezremove.png");

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

        _venom = new Venom(new Vector2(SpawnPaddingOnNextMap + VenomSpawnOffsetX, _pathY));
        _venom.LoadContent(graphicsDevice);
        _hostage = new Hostage(new Vector2(SpawnPaddingOnNextMap + VenomSpawnOffsetX + 280f, _pathY));
        _hostage.LoadContent(graphicsDevice);

        _secondMapBandits.Clear();
        for (var i = 0; i < SecondMapBanditCount; i++)
        {
            var spawnX = SpawnPaddingOnNextMap + BanditSpawnOffsetX + i * BanditSpawnGapX;
            var bandit = new Bandit(new Vector2(spawnX, _pathY));
            bandit.LoadContent(graphicsDevice);
            bandit.SetWorld(_levelBounds, _pathY);
            _secondMapBandits.Add(bandit);
        }

        _uiPixel = new Texture2D(graphicsDevice, 1, 1);
        _uiPixel.SetData(new[] { Color.White });
        _rescuePromptTexture = CreateTextTexture(graphicsDevice, "Спасти", 180, 56, 28f);
        _rescueKeyTexture = CreateTextTexture(graphicsDevice, "TAB", 110, 52, 24f);
        _playerHealth = MaxPlayerHealth;
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
        var ratioY = mapTexture.Height * PathYRatio;
        var bottomY = mapTexture.Height - BottomGroundOffset;
        var rawY = MathF.Max(ratioY, bottomY) + CharacterDropOffset;
        return MathHelper.Clamp(rawY, 0f, mapTexture.Height);
    }

    public void Update(GameTime gameTime)
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboardState = Keyboard.GetState();
        if (HandleDebugMapShortcuts(keyboardState))
        {
            _previousKeyboardState = keyboardState;
            UpdateCamera();
            return;
        }

        _player.Update(gameTime, keyboardState);

        if (_transitionState == TransitionState.None)
        {
            var triggerX = _levelBounds.Right - RightEdgeTriggerPadding;
            if (!_isSecondMapLoaded && _player.Position.X >= triggerX)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (_isSecondMapLoaded && !_isThirdMapLoaded && _player.Position.X >= triggerX)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
        }

        UpdateTransition(delta);

        if (_isSecondMapLoaded && !_isThirdMapLoaded && _secondMapBandits.Count > 0)
        {
            foreach (var bandit in _secondMapBandits)
            {
                if (!bandit.IsAlive)
                {
                    continue;
                }

                bandit.Update(gameTime, _player);
                _player.TryHitBandit(bandit);
            }
        }

        if (_isThirdMapLoaded && _venom != null)
        {
            _venom.Update(gameTime, _player);
            _player.TryHitVenom(_venom);
        }

        UpdateHostageRescue(gameTime, keyboardState);

        var pendingHits = _player.ConsumePendingHits();
        if (pendingHits > 0)
        {
            _playerHealth = MathHelper.Clamp(_playerHealth - pendingHits * BanditBulletDamage, 0f, MaxPlayerHealth);
        }

        UpdateCamera();
        _previousKeyboardState = keyboardState;
    }

    private bool HandleDebugMapShortcuts(KeyboardState keyboardState)
    {
        if (keyboardState.IsKeyDown(Keys.D1) && !_previousKeyboardState.IsKeyDown(Keys.D1))
        {
            SwapToFirstMap();
            return true;
        }

        if (keyboardState.IsKeyDown(Keys.D2) && !_previousKeyboardState.IsKeyDown(Keys.D2))
        {
            SwapToSecondMap();
            return true;
        }

        if (keyboardState.IsKeyDown(Keys.D3) && !_previousKeyboardState.IsKeyDown(Keys.D3))
        {
            if (!_isSecondMapLoaded)
            {
                SwapToSecondMap();
            }

            SwapToThirdMap();
            return true;
        }

        return false;
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
                if (!_isSecondMapLoaded)
                {
                    SwapToSecondMap();
                }
                else if (!_isThirdMapLoaded)
                {
                    SwapToThirdMap();
                }
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

        foreach (var bandit in _secondMapBandits)
        {
            bandit.SetWorld(_levelBounds, _pathY);
        }

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
    }

    private void SwapToFirstMap()
    {
        if (_newYorkTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _newYorkTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(120f, _pathY));

        _cameraPosition = Vector2.Zero;
        _transitionState = TransitionState.None;
        _transitionAlpha = 0f;
        _isSecondMapLoaded = false;
        _isThirdMapLoaded = false;
        _rescueHoldTimer = 0f;
        _showRescuePrompt = false;
        if (_hostage != null)
        {
            _hostage.SetPosition(new Vector2(_levelBounds.Right - HostageSpawnPaddingRight, _pathY));
        }
    }

    private void SwapToThirdMap()
    {
        if (_thirdMapTexture == null || _player == null || !_isSecondMapLoaded || _isThirdMapLoaded)
        {
            return;
        }

        _currentMapTexture = _thirdMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));

        _venom?.SetWorld(_levelBounds, _pathY);
        _venom?.SetPosition(new Vector2(SpawnPaddingOnNextMap + VenomSpawnOffsetX, _pathY));
        _hostage?.SetPosition(new Vector2(_levelBounds.Right - HostageSpawnPaddingRight, _pathY));
        _rescueHoldTimer = 0f;
        _showRescuePrompt = false;

        _cameraPosition = Vector2.Zero;
        _isThirdMapLoaded = true;
    }

    private void UpdateCamera()
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        _cameraPosition.X = _player.Position.X - _viewWidth / 2f;
        _cameraPosition.Y = 0f;

        var maxCameraX = Math.Max(0, _currentMapTexture.Width - _viewWidth);
        var maxCameraY = Math.Max(0, _currentMapTexture.Height - _viewHeight);

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
        _viewWidth = viewport.Width;
        _viewHeight = viewport.Height;
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

        var destinationRect = new Rectangle(0, 0, viewport.Width, viewport.Height);
        spriteBatch.Draw(_currentMapTexture, destinationRect, sourceRect, Color.White);

        if (_isSecondMapLoaded && !_isThirdMapLoaded && _secondMapBandits.Count > 0)
        {
            foreach (var bandit in _secondMapBandits)
            {
                if (bandit.IsAlive)
                {
                    bandit.Draw(spriteBatch, _cameraPosition, CharacterVisualDropOffset);
                }
            }
        }

        if (_isThirdMapLoaded && _venom != null)
        {
            var venomYOffset = CharacterVisualDropOffset + _player.VisualHeight - _venom.VisualHeight;
            _venom.Draw(spriteBatch, _cameraPosition, venomYOffset);
        }

        if (_isThirdMapLoaded && _hostage != null)
        {
            var hostageYOffset = CharacterVisualDropOffset + _player.VisualHeight - _hostage.VisualHeight;
            _hostage.Draw(spriteBatch, _cameraPosition, hostageYOffset);
        }

        _player.Draw(spriteBatch, _cameraPosition, CharacterVisualDropOffset);
        DrawHud(spriteBatch);
        DrawRescuePrompt(spriteBatch);

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
        _thirdMapTexture?.Dispose();
        _transitionTexture?.Dispose();
        _uiPixel?.Dispose();
        _rescuePromptTexture?.Dispose();
        _rescueKeyTexture?.Dispose();
        _player?.Unload();
        _venom?.Unload();
        _hostage?.Unload();
        foreach (var bandit in _secondMapBandits)
        {
            bandit.Unload();
        }

        _secondMapBandits.Clear();
    }

    private void DrawHud(SpriteBatch spriteBatch)
    {
        if (_player == null || _uiPixel == null)
        {
            return;
        }

        var barX = 18;
        var barY = 16;
        var barWidth = 260;
        var barHeight = 18;
        var gap = 10;

        var health01 = MaxPlayerHealth <= 0f ? 0f : MathHelper.Clamp(_playerHealth / MaxPlayerHealth, 0f, 1f);
        DrawBar(spriteBatch, new Rectangle(barX, barY, barWidth, barHeight), health01, new Color(215, 45, 45));
        DrawBar(spriteBatch, new Rectangle(barX, barY + barHeight + gap, barWidth, barHeight), _player.Stamina01, new Color(55, 105, 230));
        DrawBar(spriteBatch, new Rectangle(barX, barY + (barHeight + gap) * 2, barWidth, barHeight), _player.WebMeter01, Color.White);
    }

    private void DrawBar(SpriteBatch spriteBatch, Rectangle bounds, float fillPercent, Color fillColor)
    {
        if (_uiPixel == null)
        {
            return;
        }

        fillPercent = MathHelper.Clamp(fillPercent, 0f, 1f);
        spriteBatch.Draw(_uiPixel, bounds, Color.Black * 0.55f);
        var fillWidth = (int)MathF.Round((bounds.Width - 4) * fillPercent);
        if (fillWidth > 0)
        {
            spriteBatch.Draw(
                _uiPixel,
                new Rectangle(bounds.X + 2, bounds.Y + 2, fillWidth, Math.Max(1, bounds.Height - 4)),
                fillColor
            );
        }

        var border = Color.White * 0.8f;
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), border);
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), border);
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), border);
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), border);
    }

    private void UpdateHostageRescue(GameTime gameTime, KeyboardState keyboardState)
    {
        _showRescuePrompt = false;
        if (!_isThirdMapLoaded || _player == null || _hostage == null || _venom == null || !_hostage.IsLoaded)
        {
            return;
        }

        _hostage.Update(gameTime);
        if (_hostage.IsRescued || _venom.IsAlive)
        {
            _rescueHoldTimer = 0f;
            return;
        }

        var playerBounds = _player.GetCollisionBounds();
        var interactionBounds = _hostage.GetCollisionBounds();
        interactionBounds.Inflate(52, 34);
        var nearHostage = interactionBounds.Intersects(playerBounds);
        if (!nearHostage)
        {
            _rescueHoldTimer = 0f;
            return;
        }

        _showRescuePrompt = true;
        if (keyboardState.IsKeyDown(Keys.Tab))
        {
            _rescueHoldTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_rescueHoldTimer >= RescueHoldDuration)
            {
                _rescueHoldTimer = RescueHoldDuration;
                _hostage.CompleteRescue();
                _showRescuePrompt = false;
            }
        }
        else
        {
            _rescueHoldTimer = 0f;
        }
    }

    private void DrawRescuePrompt(SpriteBatch spriteBatch)
    {
        if (!_showRescuePrompt || _player == null || _hostage == null || _uiPixel == null)
        {
            return;
        }

        var hostageScreen = _hostage.Position - _cameraPosition + new Vector2(0f, CharacterVisualDropOffset - RescueVisualOffset);
        var panelX = (int)MathF.Round(hostageScreen.X - 78f);
        var panelY = (int)MathF.Round(hostageScreen.Y - 74f);
        var panel = new Rectangle(panelX, panelY, 220, 94);

        spriteBatch.Draw(_uiPixel, panel, new Color(0, 0, 0, 165));
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), Color.White * 0.9f);
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), Color.White * 0.9f);
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Y, 2, panel.Height), Color.White * 0.9f);
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.Right - 2, panel.Y, 2, panel.Height), Color.White * 0.9f);

        if (_rescuePromptTexture != null)
        {
            spriteBatch.Draw(_rescuePromptTexture, new Vector2(panel.X + 20, panel.Y + 10), Color.White);
        }

        if (_rescueKeyTexture != null)
        {
            spriteBatch.Draw(_rescueKeyTexture, new Vector2(panel.X + 18, panel.Y + 42), Color.White);
        }

        var progress = MathHelper.Clamp(_rescueHoldTimer / RescueHoldDuration, 0f, 1f);
        var barBack = new Rectangle(panel.X + 102, panel.Y + 54, 102, 18);
        var barFill = new Rectangle(barBack.X + 2, barBack.Y + 2, (int)MathF.Round((barBack.Width - 4) * progress), barBack.Height - 4);
        spriteBatch.Draw(_uiPixel, barBack, new Color(35, 35, 35, 220));
        if (barFill.Width > 0)
        {
            spriteBatch.Draw(_uiPixel, barFill, new Color(105, 225, 120));
        }
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.X, barBack.Y, barBack.Width, 1), Color.White * 0.85f);
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.X, barBack.Bottom - 1, barBack.Width, 1), Color.White * 0.85f);
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.X, barBack.Y, 1, barBack.Height), Color.White * 0.85f);
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.Right - 1, barBack.Y, 1, barBack.Height), Color.White * 0.85f);
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
}

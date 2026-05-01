using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class Hostage
{
    private readonly List<Texture2D> _rescueFrames = new();
    private Texture2D? _idleFrame;
    private Vector2 _position;
    private float _rescueAnimationTimer;
    private int _currentRescueFrame;
    private bool _isRescued;

    private const float Scale = 1.35f;
    private const float RescueFrameTime = 0.14f;
    private const string FramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\HostageFrames";

    public bool IsLoaded => _idleFrame != null;
    public bool IsRescued => _isRescued;
    public Vector2 Position => _position;
    public float VisualHeight => (_idleFrame?.Height ?? 0f) * Scale;

    public Hostage(Vector2 startPosition)
    {
        _position = startPosition;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();
        if (!Directory.Exists(FramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(FramesFolderPath, "*.png")
            .OrderBy(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            })
            .ToArray();

        for (var i = 0; i < framePaths.Length; i++)
        {
            using var stream = File.OpenRead(framePaths[i]);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            if (i == 0)
            {
                _idleFrame = texture;
                continue;
            }

            _rescueFrames.Add(texture);
        }
    }

    public void SetPosition(Vector2 position)
    {
        _position = position;
    }

    public Rectangle GetCollisionBounds()
    {
        if (!IsLoaded || _idleFrame == null)
        {
            return Rectangle.Empty;
        }

        var width = (int)(_idleFrame.Width * Scale);
        var height = (int)(_idleFrame.Height * Scale);
        var x = (int)_position.X + width / 6;
        var y = (int)_position.Y + height / 10;
        var collisionWidth = Math.Max(22, width - width * 2 / 6);
        var collisionHeight = Math.Max(26, height - height / 5);
        return new Rectangle(x, y, collisionWidth, collisionHeight);
    }

    public void CompleteRescue()
    {
        _isRescued = true;
        _currentRescueFrame = 0;
        _rescueAnimationTimer = 0f;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsLoaded || !_isRescued || _rescueFrames.Count == 0)
        {
            return;
        }

        _rescueAnimationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_rescueAnimationTimer < RescueFrameTime)
        {
            return;
        }

        _rescueAnimationTimer -= RescueFrameTime;
        if (_currentRescueFrame < _rescueFrames.Count - 1)
        {
            _currentRescueFrame++;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset = 0f)
    {
        if (!IsLoaded)
        {
            return;
        }

        var texture = GetCurrentTexture();
        if (texture == null)
        {
            return;
        }

        var drawPosition = _position - cameraPosition + new Vector2(0f, visualYOffset);
        spriteBatch.Draw(
            texture,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            Scale,
            SpriteEffects.None,
            0f
        );
    }

    public void Unload()
    {
        _idleFrame?.Dispose();
        _idleFrame = null;
        foreach (var frame in _rescueFrames)
        {
            frame.Dispose();
        }

        _rescueFrames.Clear();
    }

    private Texture2D? GetCurrentTexture()
    {
        if (_isRescued && _rescueFrames.Count > 0)
        {
            return _rescueFrames[Math.Clamp(_currentRescueFrame, 0, _rescueFrames.Count - 1)];
        }

        return _idleFrame;
    }

    private static void ApplyBlackKey(Texture2D texture)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        for (var i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];
            if (pixel.A > 0 && pixel.R < 20 && pixel.G < 20 && pixel.B < 20)
            {
                pixels[i] = Color.Transparent;
            }
        }

        texture.SetData(pixels);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class Bandit
{
    private sealed class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float Rotation;
    }

    private readonly List<Texture2D> _moveFrames = new();
    private readonly List<Bullet> _bullets = new();
    private Texture2D? _shootTexture;
    private Texture2D? _bulletSpriteSheet;
    private Rectangle _bulletRect;
    private Vector2 _position;
    private Vector2 _spawnPoint;
    private Rectangle _worldBounds;
    private float _pathY;
    private float _moveTimer;
    private float _shootTimer;
    private int _currentMoveFrame;
    private float _bulletCooldown = ShootCooldown;
    private bool _isShooting;
    private bool _facingRight = true;
    private float _patrolDirection = 1f;

    private const float Scale = 1.7f;
    private const float MoveSpeed = 70f;
    private const float PatrolRange = 170f;
    private const float DetectionRange = 460f;
    private const float ShootRange = 380f;
    private const float ShootCooldown = 1.25f;
    private const float BulletSpeed = 250f;
    private const float BulletLifetime = 3f;
    private const float MoveFrameTime = 0.12f;
    private const float ShootFrameTime = 0.09f;
    private const string MoveFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditFrames";
    private const string ShootSpriteSourcePath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditShootBase\sprite_1.png";
    private const string BulletSpriteSourcePath = @"c:\Users\user\Downloads\снаряд.png";

    public bool IsLoaded => _moveFrames.Count > 0;

    public Bandit(Vector2 startPosition)
    {
        _position = startPosition;
        _spawnPoint = startPosition;
        _pathY = startPosition.Y;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();

        LoadMoveFrames(graphicsDevice);
        _shootTexture = LoadTexture(graphicsDevice, "bandit-shoot.png", ShootSpriteSourcePath);
        if (_shootTexture != null)
        {
            ApplyBlackKey(_shootTexture);
        }

        _bulletSpriteSheet = LoadTexture(graphicsDevice, "bandit-bullet.png", BulletSpriteSourcePath);
        if (_bulletSpriteSheet != null)
        {
            ApplyBlackKey(_bulletSpriteSheet);
            _bulletRect = ExtractOpaqueBounds(_bulletSpriteSheet);
        }
    }

    public void SetWorld(Rectangle worldBounds, float pathY)
    {
        _worldBounds = worldBounds;
        _pathY = pathY;
        _position.Y = pathY;
        _spawnPoint.Y = pathY;
    }

    public void Update(GameTime gameTime, Player player)
    {
        if (!IsLoaded)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var toPlayer = player.Position - _position;
        var distanceToPlayer = toPlayer.Length();

        if (distanceToPlayer > 1f)
        {
            _facingRight = toPlayer.X >= 0f;
        }

        _bulletCooldown -= delta;
        UpdateBullets(delta, player);

        if (_isShooting)
        {
            _shootTimer += delta;
            if (_shootTimer >= ShootFrameTime)
            {
                _shootTimer = 0f;
                FireAt(player.Position);
                _isShooting = false;
            }

            return;
        }

        var shouldShoot = distanceToPlayer <= ShootRange && MathF.Abs(toPlayer.X) > 12f && _bulletCooldown <= 0f;
        if (shouldShoot && _shootTexture != null)
        {
            _isShooting = true;
            _shootTimer = 0f;
            _bulletCooldown = ShootCooldown;
            return;
        }

        UpdateMovement(delta, distanceToPlayer);
    }

    private void UpdateMovement(float delta, float distanceToPlayer)
    {
        var isAlert = distanceToPlayer <= DetectionRange;
        var leftLimit = _spawnPoint.X - PatrolRange;
        var rightLimit = _spawnPoint.X + PatrolRange;

        if (isAlert)
        {
            _patrolDirection = _facingRight ? 1f : -1f;
        }
        else if (_position.X <= leftLimit)
        {
            _patrolDirection = 1f;
            _facingRight = true;
        }
        else if (_position.X >= rightLimit)
        {
            _patrolDirection = -1f;
            _facingRight = false;
        }

        _position.X += _patrolDirection * MoveSpeed * delta;
        _position.X = MathHelper.Clamp(_position.X, Math.Max(_worldBounds.Left, leftLimit), Math.Min(_worldBounds.Right, rightLimit));
        _position.Y = _pathY;
        _facingRight = _patrolDirection >= 0f;

        if (_moveFrames.Count == 0)
        {
            return;
        }

        _moveTimer += delta;
        if (_moveTimer >= MoveFrameTime)
        {
            _moveTimer -= MoveFrameTime;
            _currentMoveFrame = (_currentMoveFrame + 1) % Math.Max(1, _moveFrames.Count);
        }
    }

    private void UpdateBullets(float delta, Player player)
    {
        for (var i = _bullets.Count - 1; i >= 0; i--)
        {
            var bullet = _bullets[i];
            bullet.Position += bullet.Velocity * delta;
            bullet.Life -= delta;

            var hitWorld = bullet.Position.X < _worldBounds.Left ||
                           bullet.Position.X > _worldBounds.Right ||
                           bullet.Position.Y < _worldBounds.Top ||
                           bullet.Position.Y > _worldBounds.Bottom;

            if (player.GetCollisionBounds().Contains(bullet.Position))
            {
                player.NotifyHit();
                _bullets.RemoveAt(i);
                continue;
            }

            if (hitWorld || bullet.Life <= 0f)
            {
                _bullets.RemoveAt(i);
                continue;
            }

            _bullets[i] = bullet;
        }
    }

    private void FireAt(Vector2 playerPosition)
    {
        if (_bulletSpriteSheet == null)
        {
            return;
        }

        var direction = playerPosition - _position;
        if (direction == Vector2.Zero)
        {
            direction = _facingRight ? Vector2.UnitX : -Vector2.UnitX;
        }

        direction.Normalize();
        var muzzleOffset = _facingRight ? new Vector2(44f, 26f) : new Vector2(10f, 26f);
        var spawnPosition = _position + muzzleOffset * Scale;

        _bullets.Add(new Bullet
        {
            Position = spawnPosition,
            Velocity = direction * BulletSpeed,
            Life = BulletLifetime,
            Rotation = MathF.Atan2(direction.Y, direction.X)
        });
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition)
    {
        if (!IsLoaded)
        {
            return;
        }

        var currentTexture = _isShooting && _shootTexture != null
            ? _shootTexture
            : _moveFrames[_currentMoveFrame];

        var effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var drawPosition = _position - cameraPosition;

        spriteBatch.Draw(
            currentTexture,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            Scale,
            effects,
            0f);

        DrawBullets(spriteBatch, cameraPosition);
    }

    private void DrawBullets(SpriteBatch spriteBatch, Vector2 cameraPosition)
    {
        if (_bulletSpriteSheet == null || _bulletRect.Width <= 0 || _bulletRect.Height <= 0)
        {
            return;
        }

        var origin = new Vector2(_bulletRect.Width / 2f, _bulletRect.Height / 2f);
        foreach (var bullet in _bullets)
        {
            var drawPosition = bullet.Position - cameraPosition;
            spriteBatch.Draw(
                _bulletSpriteSheet,
                new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
                _bulletRect,
                Color.White,
                bullet.Rotation,
                origin,
                1.4f,
                SpriteEffects.None,
                0f);
        }
    }

    public void Unload()
    {
        foreach (var frame in _moveFrames)
        {
            frame.Dispose();
        }

        _moveFrames.Clear();
        _shootTexture?.Dispose();
        _shootTexture = null;
        _bulletSpriteSheet?.Dispose();
        _bulletSpriteSheet = null;
        _bullets.Clear();
    }

    private void LoadMoveFrames(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(MoveFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(MoveFramesFolderPath, "*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _moveFrames.Add(texture);
        }
    }

    private static Texture2D? LoadTexture(GraphicsDevice graphicsDevice, string contentFileName, string externalSourcePath)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Content", contentFileName);
        if (File.Exists(externalSourcePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(externalSourcePath, outputPath, overwrite: true);
        }

        var loadPath = File.Exists(outputPath) ? outputPath : externalSourcePath;
        if (!File.Exists(loadPath))
        {
            return null;
        }

        using var stream = File.OpenRead(loadPath);
        return Texture2D.FromStream(graphicsDevice, stream);
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

    private static Rectangle ExtractOpaqueBounds(Texture2D texture)
    {
        var width = texture.Width;
        var height = texture.Height;
        var pixels = new Color[width * height];
        texture.GetData(pixels);

        var minX = width - 1;
        var maxX = 0;
        var minY = height - 1;
        var maxY = 0;
        var found = false;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (pixels[y * width + x].A <= 0)
                {
                    continue;
                }

                found = true;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
        }

        return found
            ? new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)
            : new Rectangle(0, 0, 0, 0);
    }
}

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
    private readonly List<Texture2D> _shootFrames = new();
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
    private int _currentShootFrame;
    private int _remainingBurstShots;
    private float _bulletCooldown;
    private float _betweenBurstShotTimer;
    private bool _isShooting;
    private bool _facingRight = true;
    private float _patrolDirection = 1f;
    private float _webHitFlashTimer;
    private int _health = 5;
    private const float WebHitFlashDuration = 0.12f;

    private const float Scale = 1.7f;
    private const float MoveSpeed = 70f;
    private const float PatrolRange = 170f;
    private const float DetectionRange = 460f;
    private const float ShootRange = 380f;
    private const int BurstShotCount = 3;
    private const float BurstShotInterval = 0.18f;
    private const float BurstCooldown = 5f;
    private const float BulletSpeed = 250f;
    private const float BulletLifetime = 3f;
    private const float BulletWorldBoundsPadding = 180f;
    private const float MoveFrameTime = 0.12f;
    private const float ShootFrameTime = 0.09f;
    private const string MoveFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditFrames";
    private const string ShootFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditShootBase";
    private const string ShootSpriteSourcePath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditShootBase\sprite_1.png";
    private const string BulletSpriteSourcePath = @"c:\Users\user\Downloads\снаряд.png";

    public bool IsLoaded => _moveFrames.Count > 0;
    public bool IsAlive => _health > 0;

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
        LoadShootFrames(graphicsDevice);
        _shootTexture = LoadTexture(graphicsDevice, "bandit-shoot.png", ShootSpriteSourcePath);
        if (_shootTexture != null)
        {
            ApplyBlackKey(_shootTexture);
            if (_shootFrames.Count == 0)
            {
                _shootFrames.Add(_shootTexture);
            }
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
        if (!IsLoaded || !IsAlive)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_webHitFlashTimer > 0f)
        {
            _webHitFlashTimer = Math.Max(0f, _webHitFlashTimer - delta);
        }

        var toPlayer = player.Position - _position;
        var distanceToPlayer = toPlayer.Length();

        if (distanceToPlayer > 1f)
        {
            _facingRight = toPlayer.X >= 0f;
        }

        _bulletCooldown = Math.Max(0f, _bulletCooldown - delta);
        if (_betweenBurstShotTimer > 0f)
        {
            _betweenBurstShotTimer -= delta;
            if (_betweenBurstShotTimer <= 0f && _remainingBurstShots > 0 && (_shootFrames.Count > 0 || _shootTexture != null))
            {
                BeginShootAnimation();
            }
        }
        UpdateBullets(delta, player);

        if (_isShooting)
        {
            _shootTimer += delta;
            var frameSourceCount = _shootFrames.Count > 0 ? _shootFrames.Count : (_shootTexture != null ? 1 : 0);
            if (frameSourceCount > 1)
            {
                var progress = MathHelper.Clamp(_shootTimer / ShootFrameTime, 0f, 0.999f);
                _currentShootFrame = Math.Min(frameSourceCount - 1, (int)(progress * frameSourceCount));
            }

            if (_shootTimer >= ShootFrameTime)
            {
                _shootTimer = 0f;
                FireAt(player.Position);
                _isShooting = false;
                _remainingBurstShots = Math.Max(0, _remainingBurstShots - 1);
                _currentShootFrame = 0;
                if (_remainingBurstShots > 0)
                {
                    _betweenBurstShotTimer = BurstShotInterval;
                }
                else
                {
                    _bulletCooldown = BurstCooldown;
                }
            }

            return;
        }

        var shouldShoot = distanceToPlayer <= ShootRange && MathF.Abs(toPlayer.X) > 12f && _bulletCooldown <= 0f;
        if (shouldShoot && _remainingBurstShots <= 0 && (_shootFrames.Count > 0 || _shootTexture != null))
        {
            _remainingBurstShots = BurstShotCount;
            BeginShootAnimation();
            return;
        }

        UpdateMovement(delta, distanceToPlayer);
    }

    public void NotifyWebHit()
    {
        _webHitFlashTimer = WebHitFlashDuration;
    }

    public bool ApplyDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return false;
        }

        _health = Math.Max(0, _health - amount);
        _webHitFlashTimer = WebHitFlashDuration;
        return _health == 0;
    }

    public Rectangle GetCollisionBounds()
    {
        if (!IsLoaded || !IsAlive)
        {
            return new Rectangle(0, 0, 0, 0);
        }

        var currentTexture = _isShooting && _shootTexture != null ? _shootTexture : _moveFrames[_currentMoveFrame];
        var width = (int)(currentTexture.Width * Scale);
        var height = (int)(currentTexture.Height * Scale);
        var x = (int)_position.X + width / 5;
        var y = (int)_position.Y + height / 7;
        var collisionWidth = Math.Max(18, width - width * 2 / 5);
        var collisionHeight = Math.Max(24, height - height / 4);
        return new Rectangle(x, y, collisionWidth, collisionHeight);
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
        var playerBulletBounds = player.GetBulletCollisionBounds();
        for (var i = _bullets.Count - 1; i >= 0; i--)
        {
            var bullet = _bullets[i];
            bullet.Position += bullet.Velocity * delta;
            bullet.Life -= delta;

            var hitWorld = bullet.Position.X < _worldBounds.Left - BulletWorldBoundsPadding ||
                           bullet.Position.X > _worldBounds.Right + BulletWorldBoundsPadding ||
                           bullet.Position.Y < _worldBounds.Top - BulletWorldBoundsPadding ||
                           bullet.Position.Y > _worldBounds.Bottom + BulletWorldBoundsPadding;

            if (playerBulletBounds.Contains(bullet.Position))
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

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset = 0f)
    {
        if (!IsLoaded || !IsAlive)
        {
            return;
        }

        var currentTexture = _moveFrames[_currentMoveFrame];
        if (_isShooting)
        {
            if (_shootFrames.Count > 0)
            {
                currentTexture = _shootFrames[Math.Clamp(_currentShootFrame, 0, _shootFrames.Count - 1)];
            }
            else if (_shootTexture != null)
            {
                currentTexture = _shootTexture;
            }
        }

        var effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var visualOffset = new Vector2(0f, visualYOffset);
        var drawPosition = _position - cameraPosition + visualOffset;

        var tint = _webHitFlashTimer > 0f ? new Color(175, 225, 255) : Color.White;
        spriteBatch.Draw(
            currentTexture,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            tint,
            0f,
            Vector2.Zero,
            Scale,
            effects,
            0f);

        DrawBullets(spriteBatch, cameraPosition, visualOffset);
    }

    private void DrawBullets(SpriteBatch spriteBatch, Vector2 cameraPosition, Vector2 visualOffset)
    {
        if (_bulletSpriteSheet == null || _bulletRect.Width <= 0 || _bulletRect.Height <= 0)
        {
            return;
        }

        var origin = new Vector2(_bulletRect.Width / 2f, _bulletRect.Height / 2f);
        foreach (var bullet in _bullets)
        {
            var drawPosition = bullet.Position - cameraPosition + visualOffset;
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
        foreach (var frame in _shootFrames)
        {
            if (!ReferenceEquals(frame, _shootTexture))
            {
                frame.Dispose();
            }
        }

        _shootFrames.Clear();
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

    private void LoadShootFrames(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(ShootFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(ShootFramesFolderPath, "*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _shootFrames.Add(texture);
        }
    }

    private void BeginShootAnimation()
    {
        _isShooting = true;
        _shootTimer = 0f;
        _currentShootFrame = 0;
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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
    private readonly List<Texture2D> _meleeFrames = new();
    private readonly List<Bullet> _bullets = new();
    private Texture2D? _shootTexture;
    private Texture2D? _bulletSpriteSheet;
    private SoundEffect? _bigBanditPunchSound;
    private ExternalMp3Player? _bigBanditPunchPlayer;
    private ExternalMp3Player? _shootSoundPlayer;
    private ExternalMp3Player? _deathSoundPlayer;
    private Rectangle _bulletRect;
    private Vector2 _position;
    private Vector2 _spawnPoint;
    private readonly float _engageOffsetX;
    private Rectangle _worldBounds;
    private readonly string? _customMoveFramesZipPath;
    private readonly string? _customMeleeFramesZipPath;
    private readonly bool _canShoot;
    private readonly bool _canTakeWebDamage;
    private float _pathY;
    private float _moveTimer;
    private float _shootTimer;
    private int _currentMoveFrame;
    private int _currentShootFrame;
    private int _currentMeleeFrame;
    private int _remainingBurstShots;
    private float _bulletCooldown;
    private float _betweenBurstShotTimer;
    private bool _isShooting;
    private bool _isMeleeAttacking;
    private bool _meleeDamageApplied;
    private bool _facingRight = true;
    private float _patrolDirection = 1f;
    private float _webHitFlashTimer;
    private int _health = 5;
    private const float WebHitFlashDuration = 0.12f;

    private const float Scale = 1.7f;
    private const float MoveSpeed = 70f;
    private const float PatrolRange = 170f;
    private const float EngageSpread = 520f;
    private const float EngageStopDistance = 20f;
    private const float DetectionRange = 2200f;
    private const float ShootRange = 1800f;
    private const int BurstShotCount = 3;
    private const float BurstShotInterval = 0.18f;
    private const float BurstCooldown = 5f;
    private const float BulletSpeed = 250f;
    private const float BulletLifetime = 12f;
    private const float BulletWorldBoundsPadding = 4000f;
    private const float MoveFrameTime = 0.12f;
    private const float ShootFrameTime = 0.09f;
    private const float MeleeFrameTime = 0.11f;
    private const float MeleeRange = 88f;
    private const float MeleeCooldown = 1.15f;
    private const string MoveFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditFrames";
    private const string ShootFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditShootBase";
    private const string ShootSpriteSourcePath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\BanditShootBase\sprite_1.png";
    private const string BulletSpriteSourcePath = @"c:\Users\user\Downloads\снаряд.png";
    private const string BigBanditPunchSoundPath = @"c:\Users\user\Downloads\mixkit-soft-quick-punch-2151.wav";
    private const string ShootSoundPath = @"c:\Users\user\Downloads\sudden-excellent-well-aimed-shot-from-a-pistol.mp3";
    private const string DeathSoundPath = @"c:\Users\user\Downloads\silent-fall-into-the-sand.mp3";

    public bool IsLoaded => _moveFrames.Count > 0;
    public bool IsAlive => _health > 0;
    public bool CanTakeWebDamage => _canTakeWebDamage;

    public Bandit(
        Vector2 startPosition,
        string? customMoveFramesZipPath = null,
        bool canShoot = true,
        string? customMeleeFramesZipPath = null,
        bool canTakeWebDamage = true)
    {
        _position = startPosition;
        _spawnPoint = startPosition;
        _customMoveFramesZipPath = customMoveFramesZipPath;
        _customMeleeFramesZipPath = customMeleeFramesZipPath;
        _canShoot = canShoot;
        _canTakeWebDamage = canTakeWebDamage;
        _pathY = startPosition.Y;
        // Deterministic per-bandit combat offset so they spread around player instead of stacking.
        var seed = startPosition.X * 0.037f + startPosition.Y * 0.013f;
        _engageOffsetX = MathF.Sin(seed) * EngageSpread;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();

        LoadMoveFrames(graphicsDevice);
        LoadMeleeFrames(graphicsDevice);
        LoadBigBanditPunchSound();
        LoadShootSound();
        LoadDeathSound();
        if (_canShoot)
        {
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

        if (_isMeleeAttacking)
        {
            UpdateMeleeAttack(delta, player);
            return;
        }

        if (!_canShoot && _meleeFrames.Count > 0 && distanceToPlayer <= MeleeRange && _bulletCooldown <= 0f)
        {
            BeginMeleeAttack();
            return;
        }

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

        var shouldShoot = _canShoot && distanceToPlayer <= ShootRange && MathF.Abs(toPlayer.X) > 12f && _bulletCooldown <= 0f;
        if (shouldShoot && _remainingBurstShots <= 0 && (_shootFrames.Count > 0 || _shootTexture != null))
        {
            _remainingBurstShots = BurstShotCount;
            BeginShootAnimation();
            return;
        }

        UpdateMovement(delta, distanceToPlayer, player.Position.X);
    }

    private void UpdateMeleeAttack(float delta, Player player)
    {
        _shootTimer += delta;
        if (!_meleeDamageApplied && _currentMeleeFrame >= Math.Max(1, _meleeFrames.Count / 2))
        {
            var attackBounds = GetMeleeAttackBounds();
            if (!player.IsCrouching && attackBounds.Intersects(player.GetCollisionBounds()))
            {
                player.NotifyHit();
                PlayBigBanditPunchSound();
                _meleeDamageApplied = true;
            }
        }

        if (_shootTimer < MeleeFrameTime)
        {
            return;
        }

        _shootTimer -= MeleeFrameTime;
        _currentMeleeFrame++;
        if (_currentMeleeFrame < _meleeFrames.Count)
        {
            return;
        }

        _currentMeleeFrame = 0;
        _isMeleeAttacking = false;
        _meleeDamageApplied = false;
        _bulletCooldown = MeleeCooldown;
    }

    public void NotifyWebHit()
    {
        if (!_canTakeWebDamage)
        {
            return;
        }

        _webHitFlashTimer = WebHitFlashDuration;
    }

    public void ApplyWebImpact()
    {
        if (_canTakeWebDamage)
        {
            NotifyWebHit();
        }
    }

    public bool ApplyDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return false;
        }

        _health = Math.Max(0, _health - amount);
        _webHitFlashTimer = WebHitFlashDuration;
        var died = _health == 0;
        if (died)
        {
            PlayDeathSound();
        }

        return died;
    }

    public Rectangle GetCollisionBounds()
    {
        if (!IsLoaded || !IsAlive)
        {
            return new Rectangle(0, 0, 0, 0);
        }

        var currentTexture = GetCurrentBodyTexture();
        var width = (int)(currentTexture.Width * Scale);
        var height = (int)(currentTexture.Height * Scale);
        var x = (int)_position.X + width / 5;
        var y = (int)_position.Y + height / 7;
        var collisionWidth = Math.Max(18, width - width * 2 / 5);
        var collisionHeight = Math.Max(24, height - height / 4);
        return new Rectangle(x, y, collisionWidth, collisionHeight);
    }

    private void UpdateMovement(float delta, float distanceToPlayer, float playerX)
    {
        var isAlert = distanceToPlayer <= DetectionRange;
        var leftLimit = _spawnPoint.X - PatrolRange;
        var rightLimit = _spawnPoint.X + PatrolRange;

        if (!isAlert && _position.X <= leftLimit)
        {
            _patrolDirection = 1f;
            _facingRight = true;
        }
        else if (!isAlert && _position.X >= rightLimit)
        {
            _patrolDirection = -1f;
            _facingRight = false;
        }

        if (isAlert)
        {
            var desiredX = MathHelper.Clamp(playerX + _engageOffsetX, _worldBounds.Left, _worldBounds.Right);
            var distanceX = desiredX - _position.X;
            if (MathF.Abs(distanceX) > EngageStopDistance)
            {
                _patrolDirection = MathF.Sign(distanceX);
                _facingRight = _patrolDirection >= 0f;
                _position.X += _patrolDirection * MoveSpeed * delta;
            }

            _position.X = MathHelper.Clamp(_position.X, _worldBounds.Left, _worldBounds.Right);
        }
        else
        {
            _position.X += _patrolDirection * MoveSpeed * delta;
            _position.X = MathHelper.Clamp(
                _position.X,
                Math.Max(_worldBounds.Left, leftLimit),
                Math.Min(_worldBounds.Right, rightLimit));
            _facingRight = _patrolDirection >= 0f;
        }
        _position.Y = _pathY;

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
                player.NotifyBulletHit();
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
        PlayShootSound();
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset = 0f)
    {
        if (!IsLoaded || !IsAlive)
        {
            return;
        }

        var currentTexture = GetCurrentBodyTexture();

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

    private Texture2D GetCurrentBodyTexture()
    {
        if (_isMeleeAttacking && _meleeFrames.Count > 0)
        {
            return _meleeFrames[Math.Clamp(_currentMeleeFrame, 0, _meleeFrames.Count - 1)];
        }

        if (_isShooting)
        {
            if (_shootFrames.Count > 0)
            {
                return _shootFrames[Math.Clamp(_currentShootFrame, 0, _shootFrames.Count - 1)];
            }

            if (_shootTexture != null)
            {
                return _shootTexture;
            }
        }

        return _moveFrames[_currentMoveFrame];
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
        foreach (var frame in _meleeFrames)
        {
            frame.Dispose();
        }

        _meleeFrames.Clear();
        _shootTexture?.Dispose();
        _shootTexture = null;
        _bulletSpriteSheet?.Dispose();
        _bulletSpriteSheet = null;
        _bigBanditPunchSound?.Dispose();
        _bigBanditPunchSound = null;
        _bigBanditPunchPlayer?.Dispose();
        _bigBanditPunchPlayer = null;
        _shootSoundPlayer?.Dispose();
        _shootSoundPlayer = null;
        _deathSoundPlayer?.Dispose();
        _deathSoundPlayer = null;
        _bullets.Clear();
    }

    private void LoadMoveFrames(GraphicsDevice graphicsDevice)
    {
        if (!string.IsNullOrWhiteSpace(_customMoveFramesZipPath) && File.Exists(_customMoveFramesZipPath))
        {
            LoadMoveFramesFromZip(graphicsDevice, _customMoveFramesZipPath);
            if (_moveFrames.Count > 0)
            {
                return;
            }
        }

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

    private void LoadMoveFramesFromZip(GraphicsDevice graphicsDevice, string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _moveFrames.Add(texture);
        }
    }

    private void LoadMeleeFrames(GraphicsDevice graphicsDevice)
    {
        if (string.IsNullOrWhiteSpace(_customMeleeFramesZipPath) || !File.Exists(_customMeleeFramesZipPath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(_customMeleeFramesZipPath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _meleeFrames.Add(texture);
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

    private void BeginMeleeAttack()
    {
        _isMeleeAttacking = true;
        _meleeDamageApplied = false;
        _shootTimer = 0f;
        _currentMeleeFrame = 0;
    }

    private void LoadBigBanditPunchSound()
    {
        _bigBanditPunchSound?.Dispose();
        _bigBanditPunchSound = null;
        _bigBanditPunchPlayer?.Dispose();
        _bigBanditPunchPlayer = null;
        if (_canShoot || !File.Exists(BigBanditPunchSoundPath))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(BigBanditPunchSoundPath);
            _bigBanditPunchSound = SoundEffect.FromStream(stream);
        }
        catch
        {
            _bigBanditPunchSound = null;
        }

        _bigBanditPunchPlayer = new ExternalMp3Player();
        if (_bigBanditPunchPlayer.Load(BigBanditPunchSoundPath, 95, repeat: false))
        {
            return;
        }

        _bigBanditPunchPlayer.Dispose();
        _bigBanditPunchPlayer = null;
    }

    private void PlayBigBanditPunchSound()
    {
        if (_bigBanditPunchSound != null)
        {
            _bigBanditPunchSound.Play(0.95f, 0f, 0f);
            return;
        }

        _bigBanditPunchPlayer?.PlayFromStart();
    }

    private void LoadShootSound()
    {
        _shootSoundPlayer?.Dispose();
        _shootSoundPlayer = null;
        if (!_canShoot || !File.Exists(ShootSoundPath))
        {
            return;
        }

        _shootSoundPlayer = new ExternalMp3Player();
        if (_shootSoundPlayer.Load(ShootSoundPath, 82, repeat: false))
        {
            return;
        }

        _shootSoundPlayer.Dispose();
        _shootSoundPlayer = null;
    }

    private void PlayShootSound()
    {
        _shootSoundPlayer?.PlayFromStart();
    }

    private void LoadDeathSound()
    {
        _deathSoundPlayer?.Dispose();
        _deathSoundPlayer = null;
        if (!File.Exists(DeathSoundPath))
        {
            return;
        }

        _deathSoundPlayer = new ExternalMp3Player();
        if (_deathSoundPlayer.Load(DeathSoundPath, 88, repeat: false))
        {
            return;
        }

        _deathSoundPlayer.Dispose();
        _deathSoundPlayer = null;
    }

    private void PlayDeathSound()
    {
        _deathSoundPlayer?.PlayFromStart();
    }

    private Rectangle GetMeleeAttackBounds()
    {
        var bounds = GetCollisionBounds();
        var width = Math.Max(34, bounds.Width);
        var x = _facingRight ? bounds.Right : bounds.Left - width;
        return new Rectangle(x, bounds.Y, width, bounds.Height);
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

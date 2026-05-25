using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class FlyingGoblin
{
    private sealed class Fireball
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public bool UseGroundBullet;
    }

    private readonly List<Texture2D> _frames = new();
    private readonly List<Texture2D> _fallFrames = new();
    private readonly List<Texture2D> _groundShootFrames = new();
    private readonly List<Texture2D> _groundWalkFrames = new();
    private readonly List<Texture2D> _groundJumpFrames = new();
    private readonly List<Texture2D> _groundDeathFrames = new();
    private readonly List<Fireball> _fireballs = new();
    private ExternalMp3Player? _grenadeThrowSoundPlayer;
    private ExternalMp3Player? _fallFromGliderSoundPlayer;
    private ExternalMp3Player? _deathSoundPlayer;
    private ExternalMp3Player? _playerHitSoundPlayer;
    private Texture2D? _fireballTexture;
    private Texture2D? _groundBulletTexture;
    private Rectangle _worldBounds;
    private Vector2 _position;
    private float _pathY;
    private float _animationTimer;
    private float _bobTimer;
    private int _currentFrame;
    private int _currentFallFrame;
    private int _currentGroundFrame;
    private int _currentGroundWalkFrame;
    private int _currentGroundJumpFrame;
    private int _currentGroundDeathFrame;
    private int _health = MaxHealth;
    private int _groundHealth = GroundMaxHealth;
    private bool _facingRight;
    private bool _shotThisCycle;
    private bool _isFalling;
    private bool _isGrounded;
    private bool _isGroundWalking;
    private bool _isGroundJumping;
    private bool _isDying;
    private bool _isDefeated;
    private float _groundJumpVelocity;

    private const int MaxHealth = 9;
    private const int GroundMaxHealth = 6;
    private const float Scale = 2.1f;
    private const float GroundScale = 2.1f;
    private const float FrameTime = 0.09f;
    private const float GroundFrameTime = 0.1f;
    private const float GroundWalkFrameTime = 0.11f;
    private const float GroundJumpFrameTime = 0.09f;
    private const float GroundDeathFrameTime = 0.12f;
    private const float FallFrameTime = 0.13f;
    private const float FlySpeed = 95f;
    private const float GroundWalkSpeed = 105f;
    private const float GroundPreferredDistance = 330f;
    private const float GroundDistanceTolerance = 65f;
    private const float GroundJumpVelocity = -780f;
    private const float GroundJumpGravity = 1550f;
    private const float FallSpeed = 360f;
    private const float HoverHeight = 420f;
    private const float GroundYOffset = 115f;
    private const float FireballSpeed = 315f;
    private const float FireballLife = 5.5f;
    private const float FireballScale = 2.4f;
    private const float GroundBulletScale = 2.2f;
    private const int FireballFallbackSize = 34;
    private const float FlyingHitboxTrimX = 0.34f;
    private const float FlyingHitboxTrimTop = 0.28f;
    private const float FlyingHitboxTrimBottom = 0.22f;
    private const string SpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (16).zip";
    private const string FallSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (17).zip";
    private const string FireballZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (22).zip";
    private const string GroundShootSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (18).zip";
    private const string GroundWalkSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (19).zip";
    private const string GroundJumpSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (20).zip";
    private const string GroundDeathSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (21).zip";
    private const string GrenadeThrowSoundPath = @"c:\Users\user\Downloads\lasso-around-the-target.mp3";
    private const string FallFromGliderSoundPath = @"c:\Users\user\Downloads\tmp_7901-951678082.mp3";
    private const string DeathSoundPath = @"c:\Users\user\Downloads\fortnite-death-sound-not-knockout.mp3";
    private const string PlayerHitSoundPath = @"c:\Users\user\Downloads\sound-hitting-metal.mp3";

    public bool IsAlive => !_isDefeated;
    public bool IsDefeatedOrDying => _isDying || _isDefeated;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();
        LoadSpritesFromZip(graphicsDevice);
        LoadFallSpritesFromZip(graphicsDevice);
        LoadGroundShootSpritesFromZip(graphicsDevice);
        LoadGroundWalkSpritesFromZip(graphicsDevice);
        LoadGroundJumpSpritesFromZip(graphicsDevice);
        LoadGroundDeathSpritesFromZip(graphicsDevice);
        LoadFireballFromZip(graphicsDevice);
        LoadGrenadeThrowSound();
        LoadFallFromGliderSound();
        LoadDeathSound();
        LoadPlayerHitSound();
        if (_fireballTexture == null)
        {
            _fireballTexture = new Texture2D(graphicsDevice, 1, 1);
            _fireballTexture.SetData(new[] { new Color(255, 95, 35) });
        }
    }

    public void SetWorld(Rectangle worldBounds, float pathY)
    {
        _worldBounds = worldBounds;
        _pathY = pathY;
        _position = new Vector2(_worldBounds.Width * 0.72f, _pathY - HoverHeight);
        _health = MaxHealth;
        _groundHealth = GroundMaxHealth;
        _fireballs.Clear();
        _shotThisCycle = false;
        _isFalling = false;
        _isGrounded = false;
        _isGroundWalking = false;
        _isGroundJumping = false;
        _isDying = false;
        _isDefeated = false;
        _groundJumpVelocity = 0f;
        _currentFrame = 0;
        _currentFallFrame = 0;
        _currentGroundFrame = 0;
        _currentGroundWalkFrame = 0;
        _currentGroundJumpFrame = 0;
        _currentGroundDeathFrame = 0;
        _animationTimer = 0f;
    }

    public void Update(GameTime gameTime, Player player)
    {
        if (_isDefeated)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_isDying)
        {
            UpdateDeath(delta);
            return;
        }

        if (_isFalling)
        {
            UpdateFalling(delta, player);
            return;
        }

        if (_isGrounded)
        {
            UpdateGrounded(delta, player);
            return;
        }

        if (_frames.Count == 0)
        {
            return;
        }

        _bobTimer += delta;

        var desiredX = MathHelper.Clamp(player.Position.X + 240f, _worldBounds.Left + 180f, _worldBounds.Right - 180f);
        var dx = desiredX - _position.X;
        if (MathF.Abs(dx) > 8f)
        {
            _position.X += MathF.Sign(dx) * FlySpeed * delta;
        }

        _position.Y = _pathY - HoverHeight + MathF.Sin(_bobTimer * 2.4f) * 28f;
        _facingRight = player.Position.X >= _position.X;

        _animationTimer += delta;
        if (_animationTimer >= FrameTime)
        {
            _animationTimer -= FrameTime;
            _currentFrame++;
            if (_currentFrame >= _frames.Count)
            {
                _currentFrame = 0;
                _shotThisCycle = false;
            }
        }

        if (_currentFrame == _frames.Count - 1 && !_shotThisCycle)
        {
            ShootAt(player.Position + new Vector2(0f, 80f));
            _shotThisCycle = true;
        }

        UpdateFireballs(delta, player);
    }

    public Rectangle GetCollisionBounds()
    {
        if (!IsAlive || _isFalling || _isDying)
        {
            return Rectangle.Empty;
        }

        var frame = GetActiveFrame();
        if (frame == null)
        {
            return Rectangle.Empty;
        }

        var scale = _isGrounded ? GroundScale : Scale;
        var width = (int)(frame.Width * scale);
        var height = (int)(frame.Height * scale);
        if (!_isGrounded)
        {
            var trimX = (int)MathF.Round(width * FlyingHitboxTrimX);
            var trimTop = (int)MathF.Round(height * FlyingHitboxTrimTop);
            var trimBottom = (int)MathF.Round(height * FlyingHitboxTrimBottom);
            return new Rectangle(
                (int)_position.X + trimX,
                (int)_position.Y + trimTop,
                Math.Max(1, width - trimX * 2),
                Math.Max(1, height - trimTop - trimBottom));
        }

        if (_isGroundJumping)
        {
            var jumpTrimX = (int)MathF.Round(width * 0.22f);
            var jumpTrimTop = (int)MathF.Round(height * 0.10f);
            var jumpHeight = (int)MathF.Round(height * 0.42f);
            return new Rectangle(
                (int)_position.X + jumpTrimX,
                (int)_position.Y + jumpTrimTop,
                Math.Max(1, width - jumpTrimX * 2),
                Math.Max(1, jumpHeight));
        }

        return new Rectangle((int)_position.X, (int)_position.Y, width, height);
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || _isFalling || _isDefeated)
        {
            return;
        }

        if (_isGrounded)
        {
            _groundHealth = Math.Max(0, _groundHealth - amount);
            if (_groundHealth == 0)
            {
                StartDeath();
            }

            return;
        }

        _health = Math.Max(0, _health - amount);
        if (_health == 0)
        {
            StartFalling();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset)
    {
        if (_isDefeated)
        {
            return;
        }

        if (_isDying)
        {
            DrawDeath(spriteBatch, cameraPosition, visualYOffset);
            return;
        }

        if (_isFalling)
        {
            DrawFalling(spriteBatch, cameraPosition, visualYOffset);
            return;
        }

        var frame = GetActiveFrame();
        if (frame == null)
        {
            return;
        }

        var effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var drawPosition = _position - cameraPosition + new Vector2(0f, visualYOffset);
        var scale = _isGrounded ? GroundScale : Scale;
        spriteBatch.Draw(frame, new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)), null, Color.White, 0f, Vector2.Zero, scale, effects, 0f);

        DrawFireballs(spriteBatch, cameraPosition, visualYOffset);
    }

    public void Unload()
    {
        foreach (var frame in _frames)
        {
            frame.Dispose();
        }

        foreach (var frame in _fallFrames)
        {
            frame.Dispose();
        }

        foreach (var frame in _groundShootFrames)
        {
            frame.Dispose();
        }

        foreach (var frame in _groundWalkFrames)
        {
            frame.Dispose();
        }

        foreach (var frame in _groundJumpFrames)
        {
            frame.Dispose();
        }

        foreach (var frame in _groundDeathFrames)
        {
            frame.Dispose();
        }

        _frames.Clear();
        _fallFrames.Clear();
        _groundShootFrames.Clear();
        _groundWalkFrames.Clear();
        _groundJumpFrames.Clear();
        _groundDeathFrames.Clear();
        _fireballs.Clear();
        _fireballTexture?.Dispose();
        _fireballTexture = null;
        _groundBulletTexture?.Dispose();
        _groundBulletTexture = null;
        _grenadeThrowSoundPlayer?.Dispose();
        _grenadeThrowSoundPlayer = null;
        _fallFromGliderSoundPlayer?.Dispose();
        _fallFromGliderSoundPlayer = null;
        _deathSoundPlayer?.Dispose();
        _deathSoundPlayer = null;
        _playerHitSoundPlayer?.Dispose();
        _playerHitSoundPlayer = null;
    }

    private void LoadGrenadeThrowSound()
    {
        _grenadeThrowSoundPlayer?.Dispose();
        _grenadeThrowSoundPlayer = null;

        if (!File.Exists(GrenadeThrowSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(GrenadeThrowSoundPath, 88, repeat: false))
        {
            _grenadeThrowSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayGrenadeThrowSound()
    {
        _grenadeThrowSoundPlayer?.PlayFromStart();
    }

    private void LoadFallFromGliderSound()
    {
        _fallFromGliderSoundPlayer?.Dispose();
        _fallFromGliderSoundPlayer = null;

        if (!File.Exists(FallFromGliderSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(FallFromGliderSoundPath, 90, repeat: false))
        {
            _fallFromGliderSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayFallFromGliderSound()
    {
        _fallFromGliderSoundPlayer?.PlayFromStart();
    }

    private void LoadDeathSound()
    {
        _deathSoundPlayer?.Dispose();
        _deathSoundPlayer = null;

        if (!File.Exists(DeathSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(DeathSoundPath, 90, repeat: false))
        {
            _deathSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayDeathSound()
    {
        _deathSoundPlayer?.PlayFromStart();
    }

    private void LoadPlayerHitSound()
    {
        _playerHitSoundPlayer?.Dispose();
        _playerHitSoundPlayer = null;

        if (!File.Exists(PlayerHitSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(PlayerHitSoundPath, 88, repeat: false))
        {
            _playerHitSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayPlayerHitSound()
    {
        _playerHitSoundPlayer?.PlayFromStart();
    }

    private void LoadSpritesFromZip(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(SpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(SpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var frameCount = Math.Max(0, entries.Length - 1);
        for (var i = 0; i < frameCount; i++)
        {
            var entry = entries[i];
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _frames.Add(texture);
        }
    }

    private void LoadFireballFromZip(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(FireballZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(FireballZipSourcePath);
        var entry = archive.Entries
            .Where(item => item.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (entry == null)
        {
            return;
        }

        using var stream = entry.Open();
        _fireballTexture?.Dispose();
        _fireballTexture = Texture2D.FromStream(graphicsDevice, stream);
        ApplyBlackKey(_fireballTexture);
    }

    private void LoadFallSpritesFromZip(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(FallSpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(FallSpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _fallFrames.Add(texture);
        }
    }

    private void LoadGroundShootSpritesFromZip(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(GroundShootSpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(GroundShootSpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (entries.Length == 0)
        {
            return;
        }

        var frameCount = Math.Max(0, entries.Length - 1);
        for (var i = 0; i < frameCount; i++)
        {
            using var stream = entries[i].Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _groundShootFrames.Add(texture);
        }

        using var bulletStream = entries[^1].Open();
        _groundBulletTexture?.Dispose();
        _groundBulletTexture = Texture2D.FromStream(graphicsDevice, bulletStream);
        ApplyBlackKey(_groundBulletTexture);
    }

    private void LoadGroundWalkSpritesFromZip(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(GroundWalkSpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(GroundWalkSpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _groundWalkFrames.Add(texture);
        }
    }

    private void LoadGroundJumpSpritesFromZip(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(GroundJumpSpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(GroundJumpSpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _groundJumpFrames.Add(texture);
        }
    }

    private void LoadGroundDeathSpritesFromZip(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(GroundDeathSpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(GroundDeathSpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _groundDeathFrames.Add(texture);
        }
    }

    private void StartFalling()
    {
        _isFalling = true;
        _currentFallFrame = 0;
        _animationTimer = 0f;
        _fireballs.Clear();
        PlayFallFromGliderSound();
    }

    private void UpdateFalling(float delta, Player player)
    {
        _position.Y += FallSpeed * delta;
        _facingRight = player.Position.X >= _position.X;
        _animationTimer += delta;
        if (_animationTimer >= FallFrameTime)
        {
            _animationTimer -= FallFrameTime;
            if (_currentFallFrame < Math.Max(0, _fallFrames.Count - 1))
            {
                _currentFallFrame++;
            }
        }

        var landingY = GetGroundY(GetGroundFrame());
        if (_position.Y >= landingY)
        {
            _position.Y = landingY;
            LandOnGround(player);
        }
    }

    private void LandOnGround(Player player)
    {
        _isFalling = false;
        _isGrounded = true;
        _isGroundWalking = false;
        _isGroundJumping = false;
        _currentGroundFrame = 0;
        _currentGroundWalkFrame = 0;
        _currentGroundJumpFrame = 0;
        _animationTimer = 0f;
        _shotThisCycle = false;
        _groundHealth = GroundMaxHealth;

        var frame = GetGroundFrame();
        _position.Y = GetGroundY(frame);
        _position.X = MathHelper.Clamp(_position.X, _worldBounds.Left + 40f, _worldBounds.Right - 140f);
        _facingRight = player.Position.X >= _position.X;
    }

    private void UpdateGrounded(float delta, Player player)
    {
        _facingRight = player.Position.X >= _position.X;
        if (_isGroundJumping)
        {
            UpdateGroundJump(delta, player);
            return;
        }

        if (_groundJumpFrames.Count > 0 && player.HasIncomingWebProjectile(GetCollisionBounds()))
        {
            StartGroundJump();
            UpdateGroundJump(delta, player);
            return;
        }

        var groundCenterX = _position.X + GetCenterOffset().X;
        var distanceToPlayer = player.Position.X - groundCenterX;
        var absDistance = MathF.Abs(distanceToPlayer);
        _isGroundWalking = _groundWalkFrames.Count > 0 &&
            (absDistance > GroundPreferredDistance + GroundDistanceTolerance ||
             absDistance < GroundPreferredDistance - GroundDistanceTolerance);

        if (_isGroundWalking)
        {
            float moveDirection = absDistance > GroundPreferredDistance
                ? MathF.Sign(distanceToPlayer)
                : -MathF.Sign(distanceToPlayer);
            if (moveDirection == 0f)
            {
                moveDirection = _facingRight ? 1f : -1f;
            }

            _position.X += moveDirection * GroundWalkSpeed * delta;
            _position.X = MathHelper.Clamp(_position.X, _worldBounds.Left + 40f, _worldBounds.Right - 140f);
            _facingRight = moveDirection > 0f;

            _animationTimer += delta;
            if (_animationTimer >= GroundWalkFrameTime)
            {
                _animationTimer -= GroundWalkFrameTime;
                _currentGroundWalkFrame = (_currentGroundWalkFrame + 1) % _groundWalkFrames.Count;
            }

            _shotThisCycle = false;
            _position.Y = GetGroundY(GetGroundFrame());
            UpdateFireballs(delta, player);
            return;
        }

        _animationTimer += delta;
        if (_animationTimer >= GroundFrameTime)
        {
            _animationTimer -= GroundFrameTime;
            _currentGroundFrame++;
            var frameCount = Math.Max(1, _groundShootFrames.Count);
            if (_currentGroundFrame >= frameCount)
            {
                _currentGroundFrame = 0;
                _shotThisCycle = false;
            }
        }

        var shootFrame = Math.Max(0, _groundShootFrames.Count - 1);
        if (_currentGroundFrame == shootFrame && !_shotThisCycle)
        {
            ShootAt(player.Position + new Vector2(0f, 55f), useGroundBullet: true);
            _shotThisCycle = true;
        }

        _position.Y = GetGroundY(GetGroundFrame());
        UpdateFireballs(delta, player);
    }

    private void StartGroundJump()
    {
        _isGroundJumping = true;
        _isGroundWalking = false;
        _groundJumpVelocity = GroundJumpVelocity;
        _currentGroundJumpFrame = 0;
        _animationTimer = 0f;
        _shotThisCycle = false;
    }

    private void UpdateGroundJump(float delta, Player player)
    {
        _facingRight = player.Position.X >= _position.X;
        _groundJumpVelocity += GroundJumpGravity * delta;
        _position.Y += _groundJumpVelocity * delta;

        _animationTimer += delta;
        if (_animationTimer >= GroundJumpFrameTime)
        {
            _animationTimer -= GroundJumpFrameTime;
            _currentGroundJumpFrame = Math.Min(_currentGroundJumpFrame + 1, Math.Max(0, _groundJumpFrames.Count - 1));
        }

        var landingY = GetGroundY(GetGroundFrame());
        if (_position.Y >= landingY && _groundJumpVelocity > 0f)
        {
            _position.Y = landingY;
            _groundJumpVelocity = 0f;
            _isGroundJumping = false;
            _currentGroundFrame = 0;
            _animationTimer = 0f;
        }

        UpdateFireballs(delta, player);
    }

    private void StartDeath()
    {
        _isDying = true;
        _isGroundWalking = false;
        _isGroundJumping = false;
        _groundJumpVelocity = 0f;
        _currentGroundDeathFrame = 0;
        _animationTimer = 0f;
        _fireballs.Clear();

        var frame = GetDeathFrame();
        _position.Y = GetGroundY(frame);
        PlayDeathSound();

        if (_groundDeathFrames.Count == 0)
        {
            _isDying = false;
            _isDefeated = true;
        }
    }

    private void UpdateDeath(float delta)
    {
        _animationTimer += delta;
        if (_animationTimer < GroundDeathFrameTime)
        {
            return;
        }

        _animationTimer -= GroundDeathFrameTime;
        if (_currentGroundDeathFrame < Math.Max(0, _groundDeathFrames.Count - 1))
        {
            _currentGroundDeathFrame++;
            _position.Y = GetGroundY(GetDeathFrame());
            return;
        }

        _isDying = false;
        _isDefeated = true;
    }

    private Texture2D? GetDeathFrame()
    {
        if (_groundDeathFrames.Count > 0)
        {
            return _groundDeathFrames[Math.Clamp(_currentGroundDeathFrame, 0, _groundDeathFrames.Count - 1)];
        }

        return GetGroundFrame();
    }

    private void DrawDeath(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset)
    {
        var frame = GetDeathFrame();
        if (frame == null)
        {
            return;
        }

        var effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var drawPosition = _position - cameraPosition + new Vector2(0f, visualYOffset);
        spriteBatch.Draw(
            frame,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            GroundScale,
            effects,
            0f);
    }

    private Texture2D? GetGroundFrame()
    {
        if (_isGroundJumping && _groundJumpFrames.Count > 0)
        {
            return _groundJumpFrames[Math.Clamp(_currentGroundJumpFrame, 0, _groundJumpFrames.Count - 1)];
        }

        if (_isGroundWalking && _groundWalkFrames.Count > 0)
        {
            return _groundWalkFrames[Math.Clamp(_currentGroundWalkFrame, 0, _groundWalkFrames.Count - 1)];
        }

        if (_groundShootFrames.Count > 0)
        {
            return _groundShootFrames[Math.Clamp(_currentGroundFrame, 0, _groundShootFrames.Count - 1)];
        }

        if (_fallFrames.Count > 0)
        {
            return _fallFrames[^1];
        }

        return _frames.Count > 0 ? _frames[Math.Clamp(_currentFrame, 0, _frames.Count - 1)] : null;
    }

    private float GetGroundY(Texture2D? frame)
    {
        var height = frame == null ? 0f : frame.Height * GroundScale;
        return _pathY + GroundYOffset - height;
    }

    private Texture2D? GetActiveFrame()
    {
        if (_isGrounded)
        {
            return GetGroundFrame();
        }

        if (_frames.Count == 0)
        {
            return null;
        }

        return _frames[Math.Clamp(_currentFrame, 0, _frames.Count - 1)];
    }

    private void DrawFalling(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset)
    {
        if (_fallFrames.Count == 0 && _frames.Count == 0)
        {
            return;
        }

        var frame = _fallFrames.Count > 0
            ? _fallFrames[Math.Clamp(_currentFallFrame, 0, _fallFrames.Count - 1)]
            : _frames[Math.Clamp(_currentFrame, 0, _frames.Count - 1)];
        if (frame == null)
        {
            return;
        }

        var effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var drawPosition = _position - cameraPosition + new Vector2(0f, visualYOffset);
        spriteBatch.Draw(frame, new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)), null, Color.White, 0f, Vector2.Zero, Scale, effects, 0f);
    }

    private void ShootAt(Vector2 target, bool useGroundBullet = false)
    {
        var direction = target - (_position + GetCenterOffset());
        if (direction == Vector2.Zero)
        {
            direction = Vector2.UnitX;
        }

        direction.Normalize();
        _fireballs.Add(new Fireball
        {
            Position = _position + GetCenterOffset(),
            Velocity = direction * FireballSpeed,
            Life = FireballLife,
            UseGroundBullet = useGroundBullet
        });
        PlayGrenadeThrowSound();
    }

    private void UpdateFireballs(float delta, Player player)
    {
        var playerBounds = player.GetBulletCollisionBounds();
        for (var i = _fireballs.Count - 1; i >= 0; i--)
        {
            var fireball = _fireballs[i];
            fireball.Position += fireball.Velocity * delta;
            fireball.Life -= delta;

            var fireballSize = GetFireballDrawSize(fireball);
            var fireballBounds = new Rectangle(
                (int)MathF.Round(fireball.Position.X - fireballSize.X / 2f),
                (int)MathF.Round(fireball.Position.Y - fireballSize.Y / 2f),
                (int)MathF.Round(fireballSize.X),
                (int)MathF.Round(fireballSize.Y));
            if (fireballBounds.Intersects(playerBounds))
            {
                player.NotifyHit();
                PlayPlayerHitSound();
                _fireballs.RemoveAt(i);
                continue;
            }

            if (fireball.Life <= 0f)
            {
                _fireballs.RemoveAt(i);
                continue;
            }

            _fireballs[i] = fireball;
        }
    }

    private void DrawFireballs(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset)
    {
        if (_fireballTexture == null && _groundBulletTexture == null)
        {
            return;
        }

        foreach (var fireball in _fireballs)
        {
            var drawPosition = fireball.Position - cameraPosition + new Vector2(0f, visualYOffset);
            var texture = GetFireballTexture(fireball);
            if (texture == null)
            {
                continue;
            }

            var fireballSize = GetFireballDrawSize(fireball);
            var destination = new Rectangle(
                (int)MathF.Round(drawPosition.X - fireballSize.X / 2f),
                (int)MathF.Round(drawPosition.Y - fireballSize.Y / 2f),
                (int)MathF.Round(fireballSize.X),
                (int)MathF.Round(fireballSize.Y));
            spriteBatch.Draw(texture, destination, Color.White);
        }
    }

    private Texture2D? GetFireballTexture(Fireball fireball)
    {
        if (fireball.UseGroundBullet && _groundBulletTexture != null)
        {
            return _groundBulletTexture;
        }

        return _fireballTexture;
    }

    private Vector2 GetFireballDrawSize(Fireball fireball)
    {
        var texture = GetFireballTexture(fireball);
        if (texture == null || texture.Width <= 1 || texture.Height <= 1)
        {
            return new Vector2(FireballFallbackSize, FireballFallbackSize);
        }

        var scale = fireball.UseGroundBullet ? GroundBulletScale : FireballScale;
        return new Vector2(texture.Width * scale, texture.Height * scale);
    }

    private Vector2 GetCenterOffset()
    {
        var frame = GetActiveFrame();
        if (frame == null)
        {
            return Vector2.Zero;
        }

        var scale = _isGrounded ? GroundScale : Scale;
        return new Vector2(frame.Width * scale * 0.5f, frame.Height * scale * 0.5f);
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

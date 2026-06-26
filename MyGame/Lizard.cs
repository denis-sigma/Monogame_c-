using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class Lizard
{
    private readonly List<Texture2D> _walkFrames = new();
    private readonly List<Texture2D> _attackFrames = new();
    private readonly List<Texture2D> _jumpFrames = new();
    private readonly List<Texture2D> _deathFrames = new();
    private ExternalMp3Player? _hitSoundPlayer;
    private ExternalMp3Player? _deathSoundPlayer;
    private SoundEffect? _missSound;
    private Rectangle _worldBounds;
    private Vector2 _position;
    private float _pathY;
    private float _animationTimer;
    private float _attackTimer;
    private float _attackCooldown;
    private float _jumpTimer;
    private float _jumpCooldown;
    private float _deathTimer;
    private float _hitFlashTimer;
    private int _health = MaxHealth;
    private int _currentFrame;
    private int _currentAttackFrame;
    private int _currentJumpFrame;
    private int _currentDeathFrame;
    private float _patrolDirection = -1f;
    private bool _isAttacking;
    private bool _isJumping;
    private bool _isDying;
    private bool _isDefeated;
    private bool _attackDamageApplied;

    private const int MaxHealth = 12;
    private const float Scale = 1.9f;
    private const float WalkSpeed = 165f;
    private const float PatrolPadding = 35f;
    private const float FrameTime = 0.075f;
    private const float AttackFrameTime = 0.08f;
    private const float AttackRange = 110f;
    private const float AttackCooldown = 0.85f;
    private const float JumpFrameTime = 0.075f;
    private const float JumpCooldown = 1.45f;
    private const float JumpRange = 720f;
    private const float JumpDistance = 430f;
    private const float JumpHeight = 185f;
    private const float DeathFrameTime = 0.11f;
    private const float HitFlashDuration = 0.12f;
    private const float KnockbackHorizontalVelocity = 820f;
    private const float KnockbackVerticalVelocity = -360f;
    private const string WalkSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (35).zip";
    private const string AttackSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (36).zip";
    private const string JumpSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (37).zip";
    private const string DeathSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (38).zip";
    private const string HitSoundPath = @"c:\Users\user\Downloads\short-powerful-blow-to-a-pile-of-iron.mp3";
    private const string DeathSoundPath = @"c:\Users\user\Downloads\tmp_7901-951678082.mp3";
    private const string MissSoundPath = @"c:\Users\user\Downloads\mixkit-soft-quick-punch-2151.wav";

    public bool IsLoaded => _walkFrames.Count > 0;
    public bool IsAlive => !_isDefeated;
    public bool IsDefeatedOrDying => _isDying || _isDefeated;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();
        LoadFramesFromZip(graphicsDevice, WalkSpriteZipSourcePath, _walkFrames);
        LoadFramesFromZip(graphicsDevice, AttackSpriteZipSourcePath, _attackFrames);
        LoadFramesFromZip(graphicsDevice, JumpSpriteZipSourcePath, _jumpFrames);
        LoadFramesFromZip(graphicsDevice, DeathSpriteZipSourcePath, _deathFrames);
        LoadHitSound();
        LoadDeathSound();
        LoadMissSound();
    }

    public void SetWorld(Rectangle worldBounds, float pathY)
    {
        _worldBounds = worldBounds;
        _pathY = pathY;
        _position = new Vector2(_worldBounds.Right - _worldBounds.Width * 0.28f, _pathY);
        _animationTimer = 0f;
        _attackTimer = 0f;
        _attackCooldown = 0f;
        _jumpTimer = 0f;
        _jumpCooldown = 0.65f;
        _deathTimer = 0f;
        _hitFlashTimer = 0f;
        _health = MaxHealth;
        _currentFrame = 0;
        _currentAttackFrame = 0;
        _currentJumpFrame = 0;
        _currentDeathFrame = 0;
        _patrolDirection = -1f;
        _isAttacking = false;
        _isJumping = false;
        _isDying = false;
        _isDefeated = false;
        _attackDamageApplied = false;
    }

    public void Update(GameTime gameTime, Player player)
    {
        if (!IsLoaded || _isDefeated)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_isDying)
        {
            UpdateDeath(delta);
            return;
        }

        _hitFlashTimer = Math.Max(0f, _hitFlashTimer - delta);
        _attackCooldown = Math.Max(0f, _attackCooldown - delta);
        _jumpCooldown = Math.Max(0f, _jumpCooldown - delta);
        if (_isJumping)
        {
            UpdateJump(delta);
            return;
        }

        if (_isAttacking)
        {
            UpdateAttack(delta, player);
            return;
        }

        var toPlayer = player.Position - _position;
        if (MathF.Abs(toPlayer.X) <= AttackRange && MathF.Abs(toPlayer.Y) < 140f && _attackFrames.Count > 0 && _attackCooldown <= 0f)
        {
            BeginAttack(player);
            return;
        }

        if (_jumpFrames.Count > 0 && MathF.Abs(toPlayer.X) <= JumpRange && _jumpCooldown <= 0f)
        {
            BeginJump(player);
            return;
        }

        UpdateWalkAnimation(delta);
        UpdateChase(delta, player);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset)
    {
        if (!IsLoaded || _isDefeated)
        {
            return;
        }

        var texture = GetCurrentTexture();
        var effects = _patrolDirection > 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var drawPosition = _position - cameraPosition + new Vector2(0f, visualYOffset);
        var tint = _hitFlashTimer > 0f ? new Color(255, 210, 115) : Color.White;

        spriteBatch.Draw(
            texture,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            tint,
            0f,
            Vector2.Zero,
            Scale,
            effects,
            0f);
    }

    public void Unload()
    {
        foreach (var frame in _walkFrames)
        {
            frame.Dispose();
        }

        _walkFrames.Clear();
        foreach (var frame in _attackFrames)
        {
            frame.Dispose();
        }

        _attackFrames.Clear();
        foreach (var frame in _jumpFrames)
        {
            frame.Dispose();
        }

        _jumpFrames.Clear();
        foreach (var frame in _deathFrames)
        {
            frame.Dispose();
        }

        _deathFrames.Clear();
        _hitSoundPlayer?.Dispose();
        _hitSoundPlayer = null;
        _deathSoundPlayer?.Dispose();
        _deathSoundPlayer = null;
        _missSound?.Dispose();
        _missSound = null;
    }

    private Texture2D GetCurrentTexture()
    {
        if (_isDying && _deathFrames.Count > 0)
        {
            return _deathFrames[Math.Clamp(_currentDeathFrame, 0, _deathFrames.Count - 1)];
        }

        if (_isJumping && _jumpFrames.Count > 0)
        {
            return _jumpFrames[Math.Clamp(_currentJumpFrame, 0, _jumpFrames.Count - 1)];
        }

        if (_isAttacking && _attackFrames.Count > 0)
        {
            return _attackFrames[Math.Clamp(_currentAttackFrame, 0, _attackFrames.Count - 1)];
        }

        return _walkFrames[Math.Clamp(_currentFrame, 0, _walkFrames.Count - 1)];
    }

    private void UpdateWalkAnimation(float delta)
    {
        _animationTimer += delta;
        while (_animationTimer >= FrameTime)
        {
            _animationTimer -= FrameTime;
            _currentFrame = (_currentFrame + 1) % _walkFrames.Count;
        }
    }

    private void UpdateChase(float delta, Player player)
    {
        var minX = _worldBounds.Left + PatrolPadding;
        var maxX = _worldBounds.Right - PatrolPadding;
        var dx = player.Position.X - _position.X;
        if (MathF.Abs(dx) > AttackRange * 0.65f)
        {
            _patrolDirection = MathF.Sign(dx);
        }

        _position.X += _patrolDirection * WalkSpeed * delta;

        if (_position.X <= minX)
        {
            _position.X = minX;
            _patrolDirection = 1f;
        }
        else if (_position.X >= maxX)
        {
            _position.X = maxX;
            _patrolDirection = -1f;
        }
    }

    public void ApplyDamage(int amount)
    {
        if (_isDefeated || _isDying)
        {
            return;
        }

        _health -= Math.Max(0, amount);
        _hitFlashTimer = HitFlashDuration;
        if (_health <= 0)
        {
            BeginDeath();
        }
    }

    private void BeginDeath()
    {
        if (_deathFrames.Count == 0)
        {
            _isDefeated = true;
            return;
        }

        _isDying = true;
        _isAttacking = false;
        _isJumping = false;
        _deathTimer = 0f;
        _currentDeathFrame = 0;
        _position.Y = _pathY;
        PlayDeathSound();
    }

    private void UpdateDeath(float delta)
    {
        if (_deathFrames.Count == 0)
        {
            _isDefeated = true;
            return;
        }

        _deathTimer += delta;
        while (_deathTimer >= DeathFrameTime)
        {
            _deathTimer -= DeathFrameTime;
            _currentDeathFrame++;
        }

        if (_currentDeathFrame >= _deathFrames.Count)
        {
            _currentDeathFrame = _deathFrames.Count - 1;
            _isDefeated = true;
        }
    }

    private void BeginAttack(Player player)
    {
        _isAttacking = true;
        _attackDamageApplied = false;
        _attackTimer = 0f;
        _currentAttackFrame = 0;
        _patrolDirection = player.Position.X >= _position.X ? 1f : -1f;
    }

    private void BeginJump(Player player)
    {
        _isJumping = true;
        _jumpTimer = 0f;
        _currentJumpFrame = 0;
        _patrolDirection = player.Position.X >= _position.X ? 1f : -1f;
    }

    private void UpdateJump(float delta)
    {
        if (_jumpFrames.Count == 0)
        {
            _isJumping = false;
            return;
        }

        _jumpTimer += delta;
        while (_jumpTimer >= JumpFrameTime)
        {
            _jumpTimer -= JumpFrameTime;
            _currentJumpFrame++;
        }

        var jumpDuration = Math.Max(JumpFrameTime, _jumpFrames.Count * JumpFrameTime);
        var progress = MathHelper.Clamp((_currentJumpFrame * JumpFrameTime + _jumpTimer) / jumpDuration, 0f, 1f);
        var minX = _worldBounds.Left + PatrolPadding;
        var maxX = _worldBounds.Right - PatrolPadding;
        _position.X = MathHelper.Clamp(_position.X + _patrolDirection * JumpDistance * delta / jumpDuration, minX, maxX);
        _position.Y = _pathY - MathF.Sin(progress * MathF.PI) * JumpHeight;

        if (_currentJumpFrame >= _jumpFrames.Count)
        {
            _isJumping = false;
            _currentJumpFrame = 0;
            _position.Y = _pathY;
            _jumpCooldown = JumpCooldown;
        }
    }

    private void UpdateAttack(float delta, Player player)
    {
        if (_attackFrames.Count == 0)
        {
            _isAttacking = false;
            return;
        }

        _attackTimer += delta;
        while (_attackTimer >= AttackFrameTime)
        {
            _attackTimer -= AttackFrameTime;
            _currentAttackFrame++;
        }

        if (!_attackDamageApplied && _currentAttackFrame >= Math.Max(1, _attackFrames.Count / 2))
        {
            var damagedPlayer = GetAttackBounds().Intersects(player.GetCollisionBounds()) && !player.IsCrouching;
            if (damagedPlayer)
            {
                player.NotifyHit();
                player.ApplyKnockback(_patrolDirection * KnockbackHorizontalVelocity, KnockbackVerticalVelocity);
                PlayHitSound();
            }
            else
            {
                PlayMissSound();
            }

            _attackDamageApplied = true;
        }

        if (_currentAttackFrame >= _attackFrames.Count)
        {
            _isAttacking = false;
            _currentAttackFrame = 0;
            _attackCooldown = AttackCooldown;
        }
    }

    public Rectangle GetCollisionBounds()
    {
        if (!IsLoaded || _isDefeated)
        {
            return Rectangle.Empty;
        }

        var texture = GetCurrentTexture();
        var width = (int)MathF.Round(texture.Width * Scale * 0.55f);
        var height = (int)MathF.Round(texture.Height * Scale * 0.82f);
        var x = (int)MathF.Round(_position.X + texture.Width * Scale * 0.22f);
        var y = (int)MathF.Round(_position.Y + texture.Height * Scale * 0.12f);
        return new Rectangle(x, y, width, height);
    }

    private Rectangle GetAttackBounds()
    {
        var body = GetCollisionBounds();
        if (body == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        var attackWidth = 95;
        var attackHeight = Math.Max(70, body.Height);
        var x = _patrolDirection > 0f ? body.Right - 16 : body.Left - attackWidth + 16;
        var y = body.Y + Math.Max(0, (body.Height - attackHeight) / 2);
        return new Rectangle(x, y, attackWidth, attackHeight);
    }

    private void LoadHitSound()
    {
        _hitSoundPlayer?.Dispose();
        _hitSoundPlayer = null;
        if (!File.Exists(HitSoundPath))
        {
            return;
        }

        try
        {
            var player = new ExternalMp3Player();
            if (player.Load(HitSoundPath, volume: 90, repeat: false))
            {
                _hitSoundPlayer = player;
            }
            else
            {
                player.Dispose();
            }
        }
        catch
        {
            _hitSoundPlayer = null;
        }
    }

    private void PlayHitSound()
    {
        try
        {
            _hitSoundPlayer?.PlayFromStart();
        }
        catch
        {

        }
    }

    private void LoadDeathSound()
    {
        _deathSoundPlayer?.Dispose();
        _deathSoundPlayer = null;
        if (!File.Exists(DeathSoundPath))
        {
            return;
        }

        try
        {
            var player = new ExternalMp3Player();
            if (player.Load(DeathSoundPath, volume: 90, repeat: false))
            {
                _deathSoundPlayer = player;
            }
            else
            {
                player.Dispose();
            }
        }
        catch
        {
            _deathSoundPlayer = null;
        }
    }

    private void PlayDeathSound()
    {
        try
        {
            _deathSoundPlayer?.PlayFromStart();
        }
        catch
        {

        }
    }

    private void LoadMissSound()
    {
        _missSound?.Dispose();
        _missSound = null;
        if (!File.Exists(MissSoundPath))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(MissSoundPath);
            _missSound = SoundEffect.FromStream(stream);
        }
        catch
        {
            _missSound = null;
        }
    }

    private void PlayMissSound()
    {
        try
        {
            _missSound?.Play(0.95f, 0f, 0f);
        }
        catch
        {

        }
    }

    private static void LoadFramesFromZip(GraphicsDevice graphicsDevice, string zipPath, List<Texture2D> target)
    {
        if (!File.Exists(zipPath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase))
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            target.Add(TrimTransparentPixels(graphicsDevice, texture));
        }
    }

    private static void ApplyBlackKey(Texture2D texture)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        for (var i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];
            if (color.R < 8 && color.G < 8 && color.B < 8)
            {
                pixels[i] = Color.Transparent;
            }
        }

        texture.SetData(pixels);
    }

    private static Texture2D TrimTransparentPixels(GraphicsDevice graphicsDevice, Texture2D texture)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        var minX = texture.Width;
        var minY = texture.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < texture.Height; y++)
        {
            for (var x = 0; x < texture.Width; x++)
            {
                if (pixels[y * texture.Width + x].A == 0)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return texture;
        }

        var trimmedWidth = maxX - minX + 1;
        var trimmedHeight = maxY - minY + 1;
        var trimmedPixels = new Color[trimmedWidth * trimmedHeight];
        for (var y = 0; y < trimmedHeight; y++)
        {
            Array.Copy(
                pixels,
                (minY + y) * texture.Width + minX,
                trimmedPixels,
                y * trimmedWidth,
                trimmedWidth);
        }

        var trimmedTexture = new Texture2D(graphicsDevice, trimmedWidth, trimmedHeight);
        trimmedTexture.SetData(trimmedPixels);
        texture.Dispose();
        return trimmedTexture;
    }
}

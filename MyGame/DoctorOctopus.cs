using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class DoctorOctopus
{
    private readonly List<Texture2D> _walkFrames = new();
    private readonly List<Texture2D> _attackFrames = new();
    private readonly List<Texture2D> _superAttackFrames = new();
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
    private float _superAttackCooldown;
    private float _hitFlashTimer;
    private float _deathTimer;
    private int _health = MaxHealth;
    private int _currentFrame;
    private int _currentAttackFrame;
    private int _currentDeathFrame;
    private bool _facingRight;
    private bool _isAttacking;
    private bool _isSuperAttacking;
    private bool _isDying;
    private bool _isDefeated;
    private bool _attackDamageApplied;

    private const int MaxHealth = 18;
    private const float Scale = 1.8f;
    private const float WalkSpeed = 78f;
    private const float PatrolPadding = 170f;
    private const float RightEdgeChasePadding = 900f;
    private const float FrameTime = 0.11f;
    private const float AttackFrameTime = 0.1f;
    private const float SuperAttackFrameTime = 0.095f;
    private const float DeathFrameTime = 0.12f;
    private const float AttackRange = 175f;
    private const float SuperAttackRange = 235f;
    private const float StopDistance = 135f;
    private const float AttackCooldown = 1.35f;
    private const float SuperAttackCooldown = 6.5f;
    private const float InitialSuperAttackDelay = 2.8f;
    private const float HitFlashDuration = 0.12f;
    private const int SuperAttackHitCount = 4;
    private const string WalkSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (29).zip";
    private const string AttackSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (30).zip";
    private const string SuperAttackSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (31).zip";
    private const string DeathSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (32).zip";
    private const string HitSoundPath = @"c:\Users\user\Downloads\short-powerful-blow-to-a-pile-of-iron.mp3";
    private const string DeathSoundPath = @"c:\Users\user\Downloads\tmp_7901-951678082.mp3";
    private const string MissSoundPath = @"c:\Users\user\Downloads\mixkit-soft-quick-punch-2151.wav";

    public bool IsLoaded => _walkFrames.Count > 0;
    public bool IsAlive => !_isDefeated;
    public bool IsDefeatedOrDying => _isDying || _isDefeated;
    public bool IsInvulnerable => _isSuperAttacking;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();
        LoadFramesFromZip(graphicsDevice, WalkSpriteZipSourcePath, _walkFrames);
        LoadFramesFromZip(graphicsDevice, AttackSpriteZipSourcePath, _attackFrames);
        LoadFramesFromZip(graphicsDevice, SuperAttackSpriteZipSourcePath, _superAttackFrames);
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
        _superAttackCooldown = InitialSuperAttackDelay;
        _hitFlashTimer = 0f;
        _deathTimer = 0f;
        _health = MaxHealth;
        _currentFrame = 0;
        _currentAttackFrame = 0;
        _currentDeathFrame = 0;
        _isAttacking = false;
        _isSuperAttacking = false;
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

        _attackCooldown = Math.Max(0f, _attackCooldown - delta);
        _superAttackCooldown = Math.Max(0f, _superAttackCooldown - delta);
        _hitFlashTimer = Math.Max(0f, _hitFlashTimer - delta);
        if (_isAttacking)
        {
            UpdateAttack(delta, player);
            return;
        }

        var distanceToPlayer = Vector2.Distance(player.Position, _position);
        if (_superAttackFrames.Count > 0 && distanceToPlayer <= SuperAttackRange && _superAttackCooldown <= 0f)
        {
            BeginAttack(player, superAttack: true);
            return;
        }

        if (_attackFrames.Count > 0 && distanceToPlayer <= AttackRange && _attackCooldown <= 0f)
        {
            BeginAttack(player, superAttack: false);
            return;
        }

        UpdateWalkAnimation(delta);
        UpdateMovement(delta, player);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset)
    {
        if (!IsLoaded || _isDefeated)
        {
            return;
        }

        var texture = GetCurrentTexture();
        var effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
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
        foreach (var frame in _superAttackFrames)
        {
            frame.Dispose();
        }

        _superAttackFrames.Clear();
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

        if (_isSuperAttacking && _superAttackFrames.Count > 0)
        {
            return _superAttackFrames[Math.Clamp(_currentAttackFrame, 0, _superAttackFrames.Count - 1)];
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

    public Rectangle GetCollisionBounds()
    {
        if (!IsLoaded || _isDefeated || _isDying)
        {
            return Rectangle.Empty;
        }

        var texture = GetCurrentTexture();
        var width = (int)MathF.Round(texture.Width * Scale);
        var height = (int)MathF.Round(texture.Height * Scale);
        var trimX = Math.Max(12, width / 5);
        var trimTop = Math.Max(10, height / 8);
        return new Rectangle(
            (int)MathF.Round(_position.X) + trimX,
            (int)MathF.Round(_position.Y) + trimTop,
            Math.Max(24, width - trimX * 2),
            Math.Max(40, height - trimTop));
    }

    public void ApplyDamage(int amount)
    {
        if (_isDefeated || _isDying || IsInvulnerable)
        {
            return;
        }

        _health = Math.Max(0, _health - Math.Max(0, amount));
        _hitFlashTimer = HitFlashDuration;
        if (_health <= 0)
        {
            BeginDeath();
        }
    }

    private void BeginDeath()
    {
        _isAttacking = false;
        _isSuperAttacking = false;
        _attackDamageApplied = false;
        _isDying = true;
        _deathTimer = 0f;
        _currentDeathFrame = 0;
        PlayDeathSound();
        if (_deathFrames.Count == 0)
        {
            _isDying = false;
            _isDefeated = true;
        }
    }

    private void UpdateDeath(float delta)
    {
        if (_deathFrames.Count == 0)
        {
            _isDying = false;
            _isDefeated = true;
            return;
        }

        _deathTimer += delta;
        while (_deathTimer >= DeathFrameTime)
        {
            _deathTimer -= DeathFrameTime;
            if (_currentDeathFrame < _deathFrames.Count - 1)
            {
                _currentDeathFrame++;
            }
            else
            {
                _isDying = false;
                _isDefeated = true;
                return;
            }
        }
    }

    private void BeginAttack(Player player, bool superAttack)
    {
        _isAttacking = true;
        _isSuperAttacking = superAttack;
        _attackDamageApplied = false;
        _attackTimer = 0f;
        _currentAttackFrame = 0;
        _facingRight = player.Position.X >= _position.X;
    }

    private void UpdateAttack(float delta, Player player)
    {
        var frames = _isSuperAttacking ? _superAttackFrames : _attackFrames;
        var frameTime = _isSuperAttacking ? SuperAttackFrameTime : AttackFrameTime;
        if (frames.Count == 0)
        {
            _isAttacking = false;
            _isSuperAttacking = false;
            return;
        }

        _attackTimer += delta;
        while (_attackTimer >= frameTime)
        {
            _attackTimer -= frameTime;
            _currentAttackFrame++;
        }

        if (!_attackDamageApplied && _currentAttackFrame >= Math.Max(1, frames.Count / 2))
        {
            var hitPlayer = GetAttackBounds().Intersects(player.GetCollisionBounds());
            var damagedPlayer = false;
            if (hitPlayer)
            {
                if (_isSuperAttacking)
                {
                    for (var i = 0; i < SuperAttackHitCount; i++)
                    {
                        player.NotifyHit();
                    }

                    damagedPlayer = true;
                }
                else if (!player.IsCrouching)
                {
                    player.NotifyHit();
                    damagedPlayer = true;
                }
            }

            if (damagedPlayer)
            {
                PlayHitSound();
            }
            else
            {
                PlayMissSound();
            }

            _attackDamageApplied = true;
        }

        if (_currentAttackFrame < frames.Count)
        {
            return;
        }

        _isAttacking = false;
        if (_isSuperAttacking)
        {
            _superAttackCooldown = SuperAttackCooldown;
        }

        _isSuperAttacking = false;
        _currentAttackFrame = 0;
        _attackCooldown = AttackCooldown;
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
            // Sound playback should never interrupt the fight.
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
            // Sound playback should never interrupt the fight.
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
            // Sound playback should never interrupt the fight.
        }
    }

    private void UpdateMovement(float delta, Player player)
    {
        var minX = _worldBounds.Left + PatrolPadding;
        var maxX = _worldBounds.Right + RightEdgeChasePadding;
        var dx = player.Position.X - _position.X;
        _facingRight = dx >= 0f;
        if (MathF.Abs(dx) <= StopDistance)
        {
            return;
        }

        _position.X += MathF.Sign(dx) * WalkSpeed * delta;
        _position.X = MathHelper.Clamp(_position.X, minX, maxX);
    }

    private Rectangle GetAttackBounds()
    {
        var bodyBounds = GetCollisionBounds();
        if (bodyBounds == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        var attackWidth = _isSuperAttacking ? 235 : 190;
        var attackHeight = Math.Max(80, bodyBounds.Height);
        var x = _facingRight
            ? bodyBounds.Right - 24
            : bodyBounds.Left - attackWidth + 24;
        var y = bodyBounds.Y + Math.Max(0, (bodyBounds.Height - attackHeight) / 2);

        return new Rectangle(x, y, attackWidth, attackHeight);
    }

    private static void LoadFramesFromZip(GraphicsDevice graphicsDevice, string zipPath, List<Texture2D> target)
    {
        if (!File.Exists(zipPath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
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
            var pixel = pixels[i];
            if (pixel.A > 0 && pixel.R < 20 && pixel.G < 20 && pixel.B < 20)
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
        if (trimmedWidth == texture.Width && trimmedHeight == texture.Height)
        {
            return texture;
        }

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

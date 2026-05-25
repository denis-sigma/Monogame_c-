using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class Venom
{
    private sealed class WebShot
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float FrameTimer;
        public int FrameIndex;
        public float Rotation;
    }

    private readonly List<Texture2D> _runFrames = new();
    private readonly List<Texture2D> _fightFrames = new();
    private readonly List<Texture2D> _jumpFrames = new();
    private readonly List<Texture2D> _deathFrames = new();
    private readonly List<Texture2D> _webShotFrames = new();
    private readonly List<WebShot> _webShots = new();
    private readonly List<ExternalMp3Player> _voiceLines = new();
    private ExternalMp3Player? _webHitSoundPlayer;
    private ExternalMp3Player? _meleeHitSoundPlayer;
    private ExternalMp3Player? _deathSoundPlayer;
    private Texture2D? _idleFrame;
    private Texture2D? _shootFrame;
    private Vector2 _position;
    private Vector2 _spawnPoint;
    private Rectangle _worldBounds;
    private float _pathY;
    private float _animationTimer;
    private float _fightAnimationTimer;
    private float _pounceTimer;
    private float _meleeCooldown;
    private float _shootCooldown;
    private float _shootPoseTimer;
    private float _voiceLineTimer;
    private int _currentRunFrame;
    private int _currentFightFrame;
    private int _currentJumpFrame;
    private bool _facingRight = true;
    private bool _isMoving;
    private bool _isMeleeAttacking;
    private bool _isPounceAttacking;
    private bool _meleeDamageApplied;
    private bool _pounceDamageApplied;
    private Vector2 _pounceStartPosition;
    private Vector2 _pounceTargetPosition;
    private Vector2 _pounceReturnPosition;
    private float _hitFlashTimer;
    private float _deathAnimationTimer;
    private int _health = MaxHealth;
    private int _currentDeathFrame;
    private bool _isDeathAnimationFinished;

    private const float Scale = 2f;
    private const float MoveSpeed = 120f;
    private const float DetectionRange = 560f;
    private const float StopDistance = 72f;
    private const float PatrolRange = 180f;
    private const float RunFrameTime = 0.09f;
    private const float FightFrameTime = 0.08f;
    private const float MeleeRange = 125f;
    private const float MeleeCooldown = 1.15f;
    private const int MeleeDamageHits = 2;
    private const float PounceToPlayerDuration = 0.32f;
    private const float PounceHitDuration = 0.12f;
    private const float PounceReturnDuration = 0.34f;
    private const float PounceArcHeight = 92f;
    private const float PounceStandOffDistance = 78f;
    private const float EdgeRetreatPadding = 90f;
    private const float ShootRange = 520f;
    private const float ShootCooldown = 3.2f;
    private const float ShootPoseDuration = 0.28f;
    private const float WebShotSpeed = 360f;
    private const float WebShotLifetime = 2.6f;
    private const float WebShotFrameTime = 0.08f;
    private const float WebbedDuration = 2.3f;
    private const float HitFlashDuration = 0.14f;
    private const float DeathFrameTime = 0.11f;
    private const float VoiceLineMinDelay = 1.0f;
    private const float VoiceLineMaxDelay = 2.4f;
    private const int MaxHealth = 12;
    private const string FramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\VenomFrames";
    private const string FightFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\VenomFightFrames";
    private const string JumpFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\VenomJumpFrames";
    private const string WebFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\VenomWebFrames";
    private const string DeathFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\VenomDeathFrames";
    private const string WebHitSoundPath = @"c:\Users\user\Downloads\zvuk-pautini-spidermena.mp3";
    private const string MeleeHitSoundPath = @"c:\Users\user\Downloads\sound-hitting-metal.mp3";
    private const string DeathSoundPath = @"c:\Users\user\Downloads\zhal-konechno-etogo-dobriaka.mp3";
    private static readonly string[] VoiceLinePaths =
    {
        @"c:\Users\user\Downloads\Voicy_You Are A Looser.mp3",
        @"c:\Users\user\Downloads\Voicy_We Are Venom.mp3",
        @"c:\Users\user\Downloads\Voicy_Outstanding.mp3"
    };

    public bool IsLoaded => _idleFrame != null;
    public bool IsAlive => _health > 0;
    public float VisualHeight => GetStandingHeight() * Scale;

    public Venom(Vector2 startPosition)
    {
        _position = startPosition;
        _spawnPoint = startPosition;
        _pathY = startPosition.Y;
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
            }
            else
            {
                _runFrames.Add(texture);
            }
        }

        LoadWebFrames(graphicsDevice);
        LoadFightFrames(graphicsDevice);
        LoadJumpFrames(graphicsDevice);
        LoadDeathFrames(graphicsDevice);
        LoadVoiceLines();
        LoadWebHitSound();
        LoadMeleeHitSound();
        LoadDeathSound();
        ResetVoiceLineTimer();
    }

    public void SetWorld(Rectangle worldBounds, float pathY)
    {
        _worldBounds = worldBounds;
        _pathY = pathY;
        _position.Y = pathY;
        _spawnPoint.Y = pathY;
    }

    public void SetPosition(Vector2 position)
    {
        _position = position;
        _spawnPoint = position;
        _pathY = position.Y;
    }

    public void Update(GameTime gameTime, Player player)
    {
        if (!IsLoaded)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (!IsAlive)
        {
            UpdateDeathAnimation(delta);
            return;
        }

        UpdateWebShots(delta, player);

        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer = Math.Max(0f, _hitFlashTimer - delta);
        }

        if (_shootPoseTimer > 0f)
        {
            _shootPoseTimer = Math.Max(0f, _shootPoseTimer - delta);
        }

        _meleeCooldown = Math.Max(0f, _meleeCooldown - delta);
        _shootCooldown = Math.Max(0f, _shootCooldown - delta);

        var toPlayer = player.Position - _position;
        var distanceToPlayer = MathF.Abs(toPlayer.X);
        var velocityX = 0f;
        UpdateVoiceLines(delta, distanceToPlayer);

        if (_isPounceAttacking)
        {
            UpdatePounceAttack(delta, player);
            velocityX = 0f;
        }
        else if (_isMeleeAttacking)
        {
            UpdateMeleeAttack(delta, player);
            velocityX = 0f;
        }
        else if (_meleeCooldown <= 0f && _fightFrames.Count > 0 && distanceToPlayer <= MeleeRange)
        {
            BeginMeleeAttack(toPlayer);
            velocityX = 0f;
        }
        else if (_shootCooldown <= 0f && _shootFrame != null && _webShotFrames.Count > 0 && distanceToPlayer <= ShootRange)
        {
            FireWebAt(player);
            _shootCooldown = ShootCooldown;
            _shootPoseTimer = ShootPoseDuration;
            velocityX = 0f;
        }
        else if (_shootPoseTimer > 0f)
        {
            velocityX = 0f;
        }
        else if (distanceToPlayer > StopDistance)
        {
            velocityX = MathF.Sign(toPlayer.X) * MoveSpeed;
        }

        _isMoving = MathF.Abs(velocityX) > 0.01f;
        if (_isMoving)
        {
            _facingRight = velocityX > 0f;
            _position.X += velocityX * delta;
            _animationTimer += delta;
            if (_runFrames.Count > 0 && _animationTimer >= RunFrameTime)
            {
                _animationTimer -= RunFrameTime;
                _currentRunFrame = (_currentRunFrame + 1) % _runFrames.Count;
            }
        }
        else
        {
            _animationTimer = 0f;
            _currentRunFrame = 0;
        }

        _position.X = MathHelper.Clamp(_position.X, _worldBounds.Left, _worldBounds.Right);
        if (!_isPounceAttacking)
        {
            _position.Y = _pathY;
        }
    }

    public bool ApplyDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return false;
        }

        _health = Math.Max(0, _health - amount);
        _hitFlashTimer = HitFlashDuration;
        if (_health == 0)
        {
            _isMoving = false;
            _isMeleeAttacking = false;
            _isPounceAttacking = false;
            _shootPoseTimer = 0f;
            _webShots.Clear();
            _deathAnimationTimer = 0f;
            _currentDeathFrame = 0;
            _isDeathAnimationFinished = false;
            PlayDeathSound();
        }

        return _health == 0;
    }

    public Rectangle GetCollisionBounds()
    {
        if (!IsLoaded || !IsAlive)
        {
            return new Rectangle(0, 0, 0, 0);
        }

        var texture = GetCurrentTexture();
        if (texture == null)
        {
            return new Rectangle(0, 0, 0, 0);
        }

        var width = (int)(texture.Width * Scale);
        var height = (int)(texture.Height * Scale);
        var x = (int)_position.X + width / 5;
        var y = (int)_position.Y + height / 8;
        var collisionWidth = Math.Max(22, width - width * 2 / 5);
        var collisionHeight = Math.Max(34, height - height / 4);
        return new Rectangle(x, y, collisionWidth, collisionHeight);
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
        var effects = !IsAlive ? SpriteEffects.None : (_facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
        var tint = _hitFlashTimer > 0f ? new Color(185, 235, 255) : Color.White;
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

        if (IsAlive)
        {
            DrawWebShots(spriteBatch, cameraPosition, visualYOffset);
        }
    }

    public void Unload()
    {
        _idleFrame?.Dispose();
        _idleFrame = null;
        _shootFrame?.Dispose();
        _shootFrame = null;

        foreach (var frame in _runFrames)
        {
            frame.Dispose();
        }

        _runFrames.Clear();
        foreach (var frame in _fightFrames)
        {
            frame.Dispose();
        }

        _fightFrames.Clear();
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
        foreach (var frame in _webShotFrames)
        {
            frame.Dispose();
        }

        _webShotFrames.Clear();
        _webShots.Clear();
        foreach (var voiceLine in _voiceLines)
        {
            voiceLine.Dispose();
        }

        _voiceLines.Clear();
        _webHitSoundPlayer?.Dispose();
        _webHitSoundPlayer = null;
        _meleeHitSoundPlayer?.Dispose();
        _meleeHitSoundPlayer = null;
        _deathSoundPlayer?.Dispose();
        _deathSoundPlayer = null;
    }

    private void LoadWebHitSound()
    {
        _webHitSoundPlayer?.Dispose();
        _webHitSoundPlayer = null;

        if (!File.Exists(WebHitSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(WebHitSoundPath, 88, repeat: false))
        {
            _webHitSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayWebHitSound()
    {
        _webHitSoundPlayer?.PlayFromStart();
    }

    private void LoadMeleeHitSound()
    {
        _meleeHitSoundPlayer?.Dispose();
        _meleeHitSoundPlayer = null;

        if (!File.Exists(MeleeHitSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(MeleeHitSoundPath, 90, repeat: false))
        {
            _meleeHitSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayMeleeHitSound()
    {
        _meleeHitSoundPlayer?.PlayFromStart();
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
        if (player.Load(DeathSoundPath, 92, repeat: false))
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

    private void LoadVoiceLines()
    {
        foreach (var voiceLine in _voiceLines)
        {
            voiceLine.Dispose();
        }

        _voiceLines.Clear();
        foreach (var path in VoiceLinePaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var player = new ExternalMp3Player();
            if (player.Load(path, 86, repeat: false))
            {
                _voiceLines.Add(player);
                continue;
            }

            player.Dispose();
        }
    }

    private void UpdateVoiceLines(float delta, float distanceToPlayer)
    {
        if (_voiceLines.Count == 0 || distanceToPlayer > DetectionRange)
        {
            ResetVoiceLineTimer();
            return;
        }

        _voiceLineTimer -= delta;
        if (_voiceLineTimer > 0f)
        {
            return;
        }

        var index = Random.Shared.Next(_voiceLines.Count);
        _voiceLines[index].PlayFromStart();
        ResetVoiceLineTimer();
    }

    private void ResetVoiceLineTimer()
    {
        _voiceLineTimer = VoiceLineMinDelay + Random.Shared.NextSingle() * (VoiceLineMaxDelay - VoiceLineMinDelay);
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

    private Texture2D? GetCurrentTexture()
    {
        if (!IsAlive && _deathFrames.Count > 0)
        {
            var frameIndex = _isDeathAnimationFinished
                ? _deathFrames.Count - 1
                : Math.Clamp(_currentDeathFrame, 0, _deathFrames.Count - 1);
            return _deathFrames[frameIndex];
        }

        if (_isPounceAttacking && _jumpFrames.Count > 0)
        {
            return _jumpFrames[Math.Clamp(_currentJumpFrame, 0, _jumpFrames.Count - 1)];
        }

        if (_isMeleeAttacking && _fightFrames.Count > 0)
        {
            return _fightFrames[Math.Clamp(_currentFightFrame, 0, _fightFrames.Count - 1)];
        }

        if (_shootPoseTimer > 0f && _shootFrame != null)
        {
            return _shootFrame;
        }

        return _isMoving && _runFrames.Count > 0
            ? _runFrames[Math.Clamp(_currentRunFrame, 0, _runFrames.Count - 1)]
            : _idleFrame;
    }

    private float GetStandingHeight()
    {
        return _idleFrame?.Height ?? GetCurrentTexture()?.Height ?? 0f;
    }

    private void LoadWebFrames(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(WebFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(WebFramesFolderPath, "*.png")
            .OrderBy(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            })
            .ToArray();

        foreach (var framePath in framePaths)
        {
            var name = Path.GetFileNameWithoutExtension(framePath);
            var digits = new string(name.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out var number) || number == 6)
            {
                continue;
            }

            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);

            if (number == 1)
            {
                _shootFrame = texture;
            }
            else
            {
                _webShotFrames.Add(texture);
            }
        }
    }

    private void LoadFightFrames(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(FightFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(FightFramesFolderPath, "*.png")
            .OrderBy(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            })
            .ToArray();

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _fightFrames.Add(texture);
        }
    }

    private void LoadJumpFrames(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(JumpFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(JumpFramesFolderPath, "*.png")
            .OrderBy(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            })
            .ToArray();

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _jumpFrames.Add(texture);
        }
    }

    private void LoadDeathFrames(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(DeathFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(DeathFramesFolderPath, "*.png")
            .OrderBy(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            })
            .ToArray();

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _deathFrames.Add(texture);
        }
    }

    private void UpdateDeathAnimation(float delta)
    {
        if (_isDeathAnimationFinished || _deathFrames.Count == 0)
        {
            return;
        }

        _deathAnimationTimer += delta;
        if (_deathAnimationTimer < DeathFrameTime)
        {
            return;
        }

        _deathAnimationTimer -= DeathFrameTime;
        if (_currentDeathFrame < _deathFrames.Count - 1)
        {
            _currentDeathFrame++;
            return;
        }

        _isDeathAnimationFinished = true;
    }

    private void BeginMeleeAttack(Vector2 toPlayer)
    {
        if (MathF.Abs(toPlayer.X) > 1f)
        {
            _facingRight = toPlayer.X >= 0f;
        }

        _isMoving = false;
        _isMeleeAttacking = true;
        _meleeDamageApplied = false;
        _currentFightFrame = 0;
        _fightAnimationTimer = 0f;
        _animationTimer = 0f;
    }

    private void UpdateMeleeAttack(float delta, Player player)
    {
        _fightAnimationTimer += delta;
        if (
            !_meleeDamageApplied &&
            !player.IsCrouching &&
            IsMeleeHitFrame() &&
            GetMeleeBounds().Intersects(player.GetCollisionBounds())
        )
        {
            for (var i = 0; i < MeleeDamageHits; i++)
            {
                player.NotifyHit();
            }

            PlayMeleeHitSound();
            _meleeDamageApplied = true;
        }

        if (_fightAnimationTimer < FightFrameTime)
        {
            return;
        }

        _fightAnimationTimer -= FightFrameTime;
        _currentFightFrame++;
        if (_currentFightFrame < _fightFrames.Count)
        {
            return;
        }

        _currentFightFrame = 0;
        _isMeleeAttacking = false;
        _meleeCooldown = MeleeCooldown;
        BeginRetreatToEdge(player);
    }

    private void BeginRetreatToEdge(Player player)
    {
        if (_jumpFrames.Count == 0)
        {
            return;
        }

        var retreatPosition = GetFarRetreatPosition(player);
        _facingRight = retreatPosition.X > _position.X;
        _isPounceAttacking = true;
        _isMoving = false;
        _pounceDamageApplied = true;
        _pounceTimer = PounceToPlayerDuration + PounceHitDuration;
        _currentJumpFrame = 0;
        _pounceStartPosition = _position;
        _pounceTargetPosition = _position;
        _pounceReturnPosition = retreatPosition;
    }

    private void BeginPounceAttack(Player player)
    {
        if (_jumpFrames.Count == 0)
        {
            return;
        }

        var directionToPlayer = player.Position.X >= _position.X ? 1f : -1f;
        _facingRight = directionToPlayer > 0f;
        _isPounceAttacking = true;
        _isMeleeAttacking = false;
        _isMoving = false;
        _pounceDamageApplied = false;
        _pounceTimer = 0f;
        _currentJumpFrame = 0;
        _pounceStartPosition = _position;
        _pounceReturnPosition = GetFarRetreatPosition(player);

        var targetX = player.Position.X - directionToPlayer * PounceStandOffDistance;
        targetX = MathHelper.Clamp(targetX, _worldBounds.Left, _worldBounds.Right);
        _pounceTargetPosition = new Vector2(targetX, _pathY);
    }

    private void UpdatePounceAttack(float delta, Player player)
    {
        _pounceTimer += delta;
        var totalDuration = PounceToPlayerDuration + PounceHitDuration + PounceReturnDuration;
        var frameProgress = MathHelper.Clamp(_pounceTimer / totalDuration, 0f, 0.999f);
        _currentJumpFrame = Math.Min(_jumpFrames.Count - 1, (int)(frameProgress * _jumpFrames.Count));

        if (_pounceTimer <= PounceToPlayerDuration)
        {
            var t = _pounceTimer / PounceToPlayerDuration;
            _position = Vector2.Lerp(_pounceStartPosition, _pounceTargetPosition, t);
            _position.Y = _pathY - MathF.Sin(t * MathF.PI) * PounceArcHeight;
            return;
        }

        if (_pounceTimer <= PounceToPlayerDuration + PounceHitDuration)
        {
            _position = _pounceTargetPosition;
            _position.Y = _pathY;
            if (
                !_pounceDamageApplied &&
                !player.IsCrouching &&
                GetMeleeBounds().Intersects(player.GetCollisionBounds())
            )
            {
                for (var i = 0; i < MeleeDamageHits; i++)
                {
                    player.NotifyHit();
                }

                PlayMeleeHitSound();
                _pounceDamageApplied = true;
            }

            return;
        }

        if (_pounceTimer < totalDuration)
        {
            var returnTimer = _pounceTimer - PounceToPlayerDuration - PounceHitDuration;
            var t = returnTimer / PounceReturnDuration;
            _position = Vector2.Lerp(_pounceTargetPosition, _pounceReturnPosition, t);
            _position.Y = _pathY - MathF.Sin(t * MathF.PI) * PounceArcHeight;
            return;
        }

        _position = _pounceReturnPosition;
        _position.Y = _pathY;
        _isPounceAttacking = false;
        _currentJumpFrame = 0;
        _meleeCooldown = MeleeCooldown;
        _shootCooldown = Math.Max(_shootCooldown, 0.8f);
    }

    private Vector2 GetFarRetreatPosition(Player player)
    {
        var playerIsRight = player.Position.X >= _position.X;
        var edgeX = playerIsRight
            ? _worldBounds.Left + EdgeRetreatPadding
            : _worldBounds.Right - EdgeRetreatPadding;

        return new Vector2(MathHelper.Clamp(edgeX, _worldBounds.Left, _worldBounds.Right), _pathY);
    }

    private bool IsMeleeHitFrame()
    {
        if (_fightFrames.Count <= 0)
        {
            return false;
        }

        var hitStart = Math.Max(1, _fightFrames.Count / 3);
        var hitEnd = Math.Max(hitStart, (_fightFrames.Count * 2) / 3);
        return _currentFightFrame >= hitStart && _currentFightFrame <= hitEnd;
    }

    private Rectangle GetMeleeBounds()
    {
        var bounds = GetCollisionBounds();
        var meleeWidth = Math.Max(44, bounds.Width / 2);
        var meleeHeight = Math.Max(36, bounds.Height - 8);
        var meleeX = _facingRight ? bounds.Right : bounds.Left - meleeWidth;
        var meleeY = bounds.Y + 4;
        return new Rectangle(meleeX, meleeY, meleeWidth, meleeHeight);
    }

    private void FireWebAt(Player player)
    {
        var direction = player.Position - _position;
        if (direction == Vector2.Zero)
        {
            direction = _facingRight ? Vector2.UnitX : -Vector2.UnitX;
        }

        direction.Normalize();
        _facingRight = direction.X >= 0f;
        var muzzleOffset = _facingRight ? new Vector2(46f, 24f) : new Vector2(12f, 24f);
        var spawnPosition = _position + muzzleOffset * Scale;

        _webShots.Add(new WebShot
        {
            Position = spawnPosition,
            Velocity = direction * WebShotSpeed,
            Life = WebShotLifetime,
            Rotation = MathF.Atan2(direction.Y, direction.X)
        });
    }

    private void UpdateWebShots(float delta, Player player)
    {
        var playerBounds = player.GetBulletCollisionBounds();
        for (var i = _webShots.Count - 1; i >= 0; i--)
        {
            var shot = _webShots[i];
            shot.Position += shot.Velocity * delta;
            shot.Life -= delta;
            shot.FrameTimer += delta;

            if (_webShotFrames.Count > 0 && shot.FrameTimer >= WebShotFrameTime)
            {
                shot.FrameTimer -= WebShotFrameTime;
                shot.FrameIndex = (shot.FrameIndex + 1) % _webShotFrames.Count;
            }

            var shotBounds = new Rectangle(
                (int)MathF.Round(shot.Position.X - 12f),
                (int)MathF.Round(shot.Position.Y - 12f),
                24,
                24
            );

            if (shotBounds.Intersects(playerBounds))
            {
                player.ApplyWebbed(WebbedDuration);
                PlayWebHitSound();
                BeginPounceAttack(player);
                _webShots.RemoveAt(i);
                continue;
            }

            var outOfWorld = shot.Position.X < _worldBounds.Left - 160f ||
                             shot.Position.X > _worldBounds.Right + 160f ||
                             shot.Position.Y < _worldBounds.Top - 160f ||
                             shot.Position.Y > _worldBounds.Bottom + 160f;

            if (shot.Life <= 0f || outOfWorld)
            {
                _webShots.RemoveAt(i);
                continue;
            }

            _webShots[i] = shot;
        }
    }

    private void DrawWebShots(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset)
    {
        if (_webShotFrames.Count == 0)
        {
            return;
        }

        var visualOffset = new Vector2(0f, visualYOffset);
        foreach (var shot in _webShots)
        {
            var texture = _webShotFrames[Math.Clamp(shot.FrameIndex, 0, _webShotFrames.Count - 1)];
            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            var drawPosition = shot.Position - cameraPosition + visualOffset;
            spriteBatch.Draw(
                texture,
                new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
                null,
                Color.White,
                shot.Rotation,
                origin,
                Scale,
                SpriteEffects.None,
                0f);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.IO.Compression;

namespace MyGame;

public class Player
{
    private sealed class WebProjectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float Rotation;
        public bool IsImpact;
        public float ImpactTime;
        public bool WasShotWhileJumping;
    }

    private Texture2D? _runSpriteSheet;
    private Texture2D? _fightSpriteSheet;
    private Texture2D? _webGunSpriteSheet;
    private Texture2D? _webProjectileTexture;
    private Texture2D? _webImpactTexture;
    private Texture2D? _webbedTexture;
    private Texture2D? _crouchTexture;
    private Texture2D? _jumpTexture;
    private readonly List<Texture2D> _walkTextures = new List<Texture2D>();
    private readonly List<Texture2D> _fightTextures = new List<Texture2D>();
    private readonly List<Texture2D> _sprintTextures = new List<Texture2D>();
    private readonly List<Texture2D> _webSwingTextures = new List<Texture2D>();
    private readonly List<Texture2D> _deathTextures = new List<Texture2D>();
    private Vector2 _position;
    private Vector2 _velocity;

    private const float Speed = 150f;
    private const float SprintSpeed = 245f;
    private const int RunFrameCount = 12;
    private const int FightFrameCount = 10;
    private const int WebGunFrameCount = 3;
    private const float PlayerScale = 2f;
    private const float WebProjectileScale = 2f;
    private const float AnimationFrameTime = 0.08f;
    private const float SprintAnimationFrameTime = 0.055f;
    private const float AttackFrameTime = 0.06f;
    private const float WebShootFrameTime = 0.07f;
    private const float WebSwingFrameTime = 0.22f;
    private const float DeathFrameTime = 0.13f;
    private const float WebProjectileSpeed = 420f;
    private const float WebProjectileLife = 2.3f;
    private const float WebImpactDuration = 0.12f;
    private const float WebWorldBoundsPadding = 900f;
    private const int WebDamage = 1;
    private const string SpriteSourcePath = @"c:\Users\user\Desktop\спрайты\run.png";
    private const string FightSpriteSourcePath = @"c:\Users\user\Desktop\спрайты\fight.png";
    private const string WebGunSpriteSourcePath = @"c:\Users\user\Desktop\спрайты\webgun.png";
    private const string WebProjectileSourcePath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerWeb\sprite_10.png";
    private const string WebImpactSourcePath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerWeb\sprite_2.png";
    private const string CrouchSpriteSourcePath = @"c:\Users\user\Desktop\спрайт\уклон.png";
    private const string JumpSpriteSourcePath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerJump\sprite_10.png";
    private const string WalkFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerWalkFrames";
    private const string FightFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerFightFrames";
    private const string SprintFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerSprintFrames";

    private List<Rectangle> _runFrames = new List<Rectangle>();
    private List<Rectangle> _fightFrames = new List<Rectangle>();
    private int _currentRunFrameIndex = 0;
    private float _runAnimationTimer = 0f;
    private int _currentFightFrameIndex = 0;
    private float _fightAnimationTimer = 0f;
    private int _currentWebShootFrameIndex = 0;
    private float _webShootAnimationTimer = 0f;
    private int _currentWebSwingFrameIndex = 0;
    private float _webSwingAnimationTimer = 0f;
    private float _webSwingArcTimer = 0f;
    private float _webSwingArcDirection = 1f;
    private Vector2 _webSwingArcStart;
    private int _currentDeathFrameIndex = 0;
    private float _deathAnimationTimer = 0f;
    private bool _isMoving = false;
    private bool _isAttacking = false;
    private bool _isWebShooting = false;
    private bool _isSprinting = false;
    private bool _isCrouching = false;
    private bool _isWebSwinging = false;
    private bool _isDying = false;
    private bool _isDeathAnimationFinished = false;
    private Vector2 _direction = new Vector2(1, 0);
    private KeyboardState _previousKeyboardState;
    private List<Rectangle> _webGunFrames = new List<Rectangle>();
    private List<WebProjectile> _webProjectiles = new List<WebProjectile>();
    private Rectangle _webProjectileRect;
    private Rectangle _webImpactRect;
    private Rectangle _worldBounds;
    private bool _hasWorldBounds;
    private bool _hasHorizontalPathY;
    private float _horizontalPathY;
    private float _verticalVelocity;
    private float _knockbackVelocityX;
    private bool _isJumping;
    private float _hitFlashTimer;
    private float _stamina;
    private bool _isExhausted;
    private float _webbedTimer;
    private int _webShotsRemaining;
    private float _webReloadTimer;
    private bool _meleeDamageAppliedThisAttack;
    private int _pendingHitsFromBandits;
    private SoundEffect? _webShotSound;
    private ExternalMp3Player? _webShotMp3Player;
    private SoundEffect? _punchSound;
    private ExternalMp3Player? _punchMp3Player;
    private ExternalMp3Player? _bulletHitMp3Player;
    private ExternalMp3Player? _webEnemyHitMp3Player;
    private bool _isWebShotSoundOpen;
    private const float HitFlashDuration = 0.18f;
    private const float JumpVelocity = -560f;
    private const float Gravity = 1800f;
    private const float KnockbackFriction = 1100f;
    private const int MaxWebShots = 3;
    private const float WebReloadDuration = 4f;
    private const float MaxStamina = 100f;
    private const float SprintStaminaDrainPerSecond = 28f;
    private const float AttackStaminaDrainPerSecond = 35f;
    private const float WebSwingStaminaDrainPerSecond = 45f;
    private const float StaminaRecoveryPerSecond = 20f;
    private const float ExhaustedRecoveryThreshold = 28f;
    private const float WorldRightMovePadding = 420f;
    private const float WebSwingArcDuration = 1.05f;
    private const float WebSwingArcDistance = 390f;
    private const float WebSwingArcHeight = 310f;
    private const string WebbedSpriteSourcePath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\VenomWebFrames\sprite_6.png";
    private const string WebSwingSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (33).zip";
    private const string DeathSpriteZipSourcePath = @"c:\Users\user\Downloads\sprites_collection (41).zip";
    private const string WebShotSoundPath = @"c:\Users\user\Downloads\zvuk-pautini-spidermena-2.mp3";
    private const string WebShotSoundAlias = "SpiderWebShotSound";
    private const string PunchSoundPath = @"c:\Users\user\Downloads\face-punch.mp3";
    private const string BulletHitSoundPath = @"c:\Users\user\Downloads\pulya-probivaet-jest.mp3";
    private const string WebEnemyHitSoundPath = @"c:\Users\user\Downloads\zvuk-pautini-spidermena-5.mp3";

    [DllImport("winmm.dll", EntryPoint = "mciSendStringW", CharSet = CharSet.Unicode)]
    private static extern int MciSendString(string command, IntPtr returnValue, int returnLength, IntPtr winHandle);

    public Vector2 Position => _position;
    public float VisualHeight => GetStandingFrameSize().Y * PlayerScale;
    public bool IsJumping => _isJumping;
    public bool IsAttacking => _isAttacking;
    public bool IsSprinting => _isSprinting;
    public bool IsCrouching => _isCrouching;
    public bool IsWebbed => _webbedTimer > 0f;
    public bool IsDeathAnimationFinished => _isDeathAnimationFinished;
    public float Stamina01 => MaxStamina <= 0f ? 0f : MathHelper.Clamp(_stamina / MaxStamina, 0f, 1f);
    public float WebMeter01
    {
        get
        {
            if (_webShotsRemaining > 0)
            {
                return _webShotsRemaining / (float)MaxWebShots;
            }

            if (_webReloadTimer <= 0f)
            {
                return 0f;
            }

            return MathHelper.Clamp(1f - (_webReloadTimer / WebReloadDuration), 0f, 1f);
        }
    }

    public Player(Vector2 startPosition)
    {
        _position = startPosition;
        _stamina = MaxStamina;
        _webShotsRemaining = MaxWebShots;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();
        LoadWebShotSound();
        LoadPunchSound();
        LoadBulletHitSound();
        LoadWebEnemyHitSound();

        var outputRunPath = Path.Combine(AppContext.BaseDirectory, "Content", "run.png");
        var outputFightPath = Path.Combine(AppContext.BaseDirectory, "Content", "fight.png");
        var outputWebGunPath = Path.Combine(AppContext.BaseDirectory, "Content", "webgun.png");
        var outputWebProjectilePath = Path.Combine(AppContext.BaseDirectory, "Content", "web-projectile.png");
        var outputWebImpactPath = Path.Combine(AppContext.BaseDirectory, "Content", "web-impact.png");
        var outputWebbedPath = Path.Combine(AppContext.BaseDirectory, "Content", "player-webbed.png");
        var outputCrouchPath = Path.Combine(AppContext.BaseDirectory, "Content", "crouch.png");
        var outputJumpPath = Path.Combine(AppContext.BaseDirectory, "Content", "jump.png");
        Directory.CreateDirectory(Path.GetDirectoryName(outputRunPath)!);

        if (!File.Exists(outputRunPath) && File.Exists(SpriteSourcePath))
        {
            File.Copy(SpriteSourcePath, outputRunPath, overwrite: true);
        }

        if (!File.Exists(outputFightPath) && File.Exists(FightSpriteSourcePath))
        {
            File.Copy(FightSpriteSourcePath, outputFightPath, overwrite: true);
        }
        if (!File.Exists(outputWebGunPath) && File.Exists(WebGunSpriteSourcePath))
        {
            File.Copy(WebGunSpriteSourcePath, outputWebGunPath, overwrite: true);
        }
        if (!File.Exists(outputWebProjectilePath) && File.Exists(WebProjectileSourcePath))
        {
            File.Copy(WebProjectileSourcePath, outputWebProjectilePath, overwrite: true);
        }
        if (!File.Exists(outputWebImpactPath) && File.Exists(WebImpactSourcePath))
        {
            File.Copy(WebImpactSourcePath, outputWebImpactPath, overwrite: true);
        }
        if (!File.Exists(outputWebbedPath) && File.Exists(WebbedSpriteSourcePath))
        {
            File.Copy(WebbedSpriteSourcePath, outputWebbedPath, overwrite: true);
        }
        if (!File.Exists(outputCrouchPath) && File.Exists(CrouchSpriteSourcePath))
        {
            File.Copy(CrouchSpriteSourcePath, outputCrouchPath, overwrite: true);
        }
        if (!File.Exists(outputJumpPath) && File.Exists(JumpSpriteSourcePath))
        {
            File.Copy(JumpSpriteSourcePath, outputJumpPath, overwrite: true);
        }

        var runPath = outputRunPath;
        if (!File.Exists(runPath))
        {
            var projectRunPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "run.png")
            );

            if (File.Exists(projectRunPath))
            {
                runPath = projectRunPath;
            }
        }

        var fightPath = outputFightPath;
        if (!File.Exists(fightPath))
        {
            var projectFightPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "fight.png")
            );

            if (File.Exists(projectFightPath))
            {
                fightPath = projectFightPath;
            }
        }

        var webGunPath = outputWebGunPath;
        if (!File.Exists(webGunPath))
        {
            var projectWebGunPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "webgun.png")
            );

            if (File.Exists(projectWebGunPath))
            {
                webGunPath = projectWebGunPath;
            }
        }

        var crouchPath = outputCrouchPath;
        if (!File.Exists(crouchPath))
        {
            var projectCrouchPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "crouch.png")
            );

            if (File.Exists(projectCrouchPath))
            {
                crouchPath = projectCrouchPath;
            }
        }

        var jumpPath = outputJumpPath;
        if (!File.Exists(jumpPath))
        {
            var projectJumpPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "jump.png")
            );

            if (File.Exists(projectJumpPath))
            {
                jumpPath = projectJumpPath;
            }
        }

        var webProjectilePath = outputWebProjectilePath;
        if (!File.Exists(webProjectilePath))
        {
            var projectWebProjectilePath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "web-projectile.png")
            );

            if (File.Exists(projectWebProjectilePath))
            {
                webProjectilePath = projectWebProjectilePath;
            }
        }

        var webImpactPath = outputWebImpactPath;
        if (!File.Exists(webImpactPath))
        {
            var projectWebImpactPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "web-impact.png")
            );

            if (File.Exists(projectWebImpactPath))
            {
                webImpactPath = projectWebImpactPath;
            }
        }

        var webbedPath = outputWebbedPath;
        if (!File.Exists(webbedPath))
        {
            var projectWebbedPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Content", "player-webbed.png")
            );

            if (File.Exists(projectWebbedPath))
            {
                webbedPath = projectWebbedPath;
            }
        }

        if (!File.Exists(runPath))
        {
            if (_walkTextures.Count == 0)
            {
                LoadWalkTextures(graphicsDevice);
            }
            if (_walkTextures.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"run.png не найден по путям: {outputRunPath} и {SpriteSourcePath}");
                return;
            }
        }

        if (File.Exists(runPath))
        {
            using var runStream = File.OpenRead(runPath);
            _runSpriteSheet = Texture2D.FromStream(graphicsDevice, runStream);
            ApplyBlackKey(_runSpriteSheet);

            _runFrames = ExtractFramesByOpaqueColumns(_runSpriteSheet, RunFrameCount);
            if (_runFrames.Count == 0)
            {
                _runFrames = BuildUniformFrames(_runSpriteSheet, RunFrameCount);
            }
        }
        if (_walkTextures.Count == 0)
        {
            LoadWalkTextures(graphicsDevice);
        }

        if (File.Exists(fightPath))
        {
            using var fightStream = File.OpenRead(fightPath);
            _fightSpriteSheet = Texture2D.FromStream(graphicsDevice, fightStream);
            ApplyBlackKey(_fightSpriteSheet);
            _fightFrames = ExtractFramesBySegmentsAndTrim(_fightSpriteSheet, FightFrameCount);
            if (_fightFrames.Count == 0)
            {
                _fightFrames = BuildUniformFrames(_fightSpriteSheet, FightFrameCount);
            }
        }
        LoadFightTextures(graphicsDevice);

        if (File.Exists(webGunPath))
        {
            using var webGunStream = File.OpenRead(webGunPath);
            _webGunSpriteSheet = Texture2D.FromStream(graphicsDevice, webGunStream);
            ApplyBlackKey(_webGunSpriteSheet);
            _webGunFrames = ExtractFramesBySegmentsAndTrim(_webGunSpriteSheet, WebGunFrameCount);
            if (_webGunFrames.Count == 0)
            {
                _webGunFrames = BuildUniformFrames(_webGunSpriteSheet, WebGunFrameCount);
            }

            if (_webGunFrames.Count >= 3)
            {
                BuildWebProjectileAndImpactFrames(_webGunSpriteSheet, _webGunFrames[2]);
            }
        }

        if (File.Exists(crouchPath))
        {
            using var crouchStream = File.OpenRead(crouchPath);
            _crouchTexture = Texture2D.FromStream(graphicsDevice, crouchStream);
            ApplyBlackKey(_crouchTexture);
        }

        if (File.Exists(jumpPath))
        {
            using var jumpStream = File.OpenRead(jumpPath);
            _jumpTexture = Texture2D.FromStream(graphicsDevice, jumpStream);
            ApplyBlackKey(_jumpTexture);
        }

        if (File.Exists(webProjectilePath))
        {
            using var webProjectileStream = File.OpenRead(webProjectilePath);
            _webProjectileTexture = Texture2D.FromStream(graphicsDevice, webProjectileStream);
            ApplyBlackKey(_webProjectileTexture);
        }

        if (File.Exists(webImpactPath))
        {
            using var webImpactStream = File.OpenRead(webImpactPath);
            _webImpactTexture = Texture2D.FromStream(graphicsDevice, webImpactStream);
            ApplyBlackKey(_webImpactTexture);
        }

        if (File.Exists(webbedPath))
        {
            using var webbedStream = File.OpenRead(webbedPath);
            _webbedTexture = Texture2D.FromStream(graphicsDevice, webbedStream);
            ApplyBlackKey(_webbedTexture);
        }

        LoadSprintTextures(graphicsDevice);
        LoadWebSwingTextures(graphicsDevice);
        LoadDeathTextures(graphicsDevice);
    }

    public void SetWorldBounds(Rectangle worldBounds)
    {
        _worldBounds = worldBounds;
        _hasWorldBounds = worldBounds.Width > 0 && worldBounds.Height > 0;
    }

    public void SetHorizontalPathY(float y)
    {
        _horizontalPathY = y;
        _hasHorizontalPathY = true;
        if (!_isJumping)
        {
            _position.Y = y;
        }
    }

    public void BeginDeathAnimation()
    {
        if (_isDying || _isDeathAnimationFinished)
        {
            return;
        }

        _isDying = true;
        _isDeathAnimationFinished = _deathTextures.Count == 0;
        _currentDeathFrameIndex = 0;
        _deathAnimationTimer = 0f;
        _velocity = Vector2.Zero;
        _verticalVelocity = 0f;
        _knockbackVelocityX = 0f;
        _isMoving = false;
        _isAttacking = false;
        _isWebShooting = false;
        _isSprinting = false;
        _isCrouching = false;
        _isWebSwinging = false;
        _isJumping = false;
        _webbedTimer = 0f;
        if (_hasHorizontalPathY)
        {
            _position.Y = _horizontalPathY;
        }
    }

    public void UpdateDeathAnimation(GameTime gameTime)
    {
        if (!_isDying || _isDeathAnimationFinished)
        {
            return;
        }

        if (_deathTextures.Count == 0)
        {
            _isDeathAnimationFinished = true;
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _deathAnimationTimer += delta;
        while (_deathAnimationTimer >= DeathFrameTime)
        {
            _deathAnimationTimer -= DeathFrameTime;
            if (_currentDeathFrameIndex < _deathTextures.Count - 1)
            {
                _currentDeathFrameIndex++;
            }
            else
            {
                _isDeathAnimationFinished = true;
                _currentDeathFrameIndex = _deathTextures.Count - 1;
                return;
            }
        }
    }

    public void SetPosition(Vector2 position)
    {
        _position = position;
    }

    public Rectangle GetCollisionBounds()
    {
        var currentFrameSize = GetCurrentFrameSize();
        var width = (int)(currentFrameSize.X * PlayerScale);
        var height = (int)(currentFrameSize.Y * PlayerScale);
        var x = (int)_position.X + width / 5;
        var y = (int)_position.Y + height / 8;
        var collisionWidth = Math.Max(20, width - width * 2 / 5);
        var collisionHeight = Math.Max(28, height - height / 4);

        if (_isCrouching)
        {
            var crouchHeight = Math.Max(18, collisionHeight / 3);
            y += collisionHeight - crouchHeight;
            collisionHeight = crouchHeight;
        }

        return new Rectangle(x, y, collisionWidth, collisionHeight);
    }

    public Rectangle GetBulletCollisionBounds()
    {
        var bounds = GetCollisionBounds();
        if (!_isJumping)
        {
            return bounds;
        }

        // В прыжке учитываем только верхнюю часть тела, чтобы снаряды снизу можно было перепрыгнуть.
        var airHeight = Math.Max(14, bounds.Height / 2);
        return new Rectangle(bounds.X, bounds.Y, bounds.Width, airHeight);
    }

    public void NotifyHit()
    {
        _hitFlashTimer = HitFlashDuration;
        _pendingHitsFromBandits++;
    }

    public void NotifyBulletHit()
    {
        NotifyHit();
        PlayBulletHitSound();
    }

    public void ApplyKnockback(float horizontalVelocity, float verticalVelocity)
    {
        _knockbackVelocityX = horizontalVelocity;
        if (_hasHorizontalPathY && verticalVelocity < 0f)
        {
            _isJumping = true;
            _verticalVelocity = Math.Min(_verticalVelocity, verticalVelocity);
        }
    }

    public void ApplyWebbed(float duration)
    {
        _webbedTimer = Math.Max(_webbedTimer, duration);
        _isAttacking = false;
        _isWebShooting = false;
        _isCrouching = false;
        _isSprinting = false;
        _isMoving = false;
        _velocity = Vector2.Zero;
    }

    private static void ApplyBlackKey(Texture2D texture)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (p.A > 0 && p.R < 20 && p.G < 20 && p.B < 20)
            {
                pixels[i] = Color.Transparent;
            }
        }

        texture.SetData(pixels);
    }

    private static List<Rectangle> ExtractFramesByOpaqueColumns(Texture2D texture, int expectedFrames)
    {
        var width = texture.Width;
        var height = texture.Height;
        var pixels = new Color[width * height];
        texture.GetData(pixels);

        var opaqueColumn = new bool[width];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (pixels[y * width + x].A > 0)
                {
                    opaqueColumn[x] = true;
                    break;
                }
            }
        }

        var runs = new List<(int Start, int End)>();
        var runStart = -1;

        for (var x = 0; x < width; x++)
        {
            if (opaqueColumn[x] && runStart == -1)
            {
                runStart = x;
            }
            else if (!opaqueColumn[x] && runStart != -1)
            {
                runs.Add((runStart, x - 1));
                runStart = -1;
            }
        }

        if (runStart != -1)
        {
            runs.Add((runStart, width - 1));
        }

        if (runs.Count != expectedFrames)
        {
            return new List<Rectangle>();
        }

        const int padX = 2;
        return runs
            .Select(run =>
            {
                var x = Math.Max(0, run.Start - padX);
                var right = Math.Min(width - 1, run.End + padX);
                return new Rectangle(x, 0, right - x + 1, height);
            })
            .ToList();
    }

    private static List<Rectangle> ExtractFramesBySegmentsAndTrim(Texture2D texture, int frameCount)
    {
        var width = texture.Width;
        var height = texture.Height;
        var pixels = new Color[width * height];
        texture.GetData(pixels);

        var frames = new List<Rectangle>(frameCount);
        for (var i = 0; i < frameCount; i++)
        {
            var segStart = (int)MathF.Round(i * (width / (float)frameCount));
            var segEnd = (int)MathF.Round((i + 1) * (width / (float)frameCount)) - 1;
            segStart = Math.Clamp(segStart, 0, width - 1);
            segEnd = Math.Clamp(segEnd, segStart, width - 1);

            var minX = segEnd;
            var maxX = segStart;
            var minY = height - 1;
            var maxY = 0;
            var found = false;

            for (var y = 0; y < height; y++)
            {
                for (var x = segStart; x <= segEnd; x++)
                {
                    if (pixels[y * width + x].A <= 0) continue;
                    found = true;
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (!found)
            {
                frames.Add(new Rectangle(segStart, 0, Math.Max(1, segEnd - segStart + 1), height));
                continue;
            }

            const int padX = 1;
            const int padY = 1;
            var x0 = Math.Max(segStart, minX - padX);
            var x1 = Math.Min(segEnd, maxX + padX);
            var y0 = Math.Max(0, minY - padY);
            var y1 = Math.Min(height - 1, maxY + padY);

            frames.Add(new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1));
        }

        return frames;
    }

    private static List<Rectangle> BuildUniformFrames(Texture2D texture, int frameCount)
    {
        var frames = new List<Rectangle>(frameCount);
        var frameWidth = texture.Width / frameCount;
        var frameHeight = texture.Height;
        for (var i = 0; i < frameCount; i++)
        {
            frames.Add(new Rectangle(i * frameWidth, 0, frameWidth, frameHeight));
        }

        return frames;
    }

    private void BuildWebProjectileAndImpactFrames(Texture2D texture, Rectangle sourceRect)
    {
        var pixels = new Color[sourceRect.Width * sourceRect.Height];
        texture.GetData(0, sourceRect, pixels, 0, pixels.Length);
        var visited = new bool[sourceRect.Width * sourceRect.Height];
        var parts = new List<Rectangle>();

        for (var y = 0; y < sourceRect.Height; y++)
        {
            for (var x = 0; x < sourceRect.Width; x++)
            {
                var idx = y * sourceRect.Width + x;
                if (visited[idx] || pixels[idx].A <= 0) continue;

                var minX = x;
                var maxX = x;
                var minY = y;
                var maxY = y;
                var stack = new Stack<(int X, int Y)>();
                stack.Push((x, y));
                visited[idx] = true;

                while (stack.Count > 0)
                {
                    var (cx, cy) = stack.Pop();
                    minX = Math.Min(minX, cx);
                    maxX = Math.Max(maxX, cx);
                    minY = Math.Min(minY, cy);
                    maxY = Math.Max(maxY, cy);

                    TryVisit(cx + 1, cy, sourceRect.Width, sourceRect.Height, pixels, visited, stack);
                    TryVisit(cx - 1, cy, sourceRect.Width, sourceRect.Height, pixels, visited, stack);
                    TryVisit(cx, cy + 1, sourceRect.Width, sourceRect.Height, pixels, visited, stack);
                    TryVisit(cx, cy - 1, sourceRect.Width, sourceRect.Height, pixels, visited, stack);
                }

                parts.Add(new Rectangle(
                    sourceRect.X + minX,
                    sourceRect.Y + minY,
                    maxX - minX + 1,
                    maxY - minY + 1));
            }
        }

        if (parts.Count >= 2)
        {
            parts = parts.OrderBy(p => p.X).ToList();
            _webProjectileRect = parts[0];
            _webImpactRect = parts[^1];
        }
        else
        {
            _webProjectileRect = sourceRect;
            _webImpactRect = sourceRect;
        }
    }

    private static void TryVisit(
        int x,
        int y,
        int width,
        int height,
        Color[] pixels,
        bool[] visited,
        Stack<(int X, int Y)> stack)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        var idx = y * width + x;
        if (visited[idx] || pixels[idx].A <= 0) return;
        visited[idx] = true;
        stack.Push((x, y));
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, Vector2? webAimWorldPosition = null)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_isDying)
        {
            UpdateDeathAnimation(gameTime);
            _previousKeyboardState = keyboardState;
            return;
        }

        UpdateResources(delta);
        var attackPressed = keyboardState.IsKeyDown(Keys.Q) && !_previousKeyboardState.IsKeyDown(Keys.Q);
        var webShootPressed = keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E);
        var jumpPressed = keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space);

        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer = Math.Max(0f, _hitFlashTimer - delta);
        }

        UpdateWebProjectiles(delta);

        if (_webbedTimer > 0f)
        {
            _webbedTimer = Math.Max(0f, _webbedTimer - delta);
            _velocity = Vector2.Zero;
            _knockbackVelocityX = 0f;
            _isMoving = false;
            _isSprinting = false;
            _isCrouching = false;
            _isAttacking = false;
            _isWebShooting = false;
            _isWebSwinging = false;
            if (_hasHorizontalPathY && !_isJumping)
            {
                _position.Y = _horizontalPathY;
            }

            _previousKeyboardState = keyboardState;
            return;
        }

        var canAttack = _fightTextures.Count > 0 || (_fightSpriteSheet != null && _fightFrames.Count > 0);
        if (!_isAttacking && attackPressed && canAttack && !_isJumping && _stamina > 0f)
        {
            _isAttacking = true;
            _meleeDamageAppliedThisAttack = false;
            _currentFightFrameIndex = 0;
            _fightAnimationTimer = 0f;
            PlayPunchSound();
        }

        if (!_isAttacking &&
            !_isWebShooting &&
            webShootPressed &&
            _webShotsRemaining > 0 &&
            _webGunSpriteSheet != null &&
            _webGunFrames.Count >= 2)
        {
            _isWebShooting = true;
            _currentWebShootFrameIndex = 0;
            _webShootAnimationTimer = 0f;
            SpawnWebProjectile(webAimWorldPosition);
            _webShotsRemaining = Math.Max(0, _webShotsRemaining - 1);
            if (_webShotsRemaining == 0)
            {
                _webReloadTimer = WebReloadDuration;
            }
        }

        if (_isAttacking)
        {
            _isCrouching = false;
            _isWebSwinging = false;
            _velocity = Vector2.Zero;
            _isMoving = false;

            _fightAnimationTimer += delta;
            if (_fightAnimationTimer >= AttackFrameTime)
            {
                _fightAnimationTimer -= AttackFrameTime;
                _currentFightFrameIndex++;
                var fightFrameCount = _fightTextures.Count > 0 ? _fightTextures.Count : _fightFrames.Count;
                if (_currentFightFrameIndex >= Math.Max(1, fightFrameCount))
                {
                    _currentFightFrameIndex = 0;
                    _isAttacking = false;
                }
            }

            _previousKeyboardState = keyboardState;
            return;
        }

        if (_isWebShooting)
        {
            _isCrouching = false;
            _isWebSwinging = false;
            _velocity = Vector2.Zero;
            _isMoving = false;

            _webShootAnimationTimer += delta;
            if (_webShootAnimationTimer >= WebShootFrameTime)
            {
                _webShootAnimationTimer -= WebShootFrameTime;
                _currentWebShootFrameIndex++;
                if (_currentWebShootFrameIndex >= 2)
                {
                    _currentWebShootFrameIndex = 0;
                    _isWebShooting = false;
                }
            }

            _previousKeyboardState = keyboardState;
            return;
        }

        _velocity = Vector2.Zero;
        _isMoving = false;
        _isSprinting = false;
        var webSwingPressed = keyboardState.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R);
        if ((webSwingPressed || _isWebSwinging) && _webSwingTextures.Count > 0 && _hasHorizontalPathY)
        {
            if (webSwingPressed && !_isWebSwinging)
            {
                if (_stamina > 0f && !_isExhausted)
                {
                    BeginWebSwing(keyboardState);
                }
                else
                {
                    _previousKeyboardState = keyboardState;
                    return;
                }
            }

            UpdateWebSwing(delta, keyboardState);
            _previousKeyboardState = keyboardState;
            return;
        }

        _isWebSwinging = false;
        var crouchHeld = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        _isCrouching = crouchHeld && !_isJumping;

        if (_isCrouching)
        {
            _currentRunFrameIndex = 0;
            _runAnimationTimer = 0f;
            _previousKeyboardState = keyboardState;
            return;
        }

        if (keyboardState.IsKeyDown(Keys.A))
        {
            _velocity.X -= 1f;
            _direction = new Vector2(-1, 0);
            _isMoving = true;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            _velocity.X += 1f;
            _direction = new Vector2(1, 0);
            _isMoving = true;
        }

        var sprintHeld = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        _isSprinting = sprintHeld && _isMoving && _sprintTextures.Count > 0 && _stamina > 0f && !_isExhausted;

        // Нормализуем диагональное движение
        if (_velocity.Length() > 0)
        {
            _velocity.Normalize();
            _velocity *= _isSprinting ? SprintSpeed : Speed;
        }

        _position += _velocity * delta;
        if (MathF.Abs(_knockbackVelocityX) > 1f)
        {
            _position.X += _knockbackVelocityX * delta;
            _knockbackVelocityX = Approach(_knockbackVelocityX, 0f, KnockbackFriction * delta);
        }

        if (jumpPressed && !_isJumping && _hasHorizontalPathY && !_isCrouching)
        {
            _isJumping = true;
            _verticalVelocity = JumpVelocity;
        }

        if (_isJumping)
        {
            _verticalVelocity += Gravity * delta;
            _position.Y += _verticalVelocity * delta;
            if (_hasHorizontalPathY && _position.Y >= _horizontalPathY)
            {
                _position.Y = _horizontalPathY;
                _verticalVelocity = 0f;
                _isJumping = false;
            }
        }
        else if (_hasHorizontalPathY)
        {
            _position.Y = _horizontalPathY;
        }

        if (_hasWorldBounds)
        {
            _position.X = MathHelper.Clamp(_position.X, _worldBounds.Left, _worldBounds.Right + WorldRightMovePadding);
            _position.Y = MathHelper.Clamp(_position.Y, _worldBounds.Top, _worldBounds.Bottom);
        }

        var defaultRunFrameCount = _walkTextures.Count > 0 ? _walkTextures.Count : _runFrames.Count;
        var frameCount = Math.Max(1, _isSprinting ? _sprintTextures.Count : defaultRunFrameCount);
        var frameTime = _isSprinting ? SprintAnimationFrameTime : AnimationFrameTime;
        if (_isMoving)
        {
            _runAnimationTimer += delta;
            if (_runAnimationTimer >= frameTime)
            {
                _runAnimationTimer -= frameTime;
                _currentRunFrameIndex = (_currentRunFrameIndex + 1) % frameCount;
            }
        }
        else
        {
            _currentRunFrameIndex = 0;
            _runAnimationTimer = 0f;
        }

        _previousKeyboardState = keyboardState;
    }

    private void BeginWebSwing(KeyboardState keyboardState)
    {
        _isWebSwinging = true;
        _webSwingArcTimer = 0f;
        _webSwingAnimationTimer = 0f;
        _currentWebSwingFrameIndex = 0;

        if (keyboardState.IsKeyDown(Keys.A))
        {
            _direction = new Vector2(-1f, 0f);
        }
        else if (keyboardState.IsKeyDown(Keys.D))
        {
            _direction = new Vector2(1f, 0f);
        }

        _webSwingArcDirection = _direction.X < 0f ? -1f : 1f;
        _webSwingArcStart = new Vector2(_position.X, _horizontalPathY);
        PlayWebShotSound();
    }

    private void UpdateWebSwing(float delta, KeyboardState keyboardState)
    {
        if (_stamina <= 0f && _isExhausted)
        {
            _isWebSwinging = false;
            _webSwingArcTimer = 0f;
            if (_hasHorizontalPathY)
            {
                _position.Y = _horizontalPathY;
            }

            return;
        }

        _isCrouching = false;
        _isJumping = false;
        _isSprinting = false;
        _isMoving = true;
        _verticalVelocity = 0f;

        _webSwingArcTimer += delta;
        var progress = MathHelper.Clamp(_webSwingArcTimer / WebSwingArcDuration, 0f, 1f);
        var arcX = _webSwingArcStart.X + _webSwingArcDirection * WebSwingArcDistance * progress;
        var arcY = _horizontalPathY - WebSwingArcHeight * 4f * progress * (1f - progress);
        _position = new Vector2(arcX, arcY);

        if (progress >= 1f)
        {
            _isWebSwinging = false;
            _webSwingArcTimer = 0f;
            _position.Y = _horizontalPathY;
        }

        if (_hasWorldBounds)
        {
            _position.X = MathHelper.Clamp(_position.X, _worldBounds.Left, _worldBounds.Right + WorldRightMovePadding);
            _position.Y = MathHelper.Clamp(_position.Y, _worldBounds.Top, _worldBounds.Bottom);
        }

        _webSwingAnimationTimer += delta;
        while (_webSwingAnimationTimer >= WebSwingFrameTime)
        {
            _webSwingAnimationTimer -= WebSwingFrameTime;
            _currentWebSwingFrameIndex = (_currentWebSwingFrameIndex + 1) % _webSwingTextures.Count;
        }
    }

    private void UpdateResources(float delta)
    {
        if (_webReloadTimer > 0f)
        {
            _webReloadTimer = Math.Max(0f, _webReloadTimer - delta);
            if (_webReloadTimer <= 0f)
            {
                _webShotsRemaining = MaxWebShots;
            }
        }

        var staminaDelta = 0f;
        if (_isAttacking)
        {
            staminaDelta -= AttackStaminaDrainPerSecond * delta;
        }
        else if (_isWebSwinging)
        {
            staminaDelta -= WebSwingStaminaDrainPerSecond * delta;
        }
        else if (_isSprinting && _isMoving)
        {
            staminaDelta -= SprintStaminaDrainPerSecond * delta;
        }
        else
        {
            staminaDelta += StaminaRecoveryPerSecond * delta;
        }

        _stamina = MathHelper.Clamp(_stamina + staminaDelta, 0f, MaxStamina);
        if (_stamina <= 0.01f)
        {
            _isExhausted = true;
        }
        else if (_isExhausted && _stamina >= ExhaustedRecoveryThreshold)
        {
            _isExhausted = false;
        }
    }

    private void UpdateWebProjectiles(float delta)
    {
        for (var i = _webProjectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _webProjectiles[i];
            if (projectile.IsImpact)
            {
                projectile.ImpactTime -= delta;
                if (projectile.ImpactTime <= 0f)
                {
                    _webProjectiles.RemoveAt(i);
                }
                else
                {
                    _webProjectiles[i] = projectile;
                }
                continue;
            }

            projectile.Position += projectile.Velocity * delta;
            projectile.Life -= delta;

            if (projectile.Life <= 0f)
            {
                projectile.IsImpact = true;
                projectile.Velocity = Vector2.Zero;
                projectile.ImpactTime = WebImpactDuration;
                _webProjectiles[i] = projectile;
            }
            else
            {
                _webProjectiles[i] = projectile;
            }
        }
    }

    public bool HasIncomingWebProjectile(Rectangle targetBounds)
    {
        if (targetBounds.Width <= 0 || targetBounds.Height <= 0)
        {
            return false;
        }

        var sensorBounds = targetBounds;
        sensorBounds.Inflate(430, 110);
        var targetCenter = new Vector2(targetBounds.Center.X, targetBounds.Center.Y);

        foreach (var projectile in _webProjectiles)
        {
            if (projectile.IsImpact || projectile.Velocity == Vector2.Zero)
            {
                continue;
            }

            var projectileBounds = new Rectangle(
                (int)MathF.Round(projectile.Position.X - 18f),
                (int)MathF.Round(projectile.Position.Y - 18f),
                36,
                36);
            if (!projectileBounds.Intersects(sensorBounds))
            {
                continue;
            }

            var toTarget = targetCenter - projectile.Position;
            if (Vector2.Dot(projectile.Velocity, toTarget) > 0f)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryHitBandit(Bandit bandit)
    {
        if (!bandit.IsAlive)
        {
            return false;
        }

        var hit = false;
        var banditBounds = bandit.GetCollisionBounds();
        if (banditBounds.Width <= 0 || banditBounds.Height <= 0)
        {
            return false;
        }

        for (var i = 0; i < _webProjectiles.Count; i++)
        {
            var projectile = _webProjectiles[i];
            if (projectile.IsImpact)
            {
                continue;
            }

            var projectileHitBox = new Rectangle(
                (int)MathF.Round(projectile.Position.X - 10f),
                (int)MathF.Round(projectile.Position.Y - 10f),
                20,
                20
            );
            if (!projectileHitBox.Intersects(banditBounds))
            {
                continue;
            }

            projectile.IsImpact = true;
            projectile.Velocity = Vector2.Zero;
            projectile.ImpactTime = WebImpactDuration;
            _webProjectiles[i] = projectile;
            PlayWebEnemyHitSound();
            if (bandit.CanTakeWebDamage)
            {
                bandit.ApplyDamage(WebDamage);
            }
            else
            {
                bandit.ApplyWebImpact();
            }

            hit = true;
        }

        hit |= TryDealMeleeDamage(bandit, banditBounds);
        return hit;
    }

    public bool TryHitVenom(Venom venom)
    {
        if (!venom.IsAlive)
        {
            return false;
        }

        var hit = false;
        var venomBounds = venom.GetCollisionBounds();
        if (venomBounds.Width <= 0 || venomBounds.Height <= 0)
        {
            return false;
        }

        for (var i = 0; i < _webProjectiles.Count; i++)
        {
            var projectile = _webProjectiles[i];
            if (projectile.IsImpact)
            {
                continue;
            }

            var projectileHitBox = new Rectangle(
                (int)MathF.Round(projectile.Position.X - 10f),
                (int)MathF.Round(projectile.Position.Y - 10f),
                20,
                20
            );
            if (!projectileHitBox.Intersects(venomBounds))
            {
                continue;
            }

            projectile.IsImpact = true;
            projectile.Velocity = Vector2.Zero;
            projectile.ImpactTime = WebImpactDuration;
            _webProjectiles[i] = projectile;
            PlayWebEnemyHitSound();
            venom.ApplyDamage(WebDamage);
            hit = true;
        }

        hit |= TryDealMeleeDamage(venom, venomBounds);
        return hit;
    }

    public bool TryHitFlyingGoblin(FlyingGoblin goblin)
    {
        if (!goblin.IsAlive)
        {
            return false;
        }

        var goblinBounds = goblin.GetCollisionBounds();
        if (goblinBounds.Width <= 0 || goblinBounds.Height <= 0)
        {
            return false;
        }
        goblinBounds.Inflate(6, 6);

        var hit = false;
        for (var i = 0; i < _webProjectiles.Count; i++)
        {
            var projectile = _webProjectiles[i];
            if (projectile.IsImpact)
            {
                continue;
            }

            var projectileHitBox = new Rectangle(
                (int)MathF.Round(projectile.Position.X - 22f),
                (int)MathF.Round(projectile.Position.Y - 22f),
                44,
                44
            );
            if (!projectileHitBox.Intersects(goblinBounds))
            {
                continue;
            }

            projectile.IsImpact = true;
            projectile.Velocity = Vector2.Zero;
            projectile.ImpactTime = WebImpactDuration;
            _webProjectiles[i] = projectile;
            PlayWebEnemyHitSound();
            goblin.ApplyDamage(WebDamage);
            hit = true;
        }

        return hit;
    }

    public bool TryHitDoctorOctopus(DoctorOctopus doctorOctopus)
    {
        if (!doctorOctopus.IsAlive)
        {
            return false;
        }

        var hit = false;
        var doctorBounds = doctorOctopus.GetCollisionBounds();
        if (doctorBounds.Width <= 0 || doctorBounds.Height <= 0)
        {
            return false;
        }

        for (var i = 0; i < _webProjectiles.Count; i++)
        {
            var projectile = _webProjectiles[i];
            if (projectile.IsImpact)
            {
                continue;
            }

            var projectileHitBox = new Rectangle(
                (int)MathF.Round(projectile.Position.X - 12f),
                (int)MathF.Round(projectile.Position.Y - 12f),
                24,
                24
            );
            if (!projectileHitBox.Intersects(doctorBounds))
            {
                continue;
            }

            projectile.IsImpact = true;
            projectile.Velocity = Vector2.Zero;
            projectile.ImpactTime = WebImpactDuration;
            _webProjectiles[i] = projectile;
            PlayWebEnemyHitSound();
            if (!doctorOctopus.IsInvulnerable)
            {
                doctorOctopus.ApplyDamage(WebDamage);
            }

            hit = true;
        }

        hit |= TryDealMeleeDamage(doctorOctopus, doctorBounds);
        return hit;
    }

    private bool TryDealMeleeDamage(Bandit bandit, Rectangle banditBounds)
    {
        if (!_isAttacking || _meleeDamageAppliedThisAttack)
        {
            return false;
        }

        var fightFrameCount = _fightTextures.Count > 0 ? _fightTextures.Count : _fightFrames.Count;
        var activeFrame = Math.Max(0, Math.Min(_currentFightFrameIndex, Math.Max(0, fightFrameCount - 1)));
        var hitWindowStart = Math.Max(1, fightFrameCount / 3);
        var hitWindowEnd = Math.Max(hitWindowStart, (fightFrameCount * 2) / 3);
        if (activeFrame < hitWindowStart || activeFrame > hitWindowEnd)
        {
            return false;
        }

        var playerBounds = GetCollisionBounds();
        var meleeWidth = Math.Max(26, playerBounds.Width / 2);
        var meleeHeight = Math.Max(24, playerBounds.Height - 8);
        var meleeX = _direction.X < 0f
            ? playerBounds.Left - meleeWidth
            : playerBounds.Right;
        var meleeY = playerBounds.Y + 4;
        var meleeBounds = new Rectangle(meleeX, meleeY, meleeWidth, meleeHeight);
        if (!meleeBounds.Intersects(banditBounds))
        {
            return false;
        }

        _meleeDamageAppliedThisAttack = true;
        bandit.ApplyDamage(1);
        return true;
    }

    private bool TryDealMeleeDamage(Venom venom, Rectangle venomBounds)
    {
        if (!_isAttacking || _meleeDamageAppliedThisAttack)
        {
            return false;
        }

        var fightFrameCount = _fightTextures.Count > 0 ? _fightTextures.Count : _fightFrames.Count;
        var activeFrame = Math.Max(0, Math.Min(_currentFightFrameIndex, Math.Max(0, fightFrameCount - 1)));
        var hitWindowStart = Math.Max(1, fightFrameCount / 3);
        var hitWindowEnd = Math.Max(hitWindowStart, (fightFrameCount * 2) / 3);
        if (activeFrame < hitWindowStart || activeFrame > hitWindowEnd)
        {
            return false;
        }

        var playerBounds = GetCollisionBounds();
        var meleeWidth = Math.Max(26, playerBounds.Width / 2);
        var meleeHeight = Math.Max(24, playerBounds.Height - 8);
        var meleeX = _direction.X < 0f
            ? playerBounds.Left - meleeWidth
            : playerBounds.Right;
        var meleeY = playerBounds.Y + 4;
        var meleeBounds = new Rectangle(meleeX, meleeY, meleeWidth, meleeHeight);
        if (!meleeBounds.Intersects(venomBounds))
        {
            return false;
        }

        _meleeDamageAppliedThisAttack = true;
        venom.ApplyDamage(1);
        return true;
    }

    private bool TryDealMeleeDamage(DoctorOctopus doctorOctopus, Rectangle doctorBounds)
    {
        if (!_isAttacking || _meleeDamageAppliedThisAttack)
        {
            return false;
        }

        var fightFrameCount = _fightTextures.Count > 0 ? _fightTextures.Count : _fightFrames.Count;
        var activeFrame = Math.Max(0, Math.Min(_currentFightFrameIndex, Math.Max(0, fightFrameCount - 1)));
        var hitWindowStart = Math.Max(1, fightFrameCount / 3);
        var hitWindowEnd = Math.Max(hitWindowStart, (fightFrameCount * 2) / 3);
        if (activeFrame < hitWindowStart || activeFrame > hitWindowEnd)
        {
            return false;
        }

        var playerBounds = GetCollisionBounds();
        var meleeWidth = Math.Max(26, playerBounds.Width / 2);
        var meleeHeight = Math.Max(24, playerBounds.Height - 8);
        var meleeX = _direction.X < 0f
            ? playerBounds.Left - meleeWidth
            : playerBounds.Right;
        var meleeY = playerBounds.Y + 4;
        var meleeBounds = new Rectangle(meleeX, meleeY, meleeWidth, meleeHeight);
        if (!meleeBounds.Intersects(doctorBounds))
        {
            return false;
        }

        _meleeDamageAppliedThisAttack = true;
        if (!doctorOctopus.IsInvulnerable)
        {
            doctorOctopus.ApplyDamage(1);
        }

        return true;
    }

    public bool TryHitLizard(Lizard lizard)
    {
        if (!lizard.IsAlive)
        {
            return false;
        }

        var hit = false;
        var lizardBounds = lizard.GetCollisionBounds();
        if (lizardBounds.Width <= 0 || lizardBounds.Height <= 0)
        {
            return false;
        }

        for (var i = 0; i < _webProjectiles.Count; i++)
        {
            var projectile = _webProjectiles[i];
            if (projectile.IsImpact)
            {
                continue;
            }

            var projectileHitBox = new Rectangle(
                (int)MathF.Round(projectile.Position.X - 12f),
                (int)MathF.Round(projectile.Position.Y - 12f),
                24,
                24);
            if (!projectileHitBox.Intersects(lizardBounds))
            {
                continue;
            }

            projectile.IsImpact = true;
            projectile.Velocity = Vector2.Zero;
            projectile.ImpactTime = WebImpactDuration;
            _webProjectiles[i] = projectile;
            PlayWebEnemyHitSound();
            lizard.ApplyDamage(WebDamage);
            hit = true;
        }

        hit |= TryDealMeleeDamage(lizard, lizardBounds);
        return hit;
    }

    private bool TryDealMeleeDamage(Lizard lizard, Rectangle lizardBounds)
    {
        if (!_isAttacking || _meleeDamageAppliedThisAttack)
        {
            return false;
        }

        var fightFrameCount = _fightTextures.Count > 0 ? _fightTextures.Count : _fightFrames.Count;
        var activeFrame = Math.Max(0, Math.Min(_currentFightFrameIndex, Math.Max(0, fightFrameCount - 1)));
        var hitWindowStart = Math.Max(1, fightFrameCount / 3);
        var hitWindowEnd = Math.Max(hitWindowStart, (fightFrameCount * 2) / 3);
        if (activeFrame < hitWindowStart || activeFrame > hitWindowEnd)
        {
            return false;
        }

        var playerBounds = GetCollisionBounds();
        var meleeWidth = Math.Max(26, playerBounds.Width / 2);
        var meleeHeight = Math.Max(24, playerBounds.Height - 8);
        var meleeX = _direction.X < 0f
            ? playerBounds.Left - meleeWidth
            : playerBounds.Right;
        var meleeY = playerBounds.Y + 4;
        var meleeBounds = new Rectangle(meleeX, meleeY, meleeWidth, meleeHeight);
        if (!meleeBounds.Intersects(lizardBounds))
        {
            return false;
        }

        _meleeDamageAppliedThisAttack = true;
        lizard.ApplyDamage(1);
        return true;
    }

    public int ConsumePendingHits()
    {
        var result = _pendingHitsFromBandits;
        _pendingHitsFromBandits = 0;
        return result;
    }

    private static float Approach(float value, float target, float maxDelta)
    {
        if (value < target)
        {
            return Math.Min(value + maxDelta, target);
        }

        return Math.Max(value - maxDelta, target);
    }

    private void SpawnWebProjectile(Vector2? aimWorldPosition = null)
    {
        var dir = aimWorldPosition.HasValue
            ? aimWorldPosition.Value - _position
            : _direction;
        if (dir == Vector2.Zero)
        {
            dir = new Vector2(1, 0);
        }

        dir.Normalize();
        _direction = new Vector2(dir.X >= 0f ? 1f : -1f, 0f);
        var spawnPos = _position + GetMuzzleOffsetByDirection(dir) * PlayerScale;
        if (aimWorldPosition.HasValue)
        {
            dir = aimWorldPosition.Value - spawnPos;
            if (dir == Vector2.Zero)
            {
                dir = _direction;
            }

            dir.Normalize();
        }

        _webProjectiles.Add(new WebProjectile
        {
            Position = spawnPos,
            Velocity = dir * WebProjectileSpeed,
            Life = WebProjectileLife,
            Rotation = MathF.Atan2(dir.Y, dir.X),
            IsImpact = false,
            ImpactTime = 0f,
            WasShotWhileJumping = _isJumping
        });

        PlayWebShotSound();
    }

    private void LoadWebShotSound()
    {
        _webShotSound?.Dispose();
        _webShotSound = null;
        _webShotMp3Player?.Dispose();
        _webShotMp3Player = null;
        _isWebShotSoundOpen = false;

        if (!File.Exists(WebShotSoundPath))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(WebShotSoundPath);
            _webShotSound = SoundEffect.FromStream(stream);
            return;
        }
        catch
        {
            _webShotSound = null;
        }

        _webShotMp3Player = new ExternalMp3Player();
        if (_webShotMp3Player.Load(WebShotSoundPath, 90, repeat: false))
        {
            return;
        }

        _webShotMp3Player.Dispose();
        _webShotMp3Player = null;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        CloseWebShotSound();
        _isWebShotSoundOpen = MciSendString(
            $"open \"{WebShotSoundPath}\" type mpegvideo alias {WebShotSoundAlias}",
            IntPtr.Zero,
            0,
            IntPtr.Zero) == 0;
    }

    private void PlayWebShotSound()
    {
        if (_webShotSound != null)
        {
            _webShotSound.Play(0.85f, 0f, 0f);
            return;
        }

        if (_webShotMp3Player != null)
        {
            _webShotMp3Player.PlayFromStart();
            return;
        }

        if (!_isWebShotSoundOpen)
        {
            return;
        }

        MciSendString($"seek {WebShotSoundAlias} to start", IntPtr.Zero, 0, IntPtr.Zero);
        MciSendString($"play {WebShotSoundAlias}", IntPtr.Zero, 0, IntPtr.Zero);
    }

    private void LoadWebEnemyHitSound()
    {
        _webEnemyHitMp3Player?.Dispose();
        _webEnemyHitMp3Player = null;

        if (!File.Exists(WebEnemyHitSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(WebEnemyHitSoundPath, 88, repeat: false))
        {
            _webEnemyHitMp3Player = player;
            return;
        }

        player.Dispose();
    }

    private void PlayWebEnemyHitSound()
    {
        _webEnemyHitMp3Player?.PlayFromStart();
    }

    private void LoadPunchSound()
    {
        _punchSound?.Dispose();
        _punchSound = null;
        _punchMp3Player?.Dispose();
        _punchMp3Player = null;

        if (!File.Exists(PunchSoundPath))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(PunchSoundPath);
            _punchSound = SoundEffect.FromStream(stream);
            return;
        }
        catch
        {
            _punchSound = null;
        }

        _punchMp3Player = new ExternalMp3Player();
        if (_punchMp3Player.Load(PunchSoundPath, 90, repeat: false))
        {
            return;
        }

        _punchMp3Player.Dispose();
        _punchMp3Player = null;
    }

    private void PlayPunchSound()
    {
        if (_punchSound != null)
        {
            _punchSound.Play(0.9f, 0f, 0f);
            return;
        }

        _punchMp3Player?.PlayFromStart();
    }

    private void LoadBulletHitSound()
    {
        _bulletHitMp3Player?.Dispose();
        _bulletHitMp3Player = null;
        if (!File.Exists(BulletHitSoundPath))
        {
            return;
        }

        _bulletHitMp3Player = new ExternalMp3Player();
        if (_bulletHitMp3Player.Load(BulletHitSoundPath, 88, repeat: false))
        {
            return;
        }

        _bulletHitMp3Player.Dispose();
        _bulletHitMp3Player = null;
    }

    private void PlayBulletHitSound()
    {
        _bulletHitMp3Player?.PlayFromStart();
    }

    private void CloseWebShotSound()
    {
        if (!_isWebShotSoundOpen)
        {
            return;
        }

        MciSendString($"stop {WebShotSoundAlias}", IntPtr.Zero, 0, IntPtr.Zero);
        MciSendString($"close {WebShotSoundAlias}", IntPtr.Zero, 0, IntPtr.Zero);
        _isWebShotSoundOpen = false;
    }

    private static Vector2 GetMuzzleOffsetByDirection(Vector2 dir)
    {
        if (dir.X < -0.5f) return new Vector2(9, 22);
        if (dir.Y < -0.5f) return new Vector2(28, 10);
        if (dir.Y > 0.5f) return new Vector2(26, 34);
        return new Vector2(44, 22);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition, float visualYOffset = 0f)
    {
        var visualOffset = new Vector2(0f, visualYOffset);
        var screenPosition = _position - cameraPosition + visualOffset;
        var effects = _direction.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (_webbedTimer > 0f && _webbedTexture != null)
        {
            effects = _direction.X > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }
        var frame = GetCurrentFrame();
        if (frame.Texture == null)
        {
            return;
        }

        var tint = _hitFlashTimer > 0f ? new Color(255, 145, 145) : Color.White;

        spriteBatch.Draw(
            frame.Texture,
            new Vector2((float)System.Math.Round(screenPosition.X), (float)System.Math.Round(screenPosition.Y)),
            frame.SourceRect,
            tint,
            0f,
            Vector2.Zero,
            PlayerScale,
            effects,
            0f
        );

        DrawWebProjectiles(spriteBatch, cameraPosition, visualOffset);
    }

    private void DrawWebProjectiles(SpriteBatch spriteBatch, Vector2 cameraPosition, Vector2 visualOffset)
    {
        if (_webGunSpriteSheet == null || _webGunFrames.Count < 3) return;

        foreach (var projectile in _webProjectiles)
        {
            var useCustomProjectile = !projectile.IsImpact && _webProjectileTexture != null;
            var useCustomImpact = projectile.IsImpact && _webImpactTexture != null;

            if (!useCustomProjectile && !useCustomImpact)
            {
                var webRect = projectile.IsImpact ? _webImpactRect : _webProjectileRect;
                if (webRect.Width <= 0 || webRect.Height <= 0)
                {
                    webRect = _webGunFrames[2];
                }

                var origin = new Vector2(webRect.Width / 2f, webRect.Height / 2f);
                var drawPos = projectile.Position - cameraPosition + visualOffset;
                spriteBatch.Draw(
                    _webGunSpriteSheet,
                    new Vector2((float)Math.Round(drawPos.X), (float)Math.Round(drawPos.Y)),
                    webRect,
                    Color.White,
                    projectile.Rotation,
                    origin,
                    WebProjectileScale,
                    SpriteEffects.None,
                    0f
                );
                continue;
            }

            var texture = projectile.IsImpact ? _webImpactTexture! : _webProjectileTexture!;
            var customOrigin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            var customDrawPos = projectile.Position - cameraPosition + visualOffset;
            spriteBatch.Draw(
                texture,
                new Vector2((float)Math.Round(customDrawPos.X), (float)Math.Round(customDrawPos.Y)),
                null,
                Color.White,
                projectile.IsImpact ? 0f : projectile.Rotation,
                customOrigin,
                WebProjectileScale,
                SpriteEffects.None,
                0f
            );
        }
    }

    public void Unload()
    {
        CloseWebShotSound();
        _webShotSound?.Dispose();
        _webShotSound = null;
        _webShotMp3Player?.Dispose();
        _webShotMp3Player = null;
        _punchSound?.Dispose();
        _punchSound = null;
        _punchMp3Player?.Dispose();
        _punchMp3Player = null;
        _bulletHitMp3Player?.Dispose();
        _bulletHitMp3Player = null;
        _webEnemyHitMp3Player?.Dispose();
        _webEnemyHitMp3Player = null;
        _runSpriteSheet?.Dispose();
        _runSpriteSheet = null;
        _fightSpriteSheet?.Dispose();
        _fightSpriteSheet = null;
        _webGunSpriteSheet?.Dispose();
        _webGunSpriteSheet = null;
        _webProjectileTexture?.Dispose();
        _webProjectileTexture = null;
        _webImpactTexture?.Dispose();
        _webImpactTexture = null;
        _webbedTexture?.Dispose();
        _webbedTexture = null;
        _crouchTexture?.Dispose();
        _crouchTexture = null;
        _jumpTexture?.Dispose();
        _jumpTexture = null;
        foreach (var walkTexture in _walkTextures)
        {
            walkTexture.Dispose();
        }

        _walkTextures.Clear();
        foreach (var fightTexture in _fightTextures)
        {
            fightTexture.Dispose();
        }

        _fightTextures.Clear();
        foreach (var sprintTexture in _sprintTextures)
        {
            sprintTexture.Dispose();
        }

        _sprintTextures.Clear();
        foreach (var webSwingTexture in _webSwingTextures)
        {
            webSwingTexture.Dispose();
        }

        _webSwingTextures.Clear();
        foreach (var deathTexture in _deathTextures)
        {
            deathTexture.Dispose();
        }

        _deathTextures.Clear();
    }

    private void LoadWalkTextures(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(WalkFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(WalkFramesFolderPath, "*.png")
            .OrderBy(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            });

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _walkTextures.Add(texture);
        }
    }

    private void LoadFightTextures(GraphicsDevice graphicsDevice)
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
            });

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _fightTextures.Add(texture);
        }
    }

    private void LoadSprintTextures(GraphicsDevice graphicsDevice)
    {
        if (!Directory.Exists(SprintFramesFolderPath))
        {
            return;
        }

        var framePaths = Directory
            .GetFiles(SprintFramesFolderPath, "*.png")
            .OrderBy(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            });

        foreach (var framePath in framePaths)
        {
            using var stream = File.OpenRead(framePath);
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _sprintTextures.Add(texture);
        }
    }

    private void LoadWebSwingTextures(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(WebSwingSpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(WebSwingSpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _webSwingTextures.Add(texture);
        }
    }

    private void LoadDeathTextures(GraphicsDevice graphicsDevice)
    {
        if (!File.Exists(DeathSpriteZipSourcePath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(DeathSpriteZipSourcePath);
        var entries = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry =>
            {
                var name = Path.GetFileNameWithoutExtension(entry.FullName);
                var digits = new string(name.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            })
            .ThenBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            using var stream = entry.Open();
            var texture = Texture2D.FromStream(graphicsDevice, stream);
            ApplyBlackKey(texture);
            _deathTextures.Add(texture);
        }
    }

    private (Texture2D? Texture, Rectangle? SourceRect) GetCurrentFrame()
    {
        if (_isDying && _deathTextures.Count > 0)
        {
            return (_deathTextures[Math.Clamp(_currentDeathFrameIndex, 0, _deathTextures.Count - 1)], null);
        }

        if (_webbedTimer > 0f && _webbedTexture != null)
        {
            return (_webbedTexture, null);
        }

        if (_isAttacking && _fightTextures.Count > 0)
        {
            return (_fightTextures[Math.Clamp(_currentFightFrameIndex, 0, _fightTextures.Count - 1)], null);
        }

        if (_isAttacking && _fightFrames.Count > 0)
        {
            return (_fightSpriteSheet, _fightFrames[Math.Clamp(_currentFightFrameIndex, 0, _fightFrames.Count - 1)]);
        }

        if (_isJumping && _jumpTexture != null)
        {
            return (_jumpTexture, null);
        }

        if (_isWebSwinging && _webSwingTextures.Count > 0)
        {
            return (_webSwingTextures[Math.Clamp(_currentWebSwingFrameIndex, 0, _webSwingTextures.Count - 1)], null);
        }

        if (_isWebShooting && _webGunFrames.Count >= 2)
        {
            return (_webGunSpriteSheet, _webGunFrames[Math.Clamp(_currentWebShootFrameIndex, 0, 1)]);
        }

        if (_isCrouching && _crouchTexture != null)
        {
            return (_crouchTexture, null);
        }

        if (_isSprinting && _sprintTextures.Count > 0)
        {
            return (_sprintTextures[Math.Clamp(_currentRunFrameIndex, 0, _sprintTextures.Count - 1)], null);
        }

        if (_walkTextures.Count > 0)
        {
            return (_walkTextures[Math.Clamp(_currentRunFrameIndex, 0, _walkTextures.Count - 1)], null);
        }

        if (_runFrames.Count > 0)
        {
            return (_runSpriteSheet, _runFrames[Math.Clamp(_currentRunFrameIndex, 0, _runFrames.Count - 1)]);
        }

        return (null, null);
    }

    private Vector2 GetCurrentFrameSize()
    {
        var frame = GetCurrentFrame();
        if (frame.Texture == null)
        {
            return Vector2.Zero;
        }

        if (frame.SourceRect.HasValue)
        {
            var rect = frame.SourceRect.Value;
            return new Vector2(rect.Width, rect.Height);
        }

        return new Vector2(frame.Texture.Width, frame.Texture.Height);
    }

    private Vector2 GetStandingFrameSize()
    {
        if (_walkTextures.Count > 0)
        {
            return new Vector2(_walkTextures[0].Width, _walkTextures[0].Height);
        }

        if (_runFrames.Count > 0)
        {
            var rect = _runFrames[0];
            return new Vector2(rect.Width, rect.Height);
        }

        return GetCurrentFrameSize();
    }

}

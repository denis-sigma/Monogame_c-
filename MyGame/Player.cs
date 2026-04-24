using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;

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
    }

    private Texture2D? _runSpriteSheet;
    private Texture2D? _fightSpriteSheet;
    private Texture2D? _webGunSpriteSheet;
    private Texture2D? _crouchTexture;
    private readonly List<Texture2D> _walkTextures = new List<Texture2D>();
    private readonly List<Texture2D> _sprintTextures = new List<Texture2D>();
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
    private const float WebProjectileSpeed = 420f;
    private const float WebProjectileLife = 0.8f;
    private const float WebImpactDuration = 0.12f;
    private const string SpriteSourcePath = @"c:\Users\user\Desktop\спрайты\run.png";
    private const string FightSpriteSourcePath = @"c:\Users\user\Desktop\спрайты\fight.png";
    private const string WebGunSpriteSourcePath = @"c:\Users\user\Desktop\спрайты\webgun.png";
    private const string CrouchSpriteSourcePath = @"c:\Users\user\Desktop\спрайт\уклон.png";
    private const string WalkFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerWalkFrames";
    private const string SprintFramesFolderPath = @"c:\Users\user\Desktop\Monogame\MyGame\Content\PlayerSprintFrames";

    private List<Rectangle> _runFrames = new List<Rectangle>();
    private List<Rectangle> _fightFrames = new List<Rectangle>();
    private int _currentRunFrameIndex = 0;
    private float _runAnimationTimer = 0f;
    private int _currentFightFrameIndex = 0;
    private float _fightAnimationTimer = 0f;
    private int _currentWebShootFrameIndex = 0;
    private float _webShootAnimationTimer = 0f;
    private bool _isMoving = false;
    private bool _isAttacking = false;
    private bool _isWebShooting = false;
    private bool _isSprinting = false;
    private bool _isCrouching = false;
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
    private float _hitFlashTimer;
    private const float HitFlashDuration = 0.18f;

    public Vector2 Position => _position;

    public Player(Vector2 startPosition)
    {
        _position = startPosition;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        Unload();

        var outputRunPath = Path.Combine(AppContext.BaseDirectory, "Content", "run.png");
        var outputFightPath = Path.Combine(AppContext.BaseDirectory, "Content", "fight.png");
        var outputWebGunPath = Path.Combine(AppContext.BaseDirectory, "Content", "webgun.png");
        var outputCrouchPath = Path.Combine(AppContext.BaseDirectory, "Content", "crouch.png");
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
        if (!File.Exists(outputCrouchPath) && File.Exists(CrouchSpriteSourcePath))
        {
            File.Copy(CrouchSpriteSourcePath, outputCrouchPath, overwrite: true);
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

        LoadSprintTextures(graphicsDevice);
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
        _position.Y = y;
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

    public void NotifyHit()
    {
        _hitFlashTimer = HitFlashDuration;
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

    public void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var attackPressed = keyboardState.IsKeyDown(Keys.Q) && !_previousKeyboardState.IsKeyDown(Keys.Q);
        var webShootPressed = keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E);

        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer = Math.Max(0f, _hitFlashTimer - delta);
        }

        UpdateWebProjectiles(delta);

        if (!_isAttacking && attackPressed && _fightSpriteSheet != null && _fightFrames.Count > 0)
        {
            _isAttacking = true;
            _currentFightFrameIndex = 0;
            _fightAnimationTimer = 0f;
        }

        if (!_isAttacking && !_isWebShooting && webShootPressed && _webGunSpriteSheet != null && _webGunFrames.Count >= 2)
        {
            _isWebShooting = true;
            _currentWebShootFrameIndex = 0;
            _webShootAnimationTimer = 0f;
            SpawnWebProjectile();
        }

        if (_isAttacking)
        {
            _isCrouching = false;
            _velocity = Vector2.Zero;
            _isMoving = false;

            _fightAnimationTimer += delta;
            if (_fightAnimationTimer >= AttackFrameTime)
            {
                _fightAnimationTimer -= AttackFrameTime;
                _currentFightFrameIndex++;
                if (_currentFightFrameIndex >= _fightFrames.Count)
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
        var crouchHeld = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        _isCrouching = crouchHeld;

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
        _isSprinting = sprintHeld && _isMoving && _sprintTextures.Count > 0;

        // Нормализуем диагональное движение
        if (_velocity.Length() > 0)
        {
            _velocity.Normalize();
            _velocity *= _isSprinting ? SprintSpeed : Speed;
        }

        _position += _velocity * delta;

        if (_hasWorldBounds)
        {
            _position.X = MathHelper.Clamp(_position.X, _worldBounds.Left, _worldBounds.Right);
            _position.Y = MathHelper.Clamp(_position.Y, _worldBounds.Top, _worldBounds.Bottom);
        }

        if (_hasHorizontalPathY)
        {
            _position.Y = _horizontalPathY;
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

            var hitBounds = _hasWorldBounds && (
                projectile.Position.X < _worldBounds.Left ||
                projectile.Position.X > _worldBounds.Right ||
                projectile.Position.Y < _worldBounds.Top ||
                projectile.Position.Y > _worldBounds.Bottom
            );

            if (hitBounds || projectile.Life <= 0f)
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

    private void SpawnWebProjectile()
    {
        var dir = _direction;
        if (dir == Vector2.Zero)
        {
            dir = new Vector2(1, 0);
        }

        dir.Normalize();
        var spawnPos = _position + GetMuzzleOffsetByDirection(dir) * PlayerScale;

        _webProjectiles.Add(new WebProjectile
        {
            Position = spawnPos,
            Velocity = dir * WebProjectileSpeed,
            Life = WebProjectileLife,
            Rotation = MathF.Atan2(dir.Y, dir.X),
            IsImpact = false,
            ImpactTime = 0f
        });
    }

    private static Vector2 GetMuzzleOffsetByDirection(Vector2 dir)
    {
        if (dir.X < -0.5f) return new Vector2(9, 22);
        if (dir.Y < -0.5f) return new Vector2(28, 10);
        if (dir.Y > 0.5f) return new Vector2(26, 34);
        return new Vector2(44, 22);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition)
    {
        var screenPosition = _position - cameraPosition;
        var effects = _direction.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
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

        DrawWebProjectiles(spriteBatch, cameraPosition);
    }

    private void DrawWebProjectiles(SpriteBatch spriteBatch, Vector2 cameraPosition)
    {
        if (_webGunSpriteSheet == null || _webGunFrames.Count < 3) return;

        foreach (var projectile in _webProjectiles)
        {
            var webRect = projectile.IsImpact ? _webImpactRect : _webProjectileRect;
            if (webRect.Width <= 0 || webRect.Height <= 0)
            {
                webRect = _webGunFrames[2];
            }

            var origin = new Vector2(webRect.Width / 2f, webRect.Height / 2f);
            var drawPos = projectile.Position - cameraPosition;
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
        }
    }

    public void Unload()
    {
        _runSpriteSheet?.Dispose();
        _runSpriteSheet = null;
        _fightSpriteSheet?.Dispose();
        _fightSpriteSheet = null;
        _webGunSpriteSheet?.Dispose();
        _webGunSpriteSheet = null;
        _crouchTexture?.Dispose();
        _crouchTexture = null;
        foreach (var walkTexture in _walkTextures)
        {
            walkTexture.Dispose();
        }

        _walkTextures.Clear();
        foreach (var sprintTexture in _sprintTextures)
        {
            sprintTexture.Dispose();
        }

        _sprintTextures.Clear();
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

    private (Texture2D? Texture, Rectangle? SourceRect) GetCurrentFrame()
    {
        if (_isAttacking && _fightFrames.Count > 0)
        {
            return (_fightSpriteSheet, _fightFrames[Math.Clamp(_currentFightFrameIndex, 0, _fightFrames.Count - 1)]);
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
}

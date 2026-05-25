using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class GameLevel
{
    private enum TransitionState
    {
        None,
        FadeIn,
        FadeOut
    }

    private Texture2D? _currentMapTexture;
    private Texture2D? _newYorkTexture;
    private Texture2D? _cityChurchTexture;
    private Texture2D? _thirdMapTexture;
    private Texture2D? _fourthMapTexture;
    private Texture2D? _fifthMapTexture;
    private Texture2D? _sixthMapTexture;
    private Texture2D? _eighthMapTexture;
    private Texture2D? _ninthMapTexture;
    private Texture2D? _tenthMapTexture;
    private Texture2D? _gameOverTexture;
    private Texture2D? _finalSceneTexture;
    private Texture2D? _victoryTextTexture;
    private Texture2D? _transitionTexture;
    private Texture2D? _uiPixel;
    private Player? _player;
    private Venom? _venom;
    private Hostage? _hostage;
    private FlyingGoblin? _flyingGoblin;
    private DoctorOctopus? _doctorOctopus;
    private Lizard? _lizard;
    private ExternalMp3Player? _hostageRescueSoundPlayer;
    private ExternalMp3Player? _goblinIntroSoundPlayer;
    private ExternalMp3Player? _doctorOctopusIntroSoundPlayer;
    private ExternalMp3Player? _lizardIntroSoundPlayer;
    private ExternalMp3Player? _playerDeathSoundPlayer;
    private ExternalMp3Player? _thirdHostageRescueSoundPlayer;
    private ExternalMp3Player? _thirdHostageRescueBoostSoundPlayer;
    private ExternalMp3Player? _finalHostageRescueSoundPlayer;
    private readonly List<Bandit> _secondMapBandits = new();
    private readonly List<Bandit> _fourthMapBandits = new();
    private readonly List<Bandit> _fifthMapBandits = new();
    private readonly List<Bandit> _seventhMapBandits = new();
    private readonly List<Bandit> _ninthMapBandits = new();
    private readonly Random _random = new();
    private Vector2 _cameraPosition = Vector2.Zero;
    private Rectangle _levelBounds;
    private int _viewWidth = 1280;
    private int _viewHeight = 720;

    private TransitionState _transitionState = TransitionState.None;
    private float _transitionAlpha;
    private bool _isSecondMapLoaded;
    private bool _isThirdMapLoaded;
    private bool _isFourthMapLoaded;
    private bool _isFifthMapLoaded;
    private bool _isSixthMapLoaded;
    private bool _isSeventhMapLoaded;
    private bool _isEighthMapLoaded;
    private bool _isNinthMapLoaded;
    private bool _isTenthMapLoaded;
    private KeyboardState _previousKeyboardState;
    private float _pathY;

    public bool IsPlayerDead => _playerHealth <= 0f;
    public bool IsLizardDefeated => _lizardDefeatHealApplied;

    private const float TransitionDuration = 0.9f;
    private const float RightEdgeTransitionThreshold = 4f;
    private const float LeftEdgeTransitionThreshold = 180f;
    private const float SpawnPaddingOnNextMap = 60f;
    private const float PathYRatio = 0.94f;
    private const float BottomGroundOffset = 72f;
    private const float CharacterDropOffset = 340f;
    private const float CharacterVisualDropOffset = 190f;
    private const float BanditSpawnOffsetX = 420f;
    private const int SecondMapBanditCount = 3;
    private const int FourthMapBanditCount = 3;
    private const int FifthMapBanditCount = 3;
    private const int SeventhMapBanditCount = 3;
    private const int NinthMapBanditCount = 3;
    private const float BanditSpawnGapX = 190f;
    private const float FourthMapBanditSpawnOffsetX = 430f;
    private const float FifthMapBanditSpawnOffsetX = 360f;
    private const float SeventhMapBanditSpawnOffsetX = 430f;
    private const float NinthMapBanditSpawnOffsetX = 360f;
    private const float VenomSpawnOffsetX = 760f;
    private const float HostageSpawnPaddingRight = -28f;
    private const float RescueHoldDuration = 1.2f;
    private const float RescueVisualOffset = 130f;
    private const float FourthMapRightSpawnOffset = 420f;
    private const int FourthMapBanditExtraRightBounds = 2400;
    private const float SixthMapPathYOffset = 0f;
    private const float SixthMapVisualYOffset = 80f;
    private const float EighthMapVisualYOffset = 150f;
    private const float EighthMapDoctorExtraVisualYOffset = 35f;
    private const float EighthMapPlayerExtraVisualYOffset = 60f;
    private const float NinthMapVisualYOffset = 205f;
    private const float TenthMapVisualYOffset = 205f;
    private const int TenthMapPlayerExtraRightBounds = 1200;
    private const int TenthMapLizardExtraRightBounds = 900;
    private const string SpriteFolderPath = @"c:\Users\user\Desktop\спрайт";
    private const float MaxPlayerHealth = 100f;
    private const float BanditBulletDamage = MaxPlayerHealth / 20f;
    private float _playerHealth = MaxPlayerHealth;
    private bool _playerDeathSoundPlayed;
    private bool _venomDefeatHealApplied;
    private bool _goblinDefeatHealApplied;
    private bool _doctorOctopusDefeatHealApplied;
    private bool _lizardDefeatHealApplied;
    private float _rescueHoldTimer;
    private bool _showRescuePrompt;
    private Texture2D? _rescuePromptTexture;
    private Texture2D? _rescueKeyTexture;
    private Texture2D? _sixthMapRightSprite;
    private readonly List<Texture2D> _sixthMapRightRescueFrames = new();
    private readonly List<Texture2D> _eighthMapHostageFrames = new();
    private readonly List<Texture2D> _tenthMapHostageFrames = new();
    private readonly List<Texture2D> _tenthMapHostageRescueFrames = new();
    private int _currentSixthMapRightRescueFrame;
    private int _currentEighthMapHostageRescueFrame;
    private int _currentTenthMapHostageRescueFrame;
    private float _sixthMapRightRescueAnimationTimer;
    private float _eighthMapHostageRescueAnimationTimer;
    private float _tenthMapHostageRescueAnimationTimer;
    private bool _isSixthMapRightSpriteRescued;
    private bool _isEighthMapHostageRescued;
    private bool _isTenthMapHostageRescued;
    private Vector2 _rescuePromptWorldPosition;
    private const string SixthMapRightSpriteZipPath = @"c:\Users\user\Downloads\sprites_collection (23).zip";
    private const string SixthMapRightRescueZipPath = @"c:\Users\user\Downloads\sprites_collection (24).zip";
    private const string EighthMapHostageZipPath = @"c:\Users\user\Downloads\sprites_collection (34).zip";
    private const string TenthMapHostageZipPath = @"c:\Users\user\Downloads\sprites_collection (39).zip";
    private const string TenthMapHostageRescueZipPath = @"c:\Users\user\Downloads\sprites_collection (40).zip";
    private const string HostageRescueSoundPath = @"c:\Users\user\Downloads\quottadamquot-sound.mp3";
    private const string GoblinIntroSoundPath = @"c:\Users\user\Downloads\come-on-meat.mp3";
    private const string DoctorOctopusIntroSoundPath = @"c:\Users\user\Downloads\i-am-two-meters-tall.mp3";
    private const string LizardIntroSoundPath = @"c:\Users\user\Downloads\run-gnome (1).mp3";
    private const string GameOverImagePath = @"c:\Users\user\Downloads\maxresdefault.jpg";
    private const string FinalSceneImagePath = @"c:\Users\user\Downloads\c62f5bbe66f111eeae4db646b2a0ffc1_upscaled.jpg";
    private const string PlayerDeathSoundPath = @"c:\Users\user\Downloads\let39s-do-it-again-misha.mp3";
    private const string ThirdHostageRescueSoundPath = @"c:\Users\user\Downloads\Гайдай какой то.mp3";
    private const string FinalHostageRescueSoundPath = @"c:\Users\user\Downloads\cool-zaybal-greeting-kazakhs (1).mp3";
    private const string NewBanditSpriteZipPath = @"c:\Users\user\Downloads\sprites_collection (27).zip";
    private const string NewBanditMeleeSpriteZipPath = @"c:\Users\user\Downloads\sprites_collection (28).zip";
    private const float SixthMapRightSpriteScale = 1.1f;
    private const float SixthMapRightSpritePaddingRight = 0f;
    private const float SixthMapRightRescueFrameTime = 0.14f;
    private const float EighthMapHostageScale = 1.1f;
    private const float EighthMapHostagePaddingRight = -560f;
    private const float EighthMapHostageRescueFrameTime = 0.14f;
    private const float TenthMapHostageScale = 1.1f;
    private const float TenthMapHostagePaddingRight = -780f;
    private const float TenthMapHostageRescueFrameTime = 0.14f;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _newYorkTexture = LoadTextureWithFallback(
            graphicsDevice,
            "new-york.jpeg",
            @"c:\Users\user\Downloads\new-york.jpeg");

        _cityChurchTexture = LoadTextureWithFallback(
            graphicsDevice,
            "переход-город-церковь.jpeg",
            @"c:\Users\user\Downloads\переход-город-церковь.jpeg");

        _thirdMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "third-map.jpeg",
            @"c:\Users\user\Downloads\церковь.jpeg");

        _fourthMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "city-school.jpeg",
            @"c:\Users\user\Downloads\city-school.jpeg");
        _fifthMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "school1.jpeg",
            @"c:\Users\user\Downloads\school1.jpeg");
        _sixthMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "schoolMain.jpeg",
            @"c:\Users\user\Downloads\schoolMain.jpeg");
        _eighthMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "Gemini_Generated_Image_rzj23jrzj23jrzj2.png",
            @"c:\Users\user\Downloads\Gemini_Generated_Image_rzj23jrzj23jrzj2.png");
        _ninthMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "ce1a3949-6d4e-4cf9-926a-d66fe1856fd5-95a74b4d-fbaa-42a5-ab3b-fc004637fa20.png",
            @"c:\Users\user\Downloads\ce1a3949-6d4e-4cf9-926a-d66fe1856fd5-95a74b4d-fbaa-42a5-ab3b-fc004637fa20.png");
        _tenthMapTexture = LoadTextureWithFallback(
            graphicsDevice,
            "e30f1f75-7479-42c8-b2b6-c360adc952a2-1b0baecc-4d2d-4486-a954-c14a65c51108.png",
            @"c:\Users\user\Downloads\e30f1f75-7479-42c8-b2b6-c360adc952a2-1b0baecc-4d2d-4486-a954-c14a65c51108.png");

        _transitionTexture = LoadTextureWithFallback(
            graphicsDevice,
            "переход-ezremove.png",
            @"c:\Users\user\Downloads\переход-ezremove.png");

        _gameOverTexture = LoadTextureWithFallback(
            graphicsDevice,
            "game-over.jpg",
            GameOverImagePath);
        _finalSceneTexture = LoadTextureWithFallback(
            graphicsDevice,
            "final-scene.jpg",
            FinalSceneImagePath);
        _victoryTextTexture = CreateTextTexture(graphicsDevice, "Победа", 520, 130, 84f);

        _currentMapTexture = _newYorkTexture ?? LoadTextureWithFallback(graphicsDevice, "level1.png", null);
        if (_currentMapTexture == null)
        {
            return;
        }

        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player = new Player(new Vector2(120f, _pathY));
        _player.LoadContent(graphicsDevice);
        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);

        _venom = new Venom(new Vector2(SpawnPaddingOnNextMap + VenomSpawnOffsetX, _pathY));
        _venom.LoadContent(graphicsDevice);
        _hostage = new Hostage(new Vector2(SpawnPaddingOnNextMap + VenomSpawnOffsetX + 280f, _pathY));
        _hostage.LoadContent(graphicsDevice);
        _flyingGoblin = new FlyingGoblin();
        _flyingGoblin.LoadContent(graphicsDevice);
        _doctorOctopus = new DoctorOctopus();
        _doctorOctopus.LoadContent(graphicsDevice);
        _lizard = new Lizard();
        _lizard.LoadContent(graphicsDevice);

        CreateBandits(graphicsDevice, _secondMapBandits, SecondMapBanditCount, SpawnPaddingOnNextMap + BanditSpawnOffsetX, _pathY);
        CreateBandits(graphicsDevice, _fourthMapBandits, FourthMapBanditCount, FourthMapBanditSpawnOffsetX, _pathY);
        CreateBandits(graphicsDevice, _fifthMapBandits, FifthMapBanditCount, FifthMapBanditSpawnOffsetX, _pathY);
        CreateBandits(graphicsDevice, _seventhMapBandits, SeventhMapBanditCount, SeventhMapBanditSpawnOffsetX, _pathY);
        CreateBandits(graphicsDevice, _ninthMapBandits, NinthMapBanditCount, NinthMapBanditSpawnOffsetX, _pathY);

        _uiPixel = new Texture2D(graphicsDevice, 1, 1);
        _uiPixel.SetData(new[] { Color.White });
        _rescuePromptTexture = CreateTextTexture(graphicsDevice, "Спасти", 180, 56, 28f);
        _rescueKeyTexture = CreateTextTexture(graphicsDevice, "TAB", 110, 52, 24f);
        _sixthMapRightSprite = LoadFirstSpriteFromZip(graphicsDevice, SixthMapRightSpriteZipPath);
        LoadSpritesFromZip(graphicsDevice, SixthMapRightRescueZipPath, _sixthMapRightRescueFrames);
        LoadSpritesFromZip(graphicsDevice, EighthMapHostageZipPath, _eighthMapHostageFrames);
        LoadSpritesFromZip(graphicsDevice, TenthMapHostageZipPath, _tenthMapHostageFrames);
        LoadSpritesFromZip(graphicsDevice, TenthMapHostageRescueZipPath, _tenthMapHostageRescueFrames);
        LoadHostageRescueSound();
        LoadGoblinIntroSound();
        LoadDoctorOctopusIntroSound();
        LoadLizardIntroSound();
        LoadPlayerDeathSound();
        LoadThirdHostageRescueSound();
        LoadFinalHostageRescueSound();
        _playerHealth = MaxPlayerHealth;
        _playerDeathSoundPlayed = false;
        _venomDefeatHealApplied = false;
        _goblinDefeatHealApplied = false;
        _doctorOctopusDefeatHealApplied = false;
        _lizardDefeatHealApplied = false;
    }

    private static Texture2D? LoadTextureWithFallback(GraphicsDevice graphicsDevice, string contentFileName, string? externalSourcePath)
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Content", contentFileName);
        SyncFromExternalSource(contentPath, externalSourcePath);

        if (File.Exists(contentPath))
        {
            using var contentStream = File.OpenRead(contentPath);
            return Texture2D.FromStream(graphicsDevice, contentStream);
        }

        if (!string.IsNullOrWhiteSpace(externalSourcePath) && File.Exists(externalSourcePath))
        {
            using var externalStream = File.OpenRead(externalSourcePath);
            return Texture2D.FromStream(graphicsDevice, externalStream);
        }

        return null;
    }

    private void LoadHostageRescueSound()
    {
        _hostageRescueSoundPlayer?.Dispose();
        _hostageRescueSoundPlayer = null;

        if (!File.Exists(HostageRescueSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(HostageRescueSoundPath, 90, repeat: false))
        {
            _hostageRescueSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayHostageRescueSound()
    {
        _hostageRescueSoundPlayer?.PlayFromStart();
    }

    private void LoadGoblinIntroSound()
    {
        _goblinIntroSoundPlayer?.Dispose();
        _goblinIntroSoundPlayer = null;

        if (!File.Exists(GoblinIntroSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(GoblinIntroSoundPath, 90, repeat: false))
        {
            _goblinIntroSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayGoblinIntroSound()
    {
        _goblinIntroSoundPlayer?.PlayFromStart();
    }

    private void LoadDoctorOctopusIntroSound()
    {
        _doctorOctopusIntroSoundPlayer?.Dispose();
        _doctorOctopusIntroSoundPlayer = null;

        if (!File.Exists(DoctorOctopusIntroSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(DoctorOctopusIntroSoundPath, 90, repeat: false))
        {
            _doctorOctopusIntroSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayDoctorOctopusIntroSound()
    {
        _doctorOctopusIntroSoundPlayer?.PlayFromStart();
    }

    private void LoadLizardIntroSound()
    {
        _lizardIntroSoundPlayer?.Dispose();
        _lizardIntroSoundPlayer = null;

        if (!File.Exists(LizardIntroSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(LizardIntroSoundPath, 90, repeat: false))
        {
            _lizardIntroSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayLizardIntroSound()
    {
        _lizardIntroSoundPlayer?.PlayFromStart();
    }

    private void LoadPlayerDeathSound()
    {
        _playerDeathSoundPlayer?.Dispose();
        _playerDeathSoundPlayer = null;

        if (!File.Exists(PlayerDeathSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(PlayerDeathSoundPath, 90, repeat: false))
        {
            _playerDeathSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayPlayerDeathSoundIfNeeded()
    {
        if (_playerHealth > 0f || _playerDeathSoundPlayed)
        {
            return;
        }

        _playerDeathSoundPlayed = true;
        _playerDeathSoundPlayer?.PlayFromStart();
    }

    private void LoadThirdHostageRescueSound()
    {
        _thirdHostageRescueSoundPlayer?.Dispose();
        _thirdHostageRescueSoundPlayer = null;
        _thirdHostageRescueBoostSoundPlayer?.Dispose();
        _thirdHostageRescueBoostSoundPlayer = null;

        if (!File.Exists(ThirdHostageRescueSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(ThirdHostageRescueSoundPath, 100, repeat: false))
        {
            _thirdHostageRescueSoundPlayer = player;
        }
        else
        {
            player.Dispose();
        }

        var boostPlayer = new ExternalMp3Player();
        if (boostPlayer.Load(ThirdHostageRescueSoundPath, 100, repeat: false))
        {
            _thirdHostageRescueBoostSoundPlayer = boostPlayer;
        }
        else
        {
            boostPlayer.Dispose();
        }
    }

    private void PlayThirdHostageRescueSound()
    {
        _thirdHostageRescueSoundPlayer?.PlayFromStart();
        _thirdHostageRescueBoostSoundPlayer?.PlayFromStart();
    }

    private void LoadFinalHostageRescueSound()
    {
        _finalHostageRescueSoundPlayer?.Dispose();
        _finalHostageRescueSoundPlayer = null;

        if (!File.Exists(FinalHostageRescueSoundPath))
        {
            return;
        }

        var player = new ExternalMp3Player();
        if (player.Load(FinalHostageRescueSoundPath, 90, repeat: false))
        {
            _finalHostageRescueSoundPlayer = player;
            return;
        }

        player.Dispose();
    }

    private void PlayFinalHostageRescueSound()
    {
        _finalHostageRescueSoundPlayer?.PlayFromStart();
    }

    private void CreateBandits(GraphicsDevice graphicsDevice, List<Bandit> target, int count, float startX, float pathY)
    {
        target.Clear();
        var shooterSlots = BuildRandomBanditTypeSlots(count);
        for (var i = 0; i < count; i++)
        {
            var isShooter = shooterSlots[i];
            var bandit = isShooter
                ? new Bandit(new Vector2(startX + i * BanditSpawnGapX, pathY))
                : new Bandit(
                    new Vector2(startX + i * BanditSpawnGapX, pathY),
                    NewBanditSpriteZipPath,
                    canShoot: false,
                    NewBanditMeleeSpriteZipPath,
                    canTakeWebDamage: false);
            bandit.LoadContent(graphicsDevice);
            target.Add(bandit);
        }
    }

    private bool[] BuildRandomBanditTypeSlots(int count)
    {
        var slots = new bool[count];
        if (count <= 0)
        {
            return slots;
        }

        slots[0] = true;
        if (count > 1)
        {
            slots[1] = false;
        }

        for (var i = 2; i < count; i++)
        {
            slots[i] = _random.Next(2) == 0;
        }

        for (var i = slots.Length - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        return slots;
    }

    private static void SyncFromExternalSource(string destinationPath, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        var destinationDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static Texture2D? LoadFirstSpriteFromZip(GraphicsDevice graphicsDevice, string zipPath)
    {
        if (!File.Exists(zipPath))
        {
            return null;
        }

        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.Entries
            .Where(item => item.FullName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.FullName, System.StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (entry == null)
        {
            return null;
        }

        using var stream = entry.Open();
        var texture = Texture2D.FromStream(graphicsDevice, stream);
        ApplyBlackKey(texture);
        return TrimTransparentPixels(graphicsDevice, texture);
    }

    private static void LoadSpritesFromZip(GraphicsDevice graphicsDevice, string zipPath, List<Texture2D> target)
    {
        target.Clear();
        if (!File.Exists(zipPath))
        {
            return;
        }

        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries
            .Where(item => item.FullName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.FullName, System.StringComparer.OrdinalIgnoreCase);

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

    private static float ComputePathY(Texture2D mapTexture)
    {
        var ratioY = mapTexture.Height * PathYRatio;
        var bottomY = mapTexture.Height - BottomGroundOffset;
        var rawY = MathF.Max(ratioY, bottomY) + CharacterDropOffset;
        return MathHelper.Clamp(rawY, 0f, mapTexture.Height);
    }

    public void Update(GameTime gameTime)
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboardState = Keyboard.GetState();
        if (HandleDebugMapShortcuts(keyboardState))
        {
            _previousKeyboardState = keyboardState;
            UpdateCamera();
            return;
        }

        if (_playerHealth <= 0f)
        {
            PlayPlayerDeathSoundIfNeeded();
            _player.BeginDeathAnimation();
            _player.UpdateDeathAnimation(gameTime);
            _previousKeyboardState = keyboardState;
            return;
        }

        var currentPlayerVisualYOffset = GetPlayerVisualYOffset();
        var mouseState = Mouse.GetState();
        var mouseWorldPosition = new Vector2(mouseState.X, mouseState.Y) + _cameraPosition - new Vector2(0f, currentPlayerVisualYOffset);
        _player.Update(gameTime, keyboardState, mouseWorldPosition);

        if (_transitionState == TransitionState.None)
        {
            var playerBounds = _player.GetCollisionBounds();
            var reachedRightEdge = playerBounds.Right >= _levelBounds.Right - RightEdgeTransitionThreshold;
            var reachedLeftEdge = playerBounds.Left <= _levelBounds.Left + LeftEdgeTransitionThreshold;
            if (CanTransitionToFifthMap() && reachedLeftEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (CanTransitionToSixthMap() && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (CanTransitionToSeventhMap() && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (CanTransitionToEighthMap() && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (CanTransitionToNinthMap() && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (CanTransitionToTenthMap() && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (!_isSecondMapLoaded && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (CanTransitionToThirdMap() && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
            else if (CanTransitionToFourthMap() && reachedRightEdge)
            {
                _transitionState = TransitionState.FadeIn;
                _transitionAlpha = 0f;
            }
        }

        UpdateTransition(delta);

        UpdateActiveBandits(gameTime);

        if (_isThirdMapLoaded && _venom != null)
        {
            _venom.Update(gameTime, _player);
            _player.TryHitVenom(_venom);
        }

        if (_isSixthMapLoaded && _flyingGoblin != null)
        {
            _flyingGoblin.Update(gameTime, _player);
            _player.TryHitFlyingGoblin(_flyingGoblin);
        }

        if (_isEighthMapLoaded && _doctorOctopus != null)
        {
            _doctorOctopus.Update(gameTime, _player);
            _player.TryHitDoctorOctopus(_doctorOctopus);
        }

        if (_isTenthMapLoaded && _lizard != null)
        {
            _lizard.Update(gameTime, _player);
            _player.TryHitLizard(_lizard);
        }

        UpdateHostageRescue(gameTime, keyboardState);

        var pendingHits = _player.ConsumePendingHits();
        if (pendingHits > 0)
        {
            _playerHealth = MathHelper.Clamp(_playerHealth - pendingHits * BanditBulletDamage, 0f, MaxPlayerHealth);
            PlayPlayerDeathSoundIfNeeded();
        }

        RestoreHealthAfterBossDefeats();

        UpdateCamera();
        _previousKeyboardState = keyboardState;
    }

    private void RestoreHealthAfterBossDefeats()
    {
        if (!_venomDefeatHealApplied && _venom != null && !_venom.IsAlive)
        {
            _venomDefeatHealApplied = true;
            RestorePlayerHealthToMax();
        }

        if (!_goblinDefeatHealApplied && _flyingGoblin != null && _flyingGoblin.IsDefeatedOrDying)
        {
            _goblinDefeatHealApplied = true;
            RestorePlayerHealthToMax();
        }

        if (!_doctorOctopusDefeatHealApplied && _doctorOctopus != null && _doctorOctopus.IsDefeatedOrDying)
        {
            _doctorOctopusDefeatHealApplied = true;
            RestorePlayerHealthToMax();
        }

        if (!_lizardDefeatHealApplied && _lizard != null && _lizard.IsDefeatedOrDying)
        {
            _lizardDefeatHealApplied = true;
            RestorePlayerHealthToMax();
        }
    }

    private void RestorePlayerHealthToMax()
    {
        _playerHealth = MaxPlayerHealth;
        _playerDeathSoundPlayed = false;
    }

    private bool HandleDebugMapShortcuts(KeyboardState keyboardState)
    {
        if (IsNewKeyPress(keyboardState, Keys.D1) || IsNewKeyPress(keyboardState, Keys.NumPad1))
        {
            SwapToFirstMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D2) || IsNewKeyPress(keyboardState, Keys.NumPad2))
        {
            SwapToSecondMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D3) || IsNewKeyPress(keyboardState, Keys.NumPad3))
        {
            if (!_isSecondMapLoaded)
            {
                SwapToSecondMap();
            }

            SwapToThirdMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D4) || IsNewKeyPress(keyboardState, Keys.NumPad4))
        {
            if (!_isSecondMapLoaded)
            {
                SwapToSecondMap();
            }

            if (!_isThirdMapLoaded)
            {
                SwapToThirdMap();
            }

            ForceSwapToFourthMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D5) || IsNewKeyPress(keyboardState, Keys.NumPad5))
        {
            ForceSwapToFifthMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D6) || IsNewKeyPress(keyboardState, Keys.NumPad6))
        {
            ForceSwapToSixthMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D7) || IsNewKeyPress(keyboardState, Keys.NumPad7))
        {
            ForceSwapToSeventhMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D8) || IsNewKeyPress(keyboardState, Keys.NumPad8))
        {
            ForceSwapToEighthMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D9) || IsNewKeyPress(keyboardState, Keys.NumPad9))
        {
            ForceSwapToNinthMap();
            return true;
        }

        if (IsNewKeyPress(keyboardState, Keys.D0) || IsNewKeyPress(keyboardState, Keys.NumPad0))
        {
            ForceSwapToTenthMap();
            return true;
        }

        return false;
    }

    private bool IsNewKeyPress(KeyboardState keyboardState, Keys key)
    {
        return keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    private void UpdateTransition(float delta)
    {
        if (_transitionState == TransitionState.None)
        {
            return;
        }

        if (_transitionState == TransitionState.FadeIn)
        {
            _transitionAlpha += delta / TransitionDuration;
            if (_transitionAlpha >= 1f)
            {
                _transitionAlpha = 1f;
                if (CanTransitionToFifthMap())
                {
                    SwapToFifthMap();
                }
                else if (CanTransitionToSixthMap())
                {
                    SwapToSixthMap();
                }
                else if (CanTransitionToSeventhMap())
                {
                    SwapToSeventhMap();
                }
                else if (CanTransitionToEighthMap())
                {
                    SwapToEighthMap();
                }
                else if (CanTransitionToNinthMap())
                {
                    SwapToNinthMap();
                }
                else if (CanTransitionToTenthMap())
                {
                    SwapToTenthMap();
                }
                else if (!_isSecondMapLoaded)
                {
                    SwapToSecondMap();
                }
                else if (!_isThirdMapLoaded)
                {
                    SwapToThirdMap();
                }
                else if (CanTransitionToFourthMap())
                {
                    SwapToFourthMap();
                }
                _transitionState = TransitionState.FadeOut;
            }

            return;
        }

        _transitionAlpha -= delta / TransitionDuration;
        if (_transitionAlpha <= 0f)
        {
            _transitionAlpha = 0f;
            _transitionState = TransitionState.None;
        }
    }

    private void SwapToSecondMap()
    {
        if (_cityChurchTexture == null || _player == null || _isSecondMapLoaded)
        {
            return;
        }

        _currentMapTexture = _cityChurchTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));

        SetBanditsWorld(_secondMapBandits, _levelBounds, _pathY);

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
    }

    private void SwapToFirstMap()
    {
        if (_newYorkTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _newYorkTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(120f, _pathY));

        _cameraPosition = Vector2.Zero;
        _transitionState = TransitionState.None;
        _transitionAlpha = 0f;
        _isSecondMapLoaded = false;
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
        _rescueHoldTimer = 0f;
        _showRescuePrompt = false;
        if (_hostage != null)
        {
            _hostage.SetPosition(new Vector2(_levelBounds.Right - HostageSpawnPaddingRight, _pathY));
        }
    }

    private void SwapToThirdMap()
    {
        if (_thirdMapTexture == null || _player == null || !CanTransitionToThirdMap() || _isThirdMapLoaded)
        {
            return;
        }

        _currentMapTexture = _thirdMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));

        _venom?.SetWorld(_levelBounds, _pathY);
        _venom?.SetPosition(new Vector2(SpawnPaddingOnNextMap + VenomSpawnOffsetX, _pathY));
        _hostage?.SetPosition(new Vector2(_levelBounds.Right - HostageSpawnPaddingRight, _pathY));
        _rescueHoldTimer = 0f;
        _showRescuePrompt = false;

        _cameraPosition = Vector2.Zero;
        _isThirdMapLoaded = true;
        _isFourthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
    }

    private bool CanTransitionToThirdMap()
    {
        return _isSecondMapLoaded
            && !_isThirdMapLoaded
            && !_isFourthMapLoaded
            && !_isFifthMapLoaded
            && !_isSixthMapLoaded
            && !_isSeventhMapLoaded
            && !_isEighthMapLoaded
            && !_isNinthMapLoaded
            && !_isTenthMapLoaded
            && AreAllBanditsDefeated(_secondMapBandits);
    }

    private bool CanTransitionToFourthMap()
    {
        return _isThirdMapLoaded
            && !_isFourthMapLoaded
            && _venom != null
            && _hostage != null
            && !_venom.IsAlive
            && _hostage.IsRescued;
    }

    private bool CanTransitionToFifthMap()
    {
        return _isFourthMapLoaded && !_isFifthMapLoaded;
    }

    private bool CanTransitionToSixthMap()
    {
        return _isFifthMapLoaded && !_isSixthMapLoaded;
    }

    private bool CanTransitionToSeventhMap()
    {
        return _isSixthMapLoaded && !_isSeventhMapLoaded;
    }

    private bool CanTransitionToEighthMap()
    {
        return _isSeventhMapLoaded && !_isEighthMapLoaded && !_isNinthMapLoaded && !_isTenthMapLoaded;
    }

    private bool CanTransitionToNinthMap()
    {
        return _isEighthMapLoaded
            && !_isNinthMapLoaded
            && _doctorOctopus != null
            && !_doctorOctopus.IsAlive
            && _isEighthMapHostageRescued;
    }

    private bool CanTransitionToTenthMap()
    {
        return _isNinthMapLoaded
            && !_isTenthMapLoaded;
    }

    private void SwapToFourthMap()
    {
        if (_fourthMapTexture == null || _player == null || !CanTransitionToFourthMap())
        {
            return;
        }

        _currentMapTexture = _fourthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(_levelBounds.Right + FourthMapRightSpawnOffset, _pathY));
        var fourthMapBanditBounds = new Rectangle(
            _levelBounds.X,
            _levelBounds.Y,
            _levelBounds.Width + FourthMapBanditExtraRightBounds,
            _levelBounds.Height);
        SetBanditsWorld(_fourthMapBandits, fourthMapBanditBounds, _pathY);

        UpdateCamera();
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = true;
        _isFifthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
    }

    private void ForceSwapToFourthMap()
    {
        if (_fourthMapTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _fourthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(_levelBounds.Right + FourthMapRightSpawnOffset, _pathY));
        SetBanditsWorld(_fourthMapBandits, _levelBounds, _pathY);

        UpdateCamera();
        _isFourthMapLoaded = true;
        _isThirdMapLoaded = false;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
    }

    private void SwapToFifthMap()
    {
        if (_fifthMapTexture == null || _player == null || !CanTransitionToFifthMap())
        {
            return;
        }

        _currentMapTexture = _fifthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        SetBanditsWorld(_fifthMapBandits, _levelBounds, _pathY);

        UpdateCamera();
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = true;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
    }

    private void ForceSwapToFifthMap()
    {
        if (_fifthMapTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _fifthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        SetBanditsWorld(_fifthMapBandits, _levelBounds, _pathY);

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = true;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
    }

    private void SwapToSixthMap()
    {
        if (_sixthMapTexture == null || _player == null || !CanTransitionToSixthMap())
        {
            return;
        }

        _currentMapTexture = _sixthMapTexture;
        _levelBounds = CreateSixthMapBounds(_currentMapTexture);
        _pathY = ComputePathY(_currentMapTexture) + SixthMapPathYOffset;

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        _flyingGoblin?.SetWorld(_levelBounds, _pathY);
        ResetSixthMapRightSpriteRescue();

        _cameraPosition = Vector2.Zero;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = true;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
        PlayGoblinIntroSound();
    }

    private void ForceSwapToSixthMap()
    {
        if (_sixthMapTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _sixthMapTexture;
        _levelBounds = CreateSixthMapBounds(_currentMapTexture);
        _pathY = ComputePathY(_currentMapTexture) + SixthMapPathYOffset;

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        _flyingGoblin?.SetWorld(_levelBounds, _pathY);
        ResetSixthMapRightSpriteRescue();

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = true;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
        PlayGoblinIntroSound();
    }

    private void SwapToSeventhMap()
    {
        if (_fourthMapTexture == null || _player == null || !CanTransitionToSeventhMap())
        {
            return;
        }

        _currentMapTexture = _fourthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        SetBanditsWorld(_seventhMapBandits, _levelBounds, _pathY);

        _cameraPosition = Vector2.Zero;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = true;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
    }

    private void ForceSwapToSeventhMap()
    {
        if (_fourthMapTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _fourthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        SetBanditsWorld(_seventhMapBandits, _levelBounds, _pathY);

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = true;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
    }

    private void SwapToEighthMap()
    {
        if (_eighthMapTexture == null || _player == null || !CanTransitionToEighthMap())
        {
            return;
        }

        _currentMapTexture = _eighthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        _doctorOctopus?.SetWorld(_levelBounds, _pathY);
        ResetEighthMapHostageRescue();

        _cameraPosition = Vector2.Zero;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = true;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
        PlayDoctorOctopusIntroSound();
    }

    private void ForceSwapToEighthMap()
    {
        if (_eighthMapTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _eighthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        _doctorOctopus?.SetWorld(_levelBounds, _pathY);
        ResetEighthMapHostageRescue();

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = true;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = false;
        PlayDoctorOctopusIntroSound();
    }

    private void SwapToNinthMap()
    {
        if (_ninthMapTexture == null || _player == null || !CanTransitionToNinthMap())
        {
            return;
        }

        _currentMapTexture = _ninthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        SetBanditsWorld(_ninthMapBandits, _levelBounds, _pathY);

        _cameraPosition = Vector2.Zero;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = true;
        _isTenthMapLoaded = false;
    }

    private void ForceSwapToNinthMap()
    {
        if (_ninthMapTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _ninthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(_levelBounds);
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        SetBanditsWorld(_ninthMapBandits, _levelBounds, _pathY);

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = true;
        _isTenthMapLoaded = false;
    }

    private void SwapToTenthMap()
    {
        if (_tenthMapTexture == null || _player == null || !CanTransitionToTenthMap())
        {
            return;
        }

        _currentMapTexture = _tenthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(CreateTenthMapPlayerBounds());
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        _lizard?.SetWorld(CreateTenthMapLizardBounds(), _pathY);
        ResetTenthMapHostageRescue();

        _cameraPosition = Vector2.Zero;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = true;
        PlayLizardIntroSound();
    }

    private void ForceSwapToTenthMap()
    {
        if (_tenthMapTexture == null || _player == null)
        {
            return;
        }

        _currentMapTexture = _tenthMapTexture;
        _levelBounds = new Rectangle(0, 0, _currentMapTexture.Width, _currentMapTexture.Height);
        _pathY = ComputePathY(_currentMapTexture);

        _player.SetWorldBounds(CreateTenthMapPlayerBounds());
        _player.SetHorizontalPathY(_pathY);
        _player.SetPosition(new Vector2(SpawnPaddingOnNextMap, _pathY));
        _lizard?.SetWorld(CreateTenthMapLizardBounds(), _pathY);
        ResetTenthMapHostageRescue();

        _cameraPosition = Vector2.Zero;
        _isSecondMapLoaded = true;
        _isThirdMapLoaded = false;
        _isFourthMapLoaded = false;
        _isFifthMapLoaded = false;
        _isSixthMapLoaded = false;
        _isSeventhMapLoaded = false;
        _isEighthMapLoaded = false;
        _isNinthMapLoaded = false;
        _isTenthMapLoaded = true;
        PlayLizardIntroSound();
    }

    private void UpdateCamera()
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        _cameraPosition.X = _player.Position.X - _viewWidth / 2f;
        _cameraPosition.Y = 0f;

        var maxCameraX = Math.Max(0, _currentMapTexture.Width - _viewWidth);
        var maxCameraY = Math.Max(0, _currentMapTexture.Height - _viewHeight);

        _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, maxCameraX);
        _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, maxCameraY);
    }

    private void UpdateActiveBandits(GameTime gameTime)
    {
        if (_player == null)
        {
            return;
        }

        foreach (var bandit in GetCurrentMapBandits())
        {
            if (!bandit.IsAlive)
            {
                continue;
            }

            bandit.Update(gameTime, _player);
            _player.TryHitBandit(bandit);
        }
    }

    private void DrawActiveBandits(SpriteBatch spriteBatch)
    {
        foreach (var bandit in GetCurrentMapBandits())
        {
            if (bandit.IsAlive)
            {
                bandit.Draw(spriteBatch, _cameraPosition, GetBanditVisualYOffset());
            }
        }
    }

    private List<Bandit> GetCurrentMapBandits()
    {
        if (_isSecondMapLoaded && !_isThirdMapLoaded && !_isFourthMapLoaded && !_isFifthMapLoaded && !_isSixthMapLoaded && !_isSeventhMapLoaded && !_isEighthMapLoaded && !_isNinthMapLoaded && !_isTenthMapLoaded)
        {
            return _secondMapBandits;
        }

        if (_isFourthMapLoaded && !_isFifthMapLoaded && !_isSixthMapLoaded && !_isSeventhMapLoaded && !_isEighthMapLoaded && !_isNinthMapLoaded && !_isTenthMapLoaded)
        {
            return _fourthMapBandits;
        }

        if (_isFifthMapLoaded && !_isSixthMapLoaded && !_isSeventhMapLoaded && !_isEighthMapLoaded && !_isNinthMapLoaded && !_isTenthMapLoaded)
        {
            return _fifthMapBandits;
        }

        if (_isSeventhMapLoaded && !_isNinthMapLoaded && !_isTenthMapLoaded)
        {
            return _seventhMapBandits;
        }

        if (_isNinthMapLoaded)
        {
            return _ninthMapBandits;
        }

        return new List<Bandit>();
    }

    private static void SetBanditsWorld(List<Bandit> bandits, Rectangle worldBounds, float pathY)
    {
        foreach (var bandit in bandits)
        {
            bandit.SetWorld(worldBounds, pathY);
        }
    }

    private static bool AreAllBanditsDefeated(List<Bandit> bandits)
    {
        return bandits.Count > 0 && bandits.All(bandit => !bandit.IsAlive);
    }

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        if (_currentMapTexture == null || _player == null)
        {
            return;
        }

        var viewport = graphicsDevice.Viewport;
        _viewWidth = viewport.Width;
        _viewHeight = viewport.Height;
        EnsureSixthMapBoundsMatchViewport();
        var mapCameraX = MathHelper.Clamp(_cameraPosition.X, 0f, Math.Max(0, _currentMapTexture.Width - viewport.Width));
        var mapCameraY = MathHelper.Clamp(_cameraPosition.Y, 0f, Math.Max(0, _currentMapTexture.Height - viewport.Height));
        var sourceRect = new Rectangle(
            (int)mapCameraX,
            (int)mapCameraY,
            Math.Min(viewport.Width, _currentMapTexture.Width),
            Math.Min(viewport.Height, _currentMapTexture.Height));

        if (sourceRect.Right > _currentMapTexture.Width)
        {
            sourceRect.Width = _currentMapTexture.Width - sourceRect.X;
        }

        if (sourceRect.Bottom > _currentMapTexture.Height)
        {
            sourceRect.Height = _currentMapTexture.Height - sourceRect.Y;
        }

        var destinationRect = new Rectangle(0, 0, viewport.Width, viewport.Height);
        spriteBatch.Draw(_currentMapTexture, destinationRect, sourceRect, Color.White);

        DrawActiveBandits(spriteBatch);

        if (_isThirdMapLoaded && _venom != null)
        {
            var venomYOffset = CharacterVisualDropOffset + _player.VisualHeight - _venom.VisualHeight;
            _venom.Draw(spriteBatch, _cameraPosition, venomYOffset);
        }

        if (_isThirdMapLoaded && _hostage != null)
        {
            var hostageYOffset = CharacterVisualDropOffset + _player.VisualHeight - _hostage.VisualHeight;
            _hostage.Draw(spriteBatch, _cameraPosition, hostageYOffset);
        }

        var playerVisualYOffset = GetPlayerVisualYOffset();
        if (_isSixthMapLoaded && _flyingGoblin != null)
        {
            _flyingGoblin.Draw(spriteBatch, _cameraPosition, playerVisualYOffset);
        }

        if (_isEighthMapLoaded && _doctorOctopus != null)
        {
            _doctorOctopus.Draw(spriteBatch, _cameraPosition, GetDoctorOctopusVisualYOffset());
        }

        if (_isTenthMapLoaded && _lizard != null)
        {
            _lizard.Draw(spriteBatch, _cameraPosition, GetPlayerVisualYOffset());
        }

        DrawSixthMapRightSprite(spriteBatch, playerVisualYOffset);
        DrawEighthMapHostage(spriteBatch, playerVisualYOffset);
        DrawTenthMapHostage(spriteBatch, playerVisualYOffset);

        _player.Draw(spriteBatch, _cameraPosition, playerVisualYOffset);
        DrawHud(spriteBatch);
        DrawRescuePrompt(spriteBatch);

        if (_transitionAlpha > 0f)
        {
            var overlayRect = new Rectangle(0, 0, viewport.Width, viewport.Height);
            if (_transitionTexture != null)
            {
                spriteBatch.Draw(_transitionTexture, overlayRect, Color.White * _transitionAlpha);
            }
            else
            {
                spriteBatch.Draw(_currentMapTexture, overlayRect, sourceRect, Color.Black * _transitionAlpha);
            }
        }

        DrawFinalScene(spriteBatch, viewport.Width, viewport.Height);
        DrawGameOverScreen(spriteBatch, viewport.Width, viewport.Height);
    }

    private void DrawFinalScene(SpriteBatch spriteBatch, int viewportWidth, int viewportHeight)
    {
        if (!_isTenthMapHostageRescued || _playerHealth <= 0f)
        {
            return;
        }

        var destination = new Rectangle(0, 0, viewportWidth, viewportHeight);
        if (_finalSceneTexture != null)
        {
            var source = GetCoverSourceRectangle(_finalSceneTexture, viewportWidth, viewportHeight);
            spriteBatch.Draw(_finalSceneTexture, destination, source, Color.White);
        }
        else if (_uiPixel != null)
        {
            spriteBatch.Draw(_uiPixel, destination, Color.Black);
        }

        if (_victoryTextTexture == null)
        {
            return;
        }

        var textWidth = Math.Min(viewportWidth - 80, _victoryTextTexture.Width);
        var textHeight = (int)MathF.Round(_victoryTextTexture.Height * (textWidth / (float)_victoryTextTexture.Width));
        var textDestination = new Rectangle(
            (viewportWidth - textWidth) / 2,
            118,
            textWidth,
            textHeight);
        var shadowDestination = new Rectangle(textDestination.X + 5, textDestination.Y + 5, textDestination.Width, textDestination.Height);
        spriteBatch.Draw(_victoryTextTexture, shadowDestination, Color.Black * 0.75f);
        spriteBatch.Draw(_victoryTextTexture, textDestination, Color.White);
    }

    private void DrawGameOverScreen(SpriteBatch spriteBatch, int viewportWidth, int viewportHeight)
    {
        if (_playerHealth > 0f || _gameOverTexture == null || _player?.IsDeathAnimationFinished == false)
        {
            return;
        }

        var destination = new Rectangle(0, 0, viewportWidth, viewportHeight);
        var source = GetCoverSourceRectangle(_gameOverTexture, viewportWidth, viewportHeight);
        spriteBatch.Draw(_gameOverTexture, destination, source, Color.White);
    }

    private static Rectangle GetCoverSourceRectangle(Texture2D texture, int targetWidth, int targetHeight)
    {
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return new Rectangle(0, 0, texture.Width, texture.Height);
        }

        var sourceAspect = texture.Width / (float)texture.Height;
        var targetAspect = targetWidth / (float)targetHeight;
        if (sourceAspect > targetAspect)
        {
            var sourceWidth = (int)MathF.Round(texture.Height * targetAspect);
            var sourceX = Math.Max(0, (texture.Width - sourceWidth) / 2);
            return new Rectangle(sourceX, 0, Math.Min(sourceWidth, texture.Width), texture.Height);
        }

        var sourceHeight = (int)MathF.Round(texture.Width / targetAspect);
        var sourceY = Math.Max(0, (texture.Height - sourceHeight) / 2);
        return new Rectangle(0, sourceY, texture.Width, Math.Min(sourceHeight, texture.Height));
    }

    private float GetPlayerVisualYOffset()
    {
        var offset = CharacterVisualDropOffset;
        if (_isSixthMapLoaded)
        {
            offset += SixthMapVisualYOffset;
        }
        if (_isEighthMapLoaded)
        {
            offset += EighthMapVisualYOffset;
            offset += EighthMapPlayerExtraVisualYOffset;
        }
        if (_isNinthMapLoaded)
        {
            offset += NinthMapVisualYOffset;
        }
        if (_isTenthMapLoaded)
        {
            offset += TenthMapVisualYOffset;
        }

        return offset;
    }

    private float GetBanditVisualYOffset()
    {
        return CharacterVisualDropOffset + (_isNinthMapLoaded ? NinthMapVisualYOffset : 0f);
    }

    private float GetDoctorOctopusVisualYOffset()
    {
        return CharacterVisualDropOffset + (_isEighthMapLoaded ? EighthMapVisualYOffset + EighthMapDoctorExtraVisualYOffset : 0f);
    }

    private void DrawSixthMapRightSprite(SpriteBatch spriteBatch, float playerVisualYOffset)
    {
        var texture = GetSixthMapRightSpriteTexture();
        if (!_isSixthMapLoaded || texture == null || _player == null)
        {
            return;
        }

        var drawX = GetSixthMapRightSpriteX(texture);
        var visualYOffset = playerVisualYOffset + _player.VisualHeight - texture.Height * SixthMapRightSpriteScale;
        var drawPosition = new Vector2(drawX, _pathY) - _cameraPosition + new Vector2(0f, visualYOffset);
        spriteBatch.Draw(
            texture,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            SixthMapRightSpriteScale,
            SpriteEffects.None,
            0f);
    }

    private void DrawEighthMapHostage(SpriteBatch spriteBatch, float playerVisualYOffset)
    {
        var texture = GetEighthMapHostageTexture();
        if (!_isEighthMapLoaded || texture == null || _player == null)
        {
            return;
        }

        var drawX = GetEighthMapHostageX(texture);
        var visualYOffset = playerVisualYOffset + _player.VisualHeight - texture.Height * EighthMapHostageScale;
        var drawPosition = new Vector2(drawX, _pathY) - _cameraPosition + new Vector2(0f, visualYOffset);
        spriteBatch.Draw(
            texture,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            EighthMapHostageScale,
            SpriteEffects.None,
            0f);
    }

    private void DrawTenthMapHostage(SpriteBatch spriteBatch, float playerVisualYOffset)
    {
        var texture = GetTenthMapHostageTexture();
        if (!_isTenthMapLoaded || texture == null || _player == null)
        {
            return;
        }

        var drawX = GetTenthMapHostageX(texture);
        var visualYOffset = playerVisualYOffset + _player.VisualHeight - texture.Height * TenthMapHostageScale;
        var drawPosition = new Vector2(drawX, _pathY) - _cameraPosition + new Vector2(0f, visualYOffset);
        spriteBatch.Draw(
            texture,
            new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y)),
            null,
            Color.White,
            0f,
            Vector2.Zero,
            TenthMapHostageScale,
            SpriteEffects.None,
            0f);
    }

    public void Unload()
    {
        _newYorkTexture?.Dispose();
        _cityChurchTexture?.Dispose();
        _thirdMapTexture?.Dispose();
        _fourthMapTexture?.Dispose();
        _fifthMapTexture?.Dispose();
        _sixthMapTexture?.Dispose();
        _eighthMapTexture?.Dispose();
        _ninthMapTexture?.Dispose();
        _tenthMapTexture?.Dispose();
        _gameOverTexture?.Dispose();
        _finalSceneTexture?.Dispose();
        _victoryTextTexture?.Dispose();
        _transitionTexture?.Dispose();
        _uiPixel?.Dispose();
        _rescuePromptTexture?.Dispose();
        _rescueKeyTexture?.Dispose();
        _sixthMapRightSprite?.Dispose();
        foreach (var frame in _sixthMapRightRescueFrames)
        {
            frame.Dispose();
        }

        _sixthMapRightRescueFrames.Clear();
        foreach (var frame in _eighthMapHostageFrames)
        {
            frame.Dispose();
        }

        _eighthMapHostageFrames.Clear();
        foreach (var frame in _tenthMapHostageFrames)
        {
            frame.Dispose();
        }

        _tenthMapHostageFrames.Clear();
        foreach (var frame in _tenthMapHostageRescueFrames)
        {
            frame.Dispose();
        }

        _tenthMapHostageRescueFrames.Clear();
        _player?.Unload();
        _venom?.Unload();
        _hostage?.Unload();
        _flyingGoblin?.Unload();
        _doctorOctopus?.Unload();
        _lizard?.Unload();
        _hostageRescueSoundPlayer?.Dispose();
        _hostageRescueSoundPlayer = null;
        _goblinIntroSoundPlayer?.Dispose();
        _goblinIntroSoundPlayer = null;
        _doctorOctopusIntroSoundPlayer?.Dispose();
        _doctorOctopusIntroSoundPlayer = null;
        _lizardIntroSoundPlayer?.Dispose();
        _lizardIntroSoundPlayer = null;
        _playerDeathSoundPlayer?.Dispose();
        _playerDeathSoundPlayer = null;
        _thirdHostageRescueSoundPlayer?.Dispose();
        _thirdHostageRescueSoundPlayer = null;
        _thirdHostageRescueBoostSoundPlayer?.Dispose();
        _thirdHostageRescueBoostSoundPlayer = null;
        _finalHostageRescueSoundPlayer?.Dispose();
        _finalHostageRescueSoundPlayer = null;
        foreach (var bandit in _secondMapBandits)
        {
            bandit.Unload();
        }

        foreach (var bandit in _fourthMapBandits)
        {
            bandit.Unload();
        }

        foreach (var bandit in _fifthMapBandits)
        {
            bandit.Unload();
        }

        foreach (var bandit in _seventhMapBandits)
        {
            bandit.Unload();
        }

        foreach (var bandit in _ninthMapBandits)
        {
            bandit.Unload();
        }

        _secondMapBandits.Clear();
        _fourthMapBandits.Clear();
        _fifthMapBandits.Clear();
        _seventhMapBandits.Clear();
        _ninthMapBandits.Clear();
    }

    private void DrawHud(SpriteBatch spriteBatch)
    {
        if (_player == null || _uiPixel == null)
        {
            return;
        }

        var barX = 18;
        var barY = 16;
        var barWidth = 260;
        var barHeight = 18;
        var gap = 10;

        var health01 = MaxPlayerHealth <= 0f ? 0f : MathHelper.Clamp(_playerHealth / MaxPlayerHealth, 0f, 1f);
        DrawBar(spriteBatch, new Rectangle(barX, barY, barWidth, barHeight), health01, new Color(215, 45, 45));
        DrawBar(spriteBatch, new Rectangle(barX, barY + barHeight + gap, barWidth, barHeight), _player.Stamina01, new Color(55, 105, 230));
        DrawBar(spriteBatch, new Rectangle(barX, barY + (barHeight + gap) * 2, barWidth, barHeight), _player.WebMeter01, Color.White);
    }

    private void DrawBar(SpriteBatch spriteBatch, Rectangle bounds, float fillPercent, Color fillColor)
    {
        if (_uiPixel == null)
        {
            return;
        }

        fillPercent = MathHelper.Clamp(fillPercent, 0f, 1f);
        spriteBatch.Draw(_uiPixel, bounds, Color.Black * 0.55f);
        var fillWidth = (int)MathF.Round((bounds.Width - 4) * fillPercent);
        if (fillWidth > 0)
        {
            spriteBatch.Draw(
                _uiPixel,
                new Rectangle(bounds.X + 2, bounds.Y + 2, fillWidth, Math.Max(1, bounds.Height - 4)),
                fillColor
            );
        }

        var border = Color.White * 0.8f;
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), border);
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), border);
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), border);
        spriteBatch.Draw(_uiPixel, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), border);
    }

    private void UpdateHostageRescue(GameTime gameTime, KeyboardState keyboardState)
    {
        _showRescuePrompt = false;
        if (_isSixthMapLoaded)
        {
            UpdateSixthMapRightSpriteRescue(gameTime, keyboardState);
            return;
        }

        if (_isEighthMapLoaded)
        {
            UpdateEighthMapHostageRescue(gameTime, keyboardState);
            return;
        }

        if (_isTenthMapLoaded)
        {
            UpdateTenthMapHostageRescue(gameTime, keyboardState);
            return;
        }

        if (!_isThirdMapLoaded || _player == null || _hostage == null || _venom == null || !_hostage.IsLoaded)
        {
            return;
        }

        _hostage.Update(gameTime);
        if (_hostage.IsRescued || _venom.IsAlive)
        {
            _rescueHoldTimer = 0f;
            return;
        }

        var playerBounds = _player.GetCollisionBounds();
        var interactionBounds = _hostage.GetCollisionBounds();
        interactionBounds.Inflate(52, 34);
        var nearHostage = interactionBounds.Intersects(playerBounds);
        if (!nearHostage)
        {
            _rescueHoldTimer = 0f;
            return;
        }

        _showRescuePrompt = true;
        _rescuePromptWorldPosition = _hostage.Position;
        if (keyboardState.IsKeyDown(Keys.Tab))
        {
            _rescueHoldTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_rescueHoldTimer >= RescueHoldDuration)
            {
                _rescueHoldTimer = RescueHoldDuration;
                _hostage.CompleteRescue();
                PlayHostageRescueSound();
                _showRescuePrompt = false;
            }
        }
        else
        {
            _rescueHoldTimer = 0f;
        }
    }

    private void UpdateSixthMapRightSpriteRescue(GameTime gameTime, KeyboardState keyboardState)
    {
        if (_player == null || _sixthMapRightSprite == null)
        {
            return;
        }

        if (_flyingGoblin != null && _flyingGoblin.IsAlive)
        {
            _rescueHoldTimer = 0f;
            return;
        }

        if (_isSixthMapRightSpriteRescued)
        {
            UpdateSixthMapRightRescueAnimation(gameTime);
            _rescueHoldTimer = 0f;
            return;
        }

        var playerBounds = _player.GetCollisionBounds();
        var interactionBounds = GetSixthMapRightSpriteBounds();
        interactionBounds.Inflate(72, 44);
        if (!interactionBounds.Intersects(playerBounds))
        {
            _rescueHoldTimer = 0f;
            return;
        }

        _showRescuePrompt = true;
        _rescuePromptWorldPosition = GetSixthMapRightSpriteWorldPosition();
        if (keyboardState.IsKeyDown(Keys.Tab))
        {
            _rescueHoldTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_rescueHoldTimer >= RescueHoldDuration)
            {
                _rescueHoldTimer = RescueHoldDuration;
                _isSixthMapRightSpriteRescued = true;
                _currentSixthMapRightRescueFrame = 0;
                _sixthMapRightRescueAnimationTimer = 0f;
                PlayHostageRescueSound();
                _showRescuePrompt = false;
            }
        }
        else
        {
            _rescueHoldTimer = 0f;
        }
    }

    private void UpdateEighthMapHostageRescue(GameTime gameTime, KeyboardState keyboardState)
    {
        if (_player == null || _eighthMapHostageFrames.Count == 0)
        {
            return;
        }

        if (_doctorOctopus != null && _doctorOctopus.IsAlive)
        {
            _rescueHoldTimer = 0f;
            return;
        }

        if (_isEighthMapHostageRescued)
        {
            UpdateEighthMapHostageRescueAnimation(gameTime);
            _rescueHoldTimer = 0f;
            return;
        }

        var playerBounds = _player.GetCollisionBounds();
        var interactionBounds = GetEighthMapHostageBounds();
        interactionBounds.Inflate(72, 44);
        if (!interactionBounds.Intersects(playerBounds))
        {
            _rescueHoldTimer = 0f;
            return;
        }

        _showRescuePrompt = true;
        _rescuePromptWorldPosition = GetEighthMapHostageWorldPosition();
        if (keyboardState.IsKeyDown(Keys.Tab))
        {
            _rescueHoldTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_rescueHoldTimer >= RescueHoldDuration)
            {
                _rescueHoldTimer = RescueHoldDuration;
                _isEighthMapHostageRescued = true;
                _currentEighthMapHostageRescueFrame = 0;
                _eighthMapHostageRescueAnimationTimer = 0f;
                PlayThirdHostageRescueSound();
                _showRescuePrompt = false;
            }
        }
        else
        {
            _rescueHoldTimer = 0f;
        }
    }

    private void UpdateTenthMapHostageRescue(GameTime gameTime, KeyboardState keyboardState)
    {
        if (_player == null || _tenthMapHostageFrames.Count == 0)
        {
            return;
        }

        if (_lizard != null && _lizard.IsAlive)
        {
            _rescueHoldTimer = 0f;
            return;
        }

        if (_isTenthMapHostageRescued)
        {
            UpdateTenthMapHostageRescueAnimation(gameTime);
            _rescueHoldTimer = 0f;
            return;
        }

        var playerBounds = _player.GetCollisionBounds();
        var interactionBounds = GetTenthMapHostageBounds();
        interactionBounds.Inflate(72, 44);
        if (!interactionBounds.Intersects(playerBounds))
        {
            _rescueHoldTimer = 0f;
            return;
        }

        _showRescuePrompt = true;
        _rescuePromptWorldPosition = GetTenthMapHostageWorldPosition();
        if (keyboardState.IsKeyDown(Keys.Tab))
        {
            _rescueHoldTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_rescueHoldTimer >= RescueHoldDuration)
            {
                _rescueHoldTimer = RescueHoldDuration;
                _isTenthMapHostageRescued = true;
                _currentTenthMapHostageRescueFrame = 0;
                _tenthMapHostageRescueAnimationTimer = 0f;
                PlayFinalHostageRescueSound();
                _showRescuePrompt = false;
            }
        }
        else
        {
            _rescueHoldTimer = 0f;
        }
    }

    private void UpdateSixthMapRightRescueAnimation(GameTime gameTime)
    {
        if (_sixthMapRightRescueFrames.Count == 0)
        {
            return;
        }

        _sixthMapRightRescueAnimationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_sixthMapRightRescueAnimationTimer < SixthMapRightRescueFrameTime)
        {
            return;
        }

        _sixthMapRightRescueAnimationTimer -= SixthMapRightRescueFrameTime;
        if (_currentSixthMapRightRescueFrame < _sixthMapRightRescueFrames.Count - 1)
        {
            _currentSixthMapRightRescueFrame++;
        }
    }

    private void UpdateEighthMapHostageRescueAnimation(GameTime gameTime)
    {
        if (_eighthMapHostageFrames.Count <= 1)
        {
            return;
        }

        _eighthMapHostageRescueAnimationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_eighthMapHostageRescueAnimationTimer < EighthMapHostageRescueFrameTime)
        {
            return;
        }

        _eighthMapHostageRescueAnimationTimer -= EighthMapHostageRescueFrameTime;
        if (_currentEighthMapHostageRescueFrame < _eighthMapHostageFrames.Count - 2)
        {
            _currentEighthMapHostageRescueFrame++;
        }
    }

    private void UpdateTenthMapHostageRescueAnimation(GameTime gameTime)
    {
        if (_tenthMapHostageRescueFrames.Count <= 1)
        {
            return;
        }

        _tenthMapHostageRescueAnimationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_tenthMapHostageRescueAnimationTimer < TenthMapHostageRescueFrameTime)
        {
            return;
        }

        _tenthMapHostageRescueAnimationTimer -= TenthMapHostageRescueFrameTime;
        if (_currentTenthMapHostageRescueFrame < _tenthMapHostageRescueFrames.Count - 1)
        {
            _currentTenthMapHostageRescueFrame++;
        }
    }

    private void ResetSixthMapRightSpriteRescue()
    {
        _isSixthMapRightSpriteRescued = false;
        _currentSixthMapRightRescueFrame = 0;
        _sixthMapRightRescueAnimationTimer = 0f;
        _rescueHoldTimer = 0f;
        _showRescuePrompt = false;
    }

    private void ResetEighthMapHostageRescue()
    {
        _isEighthMapHostageRescued = false;
        _currentEighthMapHostageRescueFrame = 0;
        _eighthMapHostageRescueAnimationTimer = 0f;
        _rescueHoldTimer = 0f;
        _showRescuePrompt = false;
    }

    private void ResetTenthMapHostageRescue()
    {
        _isTenthMapHostageRescued = false;
        _currentTenthMapHostageRescueFrame = 0;
        _tenthMapHostageRescueAnimationTimer = 0f;
        _rescueHoldTimer = 0f;
        _showRescuePrompt = false;
    }

    private Texture2D? GetSixthMapRightSpriteTexture()
    {
        if (_isSixthMapRightSpriteRescued && _sixthMapRightRescueFrames.Count > 0)
        {
            var frameIndex = Math.Clamp(_currentSixthMapRightRescueFrame, 0, _sixthMapRightRescueFrames.Count - 1);
            return _sixthMapRightRescueFrames[frameIndex];
        }

        return _sixthMapRightSprite;
    }

    private Texture2D? GetEighthMapHostageTexture()
    {
        if (_eighthMapHostageFrames.Count == 0)
        {
            return null;
        }

        if (_isEighthMapHostageRescued && _eighthMapHostageFrames.Count > 1)
        {
            var frameIndex = Math.Clamp(_currentEighthMapHostageRescueFrame + 1, 1, _eighthMapHostageFrames.Count - 1);
            return _eighthMapHostageFrames[frameIndex];
        }

        return _eighthMapHostageFrames[0];
    }

    private Texture2D? GetTenthMapHostageTexture()
    {
        if (_tenthMapHostageFrames.Count == 0)
        {
            return null;
        }

        if (_isTenthMapHostageRescued && _tenthMapHostageRescueFrames.Count > 0)
        {
            var frameIndex = Math.Clamp(_currentTenthMapHostageRescueFrame, 0, _tenthMapHostageRescueFrames.Count - 1);
            return _tenthMapHostageRescueFrames[frameIndex];
        }

        return _tenthMapHostageFrames[0];
    }

    private Vector2 GetSixthMapRightSpriteWorldPosition()
    {
        var texture = GetSixthMapRightSpriteTexture() ?? _sixthMapRightSprite;
        if (texture == null || _player == null)
        {
            return new Vector2(_levelBounds.Right - SixthMapRightSpritePaddingRight, _pathY);
        }

        var playerVisualYOffset = CharacterVisualDropOffset + SixthMapVisualYOffset;
        var drawX = GetSixthMapRightSpriteX(texture);
        var visualYOffset = playerVisualYOffset + _player.VisualHeight - texture.Height * SixthMapRightSpriteScale;
        return new Vector2(drawX, _pathY + visualYOffset);
    }

    private Vector2 GetEighthMapHostageWorldPosition()
    {
        var texture = GetEighthMapHostageTexture();
        if (texture == null || _player == null)
        {
            return new Vector2(_levelBounds.Right - EighthMapHostagePaddingRight, _pathY);
        }

        var playerVisualYOffset = GetPlayerVisualYOffset();
        var drawX = GetEighthMapHostageX(texture);
        var visualYOffset = playerVisualYOffset + _player.VisualHeight - texture.Height * EighthMapHostageScale;
        return new Vector2(drawX, _pathY + visualYOffset);
    }

    private Vector2 GetTenthMapHostageWorldPosition()
    {
        var texture = GetTenthMapHostageTexture();
        if (texture == null || _player == null)
        {
            return new Vector2(_levelBounds.Right - TenthMapHostagePaddingRight, _pathY);
        }

        var playerVisualYOffset = GetPlayerVisualYOffset();
        var drawX = GetTenthMapHostageX(texture);
        var visualYOffset = playerVisualYOffset + _player.VisualHeight - texture.Height * TenthMapHostageScale;
        return new Vector2(drawX, _pathY + visualYOffset);
    }

    private float GetSixthMapRightSpriteX(Texture2D texture)
    {
        return _levelBounds.Right - texture.Width * SixthMapRightSpriteScale - SixthMapRightSpritePaddingRight;
    }

    private float GetEighthMapHostageX(Texture2D texture)
    {
        return _levelBounds.Right - texture.Width * EighthMapHostageScale - EighthMapHostagePaddingRight;
    }

    private float GetTenthMapHostageX(Texture2D texture)
    {
        return _levelBounds.Right - texture.Width * TenthMapHostageScale - TenthMapHostagePaddingRight;
    }

    private Rectangle CreateSixthMapBounds(Texture2D mapTexture)
    {
        return new Rectangle(0, 0, Math.Max(mapTexture.Width, _viewWidth), mapTexture.Height);
    }

    private Rectangle CreateTenthMapLizardBounds()
    {
        return new Rectangle(
            _levelBounds.X,
            _levelBounds.Y,
            _levelBounds.Width + TenthMapLizardExtraRightBounds,
            _levelBounds.Height);
    }

    private Rectangle CreateTenthMapPlayerBounds()
    {
        return new Rectangle(
            _levelBounds.X,
            _levelBounds.Y,
            _levelBounds.Width + TenthMapPlayerExtraRightBounds,
            _levelBounds.Height);
    }

    private void EnsureSixthMapBoundsMatchViewport()
    {
        if (!_isSixthMapLoaded || _currentMapTexture == null || _player == null)
        {
            return;
        }

        var desiredBounds = CreateSixthMapBounds(_currentMapTexture);
        if (_levelBounds.Width == desiredBounds.Width && _levelBounds.Height == desiredBounds.Height)
        {
            return;
        }

        _levelBounds = desiredBounds;
        _player.SetWorldBounds(_levelBounds);
    }

    private Rectangle GetSixthMapRightSpriteBounds()
    {
        var texture = GetSixthMapRightSpriteTexture() ?? _sixthMapRightSprite;
        if (texture == null)
        {
            return Rectangle.Empty;
        }

        var worldPosition = GetSixthMapRightSpriteWorldPosition();
        return new Rectangle(
            (int)MathF.Round(worldPosition.X),
            (int)MathF.Round(_pathY - texture.Height * SixthMapRightSpriteScale),
            (int)MathF.Round(texture.Width * SixthMapRightSpriteScale),
            (int)MathF.Round(texture.Height * SixthMapRightSpriteScale + CharacterVisualDropOffset + SixthMapVisualYOffset));
    }

    private Rectangle GetEighthMapHostageBounds()
    {
        var texture = GetEighthMapHostageTexture();
        if (texture == null || _player == null)
        {
            return Rectangle.Empty;
        }

        var worldPosition = GetEighthMapHostageWorldPosition();
        var scaledHeight = texture.Height * EighthMapHostageScale;
        return new Rectangle(
            (int)MathF.Round(worldPosition.X),
            (int)MathF.Round(_pathY - scaledHeight),
            (int)MathF.Round(texture.Width * EighthMapHostageScale),
            (int)MathF.Round(scaledHeight + GetPlayerVisualYOffset() + _player.VisualHeight));
    }

    private Rectangle GetTenthMapHostageBounds()
    {
        var texture = GetTenthMapHostageTexture();
        if (texture == null || _player == null)
        {
            return Rectangle.Empty;
        }

        var worldPosition = GetTenthMapHostageWorldPosition();
        var scaledHeight = texture.Height * TenthMapHostageScale;
        return new Rectangle(
            (int)MathF.Round(worldPosition.X),
            (int)MathF.Round(_pathY - scaledHeight),
            (int)MathF.Round(texture.Width * TenthMapHostageScale),
            (int)MathF.Round(scaledHeight + GetPlayerVisualYOffset() + _player.VisualHeight));
    }

    private void DrawRescuePrompt(SpriteBatch spriteBatch)
    {
        if (!_showRescuePrompt || _player == null || _uiPixel == null)
        {
            return;
        }

        var promptScreen = _rescuePromptWorldPosition - _cameraPosition + new Vector2(0f, -RescueVisualOffset);
        if (!_isSixthMapLoaded && !_isEighthMapLoaded && !_isTenthMapLoaded)
        {
            promptScreen += new Vector2(0f, CharacterVisualDropOffset);
        }

        var panelX = (int)MathF.Round(promptScreen.X - 78f);
        var panelY = (int)MathF.Round(promptScreen.Y - 74f);
        var panel = new Rectangle(panelX, panelY, 220, 94);

        spriteBatch.Draw(_uiPixel, panel, new Color(0, 0, 0, 165));
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), Color.White * 0.9f);
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), Color.White * 0.9f);
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Y, 2, panel.Height), Color.White * 0.9f);
        spriteBatch.Draw(_uiPixel, new Rectangle(panel.Right - 2, panel.Y, 2, panel.Height), Color.White * 0.9f);

        if (_rescuePromptTexture != null)
        {
            spriteBatch.Draw(_rescuePromptTexture, new Vector2(panel.X + 20, panel.Y + 10), Color.White);
        }

        if (_rescueKeyTexture != null)
        {
            spriteBatch.Draw(_rescueKeyTexture, new Vector2(panel.X + 18, panel.Y + 42), Color.White);
        }

        var progress = MathHelper.Clamp(_rescueHoldTimer / RescueHoldDuration, 0f, 1f);
        var barBack = new Rectangle(panel.X + 102, panel.Y + 54, 102, 18);
        var barFill = new Rectangle(barBack.X + 2, barBack.Y + 2, (int)MathF.Round((barBack.Width - 4) * progress), barBack.Height - 4);
        spriteBatch.Draw(_uiPixel, barBack, new Color(35, 35, 35, 220));
        if (barFill.Width > 0)
        {
            spriteBatch.Draw(_uiPixel, barFill, new Color(105, 225, 120));
        }
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.X, barBack.Y, barBack.Width, 1), Color.White * 0.85f);
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.X, barBack.Bottom - 1, barBack.Width, 1), Color.White * 0.85f);
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.X, barBack.Y, 1, barBack.Height), Color.White * 0.85f);
        spriteBatch.Draw(_uiPixel, new Rectangle(barBack.Right - 1, barBack.Y, 1, barBack.Height), Color.White * 0.85f);
    }

    private static Texture2D? CreateTextTexture(GraphicsDevice graphicsDevice, string text, int width, int height, float fontSize)
    {
        using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(System.Drawing.Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new System.Drawing.Font("Segoe UI", fontSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
        var rect = new System.Drawing.RectangleF(0, 0, width, height);
        using var format = new System.Drawing.StringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center
        };
        g.DrawString(text, font, brush, rect, format);

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return Texture2D.FromStream(graphicsDevice, stream);
    }
}

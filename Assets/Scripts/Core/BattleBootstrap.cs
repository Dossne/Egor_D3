using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleBootstrap : MonoBehaviour
{
    private const int MaxHeroLevel = 6;
    private static readonly Color HeroFieldFallbackColor = new Color(0.27f, 0.48f, 0.72f);
    private static readonly Color HeroPlatformFallbackColor = new Color(0.93f, 0.87f, 0.62f, 0.95f);
    private static readonly Color WallFallbackColor = new Color(0.55f, 0.5f, 0.43f);
    private static readonly Color EnemyFieldFallbackColor = new Color(0.62f, 0.33f, 0.33f);
    private static readonly Color CardTitleColor = new Color(0.12f, 0.13f, 0.16f);
    private static readonly Color CardDescriptionColor = new Color(0.25f, 0.27f, 0.32f);
    private static readonly Color EnhancedCardTextColor = new Color(0.86f, 0.32f, 0.84f);
    private static readonly Color CardTextShadowColor = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color CardIconFallbackColor = new Color(0.35f, 0.38f, 0.44f);
    private static readonly Color ProgressBarFrameColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ProgressBarFillColor = new Color(0.2f, 0.9f, 0.2f);

    private GameConfigSO gameConfig;
    private WaveConfigSO waveConfig;
    private SlotMachineConfigSO slotConfig;
    private DisasterConfigSO disasterConfig;
    private HeroDataSO heroCatalog;
    private EnemyDataSO enemyCatalog;
    private readonly Dictionary<string, HeroDefinition> heroLookup = new Dictionary<string, HeroDefinition>();
    private readonly Dictionary<string, EnemyDefinition> enemyLookup = new Dictionary<string, EnemyDefinition>();

    private readonly List<HeroUnit> heroes = new List<HeroUnit>();
    private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();
    private readonly List<RectTransform> heroSlotRects = new List<RectTransform>();
    private readonly List<ProjectileView> activeProjectiles = new List<ProjectileView>();
    private readonly List<FloatingDamageView> activeFloatingDamage = new List<FloatingDamageView>();

    private RectTransform enemyArea;
    private RectTransform heroArea;
    private RectTransform heroGridLayer;
    private RectTransform heroUnitsLayer;
    private RectTransform battleEffectsLayer;
    private RectTransform rewardEffectsLayer;
    private RectTransform wallRect;
    private RectTransform coinCounterAnchor;
    private Image wallImage;
    private Image heroAreaImage;
    private Image enemyAreaImage;
    private Image enemyTopFillImage;
    private Image wallHpTopFillImage;
    private Image wallHpFrame;
    private Image waveProgressFrame;
    private Sprite heroPlatformFallbackCircleSprite;

    private Text coinsText;
    private Text heroCountText;
    private Text pullButtonText;
    private Text wallHpText;
    private RectTransform wallHpFillRect;
    private Text waveText;
    private RectTransform waveProgressFillRect;
    private Text feedbackText;
    private RectTransform waveProgressMarkerLayer;

    private Image[] slotImages = new Image[3];
    private Button pullButton;
    private GameObject resultOverlay;
    private Text resultText;
    private GameObject cardOverlay;
    private GameObject disasterOverlay;
    private RectTransform disasterSlotsRoot;
    private Image[] disasterSlotImages = new Image[3];
    private Image disasterPayoffIcon;
    private Image disasterPayoffFlash;

    private int coins;
    private int pullCount;
    private float wallHp;
    private bool isSpinning;
    private bool waitingCardChoice;
    private bool gameEnded;

    private int currentWaveIndex;
    private int completedWaveCount;
    private int totalLevelEnemyCount;
    private int killedEnemyCount;
    private float nextWaveStartTime;
    private bool allWavesStarted;
    private readonly List<int> waveAliveCounts = new List<int>();
    private readonly List<int> waveRemainingSpawnCounts = new List<int>();
    private readonly List<bool> waveMarkedCompleted = new List<bool>();

    private float heroDamageMultiplier = 1f;
    private float heroAttackSpeedMultiplier = 1f;
    private float disasterHeroDamageMultiplier = 1f;
    private float disasterHeroAttackSpeedMultiplier = 1f;
    private float disasterEnemyMoveSpeedMultiplier = 1f;
    private float disasterEnemyAttackSpeedMultiplier = 1f;
    private float disasterBuffRemainingSec;
    private DisasterBuffSide activeDisasterBuffSide = DisasterBuffSide.None;

    private bool isGameplayPaused;
    private bool disasterTransitionInProgress;
    private int pendingDisasterWaveSlot = -1;
    private readonly HashSet<int> triggeredDisasterWaveSlots = new HashSet<int>();
    private readonly Dictionary<int, RectTransform> disasterMarkersByWaveSlot = new Dictionary<int, RectTransform>();

    private int HeroRows => Mathf.Max(1, gameConfig != null ? gameConfig.heroRows : 7);
    private int HeroCols => Mathf.Max(1, gameConfig != null ? gameConfig.heroCols : 3);
    private int MaxHeroes => HeroRows * HeroCols;
    private float HeroGridSpacing => gameConfig != null ? gameConfig.heroGridSpacing : 6f;
    private float HeroGridPadding => gameConfig != null ? gameConfig.heroGridPadding : 8f;
    private float HeroRestingOffsetY => Mathf.Max(4f, (gameConfig != null ? gameConfig.heroVisualSize : 36f) * 0.1f);
    private float HeroHeldOffsetY => HeroRestingOffsetY + Mathf.Max(6f, (gameConfig != null ? gameConfig.heroVisualSize : 36f) * 0.12f);
    private float HeroStarSize => gameConfig != null ? gameConfig.heroStarSize : 20f;
    private float HeroStarOffsetY => gameConfig != null ? gameConfig.heroStarOffsetY : 6f;
    private float HeroStarSpacing => gameConfig != null ? gameConfig.heroStarSpacing : 4f;
    private float HeroPlatformSize => Mathf.Max(24f, gameConfig != null ? gameConfig.heroPlatformSize : 48f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        GameObject go = new GameObject("BattleBootstrap");
        go.AddComponent<BattleBootstrap>();
    }

    private void Start()
    {
        LoadConfigs();
        BuildUi();
        SetupGame();
    }

    private void LoadConfigs()
    {
        gameConfig = Resources.Load<GameConfigSO>("Configs/GameConfig");
        waveConfig = Resources.Load<WaveConfigSO>("Configs/WaveConfig");
        slotConfig = Resources.Load<SlotMachineConfigSO>("Configs/SlotMachineConfig");
        disasterConfig = Resources.Load<DisasterConfigSO>("Configs/DisasterConfig");
        heroCatalog = Resources.Load<HeroDataSO>("Configs/HeroData");
        enemyCatalog = Resources.Load<EnemyDataSO>("Configs/EnemyData");

        if (gameConfig == null) gameConfig = ScriptableObject.CreateInstance<GameConfigSO>();
        if (waveConfig == null) waveConfig = ScriptableObject.CreateInstance<WaveConfigSO>();
        if (slotConfig == null) slotConfig = ScriptableObject.CreateInstance<SlotMachineConfigSO>();
        if (disasterConfig == null) disasterConfig = ScriptableObject.CreateInstance<DisasterConfigSO>();
        if (heroCatalog == null) heroCatalog = ScriptableObject.CreateInstance<HeroDataSO>();
        if (enemyCatalog == null) enemyCatalog = ScriptableObject.CreateInstance<EnemyDataSO>();

        if (gameConfig.heroCatalog != null)
        {
            heroCatalog = gameConfig.heroCatalog;
        }

        if (gameConfig.enemyCatalog != null)
        {
            enemyCatalog = gameConfig.enemyCatalog;
        }

        BuildUnitLookups();
    }

    private void SetupGame()
    {
        coins = gameConfig.startingCoins;
        wallHp = gameConfig.wallMaxHp;
        currentWaveIndex = 0;
        completedWaveCount = 0;
        nextWaveStartTime = Time.time + 0.2f;
        allWavesStarted = false;
        pullCount = 0;
        isGameplayPaused = false;
        disasterTransitionInProgress = false;
        pendingDisasterWaveSlot = -1;
        disasterBuffRemainingSec = 0f;
        activeDisasterBuffSide = DisasterBuffSide.None;
        disasterHeroDamageMultiplier = 1f;
        disasterHeroAttackSpeedMultiplier = 1f;
        disasterEnemyMoveSpeedMultiplier = 1f;
        disasterEnemyAttackSpeedMultiplier = 1f;
        triggeredDisasterWaveSlots.Clear();
        InitializeWaveProgressTracking();
        InitializeEnemyKillProgressTracking();
        BuildDisasterWaveMarkers();
        killedEnemyCount = 0;
        SetBarFillRatio(wallHpFillRect, gameConfig.wallMaxHp <= 0f ? 0f : wallHp / gameConfig.wallMaxHp);
        SetBarFillRatio(waveProgressFillRect, 0f);
        SetDefaultSlotSymbols();
        TryPlaceHero(1, GetDefaultHeroId());
        RefreshUi();
        RefreshWaveUi();
    }

    private void BuildUnitLookups()
    {
        heroLookup.Clear();
        enemyLookup.Clear();

        if (heroCatalog != null && heroCatalog.heroes != null)
        {
            for (int i = 0; i < heroCatalog.heroes.Count; i++)
            {
                RegisterHero(heroCatalog.heroes[i]);
            }
        }

        if (enemyCatalog != null && enemyCatalog.enemies != null)
        {
            for (int i = 0; i < enemyCatalog.enemies.Count; i++)
            {
                RegisterEnemy(enemyCatalog.enemies[i]);
            }
        }

    }

    private void RegisterHero(HeroDefinition hero)
    {
        if (hero == null || string.IsNullOrEmpty(hero.id))
        {
            return;
        }

        heroLookup[hero.id] = hero;
    }

    private void RegisterEnemy(EnemyDefinition enemy)
    {
        if (enemy == null || string.IsNullOrEmpty(enemy.id))
        {
            return;
        }

        enemyLookup[enemy.id] = enemy;
    }

    private void Update()
    {
        if (gameEnded)
        {
            return;
        }

        UpdateDisasterBuffTimer();
        UpdateBuffVfxPulse();

        if (isGameplayPaused)
        {
            RefreshWaveUi();
            return;
        }

        HandleWaveSpawning();
        UpdateHeroes();
        UpdateProjectiles();
        UpdateFloatingDamage();
        UpdateEnemies();
        UpdateWinLose();
        RefreshWaveUi();
    }

    private void BuildUi()
    {
        var canvasGo = CreateUiObject("Canvas", null);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasGo.GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var bg = CreatePanel("Background", canvasGo.transform, new Color(0.36f, 0.54f, 0.34f), new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);

        RectTransform battleZone = CreatePanel("BattleZone", bg.transform, new Color(0.22f, 0.35f, 0.24f), new Vector2(0, 0.38f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        RectTransform wallHpZone = CreatePanel("WallHpZone", bg.transform, new Color(0.12f, 0.12f, 0.12f), new Vector2(0, 0.29f), new Vector2(1, 0.38f), Vector2.zero, Vector2.zero);
        RectTransform bottomZone = CreatePanel("BottomUi", bg.transform, new Color(0.16f, 0.2f, 0.2f), new Vector2(0, 0), new Vector2(1, 0.29f), Vector2.zero, new Vector2(0f, 1f));

        RectTransform topHudBand = CreatePanel("TopHudBand", battleZone, new Color(0f, 0f, 0f, 0.35f), new Vector2(0.03f, 0.9f), new Vector2(0.97f, 0.995f), Vector2.zero, Vector2.zero);
        RectTransform battleContent = CreatePanel("BattleContent", battleZone, Color.clear, new Vector2(0f, 0f), new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);

        heroArea = CreatePanel("HeroField", battleContent, HeroFieldFallbackColor, new Vector2(0f, 0f), new Vector2(0.24f, 1f), Vector2.zero, new Vector2(0f, 1f));
        wallRect = CreatePanel("Wall", battleContent, WallFallbackColor, new Vector2(0.24f, 0f), new Vector2(0.32f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 1f));
        enemyArea = CreatePanel("EnemyField", battleContent, EnemyFieldFallbackColor, new Vector2(0.32f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 0f), Vector2.zero);
        heroAreaImage = heroArea.GetComponent<Image>();
        wallImage = wallRect.GetComponent<Image>();
        enemyAreaImage = enemyArea.GetComponent<Image>();
        battleEffectsLayer = CreatePanel("BattleEffectsLayer", battleContent, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image battleEffectsImage = battleEffectsLayer.GetComponent<Image>();
        if (battleEffectsImage != null)
        {
            battleEffectsImage.raycastTarget = false;
        }
        BuildWallVisual();

        var topHud = CreatePanel("TopHud", topHudBand, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        waveProgressFillRect = CreateSimpleFilledBar(topHud, "WaveProgressBar", new Vector2(0.12f, 0.22f), new Vector2(0.98f, 0.78f), out waveProgressFrame);
        waveProgressMarkerLayer = CreatePanel("WaveProgressMarkers", waveProgressFrame.transform, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image markerLayerImage = waveProgressMarkerLayer.GetComponent<Image>();
        if (markerLayerImage != null)
        {
            markerLayerImage.raycastTarget = false;
        }
        waveText = CreateText("WaveText", topHud, "Wave 0 / 0", 30, TextAnchor.MiddleCenter);
        waveText.rectTransform.anchorMin = new Vector2(0.12f, 0f);
        waveText.rectTransform.anchorMax = new Vector2(0.98f, 1f);

        wallHpFillRect = CreateSimpleFilledBar(wallHpZone, "WallHpBar", new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.8f), out wallHpFrame);
        wallHpText = CreateText("WallHpText", wallHpZone, "0 / 0", 34, TextAnchor.MiddleCenter);

        coinsText = CreateText("CoinsText", bottomZone, "Coins: 0", 34, TextAnchor.MiddleLeft);
        coinsText.rectTransform.anchorMin = new Vector2(0.04f, 0.78f);
        coinsText.rectTransform.anchorMax = new Vector2(0.45f, 0.96f);
        coinCounterAnchor = CreatePanel("CoinCounterAnchor", bottomZone, Color.clear, new Vector2(0.165f, 0.87f), new Vector2(0.165f, 0.87f), new Vector2(-12f, -12f), new Vector2(12f, 12f));

        heroCountText = CreateText("HeroCountText", bottomZone, "Heroes: 0 / " + MaxHeroes, 34, TextAnchor.MiddleRight);
        heroCountText.rectTransform.anchorMin = new Vector2(0.5f, 0.78f);
        heroCountText.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);

        for (int i = 0; i < 3; i++)
        {
            RectTransform slot = CreatePanel("Slot" + i, bottomZone, new Color(0.88f, 0.88f, 0.88f), new Vector2(0.18f + (0.22f * i), 0.42f), new Vector2(0.36f + (0.22f * i), 0.72f), Vector2.zero, Vector2.zero);
            slotImages[i] = slot.GetComponent<Image>();
            CreateText("Label", slot, "?", 24, TextAnchor.MiddleCenter);
        }

        RectTransform pullRect = CreatePanel("PullButton", bottomZone, new Color(0.24f, 0.5f, 0.18f), new Vector2(0.24f, 0.08f), new Vector2(0.76f, 0.33f), Vector2.zero, Vector2.zero);
        pullButton = pullRect.gameObject.AddComponent<Button>();
        pullButton.onClick.AddListener(OnPullPressed);
        pullButtonText = CreateText("PullText", pullRect, "Pull", 38, TextAnchor.MiddleCenter);

        feedbackText = CreateText("Feedback", bottomZone, string.Empty, 26, TextAnchor.MiddleCenter);
        feedbackText.rectTransform.anchorMin = new Vector2(0.03f, 0.0f);
        feedbackText.rectTransform.anchorMax = new Vector2(0.97f, 0.08f);

        BuildHeroLayers();
        rewardEffectsLayer = CreatePanel("RewardEffectsLayer", bg.transform, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rewardEffectsLayer.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        battleEffectsLayer.SetAsLastSibling();
        rewardEffectsLayer.SetAsLastSibling();
        BuildHeroSlots();
        ApplyBattleAreaSprites();
        SetBarFillRatio(waveProgressFillRect, 0f);
        BuildResultOverlay(canvasGo.transform);
        BuildCardOverlay(canvasGo.transform);
        BuildDisasterOverlay(canvasGo.transform);
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(null, false);
    }

    private void BuildHeroLayers()
    {
        heroGridLayer = CreatePanel("HeroGridLayer", heroArea, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image heroGridLayerImage = heroGridLayer.GetComponent<Image>();
        if (heroGridLayerImage != null)
        {
            heroGridLayerImage.raycastTarget = false;
        }

        heroUnitsLayer = CreatePanel("HeroUnitsLayer", heroArea, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image heroUnitsLayerImage = heroUnitsLayer.GetComponent<Image>();
        if (heroUnitsLayerImage != null)
        {
            heroUnitsLayerImage.raycastTarget = false;
        }
        heroUnitsLayer.SetAsLastSibling();
    }

    private void BuildHeroSlots()
    {
        heroSlotRects.Clear();
        GridLayoutGroup grid = heroGridLayer.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = HeroCols;
        grid.spacing = new Vector2(HeroGridSpacing, HeroGridSpacing);
        grid.padding = new RectOffset((int)HeroGridPadding, (int)HeroGridPadding, (int)HeroGridPadding, (int)HeroGridPadding);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(heroArea);
        LayoutRebuilder.ForceRebuildLayoutImmediate(heroGridLayer);
        float width = heroGridLayer.rect.width;
        float height = heroGridLayer.rect.height;
        float cellWidth = (width - (HeroGridPadding * 2f) - (grid.spacing.x * (HeroCols - 1))) / HeroCols;
        float cellHeight = (height - (HeroGridPadding * 2f) - (grid.spacing.y * (HeroRows - 1))) / HeroRows;
        grid.cellSize = new Vector2(Mathf.Max(1f, cellWidth), Mathf.Max(1f, cellHeight));

        for (int r = 0; r < HeroRows; r++)
        {
            for (int c = 0; c < HeroCols; c++)
            {
                RectTransform slot = CreatePanel("HeroSlot_" + r + "_" + c, heroGridLayer, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image slotImage = slot.GetComponent<Image>();
                slotImage.sprite = null;
                slotImage.color = Color.clear;
                slotImage.raycastTarget = false;
                BuildHeroSlotPlatform(slot);
                heroSlotRects.Add(slot);
                slot.gameObject.AddComponent<LayoutElement>();
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(heroGridLayer);
    }

    private void BuildHeroSlotPlatform(RectTransform slot)
    {
        float heroVisualSize = gameConfig != null ? gameConfig.heroVisualSize : 36f;
        float platformDiameter = HeroPlatformSize;
        float platformYOffset = Mathf.Max(-heroVisualSize * 0.3f, -42f);

        RectTransform platformRect = CreatePanel("Platform", slot, HeroPlatformFallbackColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        platformRect.pivot = new Vector2(0.5f, 0.5f);
        platformRect.sizeDelta = new Vector2(platformDiameter, platformDiameter);
        platformRect.anchoredPosition = new Vector2(0f, platformYOffset);
        platformRect.SetAsFirstSibling();

        Image platformImage = platformRect.GetComponent<Image>();
        Sprite platformSprite = GetHeroPlatformSprite();
        if (platformSprite != null)
        {
            platformImage.sprite = platformSprite;
            platformImage.type = Image.Type.Simple;
            platformImage.preserveAspect = true;
            platformImage.color = Color.white;
        }
        else
        {
            platformImage.sprite = GetHeroPlatformFallbackCircleSprite();
            platformImage.type = Image.Type.Simple;
            platformImage.color = HeroPlatformFallbackColor;
            platformImage.preserveAspect = true;
        }

        platformImage.raycastTarget = false;
    }

    private Sprite GetHeroPlatformFallbackCircleSprite()
    {
        if (heroPlatformFallbackCircleSprite != null)
        {
            return heroPlatformFallbackCircleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "HeroPlatformFallbackCircle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float radius = (size * 0.5f) - 1f;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        heroPlatformFallbackCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return heroPlatformFallbackCircleSprite;
    }

    private Sprite GetHeroPlatformSprite()
    {
        if (gameConfig == null)
        {
            return null;
        }

        return gameConfig.heroPlatformSprite != null ? gameConfig.heroPlatformSprite : gameConfig.heroCellSprite;
    }

    private void BuildWallVisual()
    {
        if (wallRect == null)
        {
            return;
        }

        Outline wallOutline = wallRect.gameObject.AddComponent<Outline>();
        wallOutline.effectColor = new Color(0.06f, 0.06f, 0.06f, 0.9f);
        wallOutline.effectDistance = new Vector2(3f, 3f);

        CreatePanel("WallLeftBorder", wallRect, new Color(0.12f, 0.1f, 0.09f, 0.95f), new Vector2(0f, 0f), new Vector2(0.16f, 1f), Vector2.zero, Vector2.zero);
        CreatePanel("WallRightBorder", wallRect, new Color(0.12f, 0.1f, 0.09f, 0.95f), new Vector2(0.84f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform stripe = CreatePanel("WallCenterStripe", wallRect, new Color(0.96f, 0.91f, 0.75f, 0.72f), new Vector2(0.36f, 0f), new Vector2(0.64f, 1f), Vector2.zero, Vector2.zero);
        stripe.SetAsFirstSibling();

    }

    private void ApplyBattleAreaSprites()
    {
        if (heroAreaImage != null)
        {
            if (gameConfig.hero_back != null)
            {
                heroAreaImage.sprite = gameConfig.hero_back;
                heroAreaImage.type = Image.Type.Tiled;
                heroAreaImage.color = Color.white;
            }
            else if (gameConfig.heroFieldSprite != null)
            {
                heroAreaImage.sprite = gameConfig.heroFieldSprite;
                heroAreaImage.type = Image.Type.Sliced;
                heroAreaImage.color = Color.white;
            }
            else
            {
                heroAreaImage.sprite = null;
                heroAreaImage.type = Image.Type.Simple;
                heroAreaImage.color = HeroFieldFallbackColor;
            }
        }

        if (wallImage != null)
        {
            if (gameConfig.wallSprite != null)
            {
                wallImage.sprite = gameConfig.wallSprite;
                wallImage.type = Image.Type.Sliced;
                wallImage.color = Color.white;
            }
            else
            {
                wallImage.sprite = null;
                wallImage.color = WallFallbackColor;
            }
        }

        if (enemyAreaImage != null)
        {
            if (gameConfig.enemy_back != null)
            {
                enemyAreaImage.sprite = gameConfig.enemy_back;
                enemyAreaImage.type = Image.Type.Tiled;
                enemyAreaImage.color = Color.white;
            }
            else if (gameConfig.enemyFieldSprite != null)
            {
                enemyAreaImage.sprite = gameConfig.enemyFieldSprite;
                enemyAreaImage.type = Image.Type.Sliced;
                enemyAreaImage.color = Color.white;
            }
            else
            {
                enemyAreaImage.sprite = null;
                enemyAreaImage.color = EnemyFieldFallbackColor;
            }
        }

        ApplyEnemyFieldStyle(enemyTopFillImage);
        ApplyEnemyFieldStyle(wallHpTopFillImage);
    }

    private void ApplyEnemyFieldStyle(Image target)
    {
        if (target == null)
        {
            return;
        }

        if (gameConfig.enemyFieldSprite != null)
        {
            target.sprite = gameConfig.enemyFieldSprite;
            target.type = Image.Type.Sliced;
            target.color = Color.white;
            return;
        }

        target.sprite = null;
        target.type = Image.Type.Simple;
        target.color = EnemyFieldFallbackColor;
    }

    private void HandleWaveSpawning()
    {
        if (allWavesStarted || disasterTransitionInProgress)
        {
            return;
        }

        if (TryStartPendingDisaster())
        {
            return;
        }

        if (Time.time < nextWaveStartTime)
        {
            return;
        }

        if (currentWaveIndex >= waveConfig.waves.Count)
        {
            allWavesStarted = true;
            return;
        }

        int waveSlot = currentWaveIndex;
        if (IsDisasterWaveSlot(waveSlot) && !triggeredDisasterWaveSlots.Contains(waveSlot))
        {
            if (waitingCardChoice)
            {
                pendingDisasterWaveSlot = waveSlot;
                return;
            }

            StartCoroutine(RunDisasterThenStartWave(waveSlot));
            return;
        }

        StartWaveBySlot(waveSlot);
    }

    private void StartWaveBySlot(int waveSlot)
    {
        if (waveSlot < 0 || waveSlot >= waveConfig.waves.Count)
        {
            return;
        }

        WaveEntry wave = waveConfig.waves[waveSlot];
        PrepareWaveTracking(waveSlot, wave);
        StartCoroutine(SpawnWave(wave, waveSlot));
        currentWaveIndex++;
        nextWaveStartTime = Time.time + waveConfig.waveIntervalSec;
        if (currentWaveIndex >= waveConfig.waves.Count)
        {
            allWavesStarted = true;
        }

        RefreshWaveUi();
    }

    private bool IsDisasterWaveSlot(int waveSlot)
    {
        if (disasterConfig == null || disasterConfig.disasterWaveIndices == null)
        {
            return false;
        }

        int oneBasedWaveNumber = waveSlot + 1;
        return disasterConfig.disasterWaveIndices.Contains(oneBasedWaveNumber);
    }

    private IEnumerator RunDisasterThenStartWave(int waveSlot)
    {
        disasterTransitionInProgress = true;
        isGameplayPaused = true;
        pendingDisasterWaveSlot = -1;
        if (disasterOverlay != null)
        {
            disasterOverlay.SetActive(true);
        }

        yield return StartCoroutine(RunDisasterSlotRoutine());
        RemoveDisasterMarkerForWave(waveSlot);
        triggeredDisasterWaveSlots.Add(waveSlot);

        if (disasterOverlay != null)
        {
            disasterOverlay.SetActive(false);
        }

        isGameplayPaused = false;
        disasterTransitionInProgress = false;
        StartWaveBySlot(waveSlot);
    }

    private bool TryStartPendingDisaster()
    {
        if (pendingDisasterWaveSlot < 0 || waitingCardChoice)
        {
            return false;
        }

        if (triggeredDisasterWaveSlots.Contains(pendingDisasterWaveSlot))
        {
            pendingDisasterWaveSlot = -1;
            return false;
        }

        StartCoroutine(RunDisasterThenStartWave(pendingDisasterWaveSlot));
        return true;
    }

    private void InitializeWaveProgressTracking()
    {
        waveAliveCounts.Clear();
        waveRemainingSpawnCounts.Clear();
        waveMarkedCompleted.Clear();

        int totalWaves = waveConfig != null ? waveConfig.waves.Count : 0;
        for (int i = 0; i < totalWaves; i++)
        {
            waveAliveCounts.Add(0);
            waveRemainingSpawnCounts.Add(0);
            waveMarkedCompleted.Add(false);
        }
    }

    private void InitializeEnemyKillProgressTracking()
    {
        totalLevelEnemyCount = 0;
        killedEnemyCount = 0;

        if (waveConfig == null || waveConfig.waves == null)
        {
            return;
        }

        for (int i = 0; i < waveConfig.waves.Count; i++)
        {
            totalLevelEnemyCount += Mathf.Max(1, waveConfig.waves[i].enemyCount);
        }
    }

    private void PrepareWaveTracking(int waveSlot, WaveEntry wave)
    {
        if (!IsWaveSlotValid(waveSlot))
        {
            return;
        }

        waveAliveCounts[waveSlot] = 0;
        waveRemainingSpawnCounts[waveSlot] = Mathf.Max(1, wave.enemyCount);
        waveMarkedCompleted[waveSlot] = false;
    }

    private void TryMarkWaveCompleted(int waveSlot)
    {
        if (!IsWaveSlotValid(waveSlot) || waveMarkedCompleted[waveSlot])
        {
            return;
        }

        if (waveAliveCounts[waveSlot] > 0 || waveRemainingSpawnCounts[waveSlot] > 0)
        {
            return;
        }

        waveMarkedCompleted[waveSlot] = true;
        completedWaveCount = Mathf.Min(completedWaveCount + 1, waveMarkedCompleted.Count);
    }

    private bool IsWaveSlotValid(int waveSlot)
    {
        return waveSlot >= 0 && waveSlot < waveAliveCounts.Count;
    }

    private IEnumerator SpawnWave(WaveEntry wave, int waveSlot)
    {
        int count = Mathf.Max(1, wave.enemyCount);
        float verticalMargin = gameConfig.enemySpawnVerticalMargin;
        float minY = verticalMargin;
        float maxY = Mathf.Max(minY + 1f, enemyArea.rect.height - verticalMargin);
        List<float> spawnLanes = BuildSpawnLanes(minY, maxY);
        List<float> shuffledLanes = new List<float>(spawnLanes);
        ShuffleSpawnLanes(shuffledLanes);
        int laneIndex = 0;

        for (int i = 0; i < count; i++)
        {
            if (laneIndex >= shuffledLanes.Count)
            {
                ShuffleSpawnLanes(shuffledLanes);
                laneIndex = 0;
            }

            float y = shuffledLanes[laneIndex];
            laneIndex++;
            SpawnEnemy(wave.enemyId, y, waveSlot);
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator RunDisasterSlotRoutine()
    {
        float spinDuration = Mathf.Max(0.1f, disasterConfig.spinDurationMs / 1000f);
        float spinTick = Mathf.Max(0.05f, disasterConfig.spinStepSec);
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            for (int i = 0; i < disasterSlotImages.Length; i++)
            {
                SetDisasterSlotVisual(disasterSlotImages[i], Random.value < 0.5f);
            }

            elapsed += spinTick;
            yield return new WaitForSeconds(spinTick);
        }

        DisasterOutcomeType outcome = PickDisasterOutcome();
        bool[] slotCloverResults = BuildDisasterSlotResults(outcome);
        int cloverCount = 0;
        for (int i = 0; i < slotCloverResults.Length; i++)
        {
            if (slotCloverResults[i])
            {
                cloverCount++;
            }
            SetDisasterSlotVisual(disasterSlotImages[i], slotCloverResults[i]);
        }

        int skullCount = slotCloverResults.Length - cloverCount;
        yield return new WaitForSeconds(Mathf.Max(0f, disasterConfig.postSpinDelaySec));
        yield return StartCoroutine(PlayDisasterPayoffRoutine(cloverCount >= 2));
        ApplyDisasterBuffFromResult(cloverCount, skullCount);
    }

    private DisasterOutcomeType PickDisasterOutcome()
    {
        List<DisasterOutcomeWeight> weights = disasterConfig.outcomeWeights;
        if (weights == null || weights.Count == 0)
        {
            return DisasterOutcomeType.TwoSkullOneClover;
        }

        int totalWeight = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += Mathf.Max(1, weights[i].weight);
        }

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += Mathf.Max(1, weights[i].weight);
            if (roll < cumulative)
            {
                return weights[i].outcome;
            }
        }

        return weights[0].outcome;
    }

    private bool[] BuildDisasterSlotResults(DisasterOutcomeType outcome)
    {
        bool[] results = outcome switch
        {
            DisasterOutcomeType.ThreeClover => new[] { true, true, true },
            DisasterOutcomeType.TwoCloverOneSkull => new[] { true, true, false },
            DisasterOutcomeType.ThreeSkull => new[] { false, false, false },
            _ => new[] { false, false, true }
        };

        for (int i = results.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            bool temp = results[i];
            results[i] = results[swapIndex];
            results[swapIndex] = temp;
        }

        return results;
    }

    private void SetDisasterSlotVisual(Image target, bool clover)
    {
        if (target == null)
        {
            return;
        }

        Sprite symbol = clover ? disasterConfig.cloverSymbol : disasterConfig.skullSymbol;
        if (symbol != null)
        {
            target.sprite = symbol;
            target.preserveAspect = true;
            target.color = Color.white;
            return;
        }

        target.sprite = null;
        target.preserveAspect = true;
        target.color = clover ? new Color(0.3f, 0.95f, 0.45f) : new Color(0.95f, 0.28f, 0.28f);
    }

    private IEnumerator PlayDisasterPayoffRoutine(bool heroWon)
    {
        if (disasterPayoffIcon == null || disasterPayoffFlash == null)
        {
            yield break;
        }

        disasterPayoffIcon.enabled = true;
        disasterPayoffIcon.sprite = heroWon ? disasterConfig.cloverSymbol : disasterConfig.skullSymbol;
        disasterPayoffIcon.color = disasterPayoffIcon.sprite != null
            ? Color.white
            : heroWon ? new Color(0.3f, 0.95f, 0.45f) : new Color(0.95f, 0.28f, 0.28f);
        disasterPayoffIcon.rectTransform.localScale = Vector3.one * 0.45f;
        disasterPayoffFlash.color = new Color(1f, 1f, 1f, 0.85f);

        float bounceDuration = Mathf.Max(0.1f, disasterConfig.payoffBounceDurationSec);
        float flashDuration = Mathf.Max(0.05f, disasterConfig.payoffFlashDurationSec);
        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            float bounce = Mathf.Sin(t * Mathf.PI);
            float scale = Mathf.Lerp(0.45f, 1.15f, t) + (bounce * 0.18f);
            disasterPayoffIcon.rectTransform.localScale = Vector3.one * scale;
            float flashAlpha = 0.85f * (1f - Mathf.Clamp01(elapsed / flashDuration));
            disasterPayoffFlash.color = new Color(1f, 1f, 1f, Mathf.Max(0f, flashAlpha));
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, disasterConfig.payoffHoldDurationSec));
        disasterPayoffIcon.enabled = false;
        disasterPayoffIcon.rectTransform.localScale = Vector3.one;
        disasterPayoffFlash.color = new Color(1f, 1f, 1f, 0f);
    }

    private void ApplyDisasterBuffFromResult(int cloverCount, int skullCount)
    {
        if (cloverCount >= 3)
        {
            ApplyHeroDisasterBuff(disasterConfig.threeCloverBuff);
            return;
        }

        if (cloverCount >= 2)
        {
            ApplyHeroDisasterBuff(disasterConfig.twoCloverBuff);
            return;
        }

        if (skullCount >= 3)
        {
            ApplyEnemyDisasterBuff(disasterConfig.threeSkullBuff);
            return;
        }

        ApplyEnemyDisasterBuff(disasterConfig.twoSkullBuff);
    }

    private List<float> BuildSpawnLanes(float minY, float maxY)
    {
        float usableHeight = Mathf.Max(1f, maxY - minY);
        float laneSpacing = Mathf.Max(18f, gameConfig.enemyVisualSize * 0.75f);
        int laneCount = Mathf.Clamp(Mathf.FloorToInt(usableHeight / laneSpacing) + 1, 5, 7);
        List<float> lanes = new List<float>(laneCount);

        for (int i = 0; i < laneCount; i++)
        {
            float t = laneCount == 1 ? 0.5f : (float)i / (laneCount - 1);
            lanes.Add(Mathf.Lerp(minY, maxY, t));
        }

        return lanes;
    }

    private void ShuffleSpawnLanes(List<float> lanes)
    {
        for (int i = lanes.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            float tmp = lanes[i];
            lanes[i] = lanes[swapIndex];
            lanes[swapIndex] = tmp;
        }
    }

    private void SpawnEnemy(string enemyId, float y, int waveSlot)
    {
        EnemyDefinition enemyData = ResolveEnemyDefinition(enemyId);
        RectTransform enemyRect = CreateUnitRect("Enemy", enemyArea, new Color(0.1f, 0.1f, 0.1f), gameConfig.enemyVisualSize, new Vector2(enemyArea.rect.width - gameConfig.enemySpawnRightMargin, y));
        Image enemyImage = enemyRect.GetComponent<Image>();
        Sprite idleSprite = enemyData != null ? enemyData.visualSprite : null;
        Sprite attackSprite = enemyData != null ? enemyData.attackVisualSprite : null;
        Sprite initialSprite = idleSprite != null ? idleSprite : attackSprite;
        if (enemyImage != null && initialSprite != null)
        {
            enemyImage.sprite = initialSprite;
            enemyImage.color = Color.white;
        }

        enemies.Add(new EnemyUnit
        {
            rect = enemyRect,
            image = enemyImage,
            enemyDefinition = enemyData,
            idleSprite = idleSprite,
            attackSprite = attackSprite,
            hp = enemyData != null ? enemyData.hp : 0f,
            attackTimer = 0f,
            attackVisualDuration = enemyData != null ? enemyData.attackVisualDuration : 0.1f,
            attackVisualTimer = 0f,
            waveSlot = waveSlot
        });
        EnsureEnemyBuffVfx(enemies[enemies.Count - 1]);
        RefreshDisasterBuffVfxState();

        if (IsWaveSlotValid(waveSlot))
        {
            waveAliveCounts[waveSlot]++;
            waveRemainingSpawnCounts[waveSlot] = Mathf.Max(0, waveRemainingSpawnCounts[waveSlot] - 1);
        }
    }

    private void UpdateHeroes()
    {
        for (int i = 0; i < heroes.Count; i++)
        {
            HeroUnit hero = heroes[i];
            UpdateHeroAttackVisual(hero);
            if (hero.isHeld)
            {
                continue;
            }

            hero.cooldown -= Time.deltaTime;
            if (hero.cooldown > 0f)
            {
                continue;
            }

            EnemyUnit target = FindTargetForHero(hero);
            if (target == null)
            {
                Debug.Log("[Battle] Hero found no target.");
                continue;
            }

            HeroDefinition heroData = hero.heroDefinition != null ? hero.heroDefinition : ResolveHeroDefinition(hero.heroId);
            HeroLevelData lvl = heroData != null ? heroData.GetLevel(hero.level) : new HeroLevelData();
            PlayHeroAttackVisual(hero);
            float effectiveDamageMultiplier = heroDamageMultiplier * disasterHeroDamageMultiplier;
            SpawnProjectile(hero, target, lvl.damage * effectiveDamageMultiplier);
            Debug.Log("[Battle] Hero fired projectile.");
            hero.cooldown = GetHeroAttackInterval(lvl);
        }
    }

    private void PlayHeroAttackVisual(HeroUnit hero)
    {
        if (hero == null || hero.image == null)
        {
            return;
        }

        Sprite attackSprite = hero.attackSprite != null ? hero.attackSprite : hero.idleSprite;
        if (attackSprite != null)
        {
            hero.image.sprite = attackSprite;
            hero.image.color = Color.white;
        }

        hero.attackVisualTimer = Mathf.Max(0.01f, hero.attackVisualDuration);
    }

    private void UpdateHeroAttackVisual(HeroUnit hero)
    {
        if (hero == null || hero.image == null || hero.attackVisualTimer <= 0f)
        {
            return;
        }

        hero.attackVisualTimer -= Time.deltaTime;
        if (hero.attackVisualTimer > 0f)
        {
            return;
        }

        Sprite idleSprite = hero.idleSprite != null ? hero.idleSprite : hero.attackSprite;
        if (idleSprite != null)
        {
            hero.image.sprite = idleSprite;
            hero.image.color = Color.white;
        }
    }

    private float GetHeroAttackInterval(HeroLevelData levelData)
    {
        if (gameConfig.heroAttackIntervalOverride > 0f)
        {
            return gameConfig.heroAttackIntervalOverride;
        }

        float effectiveAttackSpeed = Mathf.Max(0.01f, levelData.attackSpeed * heroAttackSpeedMultiplier * disasterHeroAttackSpeedMultiplier);
        return 1f / effectiveAttackSpeed;
    }

    private void SpawnProjectile(HeroUnit hero, EnemyUnit target, float damage)
    {
        if (hero == null || hero.rect == null || target == null || target.rect == null || target.hp <= 0f)
        {
            return;
        }

        Vector2 startPosition = ToEffectsLocalPoint(hero.rect.position);
        battleEffectsLayer.SetAsLastSibling();
        RectTransform projectileRect = CreateEffectRect("Projectile", battleEffectsLayer, gameConfig.projectileColor, gameConfig.projectileSize, startPosition);
        Image projectileImage = projectileRect.GetComponent<Image>();
        ApplyProjectileVisual(projectileImage, hero.heroDefinition);
        projectileRect.SetAsLastSibling();

        Debug.Log("[Battle] Projectile spawned at " + startPosition + ".");

        activeProjectiles.Add(new ProjectileView
        {
            rect = projectileRect,
            target = target,
            damage = damage,
            speed = gameConfig.projectileSpeed
        });
    }

    private void ApplyProjectileVisual(Image projectileImage, HeroDefinition heroData)
    {
        if (projectileImage == null)
        {
            return;
        }

        if (heroData != null && heroData.projectileSprite != null)
        {
            projectileImage.sprite = heroData.projectileSprite;
            projectileImage.type = Image.Type.Simple;
            projectileImage.preserveAspect = true;
            projectileImage.color = Color.white;
            return;
        }

        projectileImage.sprite = null;
        projectileImage.type = Image.Type.Simple;
        projectileImage.preserveAspect = false;
        projectileImage.color = gameConfig.projectileColor;
    }

    private void UpdateProjectiles()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            ProjectileView projectile = activeProjectiles[i];
            if (projectile == null || projectile.rect == null || projectile.target == null || projectile.target.rect == null || projectile.target.hp <= 0f)
            {
                if (projectile != null && projectile.rect != null)
                {
                    Destroy(projectile.rect.gameObject);
                }

                activeProjectiles.RemoveAt(i);
                continue;
            }

            Vector2 targetPosition = ToEffectsLocalPoint(projectile.target.rect.position);
            Vector2 currentPosition = projectile.rect.anchoredPosition;
            Vector2 direction = targetPosition - currentPosition;
            float distance = direction.magnitude;
            float step = projectile.speed * Time.deltaTime;

            if (distance <= step || distance <= 1f)
            {
                projectile.rect.anchoredPosition = targetPosition;
                ApplyProjectileHit(projectile);
                activeProjectiles.RemoveAt(i);
                continue;
            }

            projectile.rect.anchoredPosition = currentPosition + (direction / Mathf.Max(0.0001f, distance)) * step;
        }
    }

    private void ApplyProjectileHit(ProjectileView projectile)
    {
        if (projectile == null)
        {
            return;
        }

        EnemyUnit enemy = projectile.target;
        if (enemy != null && enemy.rect != null && enemy.hp > 0f)
        {
            enemy.hp -= projectile.damage;
            ShowEnemyHitFlash(enemy);
            SpawnFloatingDamage(enemy.rect.position, projectile.damage);
            Debug.Log("[Battle] Projectile hit enemy for " + Mathf.RoundToInt(projectile.damage) + ".");
        }

        if (projectile.rect != null)
        {
            Destroy(projectile.rect.gameObject);
        }
    }

    private void ShowEnemyHitFlash(EnemyUnit enemy)
    {
        if (enemy == null || enemy.rect == null)
        {
            return;
        }

        StartCoroutine(EnemyHitFlashRoutine(enemy));
    }

    private IEnumerator EnemyHitFlashRoutine(EnemyUnit enemy)
    {
        Image enemyImage = enemy.rect.GetComponent<Image>();
        if (enemyImage == null)
        {
            yield break;
        }

        Color baseColor = enemyImage.color;
        Vector3 baseScale = enemy.rect.localScale;

        enemyImage.color = Color.white;
        enemy.rect.localScale = baseScale * 1.12f;
        yield return new WaitForSeconds(0.07f);

        if (enemyImage != null)
        {
            enemyImage.color = baseColor;
        }

        if (enemy != null && enemy.rect != null)
        {
            enemy.rect.localScale = baseScale;
        }
    }

    private void SpawnFloatingDamage(Vector3 targetWorldPosition, float damage)
    {
        Text damageText = CreateText("FloatingDamage", battleEffectsLayer, "-" + Mathf.RoundToInt(damage), 34, TextAnchor.MiddleCenter);
        damageText.color = new Color(1f, 0.95f, 0.2f);
        damageText.raycastTarget = false;
        RectTransform damageRect = damageText.rectTransform;
        damageRect.anchorMin = new Vector2(0.5f, 0.5f);
        damageRect.anchorMax = new Vector2(0.5f, 0.5f);
        damageRect.pivot = new Vector2(0.5f, 0.5f);
        damageRect.sizeDelta = gameConfig.floatingDamageSize;
        damageRect.anchoredPosition = ToEffectsLocalPoint(targetWorldPosition) + new Vector2(0f, gameConfig.floatingDamageOffsetY);
        damageRect.SetAsLastSibling();

        activeFloatingDamage.Add(new FloatingDamageView
        {
            text = damageText,
            velocity = new Vector2(0f, 110f),
            age = 0f,
            lifetime = 0.45f
        });
    }

    private void SpawnWallDamageFloatingText(Vector3 contactWorldPosition, float damage)
    {
        SpawnFloatingDamage(contactWorldPosition, damage);
    }

    private void UpdateFloatingDamage()
    {
        for (int i = activeFloatingDamage.Count - 1; i >= 0; i--)
        {
            FloatingDamageView floatingDamage = activeFloatingDamage[i];
            if (floatingDamage == null || floatingDamage.text == null)
            {
                activeFloatingDamage.RemoveAt(i);
                continue;
            }

            floatingDamage.age += Time.deltaTime;
            floatingDamage.text.rectTransform.anchoredPosition += floatingDamage.velocity * Time.deltaTime;
            Color color = floatingDamage.text.color;
            color.a = Mathf.Clamp01(1f - (floatingDamage.age / Mathf.Max(0.01f, floatingDamage.lifetime)));
            floatingDamage.text.color = color;

            if (floatingDamage.age >= floatingDamage.lifetime)
            {
                Destroy(floatingDamage.text.gameObject);
                activeFloatingDamage.RemoveAt(i);
            }
        }
    }

    private Vector2 ToEffectsLocalPoint(Vector3 worldPosition)
    {
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(battleEffectsLayer, screenPosition, null, out Vector2 localPosition);
        return localPosition;
    }

    private Vector2 ToRewardLocalPoint(Vector3 worldPosition)
    {
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rewardEffectsLayer, screenPosition, null, out Vector2 localPosition);
        return localPosition;
    }

    private EnemyUnit FindTargetForHero(HeroUnit hero)
    {
        HeroDefinition heroData = hero != null && hero.heroDefinition != null ? hero.heroDefinition : ResolveHeroDefinition(hero != null ? hero.heroId : null);
        HeroLevelData lvl = heroData != null ? heroData.GetLevel(hero.level) : new HeroLevelData();
        EnemyUnit best = null;
        float bestDistance = float.MaxValue;
        float bestX = float.MaxValue;
        Vector2 heroWorld = hero.rect.position;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit e = enemies[i];
            if (e.hp <= 0f)
            {
                continue;
            }

            Vector2 enemyWorld = e.rect.position;
            float dist = Vector2.Distance(heroWorld, enemyWorld);
            if (dist > lvl.attackRange)
            {
                continue;
            }

            float enemyX = enemyWorld.x;
            if (dist < bestDistance || (Mathf.Approximately(dist, bestDistance) && enemyX < bestX))
            {
                bestDistance = dist;
                bestX = enemyX;
                best = e;
            }
        }

        return best;
    }

    private void UpdateEnemies()
    {
        float wallNearEdgeX = GetRectMaxWorldX(wallRect);

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit e = enemies[i];
            EnemyDefinition enemyData = e.enemyDefinition != null ? e.enemyDefinition : ResolveEnemyDefinition(null);
            UpdateEnemyAttackVisual(e);

            if (e.hp <= 0f)
            {
                if (IsWaveSlotValid(e.waveSlot))
                {
                    waveAliveCounts[e.waveSlot] = Mathf.Max(0, waveAliveCounts[e.waveSlot] - 1);
                    TryMarkWaveCompleted(e.waveSlot);
                }

                killedEnemyCount = Mathf.Clamp(killedEnemyCount + 1, 0, totalLevelEnemyCount);
                RefreshWaveUi();
                coins += enemyData != null ? enemyData.killRewardCoins : 0;
                Destroy(e.rect.gameObject);
                enemies.RemoveAt(i);
                RefreshUi();
                continue;
            }

            float enemyHalfWidth = GetRectHalfWidthWorld(e.rect);
            float contactCenterX = wallNearEdgeX + enemyHalfWidth;
            float wallContactX = contactCenterX - enemyHalfWidth;

            if (e.rect.position.x > contactCenterX)
            {
                float moveSpeed = (enemyData != null ? enemyData.moveSpeed : 0f) * disasterEnemyMoveSpeedMultiplier;
                Vector3 nextPosition = e.rect.position + Vector3.left * moveSpeed * Time.deltaTime;
                if (nextPosition.x < contactCenterX)
                {
                    nextPosition.x = contactCenterX;
                }

                e.rect.position = nextPosition;
            }
            else
            {
                Vector3 clampedPosition = e.rect.position;
                clampedPosition.x = contactCenterX;
                e.rect.position = clampedPosition;
                e.attackTimer -= Time.deltaTime;
                if (e.attackTimer <= 0f)
                {
                    float damage = enemyData != null ? enemyData.damage : 0f;
                    float attackSpeed = (enemyData != null ? enemyData.attackSpeed : 1f) * disasterEnemyAttackSpeedMultiplier;
                    wallHp -= damage;
                    Vector3 wallContactWorldPosition = new Vector3(wallContactX, e.rect.position.y, e.rect.position.z);
                    SpawnWallDamageFloatingText(wallContactWorldPosition, damage);
                    PlayEnemyAttackVisual(e);
                    e.attackTimer = 1f / Mathf.Max(0.01f, attackSpeed);
                    RefreshUi();
                }
            }
        }
    }

    private void PlayEnemyAttackVisual(EnemyUnit enemy)
    {
        if (enemy == null || enemy.image == null)
        {
            return;
        }

        Sprite attackSprite = enemy.attackSprite != null ? enemy.attackSprite : enemy.idleSprite;
        if (attackSprite != null)
        {
            enemy.image.sprite = attackSprite;
            enemy.image.color = Color.white;
        }

        enemy.attackVisualTimer = Mathf.Max(0.01f, enemy.attackVisualDuration);
    }

    private void UpdateEnemyAttackVisual(EnemyUnit enemy)
    {
        if (enemy == null || enemy.image == null || enemy.attackVisualTimer <= 0f)
        {
            return;
        }

        enemy.attackVisualTimer -= Time.deltaTime;
        if (enemy.attackVisualTimer > 0f)
        {
            return;
        }

        Sprite idleSprite = enemy.idleSprite != null ? enemy.idleSprite : enemy.attackSprite;
        if (idleSprite != null)
        {
            enemy.image.sprite = idleSprite;
            enemy.image.color = Color.white;
        }
    }

    private static float GetRectMaxWorldX(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return corners[3].x;
    }

    private static float GetRectHalfWidthWorld(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[3].x - corners[0].x) * 0.5f;
    }

    private void UpdateWinLose()
    {
        if (wallHp <= 0f)
        {
            wallHp = 0f;
            EndGame(false);
            return;
        }

        if (allWavesStarted && enemies.Count == 0)
        {
            EndGame(true);
        }
    }

    private void RefreshUi()
    {
        coinsText.text = "Coins: " + coins;
        heroCountText.text = "Heroes: " + heroes.Count + " / " + MaxHeroes;
        pullButtonText.text = "Pull " + GetCurrentPullCost();
        wallHpText.text = Mathf.CeilToInt(wallHp) + " / " + Mathf.CeilToInt(gameConfig.wallMaxHp);
        SetBarFillRatio(wallHpFillRect, wallHp / Mathf.Max(1f, gameConfig.wallMaxHp));
    }

    private void RefreshWaveUi()
    {
        int total = waveConfig != null && waveConfig.waves != null ? waveConfig.waves.Count : 0;
        int startedWaves = Mathf.Clamp(currentWaveIndex, 0, total);
        waveText.text = "Wave " + startedWaves + " / " + total;
        SetBarFillRatio(waveProgressFillRect, total <= 0 ? 0f : (float)startedWaves / total);
    }

    private void BuildDisasterWaveMarkers()
    {
        for (int i = waveProgressMarkerLayer != null ? waveProgressMarkerLayer.childCount - 1 : -1; i >= 0; i--)
        {
            Destroy(waveProgressMarkerLayer.GetChild(i).gameObject);
        }

        disasterMarkersByWaveSlot.Clear();
        if (waveProgressMarkerLayer == null || waveConfig == null || disasterConfig == null || disasterConfig.disasterWaveIndices == null)
        {
            return;
        }

        int totalWaves = Mathf.Max(1, waveConfig.waves.Count);
        for (int i = 0; i < disasterConfig.disasterWaveIndices.Count; i++)
        {
            int configuredWaveNumber = disasterConfig.disasterWaveIndices[i];
            int waveSlot = configuredWaveNumber - 1;
            if (waveSlot < 0 || waveSlot >= totalWaves || disasterMarkersByWaveSlot.ContainsKey(waveSlot))
            {
                continue;
            }

            float ratio = Mathf.Clamp01((float)configuredWaveNumber / totalWaves);
            RectTransform marker = CreatePanel("DisasterMarker_" + configuredWaveNumber, waveProgressMarkerLayer, Color.clear, new Vector2(ratio, 0.5f), new Vector2(ratio, 0.5f), Vector2.zero, Vector2.zero);
            marker.pivot = new Vector2(0.5f, 0.5f);
            Vector2 markerSize = disasterConfig != null ? disasterConfig.waveMarkerSize : new Vector2(44f, 44f);
            marker.sizeDelta = new Vector2(Mathf.Max(24f, markerSize.x), Mathf.Max(24f, markerSize.y));
            marker.anchoredPosition = new Vector2(0f, disasterConfig != null ? disasterConfig.waveMarkerOffsetY : 6f);
            Image markerImage = marker.GetComponent<Image>();
            markerImage.raycastTarget = false;
            if (disasterConfig.cloverSymbol != null)
            {
                markerImage.sprite = disasterConfig.cloverSymbol;
                markerImage.color = Color.white;
            }
            else
            {
                markerImage.color = new Color(0.35f, 0.95f, 0.45f);
            }

            disasterMarkersByWaveSlot[waveSlot] = marker;
        }
    }

    private void RemoveDisasterMarkerForWave(int waveSlot)
    {
        if (!disasterMarkersByWaveSlot.TryGetValue(waveSlot, out RectTransform marker))
        {
            return;
        }

        if (marker != null)
        {
            Destroy(marker.gameObject);
        }

        disasterMarkersByWaveSlot.Remove(waveSlot);
    }

    private void ApplyHeroDisasterBuff(HeroDisasterBuff buff)
    {
        disasterHeroDamageMultiplier = 1f + (Mathf.Max(0f, buff.damagePercent) / 100f);
        disasterHeroAttackSpeedMultiplier = 1f + (Mathf.Max(0f, buff.attackSpeedPercent) / 100f);
        disasterEnemyMoveSpeedMultiplier = 1f;
        disasterEnemyAttackSpeedMultiplier = 1f;
        disasterBuffRemainingSec = Mathf.Max(0.1f, buff.durationSec);
        activeDisasterBuffSide = DisasterBuffSide.Hero;
        RefreshDisasterBuffVfxState();
    }

    private void ApplyEnemyDisasterBuff(EnemyDisasterBuff buff)
    {
        disasterHeroDamageMultiplier = 1f;
        disasterHeroAttackSpeedMultiplier = 1f;
        disasterEnemyMoveSpeedMultiplier = 1f + (Mathf.Max(0f, buff.moveSpeedPercent) / 100f);
        disasterEnemyAttackSpeedMultiplier = 1f + (Mathf.Max(0f, buff.attackSpeedPercent) / 100f);
        disasterBuffRemainingSec = Mathf.Max(0.1f, buff.durationSec);
        activeDisasterBuffSide = DisasterBuffSide.Enemy;
        RefreshDisasterBuffVfxState();
    }

    private void UpdateDisasterBuffTimer()
    {
        if (activeDisasterBuffSide == DisasterBuffSide.None || isGameplayPaused)
        {
            return;
        }

        disasterBuffRemainingSec -= Time.deltaTime;
        if (disasterBuffRemainingSec > 0f)
        {
            return;
        }

        ClearDisasterBuff();
    }

    private void ClearDisasterBuff()
    {
        disasterHeroDamageMultiplier = 1f;
        disasterHeroAttackSpeedMultiplier = 1f;
        disasterEnemyMoveSpeedMultiplier = 1f;
        disasterEnemyAttackSpeedMultiplier = 1f;
        disasterBuffRemainingSec = 0f;
        activeDisasterBuffSide = DisasterBuffSide.None;
        RefreshDisasterBuffVfxState();
    }

    private void RefreshDisasterBuffVfxState()
    {
        bool heroBuffActive = activeDisasterBuffSide == DisasterBuffSide.Hero;
        bool enemyBuffActive = activeDisasterBuffSide == DisasterBuffSide.Enemy;

        for (int i = 0; i < heroes.Count; i++)
        {
            EnsureHeroBuffVfx(heroes[i]);
            if (heroes[i].buffVfx != null && heroes[i].buffVfx.root != null)
            {
                SyncBuffVfxSprite(heroes[i].buffVfx);
                heroes[i].buffVfx.root.gameObject.SetActive(heroBuffActive);
            }
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            EnsureEnemyBuffVfx(enemies[i]);
            if (enemies[i].buffVfx != null && enemies[i].buffVfx.root != null)
            {
                SyncBuffVfxSprite(enemies[i].buffVfx);
                enemies[i].buffVfx.root.gameObject.SetActive(enemyBuffActive);
            }
        }
    }

    private void EnsureHeroBuffVfx(HeroUnit hero)
    {
        if (hero == null || hero.rect == null || hero.buffVfx != null)
        {
            return;
        }

        Color tint = disasterConfig != null ? disasterConfig.heroBuffVfxColor : new Color(0.35f, 1f, 0.5f, 0.5f);
        hero.buffVfx = CreateSpriteContourBuffVfx("HeroBuffVfx", hero.rect, hero.image, tint);
    }

    private void EnsureEnemyBuffVfx(EnemyUnit enemy)
    {
        if (enemy == null || enemy.rect == null || enemy.buffVfx != null)
        {
            return;
        }

        Color tint = disasterConfig != null ? disasterConfig.enemyBuffVfxColor : new Color(1f, 0.28f, 0.28f, 0.5f);
        enemy.buffVfx = CreateSpriteContourBuffVfx("EnemyBuffVfx", enemy.rect, enemy.image, tint);
    }

    private void UpdateBuffVfxPulse()
    {
        if (activeDisasterBuffSide == DisasterBuffSide.None || disasterConfig == null)
        {
            return;
        }

        float pulse = 1f + (Mathf.Sin(Time.time * disasterConfig.buffVfxPulseSpeed) * disasterConfig.buffVfxPulseStrength);
        if (activeDisasterBuffSide == DisasterBuffSide.Hero)
        {
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i].buffVfx != null && heroes[i].buffVfx.root != null)
                {
                    SyncBuffVfxSprite(heroes[i].buffVfx);
                    heroes[i].buffVfx.root.localScale = Vector3.one * pulse;
                }
            }
        }
        else if (activeDisasterBuffSide == DisasterBuffSide.Enemy)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].buffVfx != null && enemies[i].buffVfx.root != null)
                {
                    SyncBuffVfxSprite(enemies[i].buffVfx);
                    enemies[i].buffVfx.root.localScale = Vector3.one * pulse;
                }
            }
        }
    }

    private BuffVfxView CreateSpriteContourBuffVfx(string name, RectTransform unitRoot, Image sourceImage, Color tint)
    {
        if (unitRoot == null)
        {
            return null;
        }

        RectTransform vfxRoot = CreatePanel(name, unitRoot, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        vfxRoot.pivot = new Vector2(0.5f, 0.5f);
        Image rootImage = vfxRoot.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.raycastTarget = false;
        }

        Vector2[] offsets = new Vector2[]
        {
            new Vector2(-2f, 0f),
            new Vector2(2f, 0f),
            new Vector2(0f, 2f),
            new Vector2(0f, -2f),
            new Vector2(-1.5f, 1.5f),
            new Vector2(1.5f, 1.5f),
            new Vector2(-1.5f, -1.5f),
            new Vector2(1.5f, -1.5f),
            Vector2.zero
        };

        Image[] layers = new Image[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject layerGo = CreateUiObject("Layer_" + i, vfxRoot);
            Image layerImage = layerGo.AddComponent<Image>();
            layerImage.raycastTarget = false;
            layerImage.type = Image.Type.Simple;
            layerImage.preserveAspect = true;
            layerImage.color = i == offsets.Length - 1
                ? new Color(tint.r, tint.g, tint.b, tint.a * 0.55f)
                : new Color(tint.r, tint.g, tint.b, tint.a * 0.25f);

            RectTransform layerRect = layerGo.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.anchoredPosition = offsets[i];
            layers[i] = layerImage;
        }

        BuffVfxView view = new BuffVfxView
        {
            root = vfxRoot,
            sourceImage = sourceImage,
            layers = layers
        };
        SyncBuffVfxSprite(view);
        vfxRoot.gameObject.SetActive(false);
        return view;
    }

    private static void SyncBuffVfxSprite(BuffVfxView view)
    {
        if (view == null || view.layers == null || view.sourceImage == null)
        {
            return;
        }

        Sprite sprite = view.sourceImage.sprite;
        bool hasSprite = sprite != null;
        for (int i = 0; i < view.layers.Length; i++)
        {
            if (view.layers[i] == null)
            {
                continue;
            }

            view.layers[i].sprite = sprite;
            view.layers[i].enabled = hasSprite;
        }
    }

    private int GetCurrentPullCost()
    {
        return slotConfig.basePullCost + (pullCount * slotConfig.pullCostStep);
    }

    private void OnPullPressed()
    {
        if (gameEnded || isSpinning || waitingCardChoice || isGameplayPaused)
        {
            return;
        }

        int cost = GetCurrentPullCost();
        if (coins < cost)
        {
            ShowFeedback("Not enough coins");
            return;
        }

        coins -= cost;
        pullCount++;
        RefreshUi();
        StartCoroutine(SpinAndResolve());
    }

    private IEnumerator SpinAndResolve()
    {
        isSpinning = true;
        float duration = Mathf.Max(0.1f, slotConfig.spinDurationMs / 1000f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            for (int i = 0; i < 3; i++)
            {
                SetSlotVisual(slotImages[i], (SlotSymbol)Random.Range(0, 3));
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        SlotResultData result = PickResult();
        for (int i = 0; i < 3; i++)
        {
            SetSlotVisual(slotImages[i], result.symbols[i]);
        }

        yield return StartCoroutine(ResolveRewardRoutine(result));
        isSpinning = false;
        RefreshUi();
    }

    private SlotResultData PickResult()
    {
        int totalWeight = 0;
        for (int i = 0; i < slotConfig.results.Count; i++)
        {
            totalWeight += Mathf.Max(1, slotConfig.results[i].weight);
        }

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < slotConfig.results.Count; i++)
        {
            cumulative += Mathf.Max(1, slotConfig.results[i].weight);
            if (roll < cumulative)
            {
                return slotConfig.results[i];
            }
        }

        return slotConfig.results[0];
    }

    private IEnumerator ResolveRewardRoutine(SlotResultData result)
    {
        if (result.rewardType == RewardType.Hero)
        {
            int slotIndex = GetRandomFreeHeroSlotIndex();
            if (slotIndex < 0)
            {
                coins += slotConfig.heroOverflowCompensationCoins;
                ShowFeedback("Hero field full: +" + slotConfig.heroOverflowCompensationCoins + " coins");
                yield break;
            }

            Vector2 source = GetPullSourceWorldPosition();
            Vector2 destination = GetHeroSlotWorldPosition(slotIndex);
            yield return StartCoroutine(PlayRewardTravel(source, destination, Color.white, gameConfig.rewardTravelSpeed));

            string heroId = ResolveRewardHeroId(result);
            TryPlaceHeroAtSlot(slotIndex, result.heroLevel, heroId);
            yield break;
        }

        if (result.rewardType == RewardType.Coins)
        {
            Vector2 source = GetPullSourceWorldPosition();
            Vector2 destination = GetCoinCounterWorldPosition();
            yield return StartCoroutine(PlayRewardTravel(source, destination, new Color(1f, 0.93f, 0.2f, 1f), gameConfig.rewardTravelSpeed));
            coins += result.coinReward;
            ShowFeedback("+" + result.coinReward + " coins");
            SpawnCoinGainText(result.coinReward);
            yield break;
        }

        if (result.rewardType == RewardType.Card)
        {
            ShowCardChoice(result.cardTier);
        }
    }

    private bool TryPlaceHero(int level, string heroId)
    {
        int slotIndex = GetRandomFreeHeroSlotIndex();
        if (slotIndex < 0)
        {
            return false;
        }
        return TryPlaceHeroAtSlot(slotIndex, level, heroId);
    }

    private bool TryPlaceHeroAtSlot(int slotIndex, int level, string heroId)
    {
        if (heroes.Count >= MaxHeroes || slotIndex < 0 || slotIndex >= MaxHeroes || IsHeroSlotOccupied(slotIndex))
        {
            return false;
        }

        HeroDefinition heroData = ResolveHeroDefinition(heroId);
        if (heroData == null)
        {
            return false;
        }

        RectTransform targetSlot = heroSlotRects[slotIndex];
        if (targetSlot == null)
        {
            return false;
        }

        RectTransform heroRect = CreateUnitRect("Hero", targetSlot, new Color(0.95f, 0.9f, 0.2f), gameConfig.heroVisualSize, Vector2.zero);
        Image heroImage = heroRect.GetComponent<Image>();
        heroRect.anchorMin = new Vector2(0.5f, 0.5f);
        heroRect.anchorMax = new Vector2(0.5f, 0.5f);
        heroRect.pivot = new Vector2(0.5f, 0.5f);
        heroRect.SetAsLastSibling();
        SnapHeroToSlotVisual(heroRect, false);

        Sprite idleSprite = heroData.GetIdleVisualSprite();
        Sprite attackSprite = heroData.GetAttackVisualSprite();
        Sprite initialSprite = idleSprite != null ? idleSprite : attackSprite;
        if (heroImage != null && initialSprite != null)
        {
            heroImage.sprite = initialSprite;
            heroImage.color = Color.white;
        }

        RectTransform starsRoot = CreatePanel("Stars", heroRect, Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        starsRoot.pivot = new Vector2(0.5f, 0.5f);
        float starsRootWidth = (HeroStarSize * 3f) + (HeroStarSpacing * 2f);
        starsRoot.sizeDelta = new Vector2(starsRootWidth, HeroStarSize);
        float heroVisualSize = gameConfig != null ? gameConfig.heroVisualSize : 36f;
        starsRoot.anchoredPosition = new Vector2(0f, -(heroVisualSize * 0.5f) - HeroStarOffsetY);
        Image starsRootImage = starsRoot.GetComponent<Image>();
        if (starsRootImage != null)
        {
            starsRootImage.raycastTarget = false;
        }
        HorizontalLayoutGroup starsLayout = starsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        starsLayout.childAlignment = TextAnchor.MiddleCenter;
        starsLayout.childControlWidth = false;
        starsLayout.childControlHeight = false;
        starsLayout.childForceExpandWidth = false;
        starsLayout.childForceExpandHeight = false;
        starsLayout.spacing = HeroStarSpacing;

        HeroUnit hero = new HeroUnit
        {
            rect = heroRect,
            slotIndex = slotIndex,
            level = Mathf.Clamp(level, 1, MaxHeroLevel),
            heroDefinition = heroData,
            heroId = heroData != null ? heroData.id : string.Empty,
            starsRoot = starsRoot,
            cooldown = 0f,
            image = heroImage,
            idleSprite = idleSprite,
            attackSprite = attackSprite,
            attackVisualDuration = heroData.attackVisualDuration,
            attackVisualTimer = 0f
        };
        heroes.Add(hero);
        EnsureHeroBuffVfx(hero);
        RefreshDisasterBuffVfxState();

        HeroDragHandler dragHandler = heroRect.gameObject.AddComponent<HeroDragHandler>();
        dragHandler.Init(this, hero);
        RefreshHeroLevelVisual(hero);

        return true;
    }

    private void SnapHeroToSlotVisual(RectTransform heroRect, bool selected)
    {
        if (heroRect == null)
        {
            return;
        }

        heroRect.anchoredPosition = new Vector2(0f, selected ? HeroHeldOffsetY : HeroRestingOffsetY);
    }

    private void OnHeroPointerDown(HeroUnit hero, PointerEventData eventData)
    {
        if (isGameplayPaused || hero == null || hero.rect == null || heroUnitsLayer == null)
        {
            return;
        }

        eventData.useDragThreshold = false;
        hero.isHeld = true;
        hero.dragStartSlotIndex = hero.slotIndex;
        SnapHeroToSlotVisual(hero.rect, true);
    }

    private void OnHeroBeginDrag(HeroUnit hero, PointerEventData eventData)
    {
        if (hero == null || !hero.isHeld)
        {
            return;
        }

        OnHeroDrag(hero, eventData);
    }

    private void OnHeroDrag(HeroUnit hero, PointerEventData eventData)
    {
        if (hero == null || !hero.isHeld || hero.rect == null || heroUnitsLayer == null)
        {
            return;
        }

        if (hero.rect.parent != heroUnitsLayer)
        {
            hero.rect.SetParent(heroUnitsLayer, true);
            hero.rect.SetAsLastSibling();
        }

        UpdateDraggedHeroPosition(hero, eventData);
    }

    private void OnHeroPointerUp(HeroUnit hero, PointerEventData eventData)
    {
        if (isGameplayPaused || hero == null || !hero.isHeld)
        {
            return;
        }

        int targetSlotIndex = GetSlotIndexAtScreenPoint(eventData.position, eventData.pressEventCamera);
        ResolveHeroDrop(hero, targetSlotIndex);
        hero.isHeld = false;
    }

    private void OnHeroEndDrag(HeroUnit hero, PointerEventData eventData)
    {
        OnHeroPointerUp(hero, eventData);
    }

    private void UpdateDraggedHeroPosition(HeroUnit hero, PointerEventData eventData)
    {
        if (hero == null || hero.rect == null || heroUnitsLayer == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(heroUnitsLayer, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            hero.rect.anchorMin = new Vector2(0.5f, 0.5f);
            hero.rect.anchorMax = new Vector2(0.5f, 0.5f);
            hero.rect.anchoredPosition = localPos;
        }
    }

    private void ResolveHeroDrop(HeroUnit hero, int targetSlotIndex)
    {
        if (hero == null || hero.rect == null)
        {
            return;
        }

        int sourceSlotIndex = hero.dragStartSlotIndex;
        if (targetSlotIndex < 0 || targetSlotIndex >= heroSlotRects.Count)
        {
            PlaceHeroIntoSlot(hero, sourceSlotIndex);
            return;
        }

        HeroUnit targetHero = FindHeroAtSlot(targetSlotIndex);
        if (targetHero == null || targetHero == hero)
        {
            PlaceHeroIntoSlot(hero, targetSlotIndex);
            return;
        }

        bool mergeable = CanMergeHeroes(hero, targetHero);
        if (mergeable && hero.level < MaxHeroLevel && targetHero.level < MaxHeroLevel)
        {
            TryMergeHeroes(hero, targetHero);
            return;
        }

        if (mergeable && hero.level >= MaxHeroLevel && targetHero.level >= MaxHeroLevel)
        {
            ShowFeedback("Max level");
        }

        PlaceHeroIntoSlot(targetHero, sourceSlotIndex);
        PlaceHeroIntoSlot(hero, targetSlotIndex);
    }

    private bool CanMergeHeroes(HeroUnit a, HeroUnit b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.level != b.level)
        {
            return false;
        }

        return !string.IsNullOrEmpty(a.heroId) && a.heroId == b.heroId;
    }

    private bool TryMergeHeroes(HeroUnit draggedHero, HeroUnit targetHero)
    {
        if (!CanMergeHeroes(draggedHero, targetHero))
        {
            return false;
        }

        if (draggedHero.level >= MaxHeroLevel || targetHero.level >= MaxHeroLevel)
        {
            return false;
        }

        targetHero.level = Mathf.Min(MaxHeroLevel, targetHero.level + 1);
        targetHero.cooldown = Mathf.Min(targetHero.cooldown, 0.15f);
        RefreshHeroLevelVisual(targetHero);
        PlayHeroMergeFlash(GetHeroSlotWorldPosition(targetHero.slotIndex));

        heroes.Remove(draggedHero);
        if (draggedHero.rect != null)
        {
            Destroy(draggedHero.rect.gameObject);
        }

        RefreshUi();
        return true;
    }

    private void RefreshHeroLevelVisual(HeroUnit hero)
    {
        if (hero == null)
        {
            return;
        }

        BuildHeroStars(hero);
    }

    private void BuildHeroStars(HeroUnit hero)
    {
        if (hero == null || hero.starsRoot == null)
        {
            return;
        }

        for (int i = hero.starsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(hero.starsRoot.GetChild(i).gameObject);
        }

        int clampedLevel = Mathf.Clamp(hero.level, 1, MaxHeroLevel);
        bool usePurple = clampedLevel >= 4;
        int starCount = usePurple ? clampedLevel - 3 : clampedLevel;
        Sprite starSprite = usePurple ? gameConfig.purpleStarSprite : gameConfig.yellowStarSprite;
        Color fallbackColor = usePurple ? new Color(0.74f, 0.45f, 0.95f, 1f) : new Color(1f, 0.89f, 0.2f, 1f);

        for (int i = 0; i < starCount; i++)
        {
            RectTransform starRect = CreateUiObject("Star" + i, hero.starsRoot).GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.5f, 0.5f);
            starRect.anchorMax = new Vector2(0.5f, 0.5f);
            starRect.pivot = new Vector2(0.5f, 0.5f);
            starRect.sizeDelta = new Vector2(HeroStarSize, HeroStarSize);

            Image starImage = starRect.gameObject.AddComponent<Image>();
            starImage.raycastTarget = false;
            starImage.sprite = starSprite;
            starImage.type = Image.Type.Simple;
            starImage.color = starSprite != null ? Color.white : fallbackColor;
        }
    }

    private void PlayHeroMergeFlash(Vector3 worldPosition)
    {
        if (battleEffectsLayer == null)
        {
            return;
        }

        StartCoroutine(PlayHeroMergeFlashRoutine(worldPosition));
    }

    private IEnumerator PlayHeroMergeFlashRoutine(Vector3 worldPosition)
    {
        Vector2 localPoint = ToEffectsLocalPoint(worldPosition);
        RectTransform flash = CreateEffectRect("MergeFlash", battleEffectsLayer, new Color(1f, 0.95f, 0.55f, 0.85f), 64f, localPoint);
        flash.SetAsLastSibling();
        Image flashImage = flash.GetComponent<Image>();
        float duration = 0.14f;
        float elapsed = 0f;
        Vector2 startSize = new Vector2(24f, 24f);
        Vector2 endSize = new Vector2(92f, 92f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fade = 1f - t;
            flash.sizeDelta = Vector2.Lerp(startSize, endSize, t);
            if (flashImage != null)
            {
                flashImage.color = new Color(1f, 0.95f, 0.55f, 0.85f * fade);
            }
            yield return null;
        }

        if (flash != null)
        {
            Destroy(flash.gameObject);
        }
    }

    private void PlaceHeroIntoSlot(HeroUnit hero, int slotIndex)
    {
        if (hero == null || hero.rect == null || slotIndex < 0 || slotIndex >= heroSlotRects.Count)
        {
            return;
        }

        RectTransform slot = heroSlotRects[slotIndex];
        hero.rect.SetParent(slot, false);
        hero.rect.anchorMin = new Vector2(0.5f, 0.5f);
        hero.rect.anchorMax = new Vector2(0.5f, 0.5f);
        hero.rect.pivot = new Vector2(0.5f, 0.5f);
        hero.rect.SetAsLastSibling();
        SnapHeroToSlotVisual(hero.rect, false);
        hero.slotIndex = slotIndex;
    }

    private HeroUnit FindHeroAtSlot(int slotIndex)
    {
        for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i].slotIndex == slotIndex)
            {
                return heroes[i];
            }
        }

        return null;
    }

    private int GetSlotIndexAtScreenPoint(Vector2 screenPoint, Camera eventCamera)
    {
        for (int i = 0; i < heroSlotRects.Count; i++)
        {
            RectTransform slot = heroSlotRects[i];
            if (slot != null && RectTransformUtility.RectangleContainsScreenPoint(slot, screenPoint, eventCamera))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsHeroSlotOccupied(int slotIndex)
    {
        for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i].slotIndex == slotIndex)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetHeroSlotWorldPosition(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < heroSlotRects.Count ? GetWorldCenter(heroSlotRects[slotIndex]) : Vector2.zero;
    }

    private Vector2 GetPullSourceWorldPosition()
    {
        RectTransform pullRect = pullButton != null ? pullButton.GetComponent<RectTransform>() : null;
        return pullRect != null ? GetWorldCenter(pullRect) : Vector2.zero;
    }

    private Vector2 GetCoinCounterWorldPosition()
    {
        return coinCounterAnchor != null ? GetWorldCenter(coinCounterAnchor) : Vector2.zero;
    }

    private static Vector2 GetWorldCenter(RectTransform target)
    {
        if (target == null)
        {
            return Vector2.zero;
        }

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    private string ResolveRewardHeroId(SlotResultData result)
    {
        if (result != null && !string.IsNullOrWhiteSpace(result.heroId))
        {
            return result.heroId;
        }

        if (result != null
            && (result.resultId == "two_character" || result.resultId == "three_character")
            && heroCatalog != null)
        {
            return heroCatalog.GetRandomHeroId();
        }

        return GetDefaultHeroId();
    }

    private string GetDefaultHeroId()
    {
        if (heroCatalog != null)
        {
            HeroDefinition fallback = heroCatalog.GetDefaultHeroDefinition(gameConfig != null ? gameConfig.defaultHeroId : null);
            if (fallback != null && !string.IsNullOrEmpty(fallback.id))
            {
                return fallback.id;
            }
        }

        foreach (KeyValuePair<string, HeroDefinition> pair in heroLookup)
        {
            return pair.Key;
        }

        return "hero_basic";
    }

    private HeroDefinition ResolveHeroDefinition(string heroId)
    {
        if (heroCatalog != null)
        {
            return heroCatalog.GetHeroById(heroId);
        }

        string resolvedId = string.IsNullOrEmpty(heroId) ? GetDefaultHeroId() : heroId;
        if (heroLookup.TryGetValue(resolvedId, out HeroDefinition hero))
        {
            return hero;
        }

        return new HeroDefinition();
    }

    private EnemyDefinition ResolveEnemyDefinition(string enemyId)
    {
        if (enemyCatalog != null)
        {
            return enemyCatalog.GetEnemyById(enemyId);
        }

        if (!string.IsNullOrEmpty(enemyId) && enemyLookup.TryGetValue(enemyId, out EnemyDefinition enemy))
        {
            return enemy;
        }

        foreach (KeyValuePair<string, EnemyDefinition> pair in enemyLookup)
        {
            return pair.Value;
        }

        return new EnemyDefinition();
    }

    private IEnumerator PlayRewardTravel(Vector2 sourceWorld, Vector2 destinationWorld, Color color, float speed)
    {
        if (rewardEffectsLayer == null)
        {
            yield break;
        }

        RectTransform rewardRoot = CreateEffectRect("RewardTravel", rewardEffectsLayer, Color.clear, 24f, ToRewardLocalPoint(sourceWorld));
        rewardRoot.SetAsLastSibling();

        RectTransform trailRect = CreateEffectRect("Trail", rewardRoot, color, 12f, new Vector2(-18f, 0f));
        trailRect.pivot = new Vector2(1f, 0.5f);
        trailRect.sizeDelta = new Vector2(34f, 8f);
        Image trailImage = trailRect.GetComponent<Image>();
        trailImage.color = new Color(color.r, color.g, color.b, 0.6f);

        RectTransform coreRect = CreateEffectRect("Core", rewardRoot, color, 16f, Vector2.zero);
        Image coreImage = coreRect.GetComponent<Image>();
        coreImage.color = color;

        Vector2 start = ToRewardLocalPoint(sourceWorld);
        Vector2 end = ToRewardLocalPoint(destinationWorld);
        float distance = Vector2.Distance(sourceWorld, destinationWorld);
        float clampedSpeed = Mathf.Max(1f, speed);
        float travel = distance <= 0.01f ? 0f : distance / clampedSpeed;
        Vector2 direction = (end - start).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rewardRoot.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (travel <= 0f)
        {
            rewardRoot.anchoredPosition = end;
            Destroy(rewardRoot.gameObject);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < travel)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travel);
            rewardRoot.anchoredPosition = Vector2.Lerp(start, end, t);
            coreRect.localScale = Vector3.one * (0.8f + (Mathf.Sin(t * Mathf.PI * 6f) * 0.2f));
            yield return null;
        }

        rewardRoot.anchoredPosition = end;
        Destroy(rewardRoot.gameObject);
    }

    private void SpawnCoinGainText(int coinAmount)
    {
        if (rewardEffectsLayer == null)
        {
            return;
        }

        Text gainText = CreateText("CoinGainText", rewardEffectsLayer, "+" + coinAmount, 34, TextAnchor.MiddleCenter);
        gainText.color = new Color(1f, 0.95f, 0.35f, 1f);
        gainText.raycastTarget = false;

        RectTransform gainRect = gainText.rectTransform;
        gainRect.anchorMin = new Vector2(0.5f, 0.5f);
        gainRect.anchorMax = new Vector2(0.5f, 0.5f);
        gainRect.pivot = new Vector2(0.5f, 0.5f);
        gainRect.sizeDelta = new Vector2(220f, 72f);
        gainRect.anchoredPosition = ToRewardLocalPoint(GetCoinCounterWorldPosition()) + new Vector2(0f, 24f);

        StartCoroutine(AnimateCoinGainText(gainText));
    }

    private IEnumerator AnimateCoinGainText(Text gainText)
    {
        if (gainText == null)
        {
            yield break;
        }

        RectTransform gainRect = gainText.rectTransform;
        float duration = 0.6f;
        float elapsed = 0f;
        Vector2 start = gainRect.anchoredPosition;
        Vector2 end = start + new Vector2(0f, 65f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            gainRect.anchoredPosition = Vector2.Lerp(start, end, t);
            Color color = gainText.color;
            color.a = 1f - t;
            gainText.color = color;
            yield return null;
        }

        if (gainText != null)
        {
            Destroy(gainText.gameObject);
        }
    }

    private void SetSlotVisual(Image img, SlotSymbol symbol)
    {
        img.sprite = null;
        if (symbol == SlotSymbol.Character && slotConfig.characterSymbol != null)
        {
            img.sprite = slotConfig.characterSymbol;
            img.color = Color.white;
        }
        else if (symbol == SlotSymbol.Coins && slotConfig.coinsSymbol != null)
        {
            img.sprite = slotConfig.coinsSymbol;
            img.color = Color.white;
        }
        else if (symbol == SlotSymbol.Card && slotConfig.cardSymbol != null)
        {
            img.sprite = slotConfig.cardSymbol;
            img.color = Color.white;
        }
        else
        {
            img.color = symbol == SlotSymbol.Character ? new Color(0.94f, 0.92f, 0.2f) : symbol == SlotSymbol.Coins ? new Color(0.96f, 0.7f, 0.18f) : new Color(0.46f, 0.76f, 1f);
        }
    }

    private int GetRandomFreeHeroSlotIndex()
    {
        List<int> freeIndices = new List<int>(MaxHeroes);
        HashSet<int> occupiedIndices = new HashSet<int>();

        for (int i = 0; i < heroes.Count; i++)
        {
            occupiedIndices.Add(heroes[i].slotIndex);
        }

        for (int i = 0; i < MaxHeroes; i++)
        {
            if (!occupiedIndices.Contains(i))
            {
                freeIndices.Add(i);
            }
        }

        if (freeIndices.Count == 0)
        {
            return -1;
        }

        return freeIndices[Random.Range(0, freeIndices.Count)];
    }

    private void SetDefaultSlotSymbols()
    {
        if (slotImages == null || slotImages.Length < 3)
        {
            return;
        }

        SetSlotVisual(slotImages[0], SlotSymbol.Character);
        SetSlotVisual(slotImages[1], SlotSymbol.Character);
        SetSlotVisual(slotImages[2], SlotSymbol.Coins);
    }

    private void BuildResultOverlay(Transform canvas)
    {
        resultOverlay = CreatePanel("ResultOverlay", canvas, new Color(0f, 0f, 0f, 0.75f), new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero).gameObject;
        resultText = CreateText("ResultText", resultOverlay.transform, "Victory", 72, TextAnchor.MiddleCenter);
        resultText.rectTransform.anchorMin = new Vector2(0.2f, 0.5f);
        resultText.rectTransform.anchorMax = new Vector2(0.8f, 0.75f);

        RectTransform restartRect = CreatePanel("RestartButton", resultOverlay.transform, new Color(0.2f, 0.5f, 0.2f), new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.46f), Vector2.zero, Vector2.zero);
        var restartButton = restartRect.gameObject.AddComponent<Button>();
        restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        CreateText("RestartText", restartRect, "Restart", 38, TextAnchor.MiddleCenter);

        resultOverlay.SetActive(false);
    }

    private void BuildCardOverlay(Transform canvas)
    {
        cardOverlay = CreatePanel("CardOverlay", canvas, new Color(0f, 0f, 0f, 0.7f), new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero).gameObject;
        cardOverlay.SetActive(false);
    }

    private void ShowCardChoice(CardTier tier)
    {
        waitingCardChoice = true;
        cardOverlay.SetActive(true);

        for (int i = cardOverlay.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(cardOverlay.transform.GetChild(i).gameObject);
        }

        CreateText("CardTitle", cardOverlay.transform, tier == CardTier.Enhanced ? "Choose Enhanced Card" : "Choose Card", 52, TextAnchor.MiddleCenter).rectTransform.SetParent(cardOverlay.transform, false);

        CardTierValues values = tier == CardTier.Enhanced ? slotConfig.enhancedCards : slotConfig.normalCards;
        bool enhanced = tier == CardTier.Enhanced;

        CardChoiceData damageCard = new CardChoiceData
        {
            title = "Damage Boost",
            description = "+" + values.damagePercent + "% damage to all heroes",
            icon = slotConfig.damageBonusIcon,
            fallbackIconLabel = "DMG"
        };
        CreateCardButton(0, damageCard, enhanced, () => { heroDamageMultiplier *= 1f + (values.damagePercent / 100f); CloseCardChoice(); });

        CardChoiceData attackSpeedCard = new CardChoiceData
        {
            title = "Attack Speed",
            description = "+" + values.attackSpeedPercent + "% attack speed to all heroes",
            icon = slotConfig.attackSpeedBonusIcon,
            fallbackIconLabel = "AS"
        };
        CreateCardButton(1, attackSpeedCard, enhanced, () => { heroAttackSpeedMultiplier *= 1f + (values.attackSpeedPercent / 100f); CloseCardChoice(); });

        CardChoiceData wallRepairCard = new CardChoiceData
        {
            title = "Wall Repair",
            description = "+" + values.wallHeal + " wall HP",
            icon = slotConfig.wallHpBonusIcon,
            fallbackIconLabel = "HP"
        };
        CreateCardButton(2, wallRepairCard, enhanced, () => { wallHp = Mathf.Min(gameConfig.wallMaxHp, wallHp + values.wallHeal); CloseCardChoice(); RefreshUi(); });
    }

    private void CreateCardButton(int index, CardChoiceData data, bool enhanced, UnityEngine.Events.UnityAction action)
    {
        float xMin = 0.1f + index * 0.3f;
        float xMax = 0.36f + index * 0.3f;
        RectTransform card = CreatePanel("Card" + index, cardOverlay.transform, new Color(0.98f, 0.98f, 0.98f), new Vector2(xMin, 0.35f), new Vector2(xMax, 0.7f), Vector2.zero, Vector2.zero);
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null && slotConfig != null && slotConfig.cardRewardBackground != null)
        {
            cardImage.sprite = slotConfig.cardRewardBackground;
            cardImage.type = Image.Type.Sliced;
            cardImage.color = Color.white;
        }

        RectTransform iconBlock = CreatePanel("IconBlock", card, Color.clear, new Vector2(0.08f, 0.6f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
        RectTransform titleBlock = CreatePanel("TitleBlock", card, Color.clear, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.6f), Vector2.zero, Vector2.zero);
        RectTransform descriptionBlock = CreatePanel("DescriptionBlock", card, Color.clear, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.42f), Vector2.zero, Vector2.zero);

        BuildCardIcon(iconBlock, data);

        Text titleText = CreateText("TitleText", titleBlock, data.title, 32, TextAnchor.MiddleCenter);
        titleText.color = enhanced ? EnhancedCardTextColor : CardTitleColor;
        StyleCardText(titleText, true);

        Text descriptionText = CreateText("DescriptionText", descriptionBlock, data.description, 25, TextAnchor.UpperCenter);
        descriptionText.color = enhanced ? EnhancedCardTextColor : CardDescriptionColor;
        StyleCardText(descriptionText, false);
        descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
        descriptionText.lineSpacing = 0.9f;

        Button btn = card.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(action);
    }

    private static void StyleCardText(Text text, bool isTitle)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyle.Bold;
        if (!isTitle)
        {
            text.fontSize += 1;
        }

        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = CardTextShadowColor;
        shadow.effectDistance = new Vector2(0f, -1.4f);
    }

    private void BuildCardIcon(RectTransform iconBlock, CardChoiceData data)
    {
        RectTransform iconContainerRect = CreatePanel("IconContainer", iconBlock, Color.clear, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);
        RectTransform iconBackgroundRect = iconContainerRect;
        if (slotConfig.bonusCardIconBackground != null)
        {
            iconBackgroundRect = CreatePanel("IconBackground", iconContainerRect, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image iconBackgroundImage = iconBackgroundRect.GetComponent<Image>();
            iconBackgroundImage.sprite = slotConfig.bonusCardIconBackground;
            iconBackgroundImage.type = Image.Type.Simple;
            iconBackgroundImage.color = Color.white;
        }

        RectTransform iconRect = CreatePanel("EffectIcon", iconBackgroundRect, Color.clear, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
        Image iconImage = iconRect.GetComponent<Image>();
        iconImage.preserveAspect = true;
        if (data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = CardIconFallbackColor;
            Text fallbackText = CreateText("FallbackIconText", iconRect, data.fallbackIconLabel, 22, TextAnchor.MiddleCenter);
            fallbackText.color = Color.white;
        }
    }

    private void CloseCardChoice()
    {
        waitingCardChoice = false;
        cardOverlay.SetActive(false);
        TryStartPendingDisaster();
    }

    private void BuildDisasterOverlay(Transform canvas)
    {
        disasterOverlay = CreatePanel("DisasterOverlay", canvas, disasterConfig.overlayColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        disasterOverlay.SetActive(false);

        RectTransform modal = CreatePanel("DisasterModal", disasterOverlay.transform, new Color(0.12f, 0.1f, 0.12f, 0.95f), new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.74f), Vector2.zero, Vector2.zero);
        Text title = CreateText("DisasterTitle", modal, "Disaster!", 52, TextAnchor.UpperCenter);
        title.rectTransform.anchorMin = new Vector2(0.08f, 0.74f);
        title.rectTransform.anchorMax = new Vector2(0.92f, 0.98f);

        disasterSlotsRoot = CreatePanel("DisasterSlotsRoot", modal, Color.clear, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.7f), Vector2.zero, Vector2.zero);
        Image slotsRootImage = disasterSlotsRoot.GetComponent<Image>();
        if (slotsRootImage != null)
        {
            slotsRootImage.raycastTarget = false;
        }

        for (int i = 0; i < 3; i++)
        {
            RectTransform slot = CreatePanel("DisasterSlot" + i, disasterSlotsRoot, new Color(0.18f, 0.18f, 0.18f, 0.95f), new Vector2(0.05f + (0.32f * i), 0.1f), new Vector2(0.31f + (0.32f * i), 0.9f), Vector2.zero, Vector2.zero);
            RectTransform iconRect = CreatePanel("Icon", slot, Color.clear, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            AspectRatioFitter iconAspect = iconRect.gameObject.AddComponent<AspectRatioFitter>();
            iconAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            iconAspect.aspectRatio = 1f;

            Image iconImage = iconRect.GetComponent<Image>();
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            disasterSlotImages[i] = iconImage;
        }

        RectTransform payoffRoot = CreatePanel("DisasterPayoff", disasterOverlay.transform, Color.clear, new Vector2(0.4f, 0.43f), new Vector2(0.6f, 0.63f), Vector2.zero, Vector2.zero);
        disasterPayoffFlash = CreatePanel("DisasterPayoffFlash", payoffRoot, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
        disasterPayoffIcon = CreatePanel("DisasterPayoffIcon", payoffRoot, Color.clear, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero).GetComponent<Image>();
        disasterPayoffIcon.enabled = false;
    }

    private void EndGame(bool victory)
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        if (resultText != null)
        {
            resultText.text = victory ? "Victory" : "Defeat";
        }

        if (resultOverlay != null)
        {
            resultOverlay.SetActive(true);
        }
    }

    private void ShowFeedback(string msg)
    {
        StopCoroutine(nameof(ClearFeedback));
        feedbackText.text = msg;
        StartCoroutine(nameof(ClearFeedback));
    }

    private IEnumerator ClearFeedback()
    {
        yield return new WaitForSeconds(1.6f);
        feedbackText.text = string.Empty;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        return go;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    private static RectTransform CreateUnitRect(string name, Transform parent, Color color, float size, Vector2 anchoredPosition)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = anchoredPosition;
        return rt;
    }

    private static RectTransform CreateEffectRect(string name, Transform parent, Color color, float size, Vector2 localPosition)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = localPosition;
        return rt;
    }

    private static Text CreateText(string name, Transform parent, string textValue, int size, TextAnchor anchor)
    {
        GameObject go = CreateUiObject(name, parent);
        Text txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = textValue;
        txt.fontSize = size;
        txt.alignment = anchor;
        txt.color = Color.white;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return txt;
    }

    private static RectTransform CreateSimpleFilledBar(Transform parent, string barName, Vector2 anchorMin, Vector2 anchorMax, out Image frameImage)
    {
        RectTransform bg = CreatePanel(barName, parent, ProgressBarFrameColor, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        frameImage = bg.GetComponent<Image>();
        frameImage.sprite = null;
        frameImage.type = Image.Type.Simple;
        frameImage.color = ProgressBarFrameColor;

        RectTransform fill = CreatePanel("Fill", bg, ProgressBarFillColor, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        fill.pivot = new Vector2(0f, 0.5f);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = null;
        fillImage.type = Image.Type.Simple;
        fillImage.color = ProgressBarFillColor;
        return fill;
    }

    private static void SetBarFillRatio(RectTransform fillRect, float ratio)
    {
        if (fillRect == null)
        {
            return;
        }

        float clampedRatio = Mathf.Clamp01(ratio);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(clampedRatio, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private class HeroUnit
    {
        public RectTransform rect;
        public Image image;
        public HeroDefinition heroDefinition;
        public Sprite idleSprite;
        public Sprite attackSprite;
        public string heroId;
        public int slotIndex;
        public int level;
        public float cooldown;
        public float attackVisualDuration;
        public float attackVisualTimer;
        public RectTransform starsRoot;
        public bool isHeld;
        public int dragStartSlotIndex;
        public BuffVfxView buffVfx;
    }

    private class HeroDragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BattleBootstrap bootstrap;
        private HeroUnit hero;

        public void Init(BattleBootstrap owner, HeroUnit targetHero)
        {
            bootstrap = owner;
            hero = targetHero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (bootstrap == null || hero == null)
            {
                return;
            }

            bootstrap.OnHeroPointerDown(hero, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (bootstrap == null || hero == null)
            {
                return;
            }

            bootstrap.OnHeroPointerUp(hero, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (bootstrap == null || hero == null)
            {
                return;
            }

            bootstrap.OnHeroBeginDrag(hero, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (bootstrap == null || hero == null)
            {
                return;
            }

            bootstrap.OnHeroDrag(hero, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (bootstrap == null || hero == null)
            {
                return;
            }

            bootstrap.OnHeroEndDrag(hero, eventData);
        }
    }

    private class EnemyUnit
    {
        public RectTransform rect;
        public Image image;
        public EnemyDefinition enemyDefinition;
        public Sprite idleSprite;
        public Sprite attackSprite;
        public float hp;
        public float attackTimer;
        public float attackVisualDuration;
        public float attackVisualTimer;
        public int waveSlot;
        public BuffVfxView buffVfx;
    }

    private class BuffVfxView
    {
        public RectTransform root;
        public Image sourceImage;
        public Image[] layers;
    }

    private class ProjectileView
    {
        public RectTransform rect;
        public EnemyUnit target;
        public float damage;
        public float speed;
    }

    private class FloatingDamageView
    {
        public Text text;
        public Vector2 velocity;
        public float age;
        public float lifetime;
    }

    private struct CardChoiceData
    {
        public string title;
        public string description;
        public Sprite icon;
        public string fallbackIconLabel;
    }

    private enum DisasterBuffSide
    {
        None,
        Hero,
        Enemy
    }
}

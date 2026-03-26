using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleBootstrap : MonoBehaviour
{
    private static readonly Color HeroFieldFallbackColor = new Color(0.27f, 0.48f, 0.72f);
    private static readonly Color WallFallbackColor = new Color(0.55f, 0.5f, 0.43f);
    private static readonly Color EnemyFieldFallbackColor = new Color(0.62f, 0.33f, 0.33f);
    private static readonly Color CardTitleColor = new Color(0.12f, 0.13f, 0.16f);
    private static readonly Color CardDescriptionColor = new Color(0.25f, 0.27f, 0.32f);
    private static readonly Color EnhancedCardTextColor = new Color(0.86f, 0.32f, 0.84f);
    private static readonly Color CardTextOutlineColor = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color CardTextShadowColor = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color CardIconBackgroundFallbackColor = new Color(0.92f, 0.9f, 0.84f);
    private static readonly Color CardIconFallbackColor = new Color(0.35f, 0.38f, 0.44f);

    private GameConfigSO gameConfig;
    private HeroDataSO heroData;
    private EnemyDataSO enemyData;
    private WaveConfigSO waveConfig;
    private SlotMachineConfigSO slotConfig;

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

    private Text coinsText;
    private Text heroCountText;
    private Text pullButtonText;
    private Text wallHpText;
    private Image wallHpFill;
    private Text waveText;
    private Image waveProgressFill;
    private Text feedbackText;

    private Image[] slotImages = new Image[3];
    private Button pullButton;
    private GameObject resultOverlay;
    private Text resultText;
    private GameObject cardOverlay;

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

    private int HeroRows => Mathf.Max(1, gameConfig != null ? gameConfig.heroRows : 7);
    private int HeroCols => Mathf.Max(1, gameConfig != null ? gameConfig.heroCols : 3);
    private int MaxHeroes => HeroRows * HeroCols;
    private float HeroGridSpacing => gameConfig != null ? gameConfig.heroGridSpacing : 6f;
    private float HeroGridPadding => gameConfig != null ? gameConfig.heroGridPadding : 8f;

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
        heroData = Resources.Load<HeroDataSO>("Configs/HeroData");
        enemyData = Resources.Load<EnemyDataSO>("Configs/EnemyData");
        waveConfig = Resources.Load<WaveConfigSO>("Configs/WaveConfig");
        slotConfig = Resources.Load<SlotMachineConfigSO>("Configs/SlotMachineConfig");

        if (gameConfig == null) gameConfig = ScriptableObject.CreateInstance<GameConfigSO>();
        if (heroData == null) heroData = ScriptableObject.CreateInstance<HeroDataSO>();
        if (enemyData == null) enemyData = ScriptableObject.CreateInstance<EnemyDataSO>();
        if (waveConfig == null) waveConfig = ScriptableObject.CreateInstance<WaveConfigSO>();
        if (slotConfig == null) slotConfig = ScriptableObject.CreateInstance<SlotMachineConfigSO>();
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
        InitializeWaveProgressTracking();
        InitializeEnemyKillProgressTracking();

        SetDefaultSlotSymbols();
        TryPlaceHero(1);
        RefreshUi();
        RefreshWaveUi();
    }

    private void Update()
    {
        if (gameEnded)
        {
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

        heroArea = CreatePanel("HeroField", battleZone, HeroFieldFallbackColor, new Vector2(0f, 0f), new Vector2(0.24f, 1f), Vector2.zero, new Vector2(0f, 1f));
        wallRect = CreatePanel("Wall", battleZone, WallFallbackColor, new Vector2(0.24f, 0f), new Vector2(0.32f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 1f));
        enemyArea = CreatePanel("EnemyField", battleZone, EnemyFieldFallbackColor, new Vector2(0.32f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 0f), Vector2.zero);
        heroAreaImage = heroArea.GetComponent<Image>();
        wallImage = wallRect.GetComponent<Image>();
        enemyAreaImage = enemyArea.GetComponent<Image>();
        battleEffectsLayer = CreatePanel("BattleEffectsLayer", battleZone, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BuildWallVisual();

        var topHud = CreatePanel("TopHud", battleZone, new Color(0f, 0f, 0f, 0.35f), new Vector2(0.03f, 0.93f), new Vector2(0.97f, 0.995f), Vector2.zero, Vector2.zero);
        waveProgressFill = CreateProgressBar(topHud, new Vector2(0.12f, 0.22f), new Vector2(0.98f, 0.78f), out waveProgressFrame);
        waveText = CreateText("WaveText", topHud, "Wave 0 / 0", 30, TextAnchor.MiddleCenter);
        waveText.rectTransform.anchorMin = new Vector2(0.12f, 0f);
        waveText.rectTransform.anchorMax = new Vector2(0.98f, 1f);

        wallHpFill = CreateProgressBar(wallHpZone, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.8f), out wallHpFrame);
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
        ApplyBarVisuals();
        BuildResultOverlay(canvasGo.transform);
        BuildCardOverlay(canvasGo.transform);
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
        heroUnitsLayer = CreatePanel("HeroUnitsLayer", heroArea, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
                heroSlotRects.Add(slot);
                slot.gameObject.AddComponent<LayoutElement>();
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(heroGridLayer);
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
            if (gameConfig.heroFieldSprite != null)
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
            if (gameConfig.enemyFieldSprite != null)
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

    private void ApplyBarVisuals()
    {
        ApplyBarSprites(
            wallHpFrame,
            wallHpFill,
            gameConfig.wallHpBarFrameSprite,
            gameConfig.wallHpBarFillSprite,
            new Color(0.2f, 0.2f, 0.2f),
            new Color(0.2f, 0.9f, 0.2f));

        ApplyBarSprites(
            waveProgressFrame,
            waveProgressFill,
            null,
            gameConfig.waveProgressFillSprite,
            new Color(0.2f, 0.2f, 0.2f),
            new Color(0.2f, 0.9f, 0.2f));
    }

    private static void ApplyBarSprites(Image frame, Image fill, Sprite frameSprite, Sprite fillSprite, Color frameFallbackColor, Color fillFallbackColor)
    {
        if (frame != null)
        {
            if (frameSprite != null)
            {
                frame.sprite = frameSprite;
                frame.type = Image.Type.Sliced;
                frame.color = Color.white;
            }
            else
            {
                frame.sprite = null;
                frame.type = Image.Type.Simple;
                frame.color = frameFallbackColor;
            }
        }

        if (fill != null)
        {
            if (fillSprite != null)
            {
                fill.sprite = fillSprite;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.color = Color.white;
            }
            else
            {
                fill.sprite = null;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.color = fillFallbackColor;
            }
        }
    }

    private void HandleWaveSpawning()
    {
        if (allWavesStarted)
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

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float y = Mathf.Lerp(minY, maxY, t);
            SpawnEnemy(y, waveSlot);
            yield return new WaitForSeconds(0.12f);
        }
    }

    private void SpawnEnemy(float y, int waveSlot)
    {
        RectTransform enemyRect = CreateUnitRect("Enemy", enemyArea, new Color(0.1f, 0.1f, 0.1f), gameConfig.enemyVisualSize, new Vector2(enemyArea.rect.width - gameConfig.enemySpawnRightMargin, y));
        if (enemyData.visualSprite != null)
        {
            enemyRect.GetComponent<Image>().sprite = enemyData.visualSprite;
            enemyRect.GetComponent<Image>().color = Color.white;
        }

        enemies.Add(new EnemyUnit
        {
            rect = enemyRect,
            hp = enemyData.hp,
            attackTimer = 0f,
            waveSlot = waveSlot
        });

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

            HeroLevelData lvl = heroData.GetLevel(hero.level);
            SpawnProjectile(hero, target, lvl.damage * heroDamageMultiplier);
            Debug.Log("[Battle] Hero fired projectile.");
            hero.cooldown = GetHeroAttackInterval(lvl);
        }
    }

    private float GetHeroAttackInterval(HeroLevelData levelData)
    {
        if (gameConfig.heroAttackIntervalOverride > 0f)
        {
            return gameConfig.heroAttackIntervalOverride;
        }

        float effectiveAttackSpeed = Mathf.Max(0.01f, levelData.attackSpeed * heroAttackSpeedMultiplier);
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
        ApplyProjectileVisual(projectileImage);
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

    private void ApplyProjectileVisual(Image projectileImage)
    {
        if (projectileImage == null)
        {
            return;
        }

        if (gameConfig != null && gameConfig.projectileSprite != null)
        {
            projectileImage.sprite = gameConfig.projectileSprite;
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
        SpawnFloatingDamage(contactWorldPosition + new Vector3(0f, gameConfig.floatingDamageOffsetY, 0f), damage);
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
        HeroLevelData lvl = heroData.GetLevel(hero.level);
        EnemyUnit best = null;
        float bestX = float.MaxValue;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit e = enemies[i];
            if (e.hp <= 0f)
            {
                continue;
            }

            Vector2 heroWorld = hero.rect.position;
            Vector2 enemyWorld = e.rect.position;
            float dist = Vector2.Distance(heroWorld, enemyWorld);
            if (dist > lvl.attackRange)
            {
                continue;
            }

            if (e.rect.position.x < bestX)
            {
                bestX = e.rect.position.x;
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

            if (e.hp <= 0f)
            {
                if (IsWaveSlotValid(e.waveSlot))
                {
                    waveAliveCounts[e.waveSlot] = Mathf.Max(0, waveAliveCounts[e.waveSlot] - 1);
                    TryMarkWaveCompleted(e.waveSlot);
                }

                killedEnemyCount = Mathf.Clamp(killedEnemyCount + 1, 0, totalLevelEnemyCount);
                coins += enemyData.killRewardCoins;
                Destroy(e.rect.gameObject);
                enemies.RemoveAt(i);
                RefreshUi();
                continue;
            }

            float enemyHalfWidth = GetRectHalfWidthWorld(e.rect);
            float contactCenterX = wallNearEdgeX + enemyHalfWidth;

            if (e.rect.position.x > contactCenterX)
            {
                Vector3 nextPosition = e.rect.position + Vector3.left * enemyData.moveSpeed * Time.deltaTime;
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
                    wallHp -= enemyData.damage;
                    Vector3 wallContactWorldPosition = new Vector3(contactCenterX, e.rect.position.y, e.rect.position.z);
                    SpawnWallDamageFloatingText(wallContactWorldPosition, enemyData.damage);
                    e.attackTimer = 1f / Mathf.Max(0.01f, enemyData.attackSpeed);
                    RefreshUi();
                }
            }
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
        wallHpFill.fillAmount = Mathf.Clamp01(wallHp / Mathf.Max(1f, gameConfig.wallMaxHp));
    }

    private void RefreshWaveUi()
    {
        int total = waveConfig.waves.Count;
        int activeWave = 0;
        if (total > 0)
        {
            int startedWave = Mathf.Clamp(currentWaveIndex, 0, total);
            activeWave = Mathf.Clamp(Mathf.Max(1, startedWave), 1, total);
            if (allWavesStarted)
            {
                activeWave = total;
            }
        }

        waveText.text = "Wave " + activeWave + " / " + total;
        waveProgressFill.fillAmount = totalLevelEnemyCount <= 0 ? 0f : (float)killedEnemyCount / totalLevelEnemyCount;
    }

    private int GetCurrentPullCost()
    {
        return slotConfig.basePullCost + (pullCount * slotConfig.pullCostStep);
    }

    private void OnPullPressed()
    {
        if (gameEnded || isSpinning || waitingCardChoice)
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

            TryPlaceHeroAtSlot(slotIndex, result.heroLevel);
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

    private bool TryPlaceHero(int level)
    {
        int slotIndex = GetRandomFreeHeroSlotIndex();
        if (slotIndex < 0)
        {
            return false;
        }
        return TryPlaceHeroAtSlot(slotIndex, level);
    }

    private bool TryPlaceHeroAtSlot(int slotIndex, int level)
    {
        if (heroes.Count >= MaxHeroes || slotIndex < 0 || slotIndex >= MaxHeroes || IsHeroSlotOccupied(slotIndex))
        {
            return false;
        }

        RectTransform targetSlot = heroSlotRects[slotIndex];
        if (targetSlot == null)
        {
            return false;
        }

        RectTransform heroRect = CreateUnitRect("Hero", targetSlot, new Color(0.95f, 0.9f, 0.2f), gameConfig.heroVisualSize, Vector2.zero);
        heroRect.anchorMin = new Vector2(0.5f, 0.5f);
        heroRect.anchorMax = new Vector2(0.5f, 0.5f);
        heroRect.anchoredPosition = Vector2.zero;
        heroRect.SetAsLastSibling();

        if (heroData.visualSprite != null)
        {
            heroRect.GetComponent<Image>().sprite = heroData.visualSprite;
            heroRect.GetComponent<Image>().color = Color.white;
        }

        Text levelText = null;
        if (level > 1)
        {
            levelText = CreateText("Lvl", heroRect, level.ToString(), 24, TextAnchor.LowerCenter);
            levelText.rectTransform.anchorMin = new Vector2(0, 1f);
            levelText.rectTransform.anchorMax = new Vector2(1f, 1.7f);
            levelText.color = Color.black;
        }

        heroes.Add(new HeroUnit
        {
            rect = heroRect,
            slotIndex = slotIndex,
            level = level,
            levelText = levelText,
            cooldown = 0f
        });

        return true;
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

        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = CardTextOutlineColor;
        outline.effectDistance = isTitle ? new Vector2(1.4f, -1.4f) : new Vector2(1.2f, -1.2f);

        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = CardTextShadowColor;
        shadow.effectDistance = new Vector2(0f, -1.4f);
    }

    private void BuildCardIcon(RectTransform iconBlock, CardChoiceData data)
    {
        RectTransform iconBackgroundRect = CreatePanel("IconBackground", iconBlock, CardIconBackgroundFallbackColor, new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.94f), Vector2.zero, Vector2.zero);
        Image iconBackgroundImage = iconBackgroundRect.GetComponent<Image>();
        if (slotConfig.bonusCardIconBackground != null)
        {
            iconBackgroundImage.sprite = slotConfig.bonusCardIconBackground;
            iconBackgroundImage.type = Image.Type.Simple;
            iconBackgroundImage.color = Color.white;
        }

        RectTransform iconRect = CreatePanel("EffectIcon", iconBackgroundRect, Color.clear, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Vector2.zero, Vector2.zero);
        Image iconImage = iconRect.GetComponent<Image>();
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
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return txt;
    }

    private static Image CreateProgressBar(Transform parent, Vector2 anchorMin, Vector2 anchorMax, out Image frameImage)
    {
        RectTransform bg = CreatePanel("ProgressBg", parent, new Color(0.2f, 0.2f, 0.2f), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        frameImage = bg.GetComponent<Image>();
        RectTransform fill = CreatePanel("Fill", bg, new Color(0.2f, 0.9f, 0.2f), new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        fill.pivot = new Vector2(0f, 0.5f);
        Image img = fill.GetComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 0f;
        return img;
    }

    private class HeroUnit
    {
        public RectTransform rect;
        public int slotIndex;
        public int level;
        public float cooldown;
        public Text levelText;
    }

    private class EnemyUnit
    {
        public RectTransform rect;
        public float hp;
        public float attackTimer;
        public int waveSlot;
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
}

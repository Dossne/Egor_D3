using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleBootstrap : MonoBehaviour
{
    private const float DebugHeroAttackInterval = 1f;
    private const float DebugProjectileSize = 40f;
    private const float DebugProjectileSpeed = 400f;
    private static readonly Color DebugProjectileColor = new Color(1f, 0.95f, 0.1f, 1f);

    private const int HeroRows = 7;
    private const int HeroCols = 3;
    private const int MaxHeroes = HeroRows * HeroCols;
    private const float HeroGridSpacing = 6f;
    private const float HeroGridPadding = 8f;

    private GameConfigSO gameConfig;
    private HeroDataSO heroData;
    private EnemyDataSO enemyData;
    private WaveConfigSO waveConfig;
    private SlotMachineConfigSO slotConfig;

    private readonly List<HeroUnit> heroes = new List<HeroUnit>();
    private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();
    private readonly List<Vector2> heroSlotPositions = new List<Vector2>();
    private readonly List<ProjectileView> activeProjectiles = new List<ProjectileView>();
    private readonly List<FloatingDamageView> activeFloatingDamage = new List<FloatingDamageView>();

    private RectTransform enemyArea;
    private RectTransform heroArea;
    private RectTransform heroGridLayer;
    private RectTransform heroUnitsLayer;
    private RectTransform battleEffectsLayer;
    private RectTransform wallRect;

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
    private float nextWaveStartTime;
    private bool allWavesStarted;

    private float heroDamageMultiplier = 1f;
    private float heroAttackSpeedMultiplier = 1f;

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

        for (int i = 0; i < heroData.levels.Count; i++)
        {
            heroData.levels[i].attackSpeed = 1f;
        }
    }

    private void SetupGame()
    {
        coins = gameConfig.startingCoins;
        wallHp = gameConfig.wallMaxHp;
        currentWaveIndex = 0;
        nextWaveStartTime = Time.time + 0.2f;
        allWavesStarted = false;
        pullCount = 0;

        SetDefaultSlotSymbols();
        TryPlaceHero(1);
        RefreshUi();
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
        RectTransform bottomZone = CreatePanel("BottomUi", bg.transform, new Color(0.16f, 0.2f, 0.2f), new Vector2(0, 0), new Vector2(1, 0.29f), Vector2.zero, Vector2.zero);

        heroArea = CreatePanel("HeroField", battleZone, new Color(0.27f, 0.48f, 0.72f), new Vector2(0.03f, 0.04f), new Vector2(0.45f, 0.92f), Vector2.zero, Vector2.zero);
        wallRect = CreatePanel("Wall", battleZone, new Color(0.55f, 0.5f, 0.43f), new Vector2(0.46f, 0.04f), new Vector2(0.54f, 0.92f), Vector2.zero, Vector2.zero);
        enemyArea = CreatePanel("EnemyField", battleZone, new Color(0.62f, 0.33f, 0.33f), new Vector2(0.55f, 0.04f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);
        battleEffectsLayer = CreatePanel("BattleEffectsLayer", battleZone, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var topHud = CreatePanel("TopHud", battleZone, new Color(0f, 0f, 0f, 0.35f), new Vector2(0.03f, 0.93f), new Vector2(0.97f, 0.995f), Vector2.zero, Vector2.zero);
        waveText = CreateText("WaveText", topHud, "Wave 0 / 0", 30, TextAnchor.MiddleLeft);
        waveText.rectTransform.anchorMin = new Vector2(0.02f, 0);
        waveText.rectTransform.anchorMax = new Vector2(0.4f, 1);
        waveProgressFill = CreateProgressBar(topHud, new Vector2(0.42f, 0.22f), new Vector2(0.98f, 0.78f));

        wallHpFill = CreateProgressBar(wallHpZone, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.8f));
        wallHpText = CreateText("WallHpText", wallHpZone, "0 / 0", 34, TextAnchor.MiddleCenter);

        coinsText = CreateText("CoinsText", bottomZone, "Coins: 0", 34, TextAnchor.MiddleLeft);
        coinsText.rectTransform.anchorMin = new Vector2(0.04f, 0.78f);
        coinsText.rectTransform.anchorMax = new Vector2(0.45f, 0.96f);

        heroCountText = CreateText("HeroCountText", bottomZone, "Heroes: 0 / 21", 34, TextAnchor.MiddleRight);
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
        battleEffectsLayer.SetAsLastSibling();
        BuildHeroSlots();
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
        GridLayoutGroup grid = heroGridLayer.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = HeroCols;
        grid.spacing = new Vector2(HeroGridSpacing, HeroGridSpacing);
        grid.padding = new RectOffset((int)HeroGridPadding, (int)HeroGridPadding, (int)HeroGridPadding, (int)HeroGridPadding);

        float width = heroGridLayer.rect.width <= 0 ? 300f : heroGridLayer.rect.width;
        float height = heroGridLayer.rect.height <= 0 ? 550f : heroGridLayer.rect.height;
        grid.cellSize = new Vector2((width - (HeroGridPadding * 2f) - (grid.spacing.x * (HeroCols - 1))) / HeroCols, (height - (HeroGridPadding * 2f) - (grid.spacing.y * (HeroRows - 1))) / HeroRows);

        for (int r = 0; r < HeroRows; r++)
        {
            for (int c = 0; c < HeroCols; c++)
            {
                RectTransform slot = CreatePanel("HeroSlot_" + r + "_" + c, heroGridLayer, new Color(1f, 1f, 1f, 0.22f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                heroSlotPositions.Add(new Vector2(c, r));
                slot.gameObject.AddComponent<LayoutElement>();
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

        WaveEntry wave = waveConfig.waves[currentWaveIndex];
        StartCoroutine(SpawnWave(wave));
        currentWaveIndex++;
        nextWaveStartTime = Time.time + waveConfig.waveIntervalSec;
        if (currentWaveIndex >= waveConfig.waves.Count)
        {
            allWavesStarted = true;
        }
    }

    private IEnumerator SpawnWave(WaveEntry wave)
    {
        int count = Mathf.Max(1, wave.enemyCount);
        float minY = 25f;
        float maxY = Mathf.Max(minY + 1f, enemyArea.rect.height - 25f);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float y = Mathf.Lerp(minY, maxY, t);
            SpawnEnemy(y);
            yield return new WaitForSeconds(0.12f);
        }
    }

    private void SpawnEnemy(float y)
    {
        RectTransform enemyRect = CreateUnitRect("Enemy", enemyArea, new Color(0.1f, 0.1f, 0.1f), 42f, new Vector2(enemyArea.rect.width - 30f, y));
        if (enemyData.visualSprite != null)
        {
            enemyRect.GetComponent<Image>().sprite = enemyData.visualSprite;
            enemyRect.GetComponent<Image>().color = Color.white;
        }

        enemies.Add(new EnemyUnit
        {
            rect = enemyRect,
            hp = enemyData.hp,
            attackTimer = 0f
        });
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
                continue;
            }

            HeroLevelData lvl = heroData.GetLevel(hero.level);
            SpawnProjectile(hero, target, lvl.damage * heroDamageMultiplier);
            hero.cooldown = DebugHeroAttackInterval;
        }
    }

    private void SpawnProjectile(HeroUnit hero, EnemyUnit target, float damage)
    {
        if (hero == null || hero.rect == null || target == null || target.rect == null || target.hp <= 0f)
        {
            return;
        }

        Vector2 startPosition = ToEffectsLocalPoint(hero.rect.position);
        battleEffectsLayer.SetAsLastSibling();
        RectTransform projectileRect = CreateEffectRect("Projectile", battleEffectsLayer, DebugProjectileColor, DebugProjectileSize, startPosition);
        projectileRect.SetAsLastSibling();

        Debug.Log("[Battle] Projectile spawned at " + startPosition + ".");

        activeProjectiles.Add(new ProjectileView
        {
            rect = projectileRect,
            target = target,
            damage = damage,
            speed = DebugProjectileSpeed
        });
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
        damageRect.sizeDelta = new Vector2(140f, 56f);
        damageRect.anchoredPosition = ToEffectsLocalPoint(targetWorldPosition) + new Vector2(0f, 48f);
        damageRect.SetAsLastSibling();

        activeFloatingDamage.Add(new FloatingDamageView
        {
            text = damageText,
            velocity = new Vector2(0f, 110f),
            age = 0f,
            lifetime = 0.45f
        });
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
        float wallX = wallRect.position.x;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit e = enemies[i];

            if (e.hp <= 0f)
            {
                coins += enemyData.killRewardCoins;
                Destroy(e.rect.gameObject);
                enemies.RemoveAt(i);
                RefreshUi();
                continue;
            }

            if (e.rect.position.x > wallX + 10f)
            {
                e.rect.anchoredPosition += Vector2.left * enemyData.moveSpeed * Time.deltaTime;
            }
            else
            {
                e.attackTimer -= Time.deltaTime;
                if (e.attackTimer <= 0f)
                {
                    wallHp -= enemyData.damage;
                    e.attackTimer = 1f / Mathf.Max(0.01f, enemyData.attackSpeed);
                    RefreshUi();
                }
            }
        }
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
        int current = Mathf.Clamp(currentWaveIndex, 0, total);
        waveText.text = "Wave " + current + " / " + total;
        waveProgressFill.fillAmount = total == 0 ? 0f : (float)current / total;
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

        ResolveReward(result);
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

    private void ResolveReward(SlotResultData result)
    {
        if (result.rewardType == RewardType.Hero)
        {
            bool placed = TryPlaceHero(result.heroLevel);
            if (!placed)
            {
                coins += slotConfig.heroOverflowCompensationCoins;
                ShowFeedback("Hero field full: +" + slotConfig.heroOverflowCompensationCoins + " coins");
            }
            return;
        }

        if (result.rewardType == RewardType.Coins)
        {
            coins += result.coinReward;
            ShowFeedback("+" + result.coinReward + " coins");
            return;
        }

        if (result.rewardType == RewardType.Card)
        {
            ShowCardChoice(result.cardTier);
        }
    }

    private bool TryPlaceHero(int level)
    {
        if (heroes.Count >= MaxHeroes)
        {
            return false;
        }

        int slotIndex = heroes.Count;
        float width = heroUnitsLayer.rect.width <= 0 ? 300f : heroUnitsLayer.rect.width;
        float height = heroUnitsLayer.rect.height <= 0 ? 550f : heroUnitsLayer.rect.height;
        float colWidth = (width - (HeroGridPadding * 2f) - (HeroGridSpacing * (HeroCols - 1))) / HeroCols;
        float rowHeight = (height - (HeroGridPadding * 2f) - (HeroGridSpacing * (HeroRows - 1))) / HeroRows;
        int row = slotIndex / HeroCols;
        int col = slotIndex % HeroCols;

        float x = HeroGridPadding + (col * (colWidth + HeroGridSpacing)) + (colWidth * 0.5f);
        float y = height - HeroGridPadding - (row * (rowHeight + HeroGridSpacing)) - (rowHeight * 0.5f);
        RectTransform heroRect = CreateUnitRect("Hero", heroUnitsLayer, new Color(0.95f, 0.9f, 0.2f), 36f, new Vector2(x, y));
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
            level = level,
            levelText = levelText,
            cooldown = 0f
        });

        return true;
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

        CreateCardButton(0, "+" + values.damagePercent + "% Damage", () => { heroDamageMultiplier *= 1f + (values.damagePercent / 100f); CloseCardChoice(); });
        CreateCardButton(1, "+" + values.attackSpeedPercent + "% Attack Speed", () => { heroAttackSpeedMultiplier *= 1f + (values.attackSpeedPercent / 100f); CloseCardChoice(); });
        CreateCardButton(2, "+" + values.wallHeal + " Wall HP", () => { wallHp = Mathf.Min(gameConfig.wallMaxHp, wallHp + values.wallHeal); CloseCardChoice(); RefreshUi(); });
    }

    private void CreateCardButton(int index, string label, UnityEngine.Events.UnityAction action)
    {
        float xMin = 0.1f + index * 0.3f;
        float xMax = 0.36f + index * 0.3f;
        RectTransform card = CreatePanel("Card" + index, cardOverlay.transform, new Color(1f, 1f, 1f), new Vector2(xMin, 0.35f), new Vector2(xMax, 0.7f), Vector2.zero, Vector2.zero);
        CreateText("CardText", card, label, 28, TextAnchor.MiddleCenter);
        Button btn = card.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(action);
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

    private static Image CreateProgressBar(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform bg = CreatePanel("ProgressBg", parent, new Color(0.2f, 0.2f, 0.2f), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        RectTransform fill = CreatePanel("Fill", bg, new Color(0.2f, 0.9f, 0.2f), new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        fill.pivot = new Vector2(0f, 0.5f);
        Image img = fill.GetComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 1f;
        return img;
    }

    private class HeroUnit
    {
        public RectTransform rect;
        public int level;
        public float cooldown;
        public Text levelText;
    }

    private class EnemyUnit
    {
        public RectTransform rect;
        public float hp;
        public float attackTimer;
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
}

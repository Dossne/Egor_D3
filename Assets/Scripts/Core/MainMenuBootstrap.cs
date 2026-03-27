using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuBootstrap : MonoBehaviour
{
    private const string ShopTabName = "Shop";
    private const string FightTabName = "Fight";
    private const string CardsTabName = "Cards";

    private MainMenuConfigSO menuConfig;
    private ChapterPresentationConfigSO chapterConfig;
    private EnemyDataSO enemyData;

    private RectTransform enemyPreviewViewport;
    private readonly List<Image> basicEnemyImages = new List<Image>();
    private readonly List<float> basicEnemyLaneYs = new List<float>();
    private Image bossEnemyImage;
    private float nextBossSpawnTime;
    private Sprite roundedCrystalPanelSprite;

    private Text comingSoonText;
    private RectTransform mainMenuRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        SceneManager.sceneLoaded -= EnsureBootstrapForScene;
        SceneManager.sceneLoaded += EnsureBootstrapForScene;
        EnsureBootstrapForScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void EnsureBootstrapForScene(Scene scene, LoadSceneMode mode)
    {
        if (!IsMainMenuScene(scene.name))
        {
            return;
        }

        MainMenuBootstrap existing = FindObjectOfType<MainMenuBootstrap>();
        if (existing != null)
        {
            return;
        }

        GameObject go = new GameObject("MainMenuBootstrap");
        go.AddComponent<MainMenuBootstrap>();
    }

    private static bool IsMainMenuScene(string sceneName)
    {
        MainMenuConfigSO config = Resources.Load<MainMenuConfigSO>("Configs/MainMenuConfig");
        string targetSceneName = config != null && !string.IsNullOrEmpty(config.mainMenuSceneName)
            ? config.mainMenuSceneName
            : "MainMenuScene";

        return sceneName == targetSceneName;
    }

    private void Start()
    {
        LoadConfigs();
        BuildUi();
    }

    private void Update()
    {
        UpdateEnemyPreview();
    }

    private void LoadConfigs()
    {
        menuConfig = Resources.Load<MainMenuConfigSO>("Configs/MainMenuConfig");
        chapterConfig = Resources.Load<ChapterPresentationConfigSO>("Configs/ChapterPresentationConfig");
        enemyData = Resources.Load<EnemyDataSO>("Configs/EnemyData");

        if (menuConfig == null) menuConfig = ScriptableObject.CreateInstance<MainMenuConfigSO>();
        if (chapterConfig == null) chapterConfig = ScriptableObject.CreateInstance<ChapterPresentationConfigSO>();
        if (enemyData == null) enemyData = ScriptableObject.CreateInstance<EnemyDataSO>();
    }

    private void BuildUi()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystem = eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        Canvas canvas = new GameObject("MainMenuCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = canvas.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        mainMenuRoot = root;

        Image background = CreateImage("Background", root, new Color(0.13f, 0.19f, 0.26f));
        if (menuConfig.sceneBackgroundSprite != null)
        {
            background.sprite = menuConfig.sceneBackgroundSprite;
            background.color = Color.white;
        }
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        BuildCrystalCounter(root);
        BuildChapterAndLevel(root);
        BuildEnemyPreview(root);
        BuildPlayButton(root);
        BuildBottomTabs(root);
        BuildComingSoon(root);
    }

    private void BuildCrystalCounter(RectTransform root)
    {
        if (roundedCrystalPanelSprite == null)
        {
            roundedCrystalPanelSprite = CreateRoundedRectSprite(64, 64, 16);
        }

        Image panel = CreateImage("CrystalPanel", root, Color.black);
        panel.sprite = roundedCrystalPanelSprite;
        panel.type = Image.Type.Sliced;
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(184f, 52f);
        rect.anchoredPosition = new Vector2(-22f, -22f);

        Image icon = CreateImage("CrystalIcon", rect, Color.white);
        icon.sprite = menuConfig.crystalIconSprite;
        icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        icon.rectTransform.pivot = new Vector2(0f, 0.5f);
        icon.rectTransform.anchoredPosition = new Vector2(10f, 0f);
        icon.rectTransform.sizeDelta = new Vector2(30f, 30f);

        Text amount = CreateText("CrystalAmount", rect, menuConfig.crystalAmount.ToString(), 26, TextAnchor.MiddleRight, Color.white);
        RectTransform amountRect = amount.rectTransform;
        amountRect.anchorMin = new Vector2(0f, 0f);
        amountRect.anchorMax = new Vector2(1f, 1f);
        amountRect.offsetMin = new Vector2(50f, 0f);
        amountRect.offsetMax = new Vector2(-12f, 0f);
    }

    private void BuildChapterAndLevel(RectTransform root)
    {
        ChapterPresentationEntry chapterEntry = chapterConfig.GetChapterByNumber(menuConfig.currentChapterNumber);
        string chapterTitle = $"Chapter {chapterEntry.chapterNumber}";

        Text chapterText = CreateText("ChapterText", root, chapterTitle, 52, TextAnchor.UpperCenter, Color.white);
        RectTransform chapterRect = chapterText.rectTransform;
        chapterRect.anchorMin = new Vector2(0.5f, 1f);
        chapterRect.anchorMax = new Vector2(0.5f, 1f);
        chapterRect.pivot = new Vector2(0.5f, 1f);
        chapterRect.sizeDelta = new Vector2(560f, 84f);
        chapterRect.anchoredPosition = new Vector2(0f, -176f);

        Text levelName = CreateText("LevelNameText", root, chapterEntry.levelDisplayName, 36, TextAnchor.UpperCenter, new Color(0.9f, 0.9f, 0.9f));
        RectTransform levelRect = levelName.rectTransform;
        levelRect.anchorMin = new Vector2(0.5f, 1f);
        levelRect.anchorMax = new Vector2(0.5f, 1f);
        levelRect.pivot = new Vector2(0.5f, 1f);
        levelRect.sizeDelta = new Vector2(640f, 58f);
        levelRect.anchoredPosition = new Vector2(0f, -238f);
    }

    private void BuildEnemyPreview(RectTransform root)
    {
        Image viewport = CreateImage("EnemyPreviewViewport", root, new Color(0.2f, 0.16f, 0.14f));
        if (menuConfig.enemyPreviewBackgroundSprite != null)
        {
            viewport.sprite = menuConfig.enemyPreviewBackgroundSprite;
            viewport.color = Color.white;
        }
        enemyPreviewViewport = viewport.rectTransform;
        enemyPreviewViewport.anchorMin = new Vector2(0f, 0.36f);
        enemyPreviewViewport.anchorMax = new Vector2(1f, 0.61f);
        enemyPreviewViewport.offsetMin = Vector2.zero;
        enemyPreviewViewport.offsetMax = Vector2.zero;
        enemyPreviewViewport.gameObject.AddComponent<RectMask2D>();

        EnemyDefinition basicEnemy = enemyData.GetEnemyById("enemy_basic");
        int basicCount = Mathf.Max(1, menuConfig.basicPreviewCount);
        float width = enemyPreviewViewport.rect.width > 0f ? enemyPreviewViewport.rect.width : 700f;
        float height = enemyPreviewViewport.rect.height > 0f ? enemyPreviewViewport.rect.height : 200f;
        int laneCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(basicCount)), 2, 4);
        float topLaneY = (height * 0.5f) - 24f;
        float bottomLaneY = (-height * 0.5f) + 24f;

        basicEnemyLaneYs.Clear();

        for (int i = 0; i < basicCount; i++)
        {
            Image enemyImage = CreateImage($"BasicEnemy_{i}", enemyPreviewViewport, Color.white);
            enemyImage.sprite = basicEnemy != null ? basicEnemy.visualSprite : null;
            float size = Mathf.Clamp(basicEnemy.visualSize, 38f, 92f);
            enemyImage.rectTransform.sizeDelta = new Vector2(size, size);
            float normalized = basicCount <= 1 ? 0f : (float)i / (basicCount - 1);
            int laneIndex = laneCount <= 1 ? 0 : i % laneCount;
            float laneT = laneCount <= 1 ? 0.5f : laneIndex / (float)(laneCount - 1);
            float laneY = Mathf.Lerp(topLaneY, bottomLaneY, laneT);
            basicEnemyLaneYs.Add(laneY);
            enemyImage.rectTransform.anchoredPosition = new Vector2(width * (1f - normalized) - 120f, laneY);
            basicEnemyImages.Add(enemyImage);
        }

        nextBossSpawnTime = Time.time + Mathf.Max(0.25f, menuConfig.bossSpawnIntervalSec);
    }

    private void BuildPlayButton(RectTransform root)
    {
        Image buttonImage = CreateImage("PlayButton", root, Color.white * menuConfig.playButtonDarken);
        buttonImage.sprite = menuConfig.playButtonSprite;

        Button playButton = buttonImage.gameObject.AddComponent<Button>();
        playButton.targetGraphic = buttonImage;
        playButton.onClick.AddListener(OnPlayPressed);

        RectTransform rect = buttonImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.24f);
        rect.anchorMax = new Vector2(0.5f, 0.24f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320f, 116f);
        rect.anchoredPosition = Vector2.zero;

        Text text = CreateText("PlayText", rect, "Play", 38, TextAnchor.MiddleCenter, Color.white);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
    }

    private void BuildBottomTabs(RectTransform root)
    {
        Image bar = CreateImage("BottomBar", root, new Color(0.08f, 0.08f, 0.08f, 0.95f));
        RectTransform barRect = bar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(0f, 156f);
        barRect.anchoredPosition = Vector2.zero;

        RectTransform[] tabRoots = new RectTransform[3];
        for (int i = 0; i < tabRoots.Length; i++)
        {
            Image tabBlock = CreateImage($"Tab_{i}", barRect, new Color(0.15f, 0.15f, 0.15f));
            RectTransform tabRect = tabBlock.rectTransform;
            tabRect.anchorMin = new Vector2(i / 3f, 0f);
            tabRect.anchorMax = new Vector2((i + 1) / 3f, 1f);
            tabRect.offsetMin = new Vector2(6f, 10f);
            tabRect.offsetMax = new Vector2(-6f, -10f);
            tabRoots[i] = tabRect;
        }

        BuildTab(tabRoots[0], ShopTabName, menuConfig.shopIconSprite, false, true);
        BuildTab(tabRoots[1], FightTabName, menuConfig.fightIconSprite, true, false);
        BuildTab(tabRoots[2], CardsTabName, menuConfig.cardsIconSprite, false, true);
    }

    private void BuildTab(RectTransform parent, string label, Sprite iconSprite, bool isSelected, bool isLocked)
    {
        Color baseColor = isSelected ? new Color(0.22f, 0.3f, 0.22f) : new Color(0.14f, 0.14f, 0.14f);
        Image bg = parent.GetComponent<Image>();
        bg.color = isLocked ? baseColor * 0.7f : baseColor;

        Button button = parent.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        if (isLocked)
        {
            button.onClick.AddListener(() => ShowComingSoon(parent));
        }

        Image icon = CreateImage($"{label}Icon", parent, isLocked ? Color.gray : Color.white);
        icon.sprite = iconSprite;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        float baseIconSize = 66f;
        float selectedScale = 1.3f;
        float iconSize = isSelected ? baseIconSize * selectedScale : baseIconSize;
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = new Vector2(0f, isSelected ? 20f : 10f);

        int baseLabelSize = 24;
        int selectedLabelSize = Mathf.RoundToInt(baseLabelSize * 1.3f);
        Text tabLabel = CreateText($"{label}Label", parent, label, isSelected ? selectedLabelSize : baseLabelSize, TextAnchor.LowerCenter, isLocked ? new Color(0.7f, 0.7f, 0.7f) : Color.white);
        RectTransform labelRect = tabLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 36f);
        labelRect.anchoredPosition = new Vector2(0f, isSelected ? 12f : 8f);

        if (isLocked)
        {
            Image lockOverlay = CreateImage($"{label}Lock", parent, Color.white);
            lockOverlay.sprite = menuConfig.lockIconSprite;
            RectTransform lockRect = lockOverlay.rectTransform;
            lockRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.pivot = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(26f, 26f);
            lockRect.anchoredPosition = new Vector2(28f, 34f);
        }
    }

    private void BuildComingSoon(RectTransform root)
    {
        comingSoonText = CreateText("ComingSoonText", root, string.Empty, 36, TextAnchor.MiddleCenter, Color.white);
        RectTransform rect = comingSoonText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 68f);
        rect.anchoredPosition = new Vector2(0f, -44f);
        comingSoonText.gameObject.SetActive(false);
    }

    private void UpdateEnemyPreview()
    {
        if (enemyPreviewViewport == null)
        {
            return;
        }

        float leftBound = -enemyPreviewViewport.rect.width * 0.5f;
        float rightBound = enemyPreviewViewport.rect.width * 0.5f;

        for (int i = 0; i < basicEnemyImages.Count; i++)
        {
            RectTransform basicRect = basicEnemyImages[i].rectTransform;
            basicRect.anchoredPosition += Vector2.left * menuConfig.previewBasicMoveSpeed * Time.deltaTime;
            float offscreenX = leftBound - basicRect.sizeDelta.x;
            if (basicRect.anchoredPosition.x < offscreenX)
            {
                float laneY = i < basicEnemyLaneYs.Count ? basicEnemyLaneYs[i] : 0f;
                basicRect.anchoredPosition = new Vector2(rightBound + basicRect.sizeDelta.x + Random.Range(30f, 120f), laneY);
            }
        }

        if (bossEnemyImage == null && Time.time >= nextBossSpawnTime)
        {
            SpawnBossPreview(rightBound);
        }

        if (bossEnemyImage != null)
        {
            RectTransform bossRect = bossEnemyImage.rectTransform;
            bossRect.anchoredPosition += Vector2.left * menuConfig.previewBossMoveSpeed * Time.deltaTime;
            if (bossRect.anchoredPosition.x < leftBound - bossRect.sizeDelta.x)
            {
                Destroy(bossEnemyImage.gameObject);
                bossEnemyImage = null;
                nextBossSpawnTime = Time.time + Mathf.Max(0.25f, menuConfig.bossRespawnCooldownSec);
            }
        }
    }

    private void SpawnBossPreview(float rightBound)
    {
        EnemyDefinition boss = enemyData.GetEnemyById("enemy_boss");
        bossEnemyImage = CreateImage("BossPreview", enemyPreviewViewport, Color.white);
        bossEnemyImage.sprite = boss != null ? boss.visualSprite : null;
        float size = Mathf.Clamp(boss.visualSize * 0.5f, 88f, 180f);
        bossEnemyImage.rectTransform.sizeDelta = new Vector2(size, size);
        bossEnemyImage.rectTransform.anchoredPosition = new Vector2(rightBound + size, 8f);
    }

    private void ShowComingSoon(RectTransform tabRoot)
    {
        StopAllCoroutines();
        StartCoroutine(ShowComingSoonRoutine(tabRoot));
    }

    private IEnumerator ShowComingSoonRoutine(RectTransform tabRoot)
    {
        Vector2 anchoredPosition = new Vector2(0f, -44f);
        if (mainMenuRoot != null && tabRoot != null)
        {
            Vector3 tabTopWorld = tabRoot.TransformPoint(new Vector3(0f, tabRoot.rect.yMax, 0f));
            Vector2 tabTopScreen = RectTransformUtility.WorldToScreenPoint(null, tabTopWorld);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mainMenuRoot, tabTopScreen, null, out Vector2 localPoint))
            {
                anchoredPosition = localPoint + new Vector2(0f, 42f);
            }
        }

        comingSoonText.rectTransform.anchoredPosition = anchoredPosition;
        comingSoonText.gameObject.SetActive(true);
        comingSoonText.text = "Coming soon";

        Color color = comingSoonText.color;
        color.a = 1f;
        comingSoonText.color = color;

        float holdDuration = 0.6f;
        float fadeDuration = 0.45f;

        yield return new WaitForSeconds(holdDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            color.a = 1f - t;
            comingSoonText.color = color;
            yield return null;
        }

        comingSoonText.gameObject.SetActive(false);
    }

    private void OnPlayPressed()
    {
        GameConfigSO gameConfig = Resources.Load<GameConfigSO>("Configs/GameConfig");
        string sceneName = !string.IsNullOrEmpty(menuConfig.battleSceneName)
            ? menuConfig.battleSceneName
            : (gameConfig != null && !string.IsNullOrEmpty(gameConfig.gameplaySceneName)
                ? gameConfig.gameplaySceneName
                : "SampleScene");

        SceneManager.LoadScene(sceneName);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        int clampedRadius = Mathf.Clamp(radius, 0, Mathf.Min(width, height) / 2);
        int innerLeft = clampedRadius;
        int innerRight = width - clampedRadius - 1;
        int innerBottom = clampedRadius;
        int innerTop = height - clampedRadius - 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inHorizontalBand = x >= innerLeft && x <= innerRight;
                bool inVerticalBand = y >= innerBottom && y <= innerTop;
                bool inCorner = false;

                if (!inHorizontalBand && !inVerticalBand)
                {
                    int cornerX = x < innerLeft ? innerLeft : innerRight;
                    int cornerY = y < innerBottom ? innerBottom : innerTop;
                    float dx = x - cornerX;
                    float dy = y - cornerY;
                    inCorner = (dx * dx + dy * dy) <= clampedRadius * clampedRadius;
                }

                texture.SetPixel(x, y, (inHorizontalBand || inVerticalBand || inCorner) ? fill : clear);
            }
        }

        texture.Apply();

        Rect rect = new Rect(0f, 0f, width, height);
        Vector4 border = new Vector4(clampedRadius, clampedRadius, clampedRadius, clampedRadius);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}

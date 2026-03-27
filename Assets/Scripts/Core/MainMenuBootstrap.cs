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
    private Image bossEnemyImage;
    private float nextBossSpawnTime;

    private Text comingSoonText;

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

        Image background = CreateImage("Background", root, new Color(0.13f, 0.19f, 0.26f));
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
        Image panel = CreateImage("CrystalPanel", root, Color.black);
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(220f, 64f);
        rect.anchoredPosition = new Vector2(-22f, -22f);

        Image icon = CreateImage("CrystalIcon", rect, Color.white);
        icon.sprite = menuConfig.crystalIconSprite;
        icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        icon.rectTransform.pivot = new Vector2(0f, 0.5f);
        icon.rectTransform.anchoredPosition = new Vector2(14f, 0f);
        icon.rectTransform.sizeDelta = new Vector2(36f, 36f);

        Text amount = CreateText("CrystalAmount", rect, menuConfig.crystalAmount.ToString(), 28, TextAnchor.MiddleLeft, Color.white);
        RectTransform amountRect = amount.rectTransform;
        amountRect.anchorMin = new Vector2(0f, 0f);
        amountRect.anchorMax = new Vector2(1f, 1f);
        amountRect.offsetMin = new Vector2(60f, 0f);
        amountRect.offsetMax = new Vector2(-10f, 0f);
    }

    private void BuildChapterAndLevel(RectTransform root)
    {
        ChapterPresentationEntry chapterEntry = chapterConfig.GetChapterByNumber(menuConfig.currentChapterNumber);
        string chapterTitle = $"Chapter {chapterEntry.chapterNumber}";

        Text chapterText = CreateText("ChapterText", root, chapterTitle, 42, TextAnchor.UpperLeft, Color.white);
        RectTransform chapterRect = chapterText.rectTransform;
        chapterRect.anchorMin = new Vector2(0f, 1f);
        chapterRect.anchorMax = new Vector2(0f, 1f);
        chapterRect.pivot = new Vector2(0f, 1f);
        chapterRect.sizeDelta = new Vector2(460f, 70f);
        chapterRect.anchoredPosition = new Vector2(24f, -20f);

        Text levelName = CreateText("LevelNameText", root, chapterEntry.levelDisplayName, 28, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.9f));
        RectTransform levelRect = levelName.rectTransform;
        levelRect.anchorMin = new Vector2(0f, 1f);
        levelRect.anchorMax = new Vector2(0f, 1f);
        levelRect.pivot = new Vector2(0f, 1f);
        levelRect.sizeDelta = new Vector2(560f, 48f);
        levelRect.anchoredPosition = new Vector2(26f, -76f);
    }

    private void BuildEnemyPreview(RectTransform root)
    {
        Image previewFrame = CreateImage("EnemyPreviewFrame", root, new Color(0.08f, 0.11f, 0.14f, 0.95f));
        RectTransform frameRect = previewFrame.rectTransform;
        frameRect.anchorMin = new Vector2(0.08f, 0.29f);
        frameRect.anchorMax = new Vector2(0.92f, 0.72f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image viewport = CreateImage("EnemyPreviewViewport", frameRect, new Color(0.2f, 0.16f, 0.14f));
        enemyPreviewViewport = viewport.rectTransform;
        enemyPreviewViewport.anchorMin = new Vector2(0.03f, 0.08f);
        enemyPreviewViewport.anchorMax = new Vector2(0.97f, 0.92f);
        enemyPreviewViewport.offsetMin = Vector2.zero;
        enemyPreviewViewport.offsetMax = Vector2.zero;
        enemyPreviewViewport.gameObject.AddComponent<RectMask2D>();

        EnemyDefinition basicEnemy = enemyData.GetEnemyById("enemy_basic");
        int basicCount = Mathf.Max(1, menuConfig.basicPreviewCount);
        float width = enemyPreviewViewport.rect.width > 0f ? enemyPreviewViewport.rect.width : 700f;

        for (int i = 0; i < basicCount; i++)
        {
            Image enemyImage = CreateImage($"BasicEnemy_{i}", enemyPreviewViewport, Color.white);
            enemyImage.sprite = basicEnemy != null ? basicEnemy.visualSprite : null;
            float size = Mathf.Clamp(basicEnemy.visualSize, 38f, 92f);
            enemyImage.rectTransform.sizeDelta = new Vector2(size, size);
            float normalized = basicCount <= 1 ? 0f : (float)i / (basicCount - 1);
            enemyImage.rectTransform.anchoredPosition = new Vector2(width * (1f - normalized) - 120f, 0f);
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
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(270f, 96f);
        rect.anchoredPosition = new Vector2(0f, 126f);

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
        barRect.sizeDelta = new Vector2(0f, 120f);
        barRect.anchoredPosition = Vector2.zero;

        RectTransform[] tabRoots = new RectTransform[3];
        for (int i = 0; i < tabRoots.Length; i++)
        {
            Image tabBlock = CreateImage($"Tab_{i}", barRect, new Color(0.15f, 0.15f, 0.15f));
            RectTransform tabRect = tabBlock.rectTransform;
            tabRect.anchorMin = new Vector2(i / 3f, 0f);
            tabRect.anchorMax = new Vector2((i + 1) / 3f, 1f);
            tabRect.offsetMin = new Vector2(6f, 8f);
            tabRect.offsetMax = new Vector2(-6f, -8f);
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
            button.onClick.AddListener(() => ShowComingSoon());
        }

        Image icon = CreateImage($"{label}Icon", parent, isLocked ? Color.gray : Color.white);
        icon.sprite = iconSprite;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(56f, 56f);
        iconRect.anchoredPosition = new Vector2(0f, isSelected ? 18f : 6f);

        Text tabLabel = CreateText($"{label}Label", parent, label, 24, TextAnchor.LowerCenter, isLocked ? new Color(0.7f, 0.7f, 0.7f) : Color.white);
        RectTransform labelRect = tabLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 36f);
        labelRect.anchoredPosition = new Vector2(0f, 8f);

        if (isLocked)
        {
            Image lockOverlay = CreateImage($"{label}Lock", parent, Color.white);
            lockOverlay.sprite = menuConfig.lockIconSprite;
            RectTransform lockRect = lockOverlay.rectTransform;
            lockRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.pivot = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(26f, 26f);
            lockRect.anchoredPosition = new Vector2(22f, 26f);
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
                basicRect.anchoredPosition = new Vector2(rightBound + basicRect.sizeDelta.x + Random.Range(30f, 120f), basicRect.anchoredPosition.y);
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

    private void ShowComingSoon()
    {
        StopAllCoroutines();
        StartCoroutine(ShowComingSoonRoutine());
    }

    private IEnumerator ShowComingSoonRoutine()
    {
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
        string sceneName = gameConfig != null && !string.IsNullOrEmpty(gameConfig.gameplaySceneName)
            ? gameConfig.gameplaySceneName
            : (!string.IsNullOrEmpty(menuConfig.battleSceneName) ? menuConfig.battleSceneName : "SampleScene");

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
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        return text;
    }
}

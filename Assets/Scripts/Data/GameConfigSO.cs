using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "FarmMerger/Data/Game Config")]
public class GameConfigSO : ScriptableObject
{
    [Header("Unit Catalog")]
    public string defaultHeroId = "hero_basic";
    public HeroDataSO heroCatalog;
    public EnemyDataSO enemyCatalog;

    [Header("Economy / Wall")]
    public int startingCoins = 60;
    public float wallMaxHp = 150f;

    [Header("Hero Grid")]
    public int heroRows = 7;
    public int heroCols = 3;
    public float heroGridSpacing = 6f;
    public float heroGridPadding = 8f;
    public float heroFieldLeftInset = 0f;
    [Range(0.12f, 0.45f)] public float heroFieldWidth = 0.24f;
    [Range(0.05f, 0.25f)] public float wallFieldWidth = 0.08f;
    [Min(0f)] public float battleAreaSeamOverlap = 2f;
    [Range(0.7f, 1f)] public float heroPlatformCellSizeScale = 0.92f;

    [Header("Battle Presentation")]
    public float heroVisualSize = 36f;
    public float heroPlatformSize = 48f;
    public float enemySpawnRightMargin = 30f;
    public float enemySpawnVerticalMargin = 25f;
    public Sprite hero_back;
    public Sprite enemy_back;
    public Sprite heroFieldSprite;
    public Sprite heroPlatformSprite;
    public Sprite heroCellSprite;
    public Sprite wallSprite;
    public Sprite enemyFieldSprite;
    public Sprite resultActionButtonSprite;
    public float floatingDamageOffsetY = 48f;
    public Vector2 floatingDamageSize = new Vector2(140f, 56f);

    [Header("Bar Visuals")]
    public Sprite wallHpBarFrameSprite;
    public Sprite wallHpBarFillSprite;
    public Sprite waveProgressFillSprite;
    public Sprite bossHpBarFrameSprite;
    public Sprite bossHpBarFillSprite;

    [Header("Hero Level Stars")]
    public Sprite yellowStarSprite;
    public Sprite purpleStarSprite;
    public float heroStarSize = 20f;
    public float heroStarOffsetY = 6f;
    public float heroStarSpacing = 4f;

    [Header("Projectile")]
    public float projectileSize = 12f;
    public float projectileSpeed = 400f;
    public Color projectileColor = new Color(1f, 0.95f, 0.1f, 1f);
    public float heroAttackIntervalOverride = 0f;

    [Header("Reward Travel")]
    public float rewardTravelSpeed = 2200f;
}

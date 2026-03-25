using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "FarmMerger/Data/Game Config")]
public class GameConfigSO : ScriptableObject
{
    [Header("Economy / Wall")]
    public int startingCoins = 60;
    public float wallMaxHp = 150f;

    [Header("Hero Grid")]
    public int heroRows = 7;
    public int heroCols = 3;
    public float heroGridSpacing = 6f;
    public float heroGridPadding = 8f;

    [Header("Battle Presentation")]
    public float heroVisualSize = 36f;
    public float enemyVisualSize = 42f;
    public float enemySpawnRightMargin = 30f;
    public float enemySpawnVerticalMargin = 25f;
    public float floatingDamageOffsetY = 48f;
    public Vector2 floatingDamageSize = new Vector2(140f, 56f);

    [Header("Projectile")]
    public Sprite projectileSprite;
    public float projectileSize = 12f;
    public float projectileSpeed = 400f;
    public Color projectileColor = new Color(1f, 0.95f, 0.1f, 1f);
    public float heroAttackIntervalOverride = 0f;
}

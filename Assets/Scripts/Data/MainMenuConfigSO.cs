using UnityEngine;

[CreateAssetMenu(fileName = "MainMenuConfig", menuName = "FarmMerger/Data/Main Menu Config")]
public class MainMenuConfigSO : ScriptableObject
{
    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenuScene";
    public string battleSceneName = "SampleScene";

    [Header("Meta Placeholder")]
    public int crystalAmount = 120;
    public int currentChapterNumber = 1;

    [Header("Top Right Crystal")]
    public Sprite crystalIconSprite;

    [Header("Backgrounds")]
    public Sprite sceneBackgroundSprite;
    public Sprite enemyPreviewBackgroundSprite;

    [Header("Bottom Tabs")]
    public Sprite shopIconSprite;
    public Sprite fightIconSprite;
    public Sprite cardsIconSprite;
    public Sprite lockIconSprite;

    [Header("Buttons")]
    public Sprite playButtonSprite;
    [Range(0.1f, 1f)] public float playButtonDarken = 0.85f;

    [Header("Enemy Preview")]
    public float previewBasicMoveSpeed = 90f;
    public float previewBossMoveSpeed = 115f;
    [Min(0.25f)] public float bossSpawnIntervalSec = 6f;
    [Min(0.25f)] public float bossRespawnCooldownSec = 6f;
    [Min(1)] public int basicPreviewCount = 3;
}

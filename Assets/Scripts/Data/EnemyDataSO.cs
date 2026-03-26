using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "FarmMerger/Data/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public string id = "enemy_basic";
    public string enemyName = "Goblin";
    public float hp = 40f;
    public float damage = 5f;
    public float attackSpeed = 1f;
    public float moveSpeed = 120f;
    public int killRewardCoins = 8;
    public Sprite visualSprite;
    public Sprite attackVisualSprite;
    public float attackVisualDuration = 0.1f;
}

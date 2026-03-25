using UnityEngine;

namespace FarmMergerBattle.Data
{
    [CreateAssetMenu(menuName = "FarmMergerBattle/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public string enemyName = "Slime";
        public Sprite enemyVisual;
        public float maxHealth = 20f;
        public float damage = 1f;
        public float attackSpeed = 1f;
        public float moveSpeed = 1.5f;
        public float wallAttackRange = 0.8f;
        public int coinReward = 2;
    }
}

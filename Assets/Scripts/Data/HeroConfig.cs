using UnityEngine;

namespace FarmMergerBattle.Data
{
    [CreateAssetMenu(menuName = "FarmMergerBattle/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        public string heroName = "Farmer Hero";
        public Sprite heroIcon;
        public Sprite battleVisual;
        public float maxHealth = 100f;
        public float damage = 5f;
        public float attackSpeed = 1f;
        public float attackRange = 4f;
    }
}

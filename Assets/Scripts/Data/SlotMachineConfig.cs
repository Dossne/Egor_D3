using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmMergerBattle.Data
{
    public enum CardEffectType
    {
        GainCoins,
        HealWall,
        IncreaseHeroDamage,
        IncreaseHeroAttackSpeed
    }

    [Serializable]
    public class SlotSymbol
    {
        public string id = "coin";
        public string displayName = "Coin Burst";
        public Sprite slotSymbolSprite;
        public Sprite cardIcon;
        public Sprite cardBackground;
        public CardEffectType effectType;
        public float effectValue = 5f;
    }

    [CreateAssetMenu(menuName = "FarmMergerBattle/Slot Machine Config")]
    public class SlotMachineConfig : ScriptableObject
    {
        public List<SlotSymbol> symbols = new List<SlotSymbol>();
        public int basePullCost = 5;
        public int pullCostIncrease = 1;
    }
}

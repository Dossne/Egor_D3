using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "FarmMerger/Data/Slot Machine Config")]
public class SlotMachineConfigSO : ScriptableObject
{
    public int basePullCost = 20;
    public int pullCostStep = 5;
    public int spinDurationMs = 900;
    public int heroOverflowCompensationCoins = 10;

    public Sprite characterSymbol;
    public Sprite coinsSymbol;
    public Sprite cardSymbol;

    [Header("Card Reward Presentation")]
    public Sprite bonusCardIconBackground;
    public Sprite damageBonusIcon;
    public Sprite attackSpeedBonusIcon;
    public Sprite wallHpBonusIcon;

    public CardTierValues normalCards = new CardTierValues { damagePercent = 15f, attackSpeedPercent = 10f, wallHeal = 20f };
    public CardTierValues enhancedCards = new CardTierValues { damagePercent = 35f, attackSpeedPercent = 25f, wallHeal = 50f };

    public List<SlotResultData> results = new List<SlotResultData>
    {
        new SlotResultData { resultId = "two_character", symbols = new []{ SlotSymbol.Character, SlotSymbol.Character, SlotSymbol.Coins }, rewardType = RewardType.Hero, heroLevel = 1, weight = 28 },
        new SlotResultData { resultId = "three_character", symbols = new []{ SlotSymbol.Character, SlotSymbol.Character, SlotSymbol.Character }, rewardType = RewardType.Hero, heroLevel = 2, weight = 12 },
        new SlotResultData { resultId = "two_coins", symbols = new []{ SlotSymbol.Coins, SlotSymbol.Coins, SlotSymbol.Character }, rewardType = RewardType.Coins, coinReward = 30, weight = 25 },
        new SlotResultData { resultId = "three_coins", symbols = new []{ SlotSymbol.Coins, SlotSymbol.Coins, SlotSymbol.Coins }, rewardType = RewardType.Coins, coinReward = 55, weight = 10 },
        new SlotResultData { resultId = "two_cards", symbols = new []{ SlotSymbol.Card, SlotSymbol.Card, SlotSymbol.Character }, rewardType = RewardType.Card, cardTier = CardTier.Normal, weight = 18 },
        new SlotResultData { resultId = "three_cards", symbols = new []{ SlotSymbol.Card, SlotSymbol.Card, SlotSymbol.Card }, rewardType = RewardType.Card, cardTier = CardTier.Enhanced, weight = 7 }
    };
}

[Serializable]
public class SlotResultData
{
    public string resultId;
    public SlotSymbol[] symbols = new SlotSymbol[3];
    public RewardType rewardType;
    public string heroId;
    public int heroLevel = 1;
    public int coinReward;
    public CardTier cardTier;
    public int weight = 1;
}

[Serializable]
public class CardTierValues
{
    public float damagePercent;
    public float attackSpeedPercent;
    public float wallHeal;
}

public enum SlotSymbol
{
    Character,
    Coins,
    Card
}

public enum RewardType
{
    Hero,
    Coins,
    Card
}

public enum CardTier
{
    Normal,
    Enhanced
}

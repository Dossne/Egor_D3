using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroData", menuName = "FarmMerger/Data/Hero Data")]
public class HeroDataSO : ScriptableObject
{
    public string id = "hero_basic";
    public string heroName = "Defender";
    [Header("Hero Visuals")]
    public Sprite visualSprite;
    public Sprite idleVisualSprite;
    public Sprite attackVisualSprite;
    [Min(0.01f)] public float attackVisualDuration = 0.1f;
    public Sprite iconSprite;
    public List<HeroLevelData> levels = new List<HeroLevelData>
    {
        new HeroLevelData { level = 1, damage = 10f, attackSpeed = 1f, attackRange = 260f },
        new HeroLevelData { level = 2, damage = 18f, attackSpeed = 1.2f, attackRange = 280f }
    };

    public HeroLevelData GetLevel(int level)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].level == level)
            {
                return levels[i];
            }
        }

        return levels.Count > 0 ? levels[0] : new HeroLevelData();
    }

    public Sprite GetIdleVisualSprite()
    {
        return idleVisualSprite != null ? idleVisualSprite : visualSprite;
    }

    public Sprite GetAttackVisualSprite()
    {
        return attackVisualSprite != null ? attackVisualSprite : GetIdleVisualSprite();
    }
}

[Serializable]
public class HeroLevelData
{
    public int level = 1;
    public float damage = 10f;
    public float attackSpeed = 1f;
    public float attackRange = 200f;
}

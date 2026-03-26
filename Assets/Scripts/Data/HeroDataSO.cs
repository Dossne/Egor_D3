using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroData", menuName = "FarmMerger/Data/Hero Data")]
public class HeroDataSO : ScriptableObject
{
    [Header("Hero Catalog")]
    public List<HeroDefinition> heroes = new List<HeroDefinition>
    {
        new HeroDefinition
        {
            id = "hero_basic",
            heroName = "Defender",
            levels = new List<HeroLevelData>
            {
                new HeroLevelData { level = 1, damage = 10f, attackSpeed = 1f, attackRange = 260f },
                new HeroLevelData { level = 2, damage = 18f, attackSpeed = 1.2f, attackRange = 280f }
            }
        }
    };

    private static readonly HeroDefinition RuntimeFallbackHero = new HeroDefinition
    {
        id = "hero_basic",
        heroName = "Defender",
        levels = new List<HeroLevelData> { new HeroLevelData() }
    };

    public HeroDefinition GetHeroById(string heroId)
    {
        if (!string.IsNullOrEmpty(heroId))
        {
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroDefinition hero = heroes[i];
                if (hero != null && hero.id == heroId)
                {
                    return hero;
                }
            }
        }

        return GetDefaultHeroDefinition();
    }

    public HeroDefinition GetDefaultHeroDefinition(string preferredHeroId = null)
    {
        if (!string.IsNullOrEmpty(preferredHeroId))
        {
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroDefinition hero = heroes[i];
                if (hero != null && hero.id == preferredHeroId)
                {
                    return hero;
                }
            }
        }

        for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i] != null && !string.IsNullOrEmpty(heroes[i].id))
            {
                return heroes[i];
            }
        }

        return RuntimeFallbackHero;
    }

    public string GetRandomHeroId()
    {
        HeroDefinition hero = GetRandomHeroDefinition();
        return hero != null ? hero.id : RuntimeFallbackHero.id;
    }

    public HeroDefinition GetRandomHeroDefinition()
    {
        List<HeroDefinition> validHeroes = new List<HeroDefinition>();

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroDefinition hero = heroes[i];
            if (hero != null && !string.IsNullOrEmpty(hero.id))
            {
                validHeroes.Add(hero);
            }
        }

        if (validHeroes.Count == 0)
        {
            return GetDefaultHeroDefinition();
        }

        int index = UnityEngine.Random.Range(0, validHeroes.Count);
        return validHeroes[index];
    }
}

[Serializable]
public class HeroDefinition
{
    public string id = "hero_basic";
    public string heroName = "Defender";

    [Header("Hero Visuals")]
    public Sprite visualSprite;
    public Sprite idleVisualSprite;
    public Sprite attackVisualSprite;
    public Sprite projectileSprite;
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

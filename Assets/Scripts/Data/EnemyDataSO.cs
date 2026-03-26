using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "FarmMerger/Data/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Enemy Catalog")]
    public List<EnemyDefinition> enemies = new List<EnemyDefinition>
    {
        new EnemyDefinition()
    };

    private static readonly EnemyDefinition RuntimeFallbackEnemy = new EnemyDefinition();

    public EnemyDefinition GetEnemyById(string enemyId)
    {
        if (!string.IsNullOrEmpty(enemyId))
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyDefinition enemy = enemies[i];
                if (enemy != null && enemy.id == enemyId)
                {
                    return enemy;
                }
            }
        }

        return GetDefaultEnemyDefinition();
    }

    public EnemyDefinition GetDefaultEnemyDefinition(string preferredEnemyId = null)
    {
        if (!string.IsNullOrEmpty(preferredEnemyId))
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyDefinition enemy = enemies[i];
                if (enemy != null && enemy.id == preferredEnemyId)
                {
                    return enemy;
                }
            }
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && !string.IsNullOrEmpty(enemies[i].id))
            {
                return enemies[i];
            }
        }

        return RuntimeFallbackEnemy;
    }
}

[Serializable]
public class EnemyDefinition
{
    public string id = "enemy_basic";
    public float hp = 40f;
    public float damage = 5f;
    public float attackSpeed = 1f;
    public float moveSpeed = 120f;
    public int killRewardCoins = 8;
    [Min(1f)] public float visualSize = 42f;
    public Sprite visualSprite;
    public Sprite attackVisualSprite;
    [Min(0.01f)] public float attackVisualDuration = 0.1f;
}

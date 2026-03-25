using System.IO;
using FarmMergerBattle.Data;
using UnityEditor;
using UnityEngine;

namespace FarmMergerBattle.Editor
{
    public static class PrototypeDataSetup
    {
        private const string RootFolder = "Assets/Data";
        private const string ConfigFolder = "Assets/Resources/Configs";

        [MenuItem("FarmMergerBattle/Create Default Prototype Data")]
        public static void CreateDefaultData()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Configs");

            var hero = CreateAsset<HeroConfig>($"{RootFolder}/Hero_Default.asset");
            hero.heroName = "Farmer Hero";
            hero.damage = 5f;
            hero.attackSpeed = 1f;
            hero.attackRange = 4f;

            var enemy = CreateAsset<EnemyConfig>($"{RootFolder}/Enemy_Slime.asset");
            enemy.enemyName = "Slime";
            enemy.maxHealth = 20f;
            enemy.damage = 1f;
            enemy.attackSpeed = 0.8f;
            enemy.moveSpeed = 1.3f;
            enemy.coinReward = 2;

            var waves = CreateAsset<WaveConfig>($"{RootFolder}/Waves_Default.asset");
            waves.waves.Clear();
            waves.waves.Add(new WaveConfig.WaveEntry { enemy = enemy, count = 6, spawnInterval = 1.1f });
            waves.waves.Add(new WaveConfig.WaveEntry { enemy = enemy, count = 10, spawnInterval = 0.9f });
            waves.waves.Add(new WaveConfig.WaveEntry { enemy = enemy, count = 14, spawnInterval = 0.7f });

            var slot = CreateAsset<SlotMachineConfig>($"{RootFolder}/SlotMachine_Default.asset");
            slot.basePullCost = 5;
            slot.pullCostIncrease = 1;
            slot.symbols.Clear();
            slot.symbols.Add(new SlotSymbol { id = "coins", displayName = "Coin Rain", effectType = CardEffectType.GainCoins, effectValue = 8f });
            slot.symbols.Add(new SlotSymbol { id = "heal", displayName = "Wall Patch", effectType = CardEffectType.HealWall, effectValue = 8f });
            slot.symbols.Add(new SlotSymbol { id = "damage", displayName = "Sharp Tools", effectType = CardEffectType.IncreaseHeroDamage, effectValue = 2f });
            slot.symbols.Add(new SlotSymbol { id = "speed", displayName = "Quick Hands", effectType = CardEffectType.IncreaseHeroAttackSpeed, effectValue = 0.2f });

            var game = CreateAsset<GameConfig>($"{ConfigFolder}/BattleGameConfig.asset");
            game.hero = hero;
            game.waveConfig = waves;
            game.slotMachine = slot;
            game.startingCoins = 10;
            game.wallMaxHp = 50f;

            EditorUtility.SetDirty(hero);
            EditorUtility.SetDirty(enemy);
            EditorUtility.SetDirty(waves);
            EditorUtility.SetDirty(slot);
            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Default prototype data created under Assets/Data and Assets/Resources/Configs.");
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var folderPath = Path.Combine(parent, child).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, child);
        }
    }
}

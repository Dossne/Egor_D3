using System.Collections.Generic;
using System.Linq;
using FarmMergerBattle.Data;
using FarmMergerBattle.UI;
using FarmMergerBattle.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmMergerBattle.Runtime
{
    public class BattleRuntime : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private HeroUnit _hero;
        private readonly List<EnemyUnit> _aliveEnemies = new List<EnemyUnit>();
        private readonly List<SlotSymbol> _rolledSymbols = new List<SlotSymbol>();

        private BattleUIController _ui;
        private int _coins;
        private float _wallHp;
        private int _pullCount;
        private int _waveIndex;
        private int _spawnedInWave;
        private float _spawnTimer;
        private bool _isCardSelectionOpen;
        private bool _isFinished;

        private Sprite _heroPlaceholder;
        private Sprite _enemyPlaceholder;
        private Sprite _wallPlaceholder;
        private Sprite _cardPlaceholder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            var existing = FindObjectOfType<BattleRuntime>();
            if (existing != null)
            {
                return;
            }

            var go = new GameObject(nameof(BattleRuntime));
            go.AddComponent<BattleRuntime>();
        }

        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();
            PrepareFallbackData();
            BuildWorld();
            BuildUI();
            RefreshUI();
        }

        private void Update()
        {
            if (_isFinished || _isCardSelectionOpen)
            {
                return;
            }

            TickWaveSpawning(Time.deltaTime);
            _hero.Tick(Time.deltaTime, _aliveEnemies);

            for (var i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _aliveEnemies[i];
                if (enemy == null)
                {
                    _aliveEnemies.RemoveAt(i);
                    continue;
                }

                enemy.Tick(Time.deltaTime, config.wallX, DamageWall);
            }

            CheckWinCondition();
        }

        private void BuildWorld()
        {
            _coins = config.startingCoins;
            _wallHp = config.wallMaxHp;

            var wall = new GameObject("Wall");
            wall.transform.position = new Vector3(config.wallX, -1f, 0f);
            var wallRenderer = wall.AddComponent<SpriteRenderer>();
            wallRenderer.sprite = config.wallVisual != null ? config.wallVisual : _wallPlaceholder;
            wall.transform.localScale = new Vector3(1.2f, 3f, 1f);

            var heroGo = new GameObject("Hero");
            heroGo.transform.position = new Vector3(config.heroX, -1f, 0f);
            _hero = heroGo.AddComponent<HeroUnit>();
            _hero.Initialize(config.hero, _heroPlaceholder);
            heroGo.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        }

        private void BuildUI()
        {
            var uiGo = new GameObject("BattleUIController");
            _ui = uiGo.AddComponent<BattleUIController>();
            _ui.BuildUI();
            _ui.PullPressed += OnPullPressed;
            _ui.CardSelected += OnCardSelected;
            _ui.RestartPressed += OnRestart;
        }

        private void TickWaveSpawning(float deltaTime)
        {
            if (_waveIndex >= config.waveConfig.waves.Count)
            {
                return;
            }

            var wave = config.waveConfig.waves[_waveIndex];
            _spawnTimer += deltaTime;
            if (_spawnedInWave < wave.count && _spawnTimer >= wave.spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnEnemy(wave.enemy);
                _spawnedInWave++;
                if (_spawnedInWave >= wave.count)
                {
                    _waveIndex++;
                    _spawnedInWave = 0;
                    _spawnTimer = 0f;
                }
            }

            var progress = Mathf.Clamp01((float)_waveIndex / Mathf.Max(1, config.waveConfig.waves.Count));
            _ui.SetWave(Mathf.Min(_waveIndex + 1, config.waveConfig.waves.Count), config.waveConfig.waves.Count, progress);
        }

        private void SpawnEnemy(EnemyConfig enemyConfig)
        {
            var enemyGo = new GameObject($"Enemy_{enemyConfig.enemyName}");
            enemyGo.transform.position = new Vector3(config.enemySpawnX + Random.Range(-0.3f, 0.3f), -1f, 0f);
            enemyGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var unit = enemyGo.AddComponent<EnemyUnit>();
            unit.Initialize(enemyConfig, _enemyPlaceholder);
            unit.Died += OnEnemyDied;
            _aliveEnemies.Add(unit);
        }

        private void OnEnemyDied(EnemyUnit enemy)
        {
            _coins += enemy.Config.coinReward;
            _aliveEnemies.Remove(enemy);
            RefreshUI();
            CheckWinCondition();
        }

        private void OnPullPressed()
        {
            if (_isCardSelectionOpen || _isFinished)
            {
                return;
            }

            var cost = GetCurrentPullCost();
            if (_coins < cost)
            {
                return;
            }

            _coins -= cost;
            _pullCount++;
            RollSlotSymbols();
            _ui.ShowCardSelection(_rolledSymbols, _cardPlaceholder);
            _isCardSelectionOpen = true;
            RefreshUI();
        }

        private void RollSlotSymbols()
        {
            _rolledSymbols.Clear();
            var symbols = config.slotMachine.symbols;
            if (symbols.Count == 0)
            {
                return;
            }

            for (var i = 0; i < 3; i++)
            {
                _rolledSymbols.Add(symbols[Random.Range(0, symbols.Count)]);
            }

            _ui.SetSlotSymbols(_rolledSymbols, _cardPlaceholder);
        }

        private void OnCardSelected(SlotSymbol selected)
        {
            ApplyCardEffect(selected);
            _isCardSelectionOpen = false;
            _ui.HideCardSelection();
            RefreshUI();
        }

        private void ApplyCardEffect(SlotSymbol symbol)
        {
            switch (symbol.effectType)
            {
                case CardEffectType.GainCoins:
                    _coins += Mathf.RoundToInt(symbol.effectValue);
                    break;
                case CardEffectType.HealWall:
                    _wallHp = Mathf.Min(config.wallMaxHp, _wallHp + symbol.effectValue);
                    break;
                case CardEffectType.IncreaseHeroDamage:
                    _hero.AddDamage(symbol.effectValue);
                    break;
                case CardEffectType.IncreaseHeroAttackSpeed:
                    _hero.AddAttackSpeed(symbol.effectValue);
                    break;
            }
        }

        private void DamageWall(float amount)
        {
            if (_isFinished)
            {
                return;
            }

            _wallHp -= amount;
            if (_wallHp <= 0f)
            {
                _isFinished = true;
                _ui.ShowResult(false);
            }
        }

        private void CheckWinCondition()
        {
            if (_isFinished)
            {
                return;
            }

            var allWavesSpawned = _waveIndex >= config.waveConfig.waves.Count;
            if (allWavesSpawned && _aliveEnemies.Count == 0)
            {
                _isFinished = true;
                _ui.SetWave(config.waveConfig.waves.Count, config.waveConfig.waves.Count, 1f);
                _ui.ShowResult(true);
            }
        }

        private int GetCurrentPullCost()
        {
            return config.slotMachine.basePullCost + (config.slotMachine.pullCostIncrease * _pullCount);
        }

        private void RefreshUI()
        {
            _ui.SetCoins(_coins);
            _ui.SetPullCost(GetCurrentPullCost(), _coins >= GetCurrentPullCost());
            if (!_rolledSymbols.Any())
            {
                RollSlotSymbols();
            }
        }

        private void OnRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void PrepareFallbackData()
        {
            _heroPlaceholder = PlaceholderSpriteFactory.Create(new Color(0.2f, 0.8f, 0.2f));
            _enemyPlaceholder = PlaceholderSpriteFactory.Create(new Color(0.8f, 0.2f, 0.2f));
            _wallPlaceholder = PlaceholderSpriteFactory.Create(new Color(0.5f, 0.3f, 0.1f));
            _cardPlaceholder = PlaceholderSpriteFactory.Create(new Color(0.8f, 0.8f, 0.2f));

            if (config == null)
            {
                config = Resources.Load<GameConfig>("Configs/BattleGameConfig") ?? CreateFallbackGameConfig();
            }

            if (config.hero == null)
            {
                config.hero = ScriptableObject.CreateInstance<HeroConfig>();
            }

            if (config.waveConfig == null)
            {
                config.waveConfig = ScriptableObject.CreateInstance<WaveConfig>();
            }

            if (config.slotMachine == null)
            {
                config.slotMachine = ScriptableObject.CreateInstance<SlotMachineConfig>();
            }

            if (config.waveConfig.waves.Count == 0)
            {
                var defaultEnemy = ScriptableObject.CreateInstance<EnemyConfig>();
                var easyWave = new WaveConfig.WaveEntry { enemy = defaultEnemy, count = 6, spawnInterval = 1.2f };
                var midWave = new WaveConfig.WaveEntry { enemy = defaultEnemy, count = 10, spawnInterval = 0.9f };
                var hardWave = new WaveConfig.WaveEntry { enemy = defaultEnemy, count = 14, spawnInterval = 0.8f };
                config.waveConfig.waves = new List<WaveConfig.WaveEntry> { easyWave, midWave, hardWave };
            }

            if (config.slotMachine.symbols.Count == 0)
            {
                config.slotMachine.symbols = new List<SlotSymbol>
                {
                    new SlotSymbol { id = "coins", displayName = "Coin Rain", effectType = CardEffectType.GainCoins, effectValue = 8f },
                    new SlotSymbol { id = "heal", displayName = "Wall Patch", effectType = CardEffectType.HealWall, effectValue = 8f },
                    new SlotSymbol { id = "damage", displayName = "Sharp Tools", effectType = CardEffectType.IncreaseHeroDamage, effectValue = 2f },
                    new SlotSymbol { id = "speed", displayName = "Quick Hands", effectType = CardEffectType.IncreaseHeroAttackSpeed, effectValue = 0.2f }
                };
            }
        }

        private static GameConfig CreateFallbackGameConfig()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            cfg.hero = ScriptableObject.CreateInstance<HeroConfig>();
            cfg.waveConfig = ScriptableObject.CreateInstance<WaveConfig>();
            cfg.slotMachine = ScriptableObject.CreateInstance<SlotMachineConfig>();
            return cfg;
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 5f;
                Camera.main.transform.position = new Vector3(5f, 0f, -10f);
                return;
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(5f, 0f, -10f);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}

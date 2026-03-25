import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { enemiesConfig } from './config/enemies.config';
import { gameConfig } from './config/game.config';
import { heroesConfig } from './config/heroes.config';
import { slotMachineConfig } from './config/slotMachine.config';
import { wavesConfig } from './config/waves.config';
import type { CardBonusValues, SlotResultDefinition, SlotSymbol } from './types/game.types';

type GameStatus = 'playing' | 'victory' | 'defeat';

type HeroInstance = {
  instanceId: string;
  heroId: string;
  level: number;
  row: number;
  col: number;
  cooldown: number;
};

type EnemyInstance = {
  instanceId: string;
  enemyId: string;
  x: number;
  y: number;
  hp: number;
  attackCooldown: number;
  alive: boolean;
};

type PendingCardTier = 'normal' | 'enhanced' | null;
type CardOption = 'damage' | 'attackSpeed' | 'wallHp';

const battlefieldHeight = 610;
const gridWidth = gameConfig.grid.cols * gameConfig.cellSize.width;
const heroAreaPaddingLeft = 16;
const heroAreaWidth = gridWidth + 16;
const wallX = heroAreaPaddingLeft + heroAreaWidth;
const wallWidth = gameConfig.layoutSizing.wallWidth;
const enemyAreaStartX = wallX + wallWidth;
const enemyAreaEndX = 360;

const randomId = () => Math.random().toString(36).slice(2, 9);

const pickWeightedResult = (results: SlotResultDefinition[]): SlotResultDefinition => {
  const totalWeight = results.reduce((sum, result) => sum + result.weight, 0);
  let threshold = Math.random() * totalWeight;
  for (const result of results) {
    threshold -= result.weight;
    if (threshold <= 0) {
      return result;
    }
  }
  return results[results.length - 1];
};

function App() {
  const [gameStatus, setGameStatus] = useState<GameStatus>('playing');
  const [heroes, setHeroes] = useState<HeroInstance[]>([]);
  const [enemies, setEnemies] = useState<EnemyInstance[]>([]);
  const [coins, setCoins] = useState(gameConfig.startingCoins);
  const [pullCount, setPullCount] = useState(0);
  const [slotSymbols, setSlotSymbols] = useState<SlotSymbol[]>(['character', 'coins', 'card']);
  const [isSpinning, setIsSpinning] = useState(false);
  const [pendingCardTier, setPendingCardTier] = useState<PendingCardTier>(null);
  const [currentWave, setCurrentWave] = useState(0);
  const [spawnedWaveIndices, setSpawnedWaveIndices] = useState<number[]>([]);
  const [wallHp, setWallHp] = useState(gameConfig.wall.maxHp);
  const [damageMultiplier, setDamageMultiplier] = useState(1);
  const [attackSpeedMultiplier, setAttackSpeedMultiplier] = useState(1);

  const elapsedRef = useRef(0);
  const nextWaveStartAtRef = useRef(0);

  const currentPullCost = slotMachineConfig.basePullCost + pullCount * slotMachineConfig.pullCostStep;

  const canPull = useMemo(
    () => gameStatus === 'playing' && !isSpinning && pendingCardTier === null && coins >= currentPullCost,
    [coins, currentPullCost, gameStatus, isSpinning, pendingCardTier]
  );

  const placeHeroReward = useCallback((heroId: string, level: number) => {
    let placed = false;
    setHeroes((previous) => {
      if (previous.length >= gameConfig.grid.rows * gameConfig.grid.cols) {
        return previous;
      }
      const occupied = new Set(previous.map((hero) => `${hero.row}-${hero.col}`));
      for (let row = 0; row < gameConfig.grid.rows; row += 1) {
        for (let col = 0; col < gameConfig.grid.cols; col += 1) {
          if (!occupied.has(`${row}-${col}`)) {
            placed = true;
            return [
              ...previous,
              {
                instanceId: randomId(),
                heroId,
                level,
                row,
                col,
                cooldown: 0,
              },
            ];
          }
        }
      }
      return previous;
    });

    if (!placed) {
      setCoins((value) => value + slotMachineConfig.heroOverflowCompensationCoins);
    }
  }, []);

  const applyCardEffect = useCallback(
    (card: CardOption, values: CardBonusValues) => {
      if (card === 'damage') {
        setDamageMultiplier((value) => value * (1 + values.damagePercent / 100));
      }
      if (card === 'attackSpeed') {
        setAttackSpeedMultiplier((value) => value * (1 + values.attackSpeedPercent / 100));
      }
      if (card === 'wallHp') {
        setWallHp((value) => Math.min(gameConfig.wall.maxHp + 1000, value + values.wallHpFlat));
      }
      setPendingCardTier(null);
    },
    []
  );

  const resolveSlotResult = useCallback(
    (result: SlotResultDefinition) => {
      if (result.rewardType === 'hero' && result.heroReward) {
        placeHeroReward(result.heroReward.heroId, result.heroReward.level);
      }
      if (result.rewardType === 'coins' && result.coinReward) {
        setCoins((value) => value + result.coinReward!);
      }
      if (result.rewardType === 'card' && result.cardTier) {
        setPendingCardTier(result.cardTier);
      }
    },
    [placeHeroReward]
  );

  const onPull = useCallback(() => {
    if (!canPull) {
      return;
    }
    setCoins((value) => value - currentPullCost);
    setPullCount((value) => value + 1);
    setIsSpinning(true);

    const spinner = window.setInterval(() => {
      const symbols: SlotSymbol[] = ['character', 'coins', 'card'];
      setSlotSymbols([
        symbols[Math.floor(Math.random() * symbols.length)],
        symbols[Math.floor(Math.random() * symbols.length)],
        symbols[Math.floor(Math.random() * symbols.length)],
      ]);
    }, 100);

    const result = pickWeightedResult(slotMachineConfig.resultTable);

    window.setTimeout(() => {
      clearInterval(spinner);
      setSlotSymbols(result.symbols);
      setIsSpinning(false);
      resolveSlotResult(result);
    }, slotMachineConfig.spinDurationMs);
  }, [canPull, currentPullCost, resolveSlotResult]);

  useEffect(() => {
    if (gameStatus !== 'playing') {
      return;
    }

    const tickMs = 50;
    const timer = window.setInterval(() => {
      const dt = tickMs / 1000;
      elapsedRef.current += dt;

      if (
        currentWave < wavesConfig.totalWaves &&
        elapsedRef.current >= nextWaveStartAtRef.current &&
        !spawnedWaveIndices.includes(currentWave + 1)
      ) {
        const wave = wavesConfig.waves[currentWave];
        const spacing = battlefieldHeight / (wave.enemyCount + 1);
        const spawnedEnemies: EnemyInstance[] = [];
        for (let i = 0; i < wave.enemyCount; i += 1) {
          const enemyTemplate = enemiesConfig[wave.enemyId];
          spawnedEnemies.push({
            instanceId: randomId(),
            enemyId: wave.enemyId,
            x: enemyAreaEndX - gameConfig.layoutSizing.enemySpawnPadding,
            y: spacing * (i + 1),
            hp: enemyTemplate.hp,
            attackCooldown: 0,
            alive: true,
          });
        }

        setEnemies((previous) => [...previous, ...spawnedEnemies]);
        setCurrentWave((value) => value + 1);
        setSpawnedWaveIndices((value) => [...value, wave.waveIndex]);
        nextWaveStartAtRef.current += wavesConfig.waveIntervalSeconds;
      }

      setEnemies((previousEnemies) => {
        const updatedEnemies = previousEnemies.map((enemy) => {
          if (!enemy.alive) {
            return enemy;
          }
          const cfg = enemiesConfig[enemy.enemyId];
          const touchingWall = enemy.x <= enemyAreaStartX + 4;
          if (!touchingWall) {
            return {
              ...enemy,
              x: Math.max(enemyAreaStartX + 4, enemy.x - cfg.moveSpeed * dt),
            };
          }

          const nextCooldown = enemy.attackCooldown - dt;
          if (nextCooldown <= 0) {
            setWallHp((value) => Math.max(0, value - cfg.damage));
            return {
              ...enemy,
              attackCooldown: 1 / cfg.attackSpeed,
            };
          }
          return {
            ...enemy,
            attackCooldown: nextCooldown,
          };
        });
        return updatedEnemies;
      });

      setHeroes((previousHeroes) => {
        const mutableEnemies = [...enemies];
        return previousHeroes.map((hero) => {
          const heroCfg = heroesConfig[hero.heroId];
          const levelCfg = heroCfg.levels.find((levelData) => levelData.level === hero.level);
          if (!levelCfg) {
            return hero;
          }

          const heroX = heroAreaPaddingLeft + hero.col * gameConfig.cellSize.width + gameConfig.cellSize.width / 2;
          const heroY = hero.row * gameConfig.cellSize.height + gameConfig.cellSize.height / 2;
          const nextCooldown = hero.cooldown - dt;

          if (nextCooldown > 0) {
            return { ...hero, cooldown: nextCooldown };
          }

          const target = mutableEnemies
            .filter((enemy) => enemy.alive)
            .filter((enemy) => {
              const dx = enemy.x - heroX;
              const dy = enemy.y - heroY;
              return Math.hypot(dx, dy) <= levelCfg.attackRange;
            })
            .sort((a, b) => a.x - b.x)[0];

          if (!target) {
            return { ...hero, cooldown: 0 };
          }

          const enemyIndex = mutableEnemies.findIndex((enemy) => enemy.instanceId === target.instanceId);
          if (enemyIndex >= 0) {
            const scaledDamage = levelCfg.damage * damageMultiplier;
            mutableEnemies[enemyIndex] = {
              ...mutableEnemies[enemyIndex],
              hp: mutableEnemies[enemyIndex].hp - scaledDamage,
            };
          }

          return {
            ...hero,
            cooldown: 1 / (levelCfg.attackSpeed * attackSpeedMultiplier),
          };
        });
      });

      setEnemies((previousEnemies) => {
        const aliveEnemies: EnemyInstance[] = [];
        for (const enemy of previousEnemies) {
          if (enemy.hp <= 0 && enemy.alive) {
            const cfg = enemiesConfig[enemy.enemyId];
            setCoins((value) => value + cfg.killRewardCoins);
            continue;
          }
          if (enemy.alive) {
            aliveEnemies.push(enemy);
          }
        }
        return aliveEnemies;
      });
    }, tickMs);

    return () => clearInterval(timer);
  }, [attackSpeedMultiplier, currentWave, damageMultiplier, enemies, gameStatus, spawnedWaveIndices]);

  useEffect(() => {
    if (wallHp <= 0 && gameStatus === 'playing') {
      setGameStatus('defeat');
    }
  }, [gameStatus, wallHp]);

  useEffect(() => {
    const allWavesSpawned = currentWave >= wavesConfig.totalWaves;
    if (allWavesSpawned && enemies.length === 0 && gameStatus === 'playing') {
      setGameStatus('victory');
    }
  }, [currentWave, enemies.length, gameStatus]);

  const restart = () => {
    setGameStatus('playing');
    setHeroes([]);
    setEnemies([]);
    setCoins(gameConfig.startingCoins);
    setPullCount(0);
    setWallHp(gameConfig.wall.maxHp);
    setCurrentWave(0);
    setSpawnedWaveIndices([]);
    setDamageMultiplier(1);
    setAttackSpeedMultiplier(1);
    setPendingCardTier(null);
    elapsedRef.current = 0;
    nextWaveStartAtRef.current = 0;
  };

  const cardValues =
    pendingCardTier === 'enhanced'
      ? slotMachineConfig.enhancedCardBonusValues
      : slotMachineConfig.normalCardBonusValues;

  const waveProgress = Math.min(1, currentWave / wavesConfig.totalWaves);

  return (
    <div className="app-bg">
      <div className="portrait-frame">
        <div className="top-ui">
          <div className="progress-header">Wave {Math.min(currentWave, wavesConfig.totalWaves)} / {wavesConfig.totalWaves}</div>
          <div className="progress-track">
            <div className="progress-fill" style={{ width: `${waveProgress * 100}%` }} />
          </div>
        </div>

        <div className="battlefield">
          <div className="hero-grid" style={{ width: `${gridWidth}px` }}>
            {Array.from({ length: gameConfig.grid.rows * gameConfig.grid.cols }).map((_, index) => {
              const row = Math.floor(index / gameConfig.grid.cols);
              const col = index % gameConfig.grid.cols;
              const hero = heroes.find((heroData) => heroData.row === row && heroData.col === col);
              return (
                <div className="grid-cell" key={`${row}-${col}`}>
                  {hero ? (
                    <div className="hero-token">
                      <span>{heroesConfig[hero.heroId].visualAsset}</span>
                      {hero.level > 1 && <span className="hero-level">Lv.{hero.level}</span>}
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>

          <div className="wall" style={{ left: `${wallX}px`, width: `${wallWidth}px` }}>
            <div className="wall-visual">{gameConfig.wall.visualAsset}</div>
            <div className="wall-hp">HP {Math.max(0, Math.ceil(wallHp))}</div>
          </div>

          <div className="enemy-field">
            {enemies.map((enemy) => (
              <div
                className="enemy"
                key={enemy.instanceId}
                style={{
                  left: `${enemy.x}px`,
                  top: `${enemy.y}px`,
                }}
              >
                {enemiesConfig[enemy.enemyId].visualAsset}
                <span className="enemy-hp">{Math.max(0, Math.ceil(enemy.hp))}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="bottom-ui">
          <div className="coins-row">Coins: {Math.floor(coins)}</div>
          <div className="slot-machine">
            {slotSymbols.map((symbol, idx) => (
              <div className="slot-cell" key={`slot-${idx}`}>
                {slotMachineConfig.slotSymbolVisuals[symbol]}
              </div>
            ))}
          </div>
          <button className="pull-btn" onClick={onPull} disabled={!canPull}>
            Pull ({currentPullCost})
          </button>
        </div>

        {pendingCardTier && (
          <div className="overlay">
            <div className="card-panel">
              <h3>Choose a bonus card ({pendingCardTier})</h3>
              <div className="card-row">
                <button onClick={() => applyCardEffect('damage', cardValues)}>⚔️ +{cardValues.damagePercent}% Hero Damage</button>
                <button onClick={() => applyCardEffect('attackSpeed', cardValues)}>⏱️ +{cardValues.attackSpeedPercent}% Attack Speed</button>
                <button onClick={() => applyCardEffect('wallHp', cardValues)}>🧱 +{cardValues.wallHpFlat} Wall HP</button>
              </div>
            </div>
          </div>
        )}

        {gameStatus !== 'playing' && (
          <div className="overlay">
            <div className="result-panel">
              <h2>{gameStatus === 'victory' ? 'Victory' : 'Defeat'}</h2>
              <button onClick={restart}>Restart</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;

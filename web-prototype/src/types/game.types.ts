export type SlotSymbol = 'character' | 'coins' | 'card';

export interface HeroLevelConfig {
  level: number;
  damage: number;
  attackSpeed: number;
  attackRange: number;
}

export interface HeroConfig {
  id: string;
  name: string;
  levels: HeroLevelConfig[];
  visualAsset: string;
  iconAsset: string;
}

export interface EnemyConfig {
  id: string;
  name: string;
  hp: number;
  damage: number;
  attackSpeed: number;
  moveSpeed: number;
  killRewardCoins: number;
  visualAsset: string;
}

export interface WaveEntry {
  waveIndex: number;
  enemyId: string;
  enemyCount: number;
}

export interface WavesConfig {
  totalWaves: number;
  waveIntervalSeconds: number;
  waves: WaveEntry[];
}

export interface SlotResultDefinition {
  id: string;
  symbols: SlotSymbol[];
  weight: number;
  rewardType: 'hero' | 'coins' | 'card';
  heroReward?: {
    heroId: string;
    level: number;
  };
  coinReward?: number;
  cardTier?: 'normal' | 'enhanced';
}

export interface CardBonusValues {
  damagePercent: number;
  attackSpeedPercent: number;
  wallHpFlat: number;
}

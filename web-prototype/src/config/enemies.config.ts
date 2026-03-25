import type { EnemyConfig } from '../types/game.types';

export const enemiesConfig: Record<string, EnemyConfig> = {
  slime: {
    id: 'slime',
    name: 'Slime',
    hp: 70,
    damage: 9,
    attackSpeed: 0.9,
    moveSpeed: 55,
    killRewardCoins: 8,
    visualAsset: '🟢',
  },
};

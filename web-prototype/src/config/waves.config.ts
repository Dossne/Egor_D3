import type { WavesConfig } from '../types/game.types';

export const wavesConfig: WavesConfig = {
  totalWaves: 6,
  waveIntervalSeconds: 7,
  waves: [
    { waveIndex: 1, enemyId: 'slime', enemyCount: 4 },
    { waveIndex: 2, enemyId: 'slime', enemyCount: 5 },
    { waveIndex: 3, enemyId: 'slime', enemyCount: 6 },
    { waveIndex: 4, enemyId: 'slime', enemyCount: 7 },
    { waveIndex: 5, enemyId: 'slime', enemyCount: 8 },
    { waveIndex: 6, enemyId: 'slime', enemyCount: 10 },
  ],
};

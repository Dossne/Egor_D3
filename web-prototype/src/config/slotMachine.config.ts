import type { CardBonusValues, SlotResultDefinition, SlotSymbol } from '../types/game.types';

export interface SlotMachineConfig {
  basePullCost: number;
  pullCostStep: number;
  spinDurationMs: number;
  slotSymbolVisuals: Record<SlotSymbol, string>;
  heroOverflowCompensationCoins: number;
  resultTable: SlotResultDefinition[];
  normalCardBonusValues: CardBonusValues;
  enhancedCardBonusValues: CardBonusValues;
}

export const slotMachineConfig: SlotMachineConfig = {
  basePullCost: 20,
  pullCostStep: 6,
  spinDurationMs: 1000,
  slotSymbolVisuals: {
    character: '🧑‍🌾',
    coins: '🪙',
    card: '🃏',
  },
  heroOverflowCompensationCoins: 10,
  resultTable: [
    {
      id: 'twoCharacter',
      symbols: ['character', 'character', 'coins'],
      weight: 30,
      rewardType: 'hero',
      heroReward: { heroId: 'farmGuard', level: 1 },
    },
    {
      id: 'threeCharacter',
      symbols: ['character', 'character', 'character'],
      weight: 10,
      rewardType: 'hero',
      heroReward: { heroId: 'farmGuard', level: 2 },
    },
    {
      id: 'twoCoins',
      symbols: ['coins', 'coins', 'character'],
      weight: 28,
      rewardType: 'coins',
      coinReward: 40,
    },
    {
      id: 'threeCoins',
      symbols: ['coins', 'coins', 'coins'],
      weight: 12,
      rewardType: 'coins',
      coinReward: 90,
    },
    {
      id: 'twoCard',
      symbols: ['card', 'card', 'coins'],
      weight: 14,
      rewardType: 'card',
      cardTier: 'normal',
    },
    {
      id: 'threeCard',
      symbols: ['card', 'card', 'card'],
      weight: 6,
      rewardType: 'card',
      cardTier: 'enhanced',
    },
  ],
  normalCardBonusValues: {
    damagePercent: 20,
    attackSpeedPercent: 20,
    wallHpFlat: 80,
  },
  enhancedCardBonusValues: {
    damagePercent: 40,
    attackSpeedPercent: 35,
    wallHpFlat: 150,
  },
};

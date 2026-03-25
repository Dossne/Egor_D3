import type { HeroConfig } from '../types/game.types';

export const heroesConfig: Record<string, HeroConfig> = {
  farmGuard: {
    id: 'farmGuard',
    name: 'Farm Guard',
    levels: [
      {
        level: 1,
        damage: 12,
        attackSpeed: 1.1,
        attackRange: 230,
      },
      {
        level: 2,
        damage: 22,
        attackSpeed: 1.35,
        attackRange: 250,
      },
    ],
    visualAsset: '🧑‍🌾',
    iconAsset: '🧑‍🌾',
  },
};

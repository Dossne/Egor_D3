# Farm Merger Defense Web Prototype (React + TypeScript)

Single-screen mobile portrait (9:16) defense prototype.

## Run locally

```bash
cd web-prototype
npm install
npm run dev
```

Build production bundle:

```bash
npm run build
```

Preview build:

```bash
npm run preview
```

## Config files

All gameplay and layout data is config-driven:

- `src/config/heroes.config.ts`
- `src/config/enemies.config.ts`
- `src/config/waves.config.ts`
- `src/config/slotMachine.config.ts`
- `src/config/game.config.ts`

## Replace visuals / assets

The prototype currently uses emoji placeholders to keep setup simple.
Replace these config values with your own art references:

- Hero icon / battle visual: `heroes.config.ts` (`iconAsset`, `visualAsset`)
- Enemy visual: `enemies.config.ts` (`visualAsset`)
- Wall visual: `game.config.ts` (`wall.visualAsset`)
- Slot symbols: `slotMachine.config.ts` (`slotSymbolVisuals`)
- Bonus card visuals can be added by extending card option rendering in `src/App.tsx` and storing icon/background paths in `slotMachine.config.ts`

## Gameplay coverage

Implemented for first iteration:

- 7x3 auto-placement hero grid
- 1 hero type with level 1 and level 2 stats from config
- wall HP + lose condition
- 1 enemy type with movement + wall attack
- wave schedule with fixed interval from previous wave start
- slot machine with weighted guaranteed result combinations
- pull cost progression formula (`basePullCost + pullCount * pullCostStep`)
- hero overflow compensation coins
- normal/enhanced 3-card bonus choice UI
- top wave progress UI + bottom persistent slot machine + pull button
- victory / defeat overlay + restart

## Notes

- No backend/save/meta/multiplayer/audio per scope.
- Prototype was validated via TypeScript + Vite build checks in this environment.
- Unity editor runtime verification is not applicable for this web prototype folder.

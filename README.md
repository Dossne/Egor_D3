# Farm Merger - Battle Prototype

## Unity version
Use **Unity 2022.3.62f2**.

## Open and run
1. Open Unity Hub.
2. Add this folder as a project.
3. Open the project with Unity **2022.3.62f2**.
4. Open `Assets/Scenes/SampleScene.unity`.
5. Press **Play**.

The prototype auto-bootstraps into battle in the main scene.

## First-time data setup (ScriptableObjects)
If you want editable config assets generated for you:
1. In Unity menu, run: **FarmMergerBattle > Create Default Prototype Data**.
2. This creates:
   - `Assets/Data/` (hero, enemy, wave, slot machine assets)
   - `Assets/Resources/Configs/BattleGameConfig.asset` (main game config)

At runtime the game loads `Resources/Configs/BattleGameConfig` automatically. If it is missing, fallback in-memory defaults are used.

## Where to edit configs
- Hero: `HeroConfig` assets in `Assets/Data`
- Enemy: `EnemyConfig` assets in `Assets/Data`
- Waves: `WaveConfig` assets in `Assets/Data`
- Slot machine and card effects: `SlotMachineConfig` assets in `Assets/Data`
- Global gameplay values (coins, wall hp, positions): `GameConfig` in `Assets/Resources/Configs`

## Where to replace visuals
All visuals are data-driven via ScriptableObject sprite fields.

Replace these fields with your own sprites:
- Hero icon + hero battle visual: `HeroConfig`
- Enemy visual: `EnemyConfig`
- Wall visual: `GameConfig`
- Slot symbols + card icon/background: `SlotMachineConfig.symbols`

If a sprite field is empty, the game uses a simple color placeholder.

## Implemented gameplay summary
- Top UI: wave progress bar + current/total wave label.
- Bottom UI: coins, persistent 3-slot display, pull button with scaling cost.
- Pull flow: roll 3 cards, player must select 1, effect applies immediately, card UI closes, gameplay continues.
- Combat:
  - Hero attacks the nearest enemy to the wall in range, based on attack speed.
  - Enemy moves toward wall, then attacks wall based on attack speed.
  - Enemy death gives coins immediately.
- Win: all waves spawned and no alive enemies.
- Lose: wall HP reaches 0.
- Result overlay: Victory/Defeat + Restart button.

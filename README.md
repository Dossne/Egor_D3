# Farm Merger - Battle MVP

## Unity version
- `2022.3.62f2` (from `ProjectSettings/ProjectVersion.txt`)

## How to run
1. Open project in Unity `2022.3.62f2`.
2. Open scene: `Assets/Scenes/SampleScene.unity`.
3. Press Play.
4. The game starts directly in battle (no menu / meta scene).

## Scene structure and layout
Runtime bootstrap script builds a strict 3-zone portrait layout:
1. Top battle area (hero field + wall + enemy field)
2. Wall HP strip area below battle (HP bar + numeric HP)
3. Bottom panel (coins, hero count, 3-slot machine, Pull button)

Main runtime entry:
- `Assets/Scripts/Core/BattleBootstrap.cs`

## Editable ScriptableObject configs
Configs are loaded from `Resources/Configs/`:
- `Assets/Resources/Configs/GameConfig.asset`
  - starting coins, wall max HP
- `Assets/Resources/Configs/HeroData.asset`
  - hero id/name/levels/stats/visual/icon
- `Assets/Resources/Configs/EnemyData.asset`
  - enemy stats/reward/visual
- `Assets/Resources/Configs/WaveConfig.asset`
  - wave interval + wave entries
- `Assets/Resources/Configs/SlotMachineConfig.asset`
  - pull costs, spin duration, overflow compensation, weighted outcomes, coin rewards, card tiers

To edit values:
1. Select any asset under `Assets/Resources/Configs/`.
2. Change values in Inspector.
3. Press Play.

## Where to replace visuals
Current version uses readable placeholders.
You can swap visuals in data assets:
- Hero sprite/icon: `HeroData.asset`
- Enemy sprite: `EnemyData.asset`
- Slot symbols: `SlotMachineConfig.asset`

The screen panels (background/hero field/wall/enemy field/wall HP strip/bottom UI) are intentionally distinct placeholders and can be replaced later in runtime UI building (`BattleBootstrap.cs`).

## Notes
- Unity Editor playtesting was not executed in this environment; implementation was validated by code/data consistency only.
- Enemies spawn across multiple Y positions in the enemy area.
- Hero field is fixed 7x3 (21 slots), with overflow compensation from slot-machine config.

# README — ТЗ для агента / Codex

## 1. Цель

Нужно собрать **играбельный MVP-прототип в Unity (C#)** в portrait-формате, где игрок сразу попадает в бой.

В проекте **нет меты, нет меню, нет карты уровней**. Игра стартует сразу в одной боевой сцене.

Основной цикл:
- враги идут справа к стене
- герои стоят слева от стены и автоматически атакуют врагов
- за убийство врагов игрок получает монеты
- монеты тратятся на `Pull`
- `Pull` запускает слот-машину из 3 слотов
- слот-машина выдает героя, монеты или карточки бонусов
- игрок побеждает, если переживает все волны
- игрок проигрывает, если стена уничтожена

---

## 2. Обязательный визуальный референс

В проект будет положен референсный скриншот по пути:

`F:\Work\Project_X\Docs\base_ui`

Использовать этот скриншот как **структурный layout reference**, а не как абстрактный moodboard.

Это означает:
- нужно воспроизвести **структуру экрана** максимально близко к схеме
- особенно важно сохранить:
  - левое поле героев
  - центральную вертикальную стену
  - правое поле врагов
  - отдельную нижнюю UI-панель
  - полосу здоровья стены над нижней панелью
  - слот-машину над кнопкой pull

Если в реализации что-то противоречит этому layout, приоритет у layout со скриншота.

---

## 3. Технические требования

Сделать проект как:
- **Unity**
- язык: **C#**
- сцена запускается сразу в игровом уровне
- формат экрана: **portrait 9:16**
- все важные параметры должны быть вынесены в редактируемые данные
- предпочтительно использовать **ScriptableObjects**
- UI делать через **Unity UI**
- игра должна корректно читаться в Game View как мобильный portrait-screen

Предпочтительная реализация:
- 2D сцена
- раздельные системы:
  - combat
  - waves
  - economy
  - slot machine
  - bonuses
  - UI

---

## 4. Жесткая структура экрана

Экран делится на 3 большие части:

### 4.1. Верхняя боевая зона
Содержит:
- левое поле героев
- вертикальную стену
- правое поле врагов

### 4.2. Полоса здоровья стены
Находится **сразу под боевой зоной**.

Должна отображать:
- полоску HP стены
- числовое значение здоровья стены, например `85 / 120`

### 4.3. Нижняя UI-панель
Находится под полосой здоровья стены.

Содержит:
- деньги игрока
- число занятых героических слотов, например `10 / 21`
- слот-машину из 3 окон
- кнопку `Pull` под слот-машиной

Важно:
- нижняя UI-панель должна быть отдельной визуальной зоной
- она не должна смешиваться с боевой областью
- слот-машина должна быть **всегда видна**
- кнопка `Pull` должна быть **всегда видна**

---

## 5. Поле героев

Слева от стены расположено поле героев.

Размер:
- **7 клеток по высоте**
- **3 клетки по ширине**
- итого **21 слот**

Правила:
- герои могут находиться **только** в этих слотах
- герой ставится автоматически в **первый свободный слот**
- ручного drag & drop нет
- в первой версии реализуется только **1 тип героя**
- поддерживаются уровни героя:
  - уровень 1
  - уровень 2
- если герой выше 1 уровня, над ним отображается **цифра уровня**

Если герой выпал из pull, но мест на поле нет:
- герой не добавляется
- игрок получает **10 монет компенсации**
- это значение выносится в конфиг слот-машины

### Параметры героя
Все параметры героя должны идти из данных / конфига:
- `id`
- `name`
- `levels`
  - `level`
  - `damage`
  - `attackSpeed`
  - `attackRange`
- `visualPrefab` или `visualSprite`
- `iconSprite`

Где:
- `damage` — урон за одну атаку
- `attackSpeed` — атак в секунду
- `attackRange` — дальность атаки

### Поведение героя
- герой статичен
- автоматически атакует врагов
- выбирает целью **ближайшего к стене врага в радиусе атаки**

---

## 6. Стена

Между полем героев и полем врагов должна быть **видимая вертикальная стена фиксированной ширины**.

Это не условная линия, а отдельный игровой объект.

У стены есть:
- `maxHp`
- `currentHp`
- `visualPrefab` или `visualSprite`

Поведение:
- враг доходит до стены
- останавливается
- начинает бить стену

Отображение здоровья стены:
- обязательна **полоса здоровья**
- обязательно **числовое отображение** `currentHp / maxHp`

---

## 7. Поле врагов

Все, что справа от стены — это **единая 2D зона врагов**.

Это важно:
- враги **не должны идти по одной линии**
- это **не single-lane layout**
- враги должны появляться и двигаться на **разных высотах** в пределах правой зоны

### Требования к полю врагов
- поле визуально читается как отдельная зона
- враги спавнятся справа
- двигаются горизонтально влево к стене
- стартуют на разных `Y`
- могут одновременно находиться в разных частях правого поля

---

## 8. Враги

В первой итерации реализовать только **1 тип врага**.

### Параметры врага
Вынести в данные / конфиг:
- `id`
- `name`
- `hp`
- `damage`
- `attackSpeed`
- `moveSpeed`
- `killRewardCoins`
- `visualPrefab` или `visualSprite`

Где:
- `hp` — здоровье врага
- `damage` — урон по стене за удар
- `attackSpeed` — сколько ударов в секунду делает враг у стены
- `moveSpeed` — скорость движения к стене
- `killRewardCoins` — награда в монетах за убийство

### Поведение врага
- появляется справа
- движется к стене
- если дошел до стены — останавливается и атакует ее
- если HP <= 0 — умирает и дает монеты

---

## 9. Волны

Нужен отдельный конфиг волн.

Для уровня задаются:
- количество волн
- интервал между волнами в секундах
- количество врагов в каждой волне

### Структура
Для уровня:
- `levelId`
- `waveIntervalSec`
- `waves`

Для волны:
- `waveIndex`
- `enemyId`
- `enemyCount`

### Правила спавна внутри волны
- враги не спавнятся одной пачкой в одной точке
- враги должны распределяться **по всей высоте правой боевой зоны**
- если врагов больше, чем уникальных позиций по высоте, позиции можно переиспользовать

### Тайминг волн
Следующая волна стартует:
- через фиксированный `waveIntervalSec`
- **от старта предыдущей волны**

---

## 10. Верхний HUD

В верхней части экрана должен быть:
- текст вида `Wave X / Y`
- прогрессбар волн

Этот блок должен быть читаемым и закрепленным сверху.

---

## 11. Экономика

Игрок имеет монеты.

Источники монет:
- стартовые монеты уровня
- убийство врагов
- награды слот-машины
- компенсация за переполненное поле героев

В нижнем UI нужно показывать:
- текущее количество монет

Также в нижнем UI нужно показывать:
- текущее число героев на поле, например `10 / 21`

---

## 12. Pull и слот-машина

### 12.1. Расположение
Слот-машина должна:
- находиться в нижней UI-панели
- быть расположена **над кнопкой Pull**
- быть **постоянно видимой**, даже когда не крутится

Кнопка Pull должна:
- находиться **под слот-машиной**
- быть всегда видимой
- содержать текст и текущую цену pull

Пример:
- `Pull 20`
- `Играть 20`

### 12.2. Стоимость Pull
Cooldown нет.

Каждый новый pull стоит дороже предыдущего на `q` монет.

Вынести в конфиг:
- `basePullCost`
- `pullCostStep`

Формула:
- `currentPullCost = basePullCost + pullCount * pullCostStep`

Где:
- `pullCount` — число уже совершенных pull в текущем бою

### 12.3. Если монет не хватает
- Pull не запускается
- нужно показать понятный визуальный фидбек

---

## 13. Слот-машина

Слот-машина состоит из 3 слотов.

Каждый слот показывает один из 3 типов символов:
- `character`
- `coins`
- `card`

При нажатии Pull:
- списывается стоимость
- запускается анимация прокрутки
- затем определяется один из 6 результатов

### Обязательные результаты
1. `2 character + 1 другой символ`
   - игрок получает героя 1 уровня
2. `3 character`
   - игрок получает героя 2 уровня
3. `2 coins + 1 другой символ`
   - игрок получает `n` монет
4. `3 coins`
   - игрок получает `m` монет
5. `2 card + 1 другой символ`
   - показать 3 карточки бонусов обычного тира
6. `3 card`
   - показать 3 карточки бонусов усиленного тира

---

## 14. Конфиг слот-машины

Создать отдельные данные для слот-машины.

Вынести туда:
- `basePullCost`
- `pullCostStep`
- `spinDurationMs`
- `heroOverflowCompensationCoins`
- визуалы символов слотов
- таблицу результатов
- веса / шансы результатов
- значения наград героя
- значения наград монет
- значения обычных карточек
- значения усиленных карточек

### Для каждого результата хранить
- `resultId`
- `slotsCombination`
- `rewardType`
- `heroId`
- `heroLevel`
- `coinReward`
- `cardTier`
- `weight`

---

## 15. Карточки бонусов

При карточной награде всегда показывать **фиксированные 3 карточки**:
1. `+n% урона всем героям`
2. `+m% скорости атаки всем героям`
3. `+k HP стене`

Случайный пул карточек не нужен.

Нужно 2 тира значений:
- обычный
- усиленный

### Поведение
- карточки показываются по центру экрана
- на время выбора блокировать повторный pull
- игрок обязан выбрать 1 карточку
- после выбора бонус применяется сразу
- окно карточек закрывается

---

## 16. Победа и поражение

### Победа
Игрок побеждает, если:
- все волны были запущены
- все враги убиты
- стена жива

### Поражение
Игрок проигрывает, если:
- HP стены <= 0

### Результат
После конца уровня показывать overlay:
- `Victory` или `Defeat`
- кнопку `Restart`

---

## 17. Визуалы и фон

Текущая реализация не должна быть пустой.

Обязательно сделать:
- читаемый placeholder background
- отдельный визуал для:
  - поля героев
  - стены
  - поля врагов
  - полосы здоровья стены
  - нижней UI-панели

Все зоны экрана должны быть визуально различимы.

Все визуалы должны быть подменяемыми.

Вынести ссылки на:
- sprite / prefab героя
- sprite / prefab врага
- sprite / prefab стены
- иконки символов слот-машины
- фон и иконки карточек

---

## 18. Ограничения первой версии

Не делать:
- мету
- экран выбора уровня
- несколько типов героев
- несколько типов врагов
- merge
- ручное размещение героев
- sound/music
- save/load
- multiplayer
- backend
- pause menu

---

## 19. Рекомендуемая структура Unity-проекта

```text
Assets/
  Art/
  Docs/
  Prefabs/
    Heroes/
    Enemies/
    UI/
  Scenes/
    Main.unity
  Scripts/
    Core/
    Combat/
    Waves/
    Economy/
    SlotMachine/
    Bonuses/
    UI/
    Data/
  ScriptableObjects/
    Heroes/
    Enemies/
    Waves/
    SlotMachine/
    Game/
```

---

## 20. Acceptance criteria

Реализация считается корректной только если выполнены все условия ниже:

- Есть видимый фон сцены
- Есть отдельное левое поле героев 7x3
- Есть видимая вертикальная стена между полем героев и полем врагов
- Есть отдельное правое поле врагов
- Враги не идут по одной линии, а распределяются по разным высотам
- Есть отдельная полоса здоровья стены между боевой зоной и нижним UI
- У полосы здоровья стены есть и bar, и числовое значение HP
- В нижнем UI отображаются монеты игрока
- В нижнем UI отображается число героев на поле в формате `current / 21`
- Слот-машина всегда видима
- Кнопка Pull всегда видима
- Кнопка Pull находится под слот-машиной
- На кнопке Pull отображается текущая стоимость
- При заполненном поле герой не спавнится и конвертируется в 10 монет
- Игра стартует сразу в боевой сцене
- После победы или поражения показывается overlay результата

---

## 21. Финальный запрос для агента / Codex

```text
Build a playable Unity MVP prototype in C#.

Important: use the screenshot at:
F:\Work\Project_X\Docs\base_ui
as a strict structural layout reference.
It is not just a moodboard. The screen composition should follow it closely.

Project goals:
- single-screen 2D defense prototype
- no meta scene
- no main menu
- the game starts directly in battle
- portrait layout 9:16
- use Unity UI for HUD and controls
- prefer ScriptableObjects for editable game data

SCREEN LAYOUT (non-negotiable)
The screen must be split into:
1. Top battle area
   - left hero field
   - center vertical wall
   - right enemy field
2. Wall HP bar area directly below battle area
   - show both HP bar and numeric HP text
3. Bottom UI panel
   - player coins
   - current hero count / 21
   - 3-slot machine
   - Pull button below the slot machine

Do not collapse these areas together.
Do not make enemies move in a single lane.
Do not omit the wall.
Do not omit the background.

HERO FIELD
- fixed grid on the left side
- 7 rows x 3 columns
- total 21 hero slots
- heroes can only exist in these slots
- auto-place heroes into the first free slot
- no drag and drop
- implement only 1 hero type
- support hero level 1 and level 2
- same visual for both levels
- if hero level > 1, show the level number above the unit
- if a hero reward is granted but the field is full, do not place the hero
- instead grant 10 compensation coins
- this compensation value must come from slot machine config/data

HERO DATA
- id
- name
- levels
  - level
  - damage
  - attackSpeed
  - attackRange
- visual prefab or sprite
- icon sprite

HERO BEHAVIOR
- heroes are static
- auto-attack enemies in range
- target priority: nearest enemy to the wall within range

WALL
- visible vertical gameplay object of fixed width
- placed between hero field and enemy field
- has maxHp and currentHp
- enemies stop at the wall and attack it
- display wall HP as both:
  - HP bar
  - numeric text currentHp / maxHp

ENEMY FIELD
- right side is a 2D enemy area
- enemies must spawn across multiple Y positions
- enemies must not all move on one horizontal line
- enemies move horizontally left toward the wall

ENEMIES
- implement only 1 enemy type
- enemy data must include:
  - id
  - name
  - hp
  - damage
  - attackSpeed
  - moveSpeed
  - killRewardCoins
  - visual prefab or sprite
- when enemy reaches the wall, it stops and attacks
- on death, enemy is removed and grants coins immediately

WAVES
- create editable wave data
- level data defines:
  - waveIntervalSec
  - waves
- each wave defines:
  - waveIndex
  - enemyId
  - enemyCount
- enemies in a wave should be distributed across the full vertical height of the enemy area
- next wave starts after a fixed interval from the START of the previous wave

TOP HUD
- show Wave X / Y
- show wave progress bar

ECONOMY
- player has starting coins
- coins are earned from:
  - enemy kills
  - slot machine coin rewards
  - hero overflow compensation
- show coins in bottom UI
- show hero count as current / 21 in bottom UI

SLOT MACHINE + PULL
- slot machine is always visible in bottom UI
- slot machine is above the Pull button
- Pull button is always visible
- show current pull cost directly on the button
- no cooldown
- each new pull costs more than the previous one
- use:
  - basePullCost
  - pullCostStep
- formula:
  - currentPullCost = basePullCost + pullCount * pullCostStep
- if player has insufficient coins, do not spin and show feedback

SLOT MACHINE RULES
- 3 slots
- each slot symbol can be:
  - character
  - coins
  - card
- pressing Pull:
  - checks coins
  - spends cost
  - increments pullCount
  - runs spin animation
  - resolves one of the fixed outcomes

OUTCOMES
1. 2 character + 1 other symbol
   - give level 1 hero
2. 3 character
   - give level 2 hero
3. 2 coins + 1 other symbol
   - give n coins
4. 3 coins
   - give m coins
5. 2 card + 1 other symbol
   - show 3 fixed normal bonus cards
6. 3 card
   - show 3 fixed enhanced bonus cards

SLOT MACHINE DATA
- basePullCost
- pullCostStep
- spinDurationMs
- heroOverflowCompensationCoins
- slot symbol visuals
- result table
- result weights
- hero rewards
- coin rewards
- normal card values
- enhanced card values

BONUS CARDS
Always show the same 3 fixed cards:
1. +n% damage to all heroes
2. +m% attack speed to all heroes
3. +k wall HP

- support normal tier and enhanced tier
- when card choice is open, block repeated Pull input
- player must choose 1
- apply effect immediately

WIN / LOSE
- win when all waves are spawned and all enemies are dead and wall is alive
- lose when wall HP <= 0
- show result overlay with Victory/Defeat and Restart button

VISUALS
- do not leave the scene visually empty
- add placeholder visuals for:
  - hero field
  - wall
  - enemy field
  - wall HP strip
  - bottom UI panel
- make all visuals replaceable later

SIMPLIFICATIONS
Do not implement:
- meta progression
- multiple hero types
- multiple enemy types
- drag and drop
- merge
- save/load
- backend
- multiplayer
- sound
- pause menu

DELIVERABLES
- runnable Unity project
- main battle scene that starts immediately
- clean folder structure
- data-driven configuration
- README explaining:
  - Unity version
  - how to run
  - where to edit ScriptableObjects/configs
  - where to replace visuals

ACCEPTANCE CRITERIA
The result is acceptable only if:
- there is a visible background
- there is a visible 7x3 hero field on the left
- there is a visible vertical wall
- there is a visible enemy field on the right
- enemies spawn across multiple vertical positions
- there is a wall HP bar area between battle zone and bottom UI
- wall HP is shown both as a bar and as numbers
- bottom UI shows coins
- bottom UI shows hero count current/21
- slot machine is always visible
- Pull button is always visible
- Pull button is below the slot machine
- Pull button shows current cost
- full hero field converts hero reward into 10 coins
- the game starts directly in battle
- victory/defeat overlay works
```


# newCard

> MaxwellMod 新卡开发接口约定（持续更新）

## 0. 默认规则（优先级最高）

- 文案里的“所有卡牌”默认**不包含 Stash**。
- 除非文案明确写出“包含暂存区/包含 Stash”，否则只处理战斗标准牌区。
- 请注意， Keyword 自己会包含一份 Title 插队到 description 前边，比如 “保留”。这时 description 里就无需显式写出“保留”了。
---

## 1. 牌区语义与接口

| 文案语义 | 推荐接口 | 是否包含 Stash |
|---|---|---|
| 所有手牌 | `PileType.Hand.GetPile(Owner).Cards` | 否 |
| 所有抽牌堆 | `PileType.Draw.GetPile(Owner).Cards` | 否 |
| 所有弃牌堆 | `PileType.Discard.GetPile(Owner).Cards` | 否 |
| 所有消耗堆 | `PileType.Exhaust.GetPile(Owner).Cards` | 否 |
| 所有卡牌（战斗内） | `Owner.PlayerCombatState.AllCards` | 否 |
| 暂存区卡牌 | `StashManager.GetOrCreateStash(Owner).Cards` | 是（仅暂存区） |

### 1.1 需要“包含 Stash”时（显式写出来）

```csharp
var combatCards = Owner.PlayerCombatState?.AllCards ?? [];
var stashCards = StashManager.GetOrCreateStash(Owner).Cards;
var allCardsIncludingStash = combatCards.Concat(stashCards);
```

---

## 2. 温度与态接口

### 2.1 温度（卡牌级）

- 读取：`TemperatureManager.GetCardTemperature(card)`
- 单体修改：
  `await TemperatureManager.ApplyTemperatureDeltaAsync(card, delta, choiceContext, TemperatureCause.CardEffect)`
- 批量修改：
  `await TemperatureManager.ApplyTemperatureBatchAsync(cards, delta, choiceContext, TemperatureCause.CardEffect)`

当前约束：
- 默认温度由关键词推导：热牌 `+1`，冷牌 `-1`，其他 `0`
- 温度上限/下限：`[-1, 1]`
- 有绝缘/恒温关键词时，温度请求会被忽略
- 若卡牌实现 `ICardTemperatureListener`，在温度实际变化时会被串行 `await` 回调
- 温度请求会自动派生态（正数 -> 活泼，负数 -> 稳定），叠层按请求幅度 `|delta|`

### 2.2 态（State）

- 读取类型：`TemperatureManager.GetCardState(card)`
- 读取层数：`TemperatureManager.GetCardStateStacks(card)`
- 设置：`TemperatureManager.SetCardState(card, state, amount)`
- 态转永久增益：`TemperatureManager.ConsumeCardStateAsPermanentBonus(card)`

---

## 3. 卡牌态显示（extraCardText 风格）

当前实现通过补丁把态文字追加到描述底部：

- 文本提供：`CardStateExtraCardTextPatch` 内部逻辑（基于 `GetCardState/GetCardStateStacks`）
- 追加入口：`CardStateExtraCardTextPatch`（`CardModel.GetDescriptionForPile`）

新增态时，需要同时补：

- `CardStateExtraCardTextPatch` 的 key 映射
- 对应 localization 文本（建议放 `static_hover_tips`）

---

## 4. 动态数值显示模板（推荐）

只要卡牌的数值个数不太多，就请总是使用变量而非硬编码，这方便升级时任意地修改。

### 4.1 动态伤害（示例：熵增）

```csharp
public override IEnumerable<DynamicVar> CanonicalVars =>
[
    new CalculationBaseVar(7m),
    new ExtraDamageVar(2m),
    new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) =>
        /* 返回倍率次数 */
    )
];
```

本地化使用：

```text
{CalculatedDamage:diff()}
{ExtraDamage:diff()}
```

### 4.2 动态格挡（示例：冷墙）

```csharp
public override IEnumerable<DynamicVar> CanonicalVars =>
[
    new CalculationBaseVar(0m),
    new CalculationExtraVar(4m),
    new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) =>
        /* 返回倍率次数 */
    )
];
```

本地化使用：

```text
{CalculatedBlock:diff()}
{CalculationExtra:diff()}
```

### 4.3 可用变量列表

| 变量名                | 类                     | 常见占位写法                         |
|--------------------|-----------------------|--------------------------------|
| `Damage`           | `DamageVar`           | `{Damage:diff()}`              |
| `Block`            | `BlockVar`            | `{Block:diff()}`               |
| `Energy`           | `EnergyVar`           | `{Energy:diff()}`              |
| `Cards`            | `CardsVar`            | `{Cards:diff()}`               |
| `Repeat`           | `RepeatVar`           | `{Repeat:diff()}`              |
| `Gold`             | `GoldVar`             | `{Gold:diff()}`                |
| `Heal`             | `HealVar`             | `{Heal:diff()}`                |
| `HpLoss`           | `HpLossVar`           | `{HpLoss:diff()}`              |
| `MaxHp`            | `MaxHpVar`            | `{MaxHp:diff()}`               |
| `Summon`           | `SummonVar`           | `{Summon:diff()}`              |
| `Stars`            | `StarsVar`            | `{Stars:diff()}`               |
| `Forge`            | `ForgeVar`            | `{Forge:diff()}`               |
| `OstyDamage`       | `OstyDamageVar`       | `{OstyDamage:diff()}`          |
| `CalculationBase`  | `CalculationBaseVar`  | `{CalculationBase:diff()}`     |
| `CalculationExtra` | `CalculationExtraVar` | `{CalculationExtra:diff()}`    |
| `ExtraDamage`      | `ExtraDamageVar`      | `{ExtraDamage:diff()}`         |
| `CalculatedDamage` | `CalculatedDamageVar` | `{CalculatedDamage:diff()}`    |
| `CalculatedBlock`  | `CalculatedBlockVar`  | `{CalculatedBlock:diff()}`     |
| `IfUpgraded`       | `IfUpgradedVar`       | `{IfUpgraded:show:+X\|}`（条件文本） |

补充：
- 自定义命名变量可以直接用：`new DynamicVar("MyVar", 1m)`、`new IntVar("MyVar", 1)`、`new BoolVar("MyFlag", true)`、`new StringVar("MyText", "...")`。
- 这类变量在文案里用对应名字引用，例如：`{MyVar:diff()}`、`{MyText}`。
- `PowerVar<T>` 默认变量名为 `typeof(T).Name`，也可手动传入名字。 最好还是使用 “ DynamicVars.`T` ” 来调用最稳定，在本地化中：
`{WeakPower:diff()}`

---

## 5. Buff

## 5. 新卡最小落地清单

1. 新建卡类（`src/Cards`），继承 `AbstractMaxwellCard`
2. 可能需要创建 buff。 buff 的智能提示才能有值，普通提示不能有。
3. 本地化（`MaxwellMod/localization/zhs/cards.json`）
3. 补卡图占位（命名：`maxwellmod-xxx_yyy.png`，snake_case）
4. 若涉及温度/态，优先走 `TemperatureManager` 统一入口
5. 本地检查：`./dotcheck.sh`

---

## 6. 边界约定

- 不跨层：UI 只做显示，服务只做规则，卡命令只做行为编排。
- 当前设计不考虑持久化兼容，保持 KISS。

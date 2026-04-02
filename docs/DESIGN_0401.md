# STS2 附魔与卡牌修改机制研究

> 研究日期: 2026-04-01

## 一、核心发现：战斗中能否对卡牌进行附魔？

**结论：不能。** 游戏设计上不支持在战斗中对卡牌进行附魔（Enchant）。

### 原因分析

1. **卡牌位置限制**：`CardSelectCmd.FromDeckForEnchantment` 强制要求卡牌在 `PileType.Deck` 中
   ```csharp
   if (cards.Any((CardModel c) => c.Pile.Type != PileType.Deck || !enchantment.CanEnchant(c)))
   {
       throw new ArgumentException("All cards must be in the player's deck and enchantable.");
   }
   ```

2. **方法同步性**：`CardCmd.Enchant` 是同步方法，在 async 伤害流程中调用可能有问题

3. **使用场景**：所有现有附魔都在战斗外触发（遗物奖励、事件选项等）

---

## 二、游戏中的 Affliction（负面效果/诅咒）

现有 Affliction 实现：

| 名称 | 可叠加 | 有额外文本 | 功能说明 |
|------|--------|-----------|----------|
| **Hexed** | 否 | 否 | 配合 HexPower 使用，自动添加 Ethereal 关键词 |
| **Galvanized** | 是 | 是 | 仅标记，无特殊逻辑 |
| **Bound** | 否 | 是 | 仅标记，无特殊逻辑 |
| **Entangled** | 否 | 否 | 仅标记，无特殊逻辑 |
| **Ringing** | 否 | 是 | 仅标记，无特殊逻辑 |
| **Smog** | 否 | 否 | 仅标记，无特殊逻辑 |

### Hexed 示例（完整实现）

```csharp
public sealed class Hexed : AfflictionModel
{
    private bool _appliedEthereal;

    public bool AppliedEthereal
    {
        get => _appliedEthereal;
        set { AssertMutable(); _appliedEthereal = value; }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(CardKeyword.Ethereal) };

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != base.Card) return Task.CompletedTask;
        if (card.Owner.Creature.HasPower<HexPower>()) return Task.CompletedTask;

        if (AppliedEthereal)
            CardCmd.RemoveKeyword(base.Card, CardKeyword.Ethereal);

        CardCmd.ClearAffliction(base.Card);
        return Task.CompletedTask;
    }
}
```

---

## 三、伤害修改机制详解

### 伤害计算流程

```
Hook.ModifyDamage()
    │
    ├─→ 1. Enchantment 专属方法（直接调用）
    │      cardSource.Enchantment.EnchantDamageAdditive()
    │      cardSource.Enchantment.EnchantDamageMultiplicative()
    │
    └─→ 2. Hook Listeners 迭代（包括 Affliction）
           foreach (listener in IterateHookListeners())
               listener.ModifyDamageAdditive()
               listener.ModifyDamageMultiplicative()
               listener.ModifyDamageCap()
```

### Hook Listener 来源

`CombatState.IterateHookListeners()` 包含：

- 生物的 Powers
- 怪物本身
- 玩家的 Relics（遗物）
- 玩家的 Potions（药水）
- 玩家的 OrbQueue（球）
- **卡牌本身**
- **卡牌的 Affliction** ✅
- **卡牌的 Enchantment** ✅
- 战斗修饰符

---

## 四、Affliction 能否增加伤害？

**答案：可以！** 通过实现 Hook 方法。

### 对比

| 特性      | Enchantment                            | Affliction         |
|---------|----------------------------------------|--------------------|
| 专属伤害方法  | `EnchantDamageAdditive/Multiplicative` | 无                  |
| Hook 方法 | 继承 `AbstractModel`                     | 继承 `AbstractModel` |
| 调用时机    | 先调用（直接）                                | 后调用（Hook迭代）        |
| 战斗中可用   | ❌                                      | ✅                  |
| 可叠加     | 部分支持                                   | 部分支持               |

### 自定义伤害 Affliction 示例

```csharp
public sealed class EmpoweredAffliction : AfflictionModel
{
    public override bool IsStackable => true;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只修改这张卡的伤害
        if (cardSource == base.Card && props.IsPoweredAttack())
        {
            return base.Amount;  // 增加 Amount 点伤害
        }
        return 0m;
    }
}
```

---

## 五、CardKeyword（词缀）能否修改伤害？

**答案：不能。** CardKeyword 是纯行为标记，不参与伤害计算。

### CardKeyword 定义

```csharp
public enum CardKeyword
{
    None,
    Exhaust,    // 消耗
    Ethereal,   // 虚无（打完后消失）
    Innate,     // 固有（起手在手牌）
    Unplayable, // 不可打出
    Retain,     // 保留
    Sly,        // 狡猾
    Eternal     // 永恒（不可移除）
}
```

### CardKeyword 用途

仅用于行为判断，例如：

- `Keywords.Contains(CardKeyword.Ethereal)` → 打出后消失
- `Keywords.Contains(CardKeyword.Unplayable)` → 不能打出
- `Keywords.Contains(CardKeyword.Exhaust)` → 打出后消耗

### CardTag 同样不能

```csharp
public enum CardTag
{
    None,
    Strike,      // 打击
    Defend,      // 防御
    Minion,      // 随从
    OstyAttack,  // Osty攻击
    Shiv         // 小刀
}
```

CardTag 用于卡牌分类和计数（如 PerfectedStrike 统计 Strike 卡数量），不直接修改伤害数值。

---

## 六、BaseLib 方案：自定义 Keyword + DynamicVar + Patch

**核心发现**：原版 Affliction 不支持叠加（每张卡最多 1 个），但 BaseLib 提供了更好的解决方案。

### BaseLib 的 Keyword 扩展模式

BaseLib 通过组合三个组件实现可叠加的自定义效果：

```
自定义效果 = CardKeyword（显示）+ DynamicVar（数值）+ Harmony Patch（逻辑）
```

### 示例：Refund（退费）关键词

```csharp
// 1. 定义自定义 Keyword（仅用于显示标题/描述）
[CustomEnum]
[KeywordProperties(AutoKeywordPosition.After)]  // 自动添加到卡牌描述后
public static readonly CardKeyword Refund;

// 2. 定义 DynamicVar 存储数值
public class RefundVar : DynamicVar
{
    public const string Key = "Refund";
    public RefundVar(decimal amount) : base(Key, amount) { }
}

// 3. Harmony Patch 实现逻辑
[HarmonyPatch(typeof(Hook), "AfterCardPlayed")]
public static class RefundPatch
{
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 检查 DynamicVar 而不是 Keyword
        var refundAmount = cardPlay.Card.DynamicVars.TryGetValue(RefundVar.Key, out var val)
            ? val.IntValue : 0;
        if (refundAmount > 0 && cardPlay.Resources.EnergySpent > 0)
        {
            await PlayerCmd.GainEnergy(Math.Min(refundAmount, cardPlay.Resources.EnergySpent), cardPlay.Card.Owner);
        }
    }
}
```

### 应用：自定义伤害加成关键词

```csharp
// 1. 定义 Keyword
[CustomEnum]
[KeywordProperties(AutoKeywordPosition.After)]
public static readonly CardKeyword Empowered;

// 2. 定义 DynamicVar
public class EmpoweredVar : DynamicVar
{
    public const string Key = "Empowered";
    public EmpoweredVar(decimal amount) : base(Key, amount) { }
}

// 3. Patch Hook.ModifyDamage 或 DamageVar
[HarmonyPatch(typeof(Hook), "ModifyDamage")]
public static class EmpoweredPatch
{
    // 在 Postfix 中检查 cardSource.DynamicVars["Empowered"] 并修改伤害
}
```

### 战斗内伤害修改的核心逻辑

战斗中的伤害修改（易伤 Vulnerable、力量 Strength 等）通过 `Hook.ModifyDamage` 实现：

```csharp
// Hook.cs - ModifyDamageInternal
private static decimal ModifyDamageInternal(...)
{
    // 1. 加法修改（Strength 等）
    foreach (AbstractModel item in runState.IterateHookListeners(combatState))
    {
        decimal add = item.ModifyDamageAdditive(target, num, props, dealer, cardSource);
        num += add;
    }

    // 2. 乘法修改（Vulnerable 等）
    foreach (AbstractModel item2 in runState.IterateHookListeners(combatState))
    {
        decimal mult = item2.ModifyDamageMultiplicative(target, num, props, dealer, cardSource);
        num *= mult;
    }

    // 3. 上限修改
    foreach (AbstractModel item3 in runState.IterateHookListeners(combatState))
    {
        decimal cap = item3.ModifyDamageCap(target, props, dealer, cardSource);
        num = Math.Min(num, cap);
    }
}
```

### StrengthPower 示例（加法）

```csharp
public sealed class StrengthPower : PowerModel
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (base.Owner != dealer) return 0m;        // 只影响自己打出的伤害
        if (!props.IsPoweredAttack()) return 0m;    // 只影响攻击伤害
        return base.Amount;                          // 返回力量值作为加成
    }
}
```

### VulnerablePower 示例（乘法）

```csharp
public sealed class VulnerablePower : PowerModel
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner) return 1m;        // 只影响自己受到的伤害
        if (!props.IsPoweredAttack()) return 1m;    // 只影响攻击伤害
        return 1.5m;                                 // 返回 1.5 倍伤害
    }
}
```

### Patch 点

要修改伤害计算，可以 Patch：

- **`Hook.ModifyDamage`** - 最直接，所有伤害计算都会经过这里
- **`Hook.ModifyDamageInternal`** - 内部迭代逻辑

---

## 七、总结：修改卡牌伤害的可选方案

| 方案 | 战斗中可用 | 可叠加 | 实现复杂度 | 持久性 |
|------|-----------|--------|-----------|--------|
| **Enchantment** | ❌ | ✅ | 低 | 永久 |
| **Affliction** | ✅ | ❌（每卡1个） | 中 | 战斗内 |
| **Power** | ✅ | ✅ | 中 | 战斗内 |
| **BaseLib: Keyword+Var+Patch** | ✅ | ✅ | 高 | 灵活 |
| **CardKeyword（原生）** | - | - | - | 不支持数值 |

### 推荐方案

| 场景               | 推荐方案                                      |
|------------------|-------------------------------------------|
| 永久卡牌增强           | Enchantment（战斗外）                          |
| 战斗内全局效果          | Power                                     |
| 战斗内单卡效果（不叠加）     | Affliction                                |
| **战斗内单卡效果（可叠加）** | **BaseLib: Keyword + DynamicVar + Patch** |

### BaseLib 方案优势

1. **可叠加** - DynamicVar 可以存储任意数值
2. **战斗内可用** - 不受 Deck 位置限制
3. **显示友好** - 自定义 Keyword 自动添加到卡牌描述
4. **兼容性好** - 不破坏原版 Affliction 设计

---

## 八、能否直接订阅 Hook？（不使用 Patch）

**结论：不能。** Hook 系统没有提供订阅机制。

### Hook 系统的设计

`Hook` 是一个静态类，所有方法都是静态的，直接遍历 `IterateHookListeners()`：

```csharp
public static class Hook
{
    public static decimal ModifyDamage(...)
    {
        // 直接遍历，没有事件/委托机制
        foreach (AbstractModel item in runState.IterateHookListeners(combatState))
        {
            item.ModifyDamageAdditive(...);
        }
    }
}
```

### IterateHookListeners 的硬编码列表

Hook Listeners 来自 `CombatState.IterateHookListeners()` 的硬编码枚举：

```csharp
public IEnumerable<AbstractModel> IterateHookListeners()
{
    // 硬编码的来源，无法动态添加自定义 Listener
    list.AddRange(creature.Powers);           // 生物的 Powers
    list.Add(creature.Monster);               // 怪物本身
    list.AddRange(player.Relics);             // 玩家遗物
    list.AddRange(player.PotionSlots);        // 玩家药水
    list.AddRange(player.OrbQueue.Orbs);      // 玩家球
    list.Add(cardModel);                      // 卡牌本身
    list.Add(cardModel.Affliction);           // 卡牌 Affliction
    list.Add(cardModel.Enchantment);          // 卡牌 Enchantment
    list.AddRange(Modifiers);                 // 战斗修饰符（构造时传入）
}
```

### 成为 Hook Listener 的途径

要参与 Hook 调用，必须让对象出现在上述列表中：

| 途径                | 适用场景   | 动态添加                  |
|-------------------|--------|-----------------------|
| **Power**         | 生物级别效果 | ✅ `PowerCmd.Apply`    |
| **Relic**         | 玩家级别效果 | ✅ `RunState.AddRelic` |
| **Affliction**    | 单卡效果   | ✅ `CardCmd.Afflict`   |
| **Enchantment**   | 单卡效果   | ❌ 仅战斗外                |
| **ModifierModel** | 战斗级别效果 | ❌ 构造时传入               |
| **自定义 Listener**  | -      | ❌ 无扩展点                |

### 为什么 BaseLib 选择 Patch

1. **无订阅机制** - Hook 没有提供事件/委托订阅方式
2. **无扩展点** - `IterateHookListeners` 是硬编码的，无法添加自定义 Listener
3. **最灵活** - Patch 可以在任何位置注入逻辑
4. **社区惯例** - Harmony Patch 是 STS2 Modding 的标准做法

### 如果不想用 Patch 的替代方案

**方案 A：使用 Power**
- 创建一个隐藏的 Power 绑定到玩家
- Power 实现 `ModifyDamageAdditive`
- 通过 Power 的 `Amount` 或自定义字段存储数据

**方案 B：使用 ModifierModel**
- 继承 `ModifierModel`
- 实现 `ModifyDamage*` 方法
- 但 ModifierModel 是战斗级别的，无法动态添加

**方案 C：Patch `IterateHookListeners`**
- Patch `IterateHookListeners` 方法
- 在返回前添加自定义 Listener
- 这样自定义 Listener 就能参与所有 Hook

# 温度系统设计

> 设计日期: 2026-04-02

## 一、机制概述

| 名称   | ID       | 类型              | 核心功能                  |
|------|----------|-----------------|-----------------------|
| 温度环境 | TEMP_ENV | Power           | 全局温度管理，温度阈值效果         |
| 热    | HEAT     | **CustomKeyword** | 打出时升温，影响周围卡牌温度+1并施活泼 |
| 冷    | COLD     | **CustomKeyword** | 打出时降温，影响周围卡牌温度-1并施稳定 |
| 活泼   | LIVELY   | State (Var)     | 伤害+2                  |
| 稳定   | STABLE   | State (Var)     | 防御+2                  |
| 绿    | GREEN    | CustomKeyword   | 虚无+消耗，态转化，传递          |
| 活塞   | PISTON   | Card            | 自己温度变化时触发效果           |

---

## 二、架构设计

### 核心概念

1. **热/冷是自定义 Keyword** - 卡牌直接标记为"热牌"或"冷牌"（用 BaseLib `[CustomEnum]`）
2. **卡牌有自己的温度值** - 存在 `DynamicVars["Maxwell_CardTemp"]`，初始为 0
3. **卡牌可监听自己温度变化** - 实现 `ICardTemperatureListener` 接口

```
┌─────────────────────────────────────────────────────────────────┐
│                        TemperatureManager                        │
│  (静态工具类，提供温度操作的统一入口)                                   │
├─────────────────────────────────────────────────────────────────┤
│  + ModifyGlobalTemperature(combatState, delta) → Task            │
│  + GetGlobalTemperature(combatState) → int                       │
│  + GetCardTemperature(card) → int                                │
│  + ModifyCardTemperature(card, delta) → void  ← 会触发回调！       │
│  + GetCardState(card) → StateType                                │
│  + SetCardState(card, state) → void                              │
└─────────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┴─────────────────┐
            ▼                                   ▼
┌───────────────────────┐          ┌─────────────────────────────┐
│   TemperaturePower    │          │  ICardTemperatureListener   │
│  (全局温度 Power)       │          │  (接口，卡牌实现它接收通知)       │
├───────────────────────┤          ├─────────────────────────────┤
│  Amount = 全局温度      │          │  OnCardTemperatureChanged(  │
│  TryModifyEnergy...   │          │    oldTemp, newTemp, delta) │
│  ModifyDamageReceived │          └─────────────────────────────┘
└───────────────────────┘
```

### 温度变化通知流程

```
热牌打出
    │
    ├─→ 1. TemperatureManager.ModifyGlobalTemperature(+1)
    │         └─→ TemperaturePower.Amount 改变
    │
    └─→ 2. TemperatureManager.ModifyCardTemperature(左边牌, +1)
              │
              ├─→ 设置 DynamicVars["Maxwell_CardTemp"]
              │
              └─→ 检查卡牌是否实现 ICardTemperatureListener
                    │
                    └─→ 调用 card.OnCardTemperatureChanged(0, 1, +1)
                          │
                          └─→ 活塞卡：升温 → 丢弃一张牌
```

---

## 三、核心组件实现

### 3.1 自定义 Keyword 定义

```csharp
namespace MaxwellsSlayingDemon.Keywords;

/// <summary>
/// 热词缀 - 打出时升温，影响周围卡牌
/// </summary>
[CustomEnum]
[KeywordProperties(AutoKeywordPosition.After)]
public static readonly CardKeyword HeatKeyword;

/// <summary>
/// 冷词缀 - 打出时降温，影响周围卡牌
/// </summary>
[CustomEnum]
[KeywordProperties(AutoKeywordPosition.After)]
public static readonly CardKeyword ColdKeyword;

/// <summary>
/// 绿词缀 - 虚无、消耗，态转化
/// </summary>
[CustomEnum]
[KeywordProperties(AutoKeywordPosition.After)]
public static readonly CardKeyword GreenKeyword;
```

### 3.2 卡牌温度变化监听接口

```csharp
namespace MaxwellsSlayingDemon.Temperature;

/// <summary>
/// 卡牌温度变化监听接口
/// 实现此接口的卡牌会在自己温度变化时收到通知
/// </summary>
public interface ICardTemperatureListener
{
    /// <summary>
    /// 当卡牌自己的温度发生变化时调用
    /// </summary>
    /// <param name="oldTemp">变化前的温度</param>
    /// <param name="newTemp">变化后的温度</param>
    /// <param name="delta">变化量 (正数=升温，负数=降温)</param>
    Task OnCardTemperatureChanged(int oldTemp, int newTemp, int delta);
}
```

### 3.3 TemperatureManager (静态工具类)

```csharp
namespace MaxwellsSlayingDemon.Temperature;

/// <summary>
/// 温度系统的核心管理器
/// </summary>
public static class TemperatureManager
{
    // DynamicVar Keys
    public const string CardTempVarKey = "Maxwell_CardTemp";
    public const string StateVarKey = "Maxwell_State";
    public const string GreenVarKey = "Maxwell_Green";

    #region 全局温度

    /// <summary>
    /// 获取全局温度值
    /// </summary>
    public static int GetGlobalTemperature(CombatState combatState)
    {
        var power = combatState.Player.Creature.GetPower<TemperaturePower>();
        return power?.Amount ?? 0;
    }

    /// <summary>
    /// 修改全局温度
    /// </summary>
    public static async Task ModifyGlobalTemperature(CombatState combatState, int delta)
    {
        if (delta == 0) return;

        var power = combatState.Player.Creature.GetPower<TemperaturePower>();
        if (power == null)
        {
            await PowerCmd.Apply<TemperaturePower>(
                combatState.Player.Creature,
                combatState.Player.Creature,
                delta
            );
        }
        else
        {
            if (delta > 0)
                await PowerCmd.Increment(power, delta);
            else
                await PowerCmd.Decrement(power, -delta);
        }
    }

    #endregion

    #region 卡牌温度

    /// <summary>
    /// 获取卡牌温度值
    /// </summary>
    public static int GetCardTemperature(CardModel card)
    {
        if (card.DynamicVars.TryGetValue(CardTempVarKey, out var v))
            return v.IntValue;
        return 0;
    }

    /// <summary>
    /// 修改卡牌温度值（会触发 ICardTemperatureListener 回调）
    /// </summary>
    public static void ModifyCardTemperature(CardModel card, int delta)
    {
        if (delta == 0) return;

        var oldTemp = GetCardTemperature(card);
        var newTemp = oldTemp + delta;

        // 设置新值
        card.DynamicVars.SetDynamicVar(new CardTempVar(newTemp));

        // 触发回调
        if (card is ICardTemperatureListener listener)
        {
            _ = listener.OnCardTemperatureChanged(oldTemp, newTemp, delta);
        }
    }

    /// <summary>
    /// 设置卡牌温度值（会触发 ICardTemperatureListener 回调）
    /// </summary>
    public static void SetCardTemperature(CardModel card, int value)
    {
        var oldTemp = GetCardTemperature(card);
        if (oldTemp == value) return;

        card.DynamicVars.SetDynamicVar(new CardTempVar(value));

        // 触发回调
        if (card is ICardTemperatureListener listener)
        {
            _ = listener.OnCardTemperatureChanged(oldTemp, value, value - oldTemp);
        }
    }

    #endregion

    #region 卡牌态

    /// <summary>
    /// 获取卡牌当前态
    /// </summary>
    public static StateType GetCardState(CardModel card)
    {
        if (card.DynamicVars.TryGetValue(StateVarKey, out var v) && v is StateVar sv)
            return sv.State;
        return StateType.None;
    }

    /// <summary>
    /// 设置卡牌态（会覆盖不同态，叠加相同态）
    /// </summary>
    public static void SetCardState(CardModel card, StateType state, int amount = 1)
    {
        var current = GetCardState(card);
        if (current != StateType.None && current != state)
        {
            // 覆盖不同的态
            card.DynamicVars.SetDynamicVar(new StateVar(state, amount));
        }
        else
        {
            // 叠加相同的态
            var existingAmount = card.DynamicVars.TryGetValue(StateVarKey, out var v) ? v.IntValue : 0;
            card.DynamicVars.SetDynamicVar(new StateVar(state, existingAmount + amount));
        }
    }

    #endregion

    #region 关键词检查

    /// <summary>
    /// 检查卡牌是否有热词缀
    /// </summary>
    public static bool HasHeatKeyword(CardModel card)
    {
        return card.Keywords.Contains(Keywords.HeatKeyword);
    }

    /// <summary>
    /// 检查卡牌是否有冷词缀
    /// </summary>
    public static bool HasColdKeyword(CardModel card)
    {
        return card.Keywords.Contains(Keywords.ColdKeyword);
    }

    #endregion
}

/// <summary>
/// 卡牌温度值 DynamicVar
/// </summary>
public class CardTempVar : DynamicVar
{
    public const string Key = TemperatureManager.CardTempVarKey;

    public CardTempVar(int value) : base(Key, value) { }

    public bool IsHot => BaseValue > 0;
    public bool IsCold => BaseValue < 0;
}

/// <summary>
/// 卡牌态类型
/// </summary>
public enum StateType
{
    None,
    Lively,   // 活泼：伤害+2
    Stable    // 稳定：防御+2
}

/// <summary>
/// 卡牌态值 DynamicVar
/// </summary>
public class StateVar : DynamicVar
{
    public const string Key = TemperatureManager.StateVarKey;

    private StateType _state;

    public StateType State
    {
        get => _state;
        set => _state = value;
    }

    public StateVar(StateType state, int amount = 1) : base(Key, amount)
    {
        _state = state;
    }

    public int LivelyStacks => State == StateType.Lively ? IntValue : 0;
    public int StableStacks => State == StateType.Stable ? IntValue : 0;
}
```

### 3.4 TemperaturePower (全局温度 Power)

```csharp
namespace MaxwellsSlayingDemon.Powers;

/// <summary>
/// 温度环境 Power
/// - 温度 > 3：麦妖受到的伤害 +1
/// - 温度 < -3：冷牌费用 +1
/// </summary>
public sealed class TemperaturePower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.Neutral;
    public override PowerStackType StackType => PowerStackType.Counter;

    private const int HotThreshold = 3;
    private const int ColdThreshold = -3;

    public int GlobalTemperature => Amount;

    /// <summary>
    /// 修改费用：温度 < -3 时，冷牌费用 +1
    /// </summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (GlobalTemperature >= ColdThreshold) return false;
        if (card.Owner.Creature != base.Owner) return false;

        // 检查是否为冷牌（温度 < 0）
        if (TemperatureManager.GetCardTemperature(card) < 0)
        {
            modifiedCost = originalCost + 1;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 修改受到的伤害：温度 > 3 时，受伤 +1
    /// </summary>
    public override decimal ModifyDamageReceivedAdditive(Creature? dealer, decimal amount,
        ValueProp props, Creature? target, CardModel? cardSource)
    {
        if (target != base.Owner) return 0m;
        if (GlobalTemperature <= HotThreshold) return 0m;
        if (!props.IsPoweredAttack()) return 0m;

        return 1m;
    }
}
```

---

## 四、Harmony Patch 实现

### 4.1 热牌/冷牌打出效果

```csharp
namespace MaxwellsSlayingDemon.Temperature.Patches;

/// <summary>
/// 热牌/冷牌打出时的效果
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class TemperatureCardPlayPatch
{
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;

        // 热牌
        if (TemperatureManager.HasHeatKeyword(card))
        {
            // 1. 升温
            await TemperatureManager.ModifyGlobalTemperature(combatState, +1);

            // 2. 影响周围卡牌
            AffectAdjacentCards(combatState, card, +1, StateType.Lively);
        }
        // 冷牌
        else if (TemperatureManager.HasColdKeyword(card))
        {
            // 1. 降温
            await TemperatureManager.ModifyGlobalTemperature(combatState, -1);

            // 2. 影响周围卡牌
            AffectAdjacentCards(combatState, card, -1, StateType.Stable);
        }
    }

    private static void AffectAdjacentCards(CombatState combatState, CardModel source, int tempDelta, StateType state)
    {
        var hand = combatState.Player.PlayerCombatState.Hand;
        var cards = hand.Cards.ToList();
        var index = cards.IndexOf(source);

        if (index < 0) return;

        // 左边卡牌
        if (index > 0)
        {
            var left = cards[index - 1];
            TemperatureManager.ModifyCardTemperature(left, tempDelta);  // ← 会触发回调！
            TemperatureManager.SetCardState(left, state);
        }

        // 右边卡牌
        if (index < cards.Count - 1)
        {
            var right = cards[index + 1];
            TemperatureManager.ModifyCardTemperature(right, tempDelta);  // ← 会触发回调！
            TemperatureManager.SetCardState(right, state);
        }
    }
}
```

### 4.2 态伤害/防御修改

```csharp
namespace MaxwellsSlayingDemon.Temperature.Patches;

/// <summary>
/// 态对伤害的修改：活泼 +2 * 层数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class StateDamagePatch
{
    public static void Postfix(ref decimal __result, CardModel? cardSource, ValueProp props)
    {
        if (cardSource == null) return;
        if (!props.IsPoweredAttack()) return;

        var state = TemperatureManager.GetCardState(cardSource);
        if (state != StateType.Lively) return;

        var stacks = cardSource.DynamicVars.TryGetValue(TemperatureManager.StateVarKey, out var v)
            ? v.IntValue : 0;
        __result += 2 * stacks;
    }
}

/// <summary>
/// 态对防御的修改：稳定 +2 * 层数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
public static class StateBlockPatch
{
    public static void Postfix(ref decimal __result, CardModel? cardSource)
    {
        if (cardSource == null) return;

        var state = TemperatureManager.GetCardState(cardSource);
        if (state != StateType.Stable) return;

        var stacks = cardSource.DynamicVars.TryGetValue(TemperatureManager.StateVarKey, out var v)
            ? v.IntValue : 0;
        __result += 2 * stacks;
    }
}
```

### 4.3 绿关键词效果

```csharp
namespace MaxwellsSlayingDemon.Temperature.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class GreenKeywordPatch
{
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;

        if (!card.Keywords.Contains(Keywords.GreenKeyword))
            return;

        // 1. 将态转化为永久数值
        // TODO: 实现态转化逻辑

        // 2. 向周围无温度卡牌传递"绿"
        PropagateGreen(combatState, card);

        // 3. 添加虚无和消耗
        if (!card.Keywords.Contains(CardKeyword.Ethereal))
            CardCmd.AddKeyword(card, CardKeyword.Ethereal);
        if (!card.Keywords.Contains(CardKeyword.Exhaust))
            CardCmd.AddKeyword(card, CardKeyword.Exhaust);
    }

    private static void PropagateGreen(CombatState combatState, CardModel source)
    {
        var hand = combatState.Player.PlayerCombatState.Hand;
        var cards = hand.Cards.ToList();
        var index = cards.IndexOf(source);

        if (index < 0) return;

        void TryPropagate(CardModel target)
        {
            if (TemperatureManager.GetCardTemperature(target) == 0)
            {
                target.DynamicVars.SetDynamicVar(new GreenVar(1));
            }
        }

        if (index > 0) TryPropagate(cards[index - 1]);
        if (index < cards.Count - 1) TryPropagate(cards[index + 1]);
    }
}
```

---

## 五、卡牌实现示例

### 5.1 活塞卡牌（实现 ICardTemperatureListener）

```csharp
namespace MaxwellsSlayingDemon.Cards;

/// <summary>
/// 活塞卡牌
/// - 无法被打出
/// - 升温时，丢弃一张牌
/// - 降温时，抽取一张牌
/// </summary>
public class Piston : AbstractMaxwellCard, ICardTemperatureListener
{
    public Piston() : base(0, CardType.Status, CardRarity.Special, TargetType.None, false)
    {
    }

    protected override void OnInitialized()
    {
        Keywords.Add(CardKeyword.Unplayable);
    }

    /// <summary>
    /// 当卡牌自己的温度变化时触发
    /// </summary>
    public async Task OnCardTemperatureChanged(int oldTemp, int newTemp, int delta)
    {
        if (CombatState == null) return;

        if (delta > 0)
        {
            // 升温：丢弃一张牌
            // TODO: 让玩家选择丢弃哪张牌
            Entry.Logger.Info($"Piston: Temperature increased by {delta}, discard a card");
        }
        else if (delta < 0)
        {
            // 降温：抽取一张牌
            await CardPileCmd.Draw(Owner.PlayerChoiceContext, 1, Owner);
            Entry.Logger.Info($"Piston: Temperature decreased by {delta}, draw a card");
        }
    }
}
```

### 5.2 热牌/冷牌基类

```csharp
namespace MaxwellsSlayingDemon.Cards;

/// <summary>
/// 热牌基类 - 自动添加热词缀
/// </summary>
public abstract class HotCard : AbstractMaxwellCard
{
    protected HotCard(int energyCost, CardType type, CardRarity rarity, TargetType targetType)
        : base(energyCost, type, rarity, targetType)
    {
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Keywords.Add(Keywords.HeatKeyword);
    }
}

/// <summary>
/// 冷牌基类 - 自动添加冷词缀
/// </summary>
public abstract class ColdCard : AbstractMaxwellCard
{
    protected ColdCard(int energyCost, CardType type, CardRarity rarity, TargetType targetType)
        : base(energyCost, type, rarity, targetType)
    {
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Keywords.Add(Keywords.ColdKeyword);
    }
}
```

### 5.3 具体卡牌示例

```csharp
/// <summary>
/// 热焰斩 - 热牌，造成 6 点伤害
/// </summary>
public class HeatSlash : HotCard
{
    public HeatSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.Enemy) { }

    public override async Task Play(CombatState combatState, CardPlay cardPlay)
    {
        await DamageCmd.DealDamage(cardPlay.Target!, 6, cardPlay);
    }
}

/// <summary>
/// 冰霜守卫 - 冷牌，获得 5 点格挡
/// </summary>
public class FrostGuard : ColdCard
{
    public FrostGuard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override async Task Play(CombatState combatState, CardPlay cardPlay)
    {
        await BlockCmd.GainBlock(cardPlay.Player.Creature, 5);
    }
}
```

---

## 六、Localization 配置

```json
{
  "Maxwell_HeatKeyword": {
    "NAMES": ["热"],
    "DESCRIPTION": "打出时升温。影响周围卡牌温度+1并施加 活泼"
  },
  "Maxwell_ColdKeyword": {
    "NAMES": ["冷"],
    "DESCRIPTION": "打出时降温。影响周围卡牌温度-1并施加 稳定"
  },
  "Maxwell_Lively": {
    "NAMES": ["活泼"],
    "DESCRIPTION": "伤害数值 +2"
  },
  "Maxwell_Stable": {
    "NAMES": ["稳定"],
    "DESCRIPTION": "防御数值 +2"
  },
  "Maxwell_GreenKeyword": {
    "NAMES": ["绿"],
    "DESCRIPTION": "虚无，消耗。将态转化为永久数值，并向周围无温度卡牌传递 绿"
  }
}
```

---

## 七、关键设计总结

| 需求 | 解决方案 |
|-----|---------|
| 热/冷是词缀 | `CustomEnum` + `CardKeyword` |
| 卡牌温度变化回调 | `ICardTemperatureListener` 接口 |
| 统一操作入口 | `TemperatureManager` 静态类 |
| 触发回调时机 | `ModifyCardTemperature()` 内部检查接口 |
| 全局温度效果 | `TemperaturePower` 实现 Hook 方法 |

---

## 八、实现优先级

1. **自定义 Keyword** - HeatKeyword / ColdKeyword / GreenKeyword
2. **TemperatureManager** - 核心工具类
3. **ICardTemperatureListener** - 回调接口
4. **TemperaturePower** - 全局效果
5. **Patch** - 打出效果、态修改
6. **卡牌** - 基类和具体实现
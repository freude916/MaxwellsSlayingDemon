using BaseLib.Utils;
using MaxwellMod._Utils;
using MaxwellMod.Keywords;
using MaxwellMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature;

/// <summary>
///     温度系统的核心管理器
/// </summary>
public static class TemperatureManager
{
    // 使用 SpireField 存储卡牌温度
    // 默认温度由关键词推导：热牌 +1，冷牌 -1，其余为 0
    public static readonly SpireField<CardModel, int> CardTemperatureField = new(card =>
    {
        if (card.Keywords.Contains(MaxwellKeywords.HeatKeyword)) return 1;
        if (card.Keywords.Contains(MaxwellKeywords.ColdKeyword)) return -1;
        return 0;
    });

    // 态：用两个字段分别存储类型和层数
    public static readonly SpireField<CardModel, StateType> CardStateTypeField = new(() => StateType.None);
    public static readonly SpireField<CardModel, int> CardStateStacksField = new(() => 0);

    // 存储卡牌在手牌中的索引（在卡牌打出前记录，打出后使用）
    public static readonly SpireField<CardModel, int> HandIndexField = new(() => -1);

    #region 全局温度

    /// <summary>
    ///     获取全局温度值
    /// </summary>
    public static int GetGlobalTemperature(Player? player)
    {
        if (player == null) return 0;
        var power = player.Creature.GetPower<EnvironTempPower>();
        return power?.Amount ?? 0;
    }

    /// <summary>
    ///     修改全局温度
    /// </summary>
    /// <param name="player">目标玩家</param>
    /// <param name="delta">温度变化量</param>
    public static async Task ModifyGlobalTemperature(Player player, int delta)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (delta == 0) return;

        // PowerCmd.Apply 本身会处理叠加逻辑：如果 Power 不存在则创建，存在则叠加
        await PowerCmd.Apply<EnvironTempPower>(
            player.Creature,
            delta,
            player.Creature,
            null
        );
    }

    #endregion

    #region 卡牌温度

    /// <summary>
    ///     获取卡牌温度值
    /// </summary>
    public static int GetCardTemperature(CardModel card)
    {
        return CardTemperatureField.Get(card);
    }

    /// <summary>
    ///     修改卡牌温度值（会触发 ICardTemperatureListener 回调）
    /// </summary>
    public static void ModifyCardTemperature(CardModel card, int delta, PlayerChoiceContext choiceContext)
    {
        Entry.Logger.Info($"[TempManager] ModifyCardTemperature: {card.Id}, delta={delta}");

        if (card.Keywords.Contains(MaxwellKeywords.IsothermalKeyword) ||
            card.Keywords.Contains(MaxwellKeywords.InsulationKeyword))
        {
            Entry.Logger.Info("[TempManager] Card has thermal isolation, skipping temperature modification");
            return;
        }

        if (delta == 0)
        {
            Entry.Logger.Info("[TempManager] Delta is 0, skipping");
            return;
        }

        var oldTemp = GetCardTemperature(card);
        var newTemp = Math.Clamp(oldTemp + delta, -1, 1);
        if (newTemp == oldTemp)
        {
            Entry.Logger.Info("[TempManager] Temperature unchanged after clamp, skipping");
            return;
        }

        var appliedDelta = newTemp - oldTemp;
        
        Entry.Logger.Info($"[TempManager] Temperature change: {oldTemp} -> {newTemp}");

        // 设置新值
        CardTemperatureField.Set(card, newTemp);

        switch (newTemp)
        {
            case > 0:
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.ColdKeyword);
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.GreenKeyword);
                card.AddKeyword(MaxwellKeywords.HeatKeyword);
                break;
            case 0:
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.ColdKeyword);
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.HeatKeyword);
                break;
            case < 0:
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.HeatKeyword);
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.GreenKeyword);
                card.AddKeyword(MaxwellKeywords.ColdKeyword);
                break;
        }

        // 触发回调
        if (card is ICardTemperatureListener listener)
        {
            Entry.Logger.Info("[TempManager] Card implements ICardTemperatureListener, triggering callback");
            _ = listener.OnCardTemperatureChanged(oldTemp, newTemp, appliedDelta, choiceContext);
        }
        else
        {
            Entry.Logger.Debug("[TempManager] Card does NOT implement ICardTemperatureListener");
        }
    }

    #endregion

    #region 卡牌态

    /// <summary>
    ///     获取卡牌当前态类型
    /// </summary>
    public static StateType GetCardState(CardModel card)
    {
        return CardStateTypeField.Get(card);
    }

    /// <summary>
    ///     获取卡牌态层数
    /// </summary>
    public static int GetCardStateStacks(CardModel card)
    {
        return CardStateStacksField.Get(card);
    }

    /// <summary>
    ///     消耗卡牌当前态，并转换为永久攻防加成数值
    /// </summary>
    public static (int damageBonus, int blockBonus) ConsumeCardStateAsPermanentBonus(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var state = GetCardState(card);
        var stacks = GetCardStateStacks(card);

        if (state == StateType.None || stacks <= 0)
        {
            SetCardState(card, StateType.None);
            return (0, 0);
        }

        var damageBonus = 0;
        var blockBonus = 0;
        switch (state)
        {
            case StateType.Lively:
                damageBonus = 2 * stacks;
                break;
            case StateType.Stable:
                blockBonus = 2 * stacks;
                break;
        }

        // 固化后清空临时态，避免临时和永久重复叠加
        SetCardState(card, StateType.None);
        Entry.Logger.Info(
            $"[TempManager] ConsumeCardStateAsPermanentBonus: {card.Id}, state={state}, stacks={stacks}, damage+={damageBonus}, block+={blockBonus}");
        return (damageBonus, blockBonus);
    }

    /// <summary>
    ///     设置卡牌态（不同态会覆盖，相同态会叠加）
    /// </summary>
    public static void SetCardState(CardModel card, StateType state, int amount = 1)
    {
        var current = GetCardState(card);
        var currentStacks = GetCardStateStacks(card);
        var changed = false;
        Entry.Logger.Info($"[TempManager] SetCardState: {card.Id}, current={current}, new={state}, amount={amount}");

        if (state == StateType.None)
        {
            // 清除态
            if (current != StateType.None || currentStacks != 0)
            {
                CardStateTypeField.Set(card, StateType.None);
                CardStateStacksField.Set(card, 0);
                changed = true;
                Entry.Logger.Info("[TempManager] State cleared");
            }
        }
        else if (current == state)
        {
            // 相同态：叠加
            if (amount != 0)
            {
                var newStacks = currentStacks + amount;
                CardStateStacksField.Set(card, newStacks);
                changed = true;
                Entry.Logger.Info($"[TempManager] Same state, stacks: {currentStacks} -> {newStacks}");
            }
        }
        else
        {
            // 不同态：覆盖
            CardStateTypeField.Set(card, state);
            CardStateStacksField.Set(card, amount);
            changed = true;
            Entry.Logger.Info("[TempManager] Different state, overwritten");
        }

        if (changed)
        {
            card.Owner.PlayerCombatState?.RecalculateCardValues();
            // 刷新卡牌 UI 显示
            var nCard = MegaCrit.Sts2.Core.Nodes.Cards.NCard.FindOnTable(card);
            nCard?.UpdateVisuals(card.Pile?.Type ?? MegaCrit.Sts2.Core.Entities.Cards.PileType.None, 
                MegaCrit.Sts2.Core.Entities.Cards.CardPreviewMode.Normal);
        }
    }

    /// <summary>
    ///     获取卡牌态的额外描述文本（用于追加到卡牌描述底部）
    /// </summary>
    public static string? GetCardStateExtraCardText(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var state = GetCardState(card);
        var stacks = GetCardStateStacks(card);
        if (state == StateType.None || stacks <= 0) return null;

        var key = state switch
        {
            StateType.Lively => "MAXWELLMOD-STATE_LIVELY.extraCardText",
            StateType.Stable => "MAXWELLMOD-STATE_STABLE.extraCardText",
            _ => null
        };
        if (key == null) return null;

        var text = new LocString("static_hover_tips", key);
        text.Add("Amount", stacks);
        text.Add("Bonus", stacks * 2);
        return text.GetFormattedText();
    }

    #endregion
}

/// <summary>
///     卡牌态类型
/// </summary>
public enum StateType
{
    None,
    Lively, // 活泼：伤害+2
    Stable // 稳定：防御+2
}

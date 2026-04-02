using BaseLib.Utils;
using MaxwellMod._Utils;
using MaxwellMod.Keywords;
using MaxwellMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature;

/// <summary>
/// 温度系统的核心管理器
/// </summary>
public static class TemperatureManager
{
    // 使用 SpireField 存储卡牌温度
    public static readonly SpireField<CardModel, int> CardTemperatureField = new(() => 0);

    // 态：用两个字段分别存储类型和层数
    public static readonly SpireField<CardModel, StateType> CardStateTypeField = new(() => StateType.None);
    public static readonly SpireField<CardModel, int> CardStateStacksField = new(() => 0);

    // 存储卡牌在手牌中的索引（在卡牌打出前记录，打出后使用）
    public static readonly SpireField<CardModel, int> HandIndexField = new(() => -1);

    #region 全局温度

    /// <summary>
    /// 获取全局温度值
    /// </summary>
    public static int GetGlobalTemperature(Player? player)
    {
        if (player == null) return 0;
        var power = player.Creature.GetPower<EnvironTempPower>();
        return power?.Amount ?? 0;
    }

    /// <summary>
    /// 修改全局温度
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
    /// 获取卡牌温度值
    /// </summary>
    public static int GetCardTemperature(CardModel card)
    {
        return CardTemperatureField.Get(card);
    }

    /// <summary>
    /// 修改卡牌温度值（会触发 ICardTemperatureListener 回调）
    /// </summary>
    public static void ModifyCardTemperature(CardModel card, int delta, PlayerChoiceContext choiceContext)
    {
        Entry.Logger.Info($"[TempManager] ModifyCardTemperature: {card.Id}, delta={delta}");

        if (delta == 0)
        {
            Entry.Logger.Info("[TempManager] Delta is 0, skipping");
            return;
        }

        var oldTemp = GetCardTemperature(card);
        var newTemp = oldTemp + delta;

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
            Entry.Logger.Info($"[TempManager] Card implements ICardTemperatureListener, triggering callback");
            _ = listener.OnCardTemperatureChanged(oldTemp, newTemp, delta, choiceContext);
        }
        else
        {
            Entry.Logger.Info($"[TempManager] Card does NOT implement ICardTemperatureListener");
        }
    }

    #endregion

    #region 卡牌态

    /// <summary>
    /// 获取卡牌当前态类型
    /// </summary>
    public static StateType GetCardState(CardModel card)
    {
        return CardStateTypeField.Get(card);
    }

    /// <summary>
    /// 获取卡牌态层数
    /// </summary>
    public static int GetCardStateStacks(CardModel card)
    {
        return CardStateStacksField.Get(card);
    }

    /// <summary>
    /// 设置卡牌态（不同态会覆盖，相同态会叠加）
    /// </summary>
    public static void SetCardState(CardModel card, StateType state, int amount = 1)
    {
        var current = GetCardState(card);
        Entry.Logger.Info($"[TempManager] SetCardState: {card.Id}, current={current}, new={state}, amount={amount}");

        if (state == StateType.None)
        {
            // 清除态
            CardStateTypeField.Set(card, StateType.None);
            CardStateStacksField.Set(card, 0);
            Entry.Logger.Info($"[TempManager] State cleared");
        }
        else if (current == state)
        {
            // 相同态：叠加
            var existingStacks = GetCardStateStacks(card);
            CardStateStacksField.Set(card, existingStacks + amount);
            Entry.Logger.Info($"[TempManager] Same state, stacks: {existingStacks} -> {existingStacks + amount}");
        }
        else
        {
            // 不同态：覆盖
            CardStateTypeField.Set(card, state);
            CardStateStacksField.Set(card, amount);
            Entry.Logger.Info($"[TempManager] Different state, overwritten");
        }
    }

    #endregion

    #region 关键词检查

    /// <summary>
    /// 检查卡牌是否有热词缀
    /// </summary>
    public static bool HasHeatKeyword(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Keywords.Contains(MaxwellKeywords.HeatKeyword);
    }

    /// <summary>
    /// 检查卡牌是否有冷词缀
    /// </summary>
    public static bool HasColdKeyword(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Keywords.Contains(MaxwellKeywords.ColdKeyword);
    }

    /// <summary>
    /// 检查卡牌是否有绿词缀
    /// </summary>
    public static bool HasGreenKeyword(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Keywords.Contains(MaxwellKeywords.GreenKeyword);
    }

    #endregion
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

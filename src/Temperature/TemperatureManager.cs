using BaseLib.Utils;
using MaxwellMod._Utils;
using MaxwellMod.Keywords;
using MaxwellMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature;

/// <summary>
///     温度系统的核心管理器（规则层）
/// </summary>
public static class TemperatureManager
{
    private const int MinTemperature = -1;
    private const int MaxTemperature = 1;

    private static readonly SpireField<CardModel, int> CardTemperatureField = new(card =>
    {
        if (card.Keywords.Contains(MaxwellKeywords.HeatKeyword)) return 1;
        if (card.Keywords.Contains(MaxwellKeywords.ColdKeyword)) return -1;
        return 0;
    });

    private static readonly SpireField<CardModel, ReactivityType> CardReactivityTypeField =
        new(() => ReactivityType.None);

    private static readonly SpireField<CardModel, int> CardReactivityStacksField = new(() => 0);

    /// <summary>
    ///     规则层通知 UI 层某张卡需要刷新显示
    /// </summary>
    public static event Action<CardModel>? CardVisualRefreshRequested;

    #region 事件通知

    private static void NotifyCardsChanged(IEnumerable<CardModel> cards)
    {
        HashSet<CardModel> distinctCards = new(ReferenceEqualityComparer.Instance);
        foreach (var card in cards)
            distinctCards.Add(card);

        if (distinctCards.Count == 0) return;

        HashSet<PlayerCombatState> combatStates = new(ReferenceEqualityComparer.Instance);
        foreach (var card in distinctCards)
        {
            var combatState = card.Owner.PlayerCombatState;
            if (combatState != null)
                combatStates.Add(combatState);
        }

        foreach (var combatState in combatStates)
            combatState.RecalculateCardValues();

        foreach (var card in distinctCards)
            CardVisualRefreshRequested?.Invoke(card);
    }

    #endregion

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
    public static async Task ModifyGlobalTemperature(Player player, int delta)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (delta == 0) return;

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
        ArgumentNullException.ThrowIfNull(card);
        return CardTemperatureField.Get(card);
    }

    /// <summary>
    ///     对单卡应用温度变化（统一入口）
    /// </summary>
    public static async Task<TemperatureChangeResult> ApplyTemperatureDeltaAsync(
        CardModel card,
        int requestedDelta,
        PlayerChoiceContext choiceContext,
        TemperatureCause cause)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(choiceContext);

        var result = await ApplyTemperatureDeltaCoreAsync(card, requestedDelta, choiceContext, cause);
        if (result.AppliedDelta != 0 || result.ReactivityChanged)
            NotifyCardsChanged([card]);

        return result;
    }

    /// <summary>
    ///     对一组卡牌顺序应用温度变化（统一入口）
    /// </summary>
    public static async Task<IReadOnlyList<TemperatureChangeResult>> ApplyTemperatureBatchAsync(
        IEnumerable<CardModel> cards,
        int requestedDelta,
        PlayerChoiceContext choiceContext,
        TemperatureCause cause)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(choiceContext);

        List<TemperatureChangeResult> results = [];
        List<CardModel> changedCards = [];

        foreach (var card in cards)
        {
            var result = await ApplyTemperatureDeltaCoreAsync(card, requestedDelta, choiceContext, cause);
            results.Add(result);

            if (result.AppliedDelta != 0 || result.ReactivityChanged)
                changedCards.Add(card);
        }

        NotifyCardsChanged(changedCards);
        return results;
    }

    private static async Task<TemperatureChangeResult> ApplyTemperatureDeltaCoreAsync(
        CardModel card,
        int requestedDelta,
        PlayerChoiceContext choiceContext,
        TemperatureCause cause)
    {
        Entry.Logger.Info(
            $"[TempManager] ApplyTemperatureDelta: card={card.Id}, requestedDelta={requestedDelta}, cause={cause}");

        if (requestedDelta == 0)
        {
            var unchanged = GetCardTemperature(card);
            return new TemperatureChangeResult(
                card,
                cause,
                requestedDelta,
                0,
                unchanged,
                unchanged,
                false,
                true
            );
        }

        if (IsThermallyIsolated(card))
        {
            Entry.Logger.Info("[TempManager] Card is thermally isolated, skipping temperature and reactivity changes");
            var unchanged = GetCardTemperature(card);
            return new TemperatureChangeResult(
                card,
                cause,
                requestedDelta,
                0,
                unchanged,
                unchanged,
                false,
                true
            );
        }

        var oldTemp = GetCardTemperature(card);
        var newTemp = Math.Clamp(oldTemp + requestedDelta, MinTemperature, MaxTemperature);
        var appliedDelta = newTemp - oldTemp;

        if (appliedDelta != 0)
        {
            CardTemperatureField.Set(card, newTemp);
            SyncTemperatureKeywords(card, newTemp);
            Entry.Logger.Info($"[TempManager] Temperature changed: {oldTemp} -> {newTemp}");
        }
        else
        {
            Entry.Logger.Info("[TempManager] Temperature unchanged after clamp");
        }

        // 规则约定：按请求幅度派生态（而非按实际生效温度变化）
        var reactivityChanged = ApplyAutoReactivityFromRequestedDelta(card, requestedDelta);

        if (appliedDelta != 0 && card is ICardTemperatureListener listener)
            try
            {
                await listener.OnCardTemperatureChanged(oldTemp, newTemp, appliedDelta, choiceContext);
            }
            catch (Exception ex)
            {
                Entry.Logger.Error($"[TempManager] Listener failed for {card.Id}: {ex}");
            }

        return new TemperatureChangeResult(
            card,
            cause,
            requestedDelta,
            appliedDelta,
            oldTemp,
            newTemp,
            reactivityChanged,
            false
        );
    }

    private static bool IsThermallyIsolated(CardModel card)
    {
        return card.Keywords.Contains(MaxwellKeywords.IsothermalKeyword)
               || card.Keywords.Contains(MaxwellKeywords.InsulationKeyword);
    }

    private static void SyncTemperatureKeywords(CardModel card, int temperature)
    {
        switch (temperature)
        {
            case > 0:
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.ColdKeyword);
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.GreenKeyword);
                card.AddKeyword(MaxwellKeywords.HeatKeyword);
                break;
            case < 0:
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.HeatKeyword);
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.GreenKeyword);
                card.AddKeyword(MaxwellKeywords.ColdKeyword);
                break;
            default:
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.ColdKeyword);
                CardUtil.RemoveKeywordIfExist(card, MaxwellKeywords.HeatKeyword);
                break;
        }
    }

    #endregion

    #region 卡牌态

    /// <summary>
    ///     获取卡牌当前态类型
    /// </summary>
    public static ReactivityType GetCardReactivity(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return CardReactivityTypeField.Get(card);
    }

    /// <summary>
    ///     获取卡牌态层数（永不返回负数）
    /// </summary>
    public static int GetCardReactivityStacks(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var stacks = CardReactivityStacksField.Get(card);
        return Math.Max(0, stacks);
    }

    /// <summary>
    ///     显式设置卡牌态（不同态覆盖，相同态叠加，层数小于等于 0 时清空）
    /// </summary>
    public static void SetCardReactivity(CardModel card, ReactivityType reactivity, int amount = 1)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (SetCardReactivityInternal(card, reactivity, amount))
            NotifyCardsChanged([card]);
    }

    /// <summary>
    ///     消耗卡牌当前态，并转换为永久攻防加成数值
    /// </summary>
    public static (int damageBonus, int blockBonus) ConsumeCardReactivityAsPermanentBonus(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var reactivity = GetCardReactivity(card);
        var stacks = GetCardReactivityStacks(card);

        if (reactivity == ReactivityType.None || stacks <= 0)
        {
            SetCardReactivity(card, ReactivityType.None);
            return (0, 0);
        }

        var damageBonus = 0;
        var blockBonus = 0;
        switch (reactivity)
        {
            case ReactivityType.Potent:
                damageBonus = 2 * stacks;
                break;
            case ReactivityType.Stable:
                blockBonus = 2 * stacks;
                break;
        }

        SetCardReactivity(card, ReactivityType.None);
        Entry.Logger.Info(
            $"[TempManager] ConsumeCardReactivityAsPermanentBonus: {card.Id}, reactivity={reactivity}, stacks={stacks}, damage+={damageBonus}, block+={blockBonus}");
        return (damageBonus, blockBonus);
    }

    private static bool ApplyAutoReactivityFromRequestedDelta(CardModel card, int requestedDelta)
    {
        if (requestedDelta == 0) return false;

        var reactivity = requestedDelta > 0 ? ReactivityType.Potent : ReactivityType.Stable;
        var amount = Math.Abs(requestedDelta);
        return SetCardReactivityInternal(card, reactivity, amount);
    }

    private static bool SetCardReactivityInternal(CardModel card, ReactivityType reactivity, int amount)
    {
        var current = GetCardReactivity(card);
        var currentStacks = GetCardReactivityStacks(card);

        if (reactivity == ReactivityType.None || amount <= 0)
        {
            if (current == ReactivityType.None && currentStacks == 0) return false;

            CardReactivityTypeField.Set(card, ReactivityType.None);
            CardReactivityStacksField.Set(card, 0);
            return true;
        }

        if (current == reactivity)
        {
            var newStacks = currentStacks + amount;
            if (newStacks <= 0)
            {
                CardReactivityTypeField.Set(card, ReactivityType.None);
                CardReactivityStacksField.Set(card, 0);
                return true;
            }

            if (newStacks == currentStacks) return false;

            CardReactivityStacksField.Set(card, newStacks);
            return true;
        }

        CardReactivityTypeField.Set(card, reactivity);
        CardReactivityStacksField.Set(card, amount);
        return current != reactivity || currentStacks != amount;
    }

    #endregion
}
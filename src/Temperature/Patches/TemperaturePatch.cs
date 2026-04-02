using BaseLib.Extensions;
using HarmonyLib;
using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
/// 在卡牌从手牌移出前记录索引
/// CardPileCmd.AddDuringManualCardPlay 会在 BeforeCardPlayed 之前被调用，此时卡牌还在手牌中
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddDuringManualCardPlay))]
public static class RecordHandIndexPatch
{
    public static void Prefix(CardModel card)
    {
        var owner = card.Owner;
        if (owner.PlayerCombatState == null) return;

        var hand = owner.PlayerCombatState.Hand;
        var cards = hand.Cards.ToList();
        var index = cards.IndexOf(card);

        Entry.Logger.Info($"[TemperaturePatch] AddDuringManualCardPlay Prefix: {card.Id}, hand index: {index}");

        // 记录手牌索引
        TemperatureManager.HandIndexField.Set(card, index);
    }
}

/// <summary>
/// 热牌/冷牌打出后的效果（使用之前记录的手牌索引）
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class TemperatureCardPlayPatch
{
#pragma warning disable IDE0060
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
#pragma warning restore IDE0060
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        try
        {
            var card = cardPlay.Card;
            var owner = card.Owner;

            Entry.Logger.Info($"[TemperaturePatch] AfterCardPlayed triggered for card: {card.Id}");

            // 检查卡牌的所有关键词
            var keywords = card.Keywords;
            Entry.Logger.Info($"[TemperaturePatch] Card keywords count: {keywords.Count}");
            foreach (var kw in keywords)
            {
                Entry.Logger.Info($"[TemperaturePatch]   - Keyword: {kw.GetTitle().GetFormattedText()}");
            }

            // 热牌
            var hasHeat = keywords.Contains(MaxwellKeywords.HeatKeyword);
            var hasCold = keywords.Contains(MaxwellKeywords.ColdKeyword);
            Entry.Logger.Info($"[TemperaturePatch] HasHeat: {hasHeat}, HasCold: {hasCold}");

            if (hasHeat)
            {
                Entry.Logger.Info("[TemperaturePatch] Heat card detected! Applying effects...");
                // 1. 升温
                await TemperatureManager.ModifyGlobalTemperature(owner, +1);
                Entry.Logger.Info("[TemperaturePatch] Global temperature +1 applied");

                // 2. 影响周围卡牌（使用记录的索引）
                AffectAdjacentCards(card, +1, StateType.Lively, choiceContext);
            }
            // 冷牌
            else if (hasCold)
            {
                Entry.Logger.Info("[TemperaturePatch] Cold card detected! Applying effects...");
                // 1. 降温
                await TemperatureManager.ModifyGlobalTemperature(owner, -1);
                Entry.Logger.Info("[TemperaturePatch] Global temperature -1 applied");

                // 2. 影响周围卡牌（使用记录的索引）
                AffectAdjacentCards(card, -1, StateType.Stable, choiceContext);
            }
            else
            {
                Entry.Logger.Info("[TemperaturePatch] No heat/cold keyword found, skipping");
            }

            // 清理记录的索引
            TemperatureManager.HandIndexField.Set(card, -1);
        }
        catch (Exception e)
        {
            Entry.Logger.Error($"[TemperaturePatch] Error: {e}");
        }
    }

    private static void AffectAdjacentCards(CardModel source, int tempDelta, StateType state, PlayerChoiceContext choiceContext)
    {
        Entry.Logger.Info($"[TemperaturePatch] AffectAdjacentCards called for {source.Id}, delta={tempDelta}, state={state}");

        var owner = source.Owner;

        if (owner.PlayerCombatState == null)
        {
            Entry.Logger.Warn("[TemperaturePatch] PlayerCombatState is null!");
            return;
        }

        // 使用之前记录的手牌索引
        var recordedIndex = TemperatureManager.HandIndexField.Get(source);
        Entry.Logger.Info($"[TemperaturePatch] Recorded hand index: {recordedIndex}");

        if (recordedIndex < 0)
        {
            Entry.Logger.Warn("[TemperaturePatch] No recorded hand index found!");
            return;
        }

        var hand = owner.PlayerCombatState.Hand;
        var cards = hand.Cards.ToList();
        Entry.Logger.Info($"[TemperaturePatch] Current hand size: {cards.Count}");

        // 左边卡牌
        if (recordedIndex > 0 && recordedIndex - 1 < cards.Count)
        {
            var left = cards[recordedIndex - 1];
            Entry.Logger.Info($"[TemperaturePatch] Affecting LEFT card: {left.Id}");
            TemperatureManager.ModifyCardTemperature(left, tempDelta, choiceContext);
            TemperatureManager.SetCardState(left, state);
        }
        else
        {
            Entry.Logger.Info("[TemperaturePatch] No card on the left");
        }

        // 右边卡牌
        if (recordedIndex < cards.Count)
        {
            var right = cards[recordedIndex];
            Entry.Logger.Info($"[TemperaturePatch] Affecting RIGHT card: {right.Id}");
            TemperatureManager.ModifyCardTemperature(right, tempDelta, choiceContext);
            TemperatureManager.SetCardState(right, state);
        }
        else
        {
            Entry.Logger.Info("[TemperaturePatch] No card on the right");
        }
    }
}

/// <summary>
/// 态对伤害的修改：活泼 +2 * 层数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class StateDamagePatch
{
    public static void Postfix(ref decimal __result, CardModel? cardSource, ValueProp props)
    {
        if (cardSource == null) return;
        if (!props.IsPoweredAttack_()) return;

        var state = TemperatureManager.GetCardState(cardSource);
        if (state != StateType.Lively) return;

        var stacks = TemperatureManager.GetCardStateStacks(cardSource);
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

        var stacks = TemperatureManager.GetCardStateStacks(cardSource);
        __result += 2 * stacks;
    }
}

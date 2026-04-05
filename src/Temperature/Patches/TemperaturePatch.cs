using BaseLib.Extensions;
using HarmonyLib;
using MaxwellMod.Cards;
using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     在卡牌从手牌移出前记录索引
///     CardPileCmd.AddDuringManualCardPlay 会在 BeforeCardPlayed 之前被调用，此时卡牌还在手牌中
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddDuringManualCardPlay))]
public static class RecordHandIndexPatch
{
    public static void Prefix(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        
        var owner = card.Owner;
        if (owner.PlayerCombatState == null) return;

        var hand = owner.PlayerCombatState.Hand;
        var cards = hand.Cards.ToList();
        var index = cards.IndexOf(card);

        Entry.Logger.Debug($"[TemperaturePatch] AddDuringManualCardPlay Prefix: {card.Id}, hand index: {index}");

        // 记录手牌索引
        TemperatureManager.HandIndexField.Set(card, index);
    }
}

/// <summary>
///     热牌/冷牌/绿牌打出后的效果（使用之前记录的手牌索引）
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class TemperatureCardPlayPatch
{
#pragma warning disable IDE0060
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
#pragma warning restore IDE0060
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        var card = cardPlay.Card;
        var owner = card.Owner;

        // 热牌
        var hasHeat = card.Keywords.Contains(MaxwellKeywords.HeatKeyword);
        var hasCold = card.Keywords.Contains(MaxwellKeywords.ColdKeyword);
        var hasGreen = card.Keywords.Contains(MaxwellKeywords.GreenKeyword);
        var hasInsulation = card.Keywords.Contains(MaxwellKeywords.InsulationKeyword);
        Entry.Logger.Debug($"[TemperaturePatch] HasHeat: {hasHeat}, HasCold: {hasCold}, HasGreen: {hasGreen}, HasInsulation: {hasInsulation}");
        
        // 传热
        if (hasHeat && !hasInsulation)
        {   
            Entry.Logger.Info("[TemperaturePatch] Heat card detected! Applying effects...");
            
            // 1. 升温
            await TemperatureManager.ModifyGlobalTemperature(owner, +1);

            // 2. 影响周围卡牌（使用记录的索引）
            ConductToAdjacentCards(card, +1, StateType.Lively, choiceContext);
            
        }
        // 传冷
        else if (hasCold&& !hasInsulation)
        {
            Entry.Logger.Info("[TemperaturePatch] Cold card detected! Applying effects...");
            
            // 1. 降温
            await TemperatureManager.ModifyGlobalTemperature(owner, -1);

            // 2. 影响周围卡牌（使用记录的索引）
            ConductToAdjacentCards(card, -1, StateType.Stable, choiceContext);
        }
        // 绿牌
        else if (hasGreen)
        {
            ConvertStateToPermanent(card);
        }

        // 清理记录的索引
        TemperatureManager.HandIndexField.Set(card, -1);
    }

    private static void ConductToAdjacentCards(CardModel source, int tempDelta, StateType state,
        PlayerChoiceContext choiceContext)
    {
        Entry.Logger.Debug($"[TemperaturePatch] ConductToAdjacentCards called for {source.Id}, delta={tempDelta}, state={state}");

        var owner = source.Owner;

        if (owner.PlayerCombatState == null)
        {
            Entry.Logger.Warn("[TemperaturePatch] PlayerCombatState is null!");
            return;
        }

        // 使用之前记录的手牌索引
        var recordedIndex = TemperatureManager.HandIndexField.Get(source);
        Entry.Logger.Debug($"[TemperaturePatch] Recorded hand index: {recordedIndex}");

        if (recordedIndex < 0)
        {
            Entry.Logger.Warn("[TemperaturePatch] No recorded hand index found!");
            return;
        }

        var hand = owner.PlayerCombatState.Hand;
        var cards = hand.Cards.ToList();
        Entry.Logger.Debug($"[TemperaturePatch] Current hand size: {cards.Count}");

        // 左边卡牌
        if (recordedIndex > 0 && recordedIndex - 1 < cards.Count)
        {
            var left = cards[recordedIndex - 1];
            Entry.Logger.Debug($"[TemperaturePatch] Affecting LEFT card: {left.Id}");
            if (left.Keywords.Contains(MaxwellKeywords.IsothermalKeyword) ||
                left.Keywords.Contains(MaxwellKeywords.InsulationKeyword))
            {
                Entry.Logger.Debug("[TemperaturePatch] LEFT card is thermal-isolated, skipping");
            }
            else
            {
                TemperatureManager.ModifyCardTemperature(left, tempDelta, choiceContext);
                TemperatureManager.SetCardState(left, state);
            }
        }
        else
        {
            Entry.Logger.Debug("[TemperaturePatch] No card on the left");
        }

        // 右边卡牌
        if (recordedIndex < cards.Count)
        {
            var right = cards[recordedIndex];
            Entry.Logger.Debug($"[TemperaturePatch] Affecting RIGHT card: {right.Id}");
            if (right.Keywords.Contains(MaxwellKeywords.IsothermalKeyword) ||
                right.Keywords.Contains(MaxwellKeywords.InsulationKeyword))
            {
                Entry.Logger.Debug("[TemperaturePatch] RIGHT card is thermal-isolated, skipping");
            }
            else
            {
                TemperatureManager.ModifyCardTemperature(right, tempDelta, choiceContext);
                TemperatureManager.SetCardState(right, state);
            }
        }
        else
        {
            Entry.Logger.Debug("[TemperaturePatch] No card on the right");
        }
    }

    private static void ConvertStateToPermanent(CardModel card)
    {
        var hasConvertibleTarget = card is AbstractMaxwellCard || card.DeckVersion is AbstractMaxwellCard;
        if (!hasConvertibleTarget)
        {
            Entry.Logger.Warn($"[TemperaturePatch] Skip green conversion: no AbstractMaxwellCard target ({card.Id})");
            return;
        }

        var (damageBonus, blockBonus) = TemperatureManager.ConsumeCardStateAsPermanentBonus(card);
        if (damageBonus == 0 && blockBonus == 0)
        {
            Entry.Logger.Debug("[TemperaturePatch] No state bonus to convert");
            return;
        }

        ApplyPermanentBonusToCard(card, damageBonus, blockBonus);

        if (card.DeckVersion != null && !ReferenceEquals(card.DeckVersion, card))
            ApplyPermanentBonusToCard(card.DeckVersion, damageBonus, blockBonus);
    }

    private static void ApplyPermanentBonusToCard(CardModel card, int damageBonus, int blockBonus)
    {
        if (card is not AbstractMaxwellCard maxwellCard)
        {
            Entry.Logger.Warn($"[TemperaturePatch] Skip permanent bonus: card is not AbstractMaxwellCard ({card.Id})");
            return;
        }

        maxwellCard.ApplyPermanentStateBonus(damageBonus, blockBonus);
        Entry.Logger.Debug(
            $"[TemperaturePatch] Permanent bonus applied: {card.Id}, damage+={damageBonus}, block+={blockBonus}");
    }
}

/// <summary>
///     态对伤害的修改：活泼 +2 * 层数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class StateDamagePatch
{
    public static void Postfix(ref decimal __result, CardModel? cardSource, ValueProp props)
    {
        if (cardSource == null) return;
        if (!props.IsPoweredAttack_()) return;

        if (cardSource is AbstractMaxwellCard maxwellCard) __result += maxwellCard.MaxwellMod_PermDamage;

        var state = TemperatureManager.GetCardState(cardSource);
        if (state == StateType.Lively)
        {
            var stacks = TemperatureManager.GetCardStateStacks(cardSource);
            __result += 2 * stacks;
        }
    }
}

/// <summary>
///     态对防御的修改：稳定 +2 * 层数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
public static class StateBlockPatch
{
    public static void Postfix(ref decimal __result, CardModel? cardSource)
    {
        if (cardSource == null) return;

        if (cardSource is AbstractMaxwellCard maxwellCard) __result += maxwellCard.MaxwellMod_PermBlock;

        var state = TemperatureManager.GetCardState(cardSource);
        if (state == StateType.Stable)
        {
            var stacks = TemperatureManager.GetCardStateStacks(cardSource);
            __result += 2 * stacks;
        }
    }
}

/// <summary>
///     将卡牌态以额外文本的形式追加到卡牌描述底部（类似 extraCardText）
///     直接补丁到私有核心重载，避免 public wrapper 被 JIT 内联后不触发。
/// </summary>
[HarmonyPatch]
public static class CardStateExtraCardTextPatch
{
    private static MethodBase TargetMethod()
    {
        var previewType = AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType")
                          ?? throw new MissingMemberException(typeof(CardModel).FullName, "DescriptionPreviewType");

        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", [typeof(PileType), previewType, typeof(Creature)])
               ?? throw new MissingMethodException(typeof(CardModel).FullName, "GetDescriptionForPile(PileType, DescriptionPreviewType, Creature)");
    }

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance.CombatState == null) return;

        var extraText = TemperatureManager.GetCardStateExtraCardText(__instance);
        if (string.IsNullOrEmpty(extraText)) return;
        
        __result = string.IsNullOrWhiteSpace(__result)
            ? extraText
            : $"{__result}\n{extraText}";
    }
}

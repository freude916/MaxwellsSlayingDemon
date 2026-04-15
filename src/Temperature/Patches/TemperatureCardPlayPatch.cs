using HarmonyLib;
using MaxwellMod._Utils;
using MaxwellMod.Cards;
using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     热牌/冷牌/绿牌打出后的效果（消费打出前相邻快照）
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class TemperatureCardPlayPatch
{
#pragma warning disable IDE0060
    public static void Postfix(ref Task __result, CombatState combatState, PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
#pragma warning restore IDE0060
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        __result = WrapAfterCardPlayedAsync(__result, choiceContext, cardPlay);
    }

    private static async Task WrapAfterCardPlayedAsync(Task originalTask, PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await originalTask;

        var card = cardPlay.Card;
        try
        {
            await ProcessCardTemperature(card, choiceContext);
        }
        finally
        {
            CardAdjacencySnapshotStore.ClearPlayAdjacencySnapshot(card);
        }
    }

    private static async Task ProcessCardTemperature(CardModel card, PlayerChoiceContext choiceContext)
    {
        var owner = card.Owner;

        var hasHeat = card.Keywords.Contains(MaxwellKeywords.HeatKeyword);
        var hasCold = card.Keywords.Contains(MaxwellKeywords.ColdKeyword);
        var hasGreen = card.Keywords.Contains(MaxwellKeywords.GreenKeyword);
        var hasInsulation = card.Keywords.Contains(MaxwellKeywords.InsulationKeyword);

        Entry.Logger.Debug(
            $"[TemperaturePatch] Process play keyword flags: heat={hasHeat}, cold={hasCold}, green={hasGreen}, insulation={hasInsulation}");

        if (hasHeat && !hasInsulation)
        {
            await TemperatureManager.ModifyGlobalTemperature(owner, +1);
            await ConductToAdjacentCards(card, +1, choiceContext);
        }
        else if (hasCold && !hasInsulation)
        {
            await TemperatureManager.ModifyGlobalTemperature(owner, -1);
            await ConductToAdjacentCards(card, -1, choiceContext);
        }
        else if (hasGreen)
        {
            ConvertReactivityToPermanent(card);
        }
    }

    private static async Task ConductToAdjacentCards(CardModel source, int requestedDelta,
        PlayerChoiceContext choiceContext)
    {
        if (!CardAdjacencySnapshotStore.TryGetPlayAdjacencySnapshot(source, out var left, out var right))
        {
            Entry.Logger.Warn($"[TemperaturePatch] Missing adjacency snapshot for {source.Id}");
            return;
        }

        List<CardModel> targets = [];
        if (left != null) targets.Add(left);
        if (right != null) targets.Add(right);

        if (targets.Count == 0) return;

        await TemperatureManager.ApplyTemperatureBatchAsync(
            targets,
            requestedDelta,
            choiceContext,
            TemperatureCause.PlayedHeatCold
        );
    }

    private static void ConvertReactivityToPermanent(CardModel card)
    {
        var hasConvertibleTarget = card is AbstractMaxwellCard || card.DeckVersion is AbstractMaxwellCard;
        if (!hasConvertibleTarget)
        {
            Entry.Logger.Warn($"[TemperaturePatch] Skip green conversion: no AbstractMaxwellCard target ({card.Id})");
            return;
        }

        var (damageBonus, blockBonus) = TemperatureManager.ConsumeCardReactivityAsPermanentBonus(card);
        if (damageBonus == 0 && blockBonus == 0)
        {
            Entry.Logger.Debug("[TemperaturePatch] No reactivity bonus to convert");
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

        maxwellCard.ApplyPermanentReactivityBonus(damageBonus, blockBonus);
        Entry.Logger.Debug(
            $"[TemperaturePatch] Permanent bonus applied: {card.Id}, damage+={damageBonus}, block+={blockBonus}");
    }
}
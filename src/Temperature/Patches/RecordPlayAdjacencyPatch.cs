using HarmonyLib;
using MaxwellMod._Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     在卡牌离开手牌前记录“打出前相邻快照”
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddDuringManualCardPlay))]
public static class RecordPlayAdjacencyPatch
{
    public static void Prefix(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var hand = card.Owner.PlayerCombatState?.Hand;
        if (hand == null)
        {
            CardAdjacencySnapshotStore.ClearPlayAdjacencySnapshot(card);
            return;
        }

        var cards = hand.Cards.ToList();
        var index = cards.IndexOf(card);
        if (index < 0)
        {
            CardAdjacencySnapshotStore.ClearPlayAdjacencySnapshot(card);
            return;
        }

        var left = cards.ElementAtOrDefault(index - 1);
        var right = cards.ElementAtOrDefault(index + 1);

        Entry.Logger.Debug(
            $"[TemperaturePatch] Record snapshot: {card.Id}, left={left?.Id.ToString() ?? "null"}, right={right?.Id.ToString() ?? "null"}");

        CardAdjacencySnapshotStore.RecordPlayAdjacencySnapshot(card, left, right);
    }
}
using BaseLib.Utils;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod._Utils;

/// <summary>
///     手牌相邻位置快照存储壳类。
///     后续可将 TemperatureManager 中的快照字段与读写逻辑迁移到这里。
/// </summary>
public static class CardAdjacencySnapshotStore
{
    private static readonly SpireField<CardModel, PlayAdjacencySnapshot?> PlayAdjacencySnapshotField = new(() => null);

    public static void RecordPlayAdjacencySnapshot(CardModel source, CardModel? left, CardModel? right)
    {
        ArgumentNullException.ThrowIfNull(source);
        PlayAdjacencySnapshotField.Set(source, new PlayAdjacencySnapshot(left, right));
    }

    public static bool TryGetPlayAdjacencySnapshot(CardModel source, out CardModel? left, out CardModel? right)
    {
        ArgumentNullException.ThrowIfNull(source);

        var snapshot = PlayAdjacencySnapshotField.Get(source);
        if (snapshot == null)
        {
            left = null;
            right = null;
            return false;
        }

        left = snapshot.Value.Left;
        right = snapshot.Value.Right;
        return true;
    }

    public static void ClearPlayAdjacencySnapshot(CardModel source)
    {
        ArgumentNullException.ThrowIfNull(source);
        PlayAdjacencySnapshotField.Set(source, null);
    }

    /// <summary>
    ///     获取“当前手牌”语义下的左右相邻（用于非打出流程）
    /// </summary>
    public static (CardModel? left, CardModel? right) GetAdjacentCardsInHand(CardModel source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var hand = source.Owner.PlayerCombatState?.Hand;
        if (hand == null) return (null, null);

        var cards = hand.Cards.ToList();
        var index = cards.IndexOf(source);
        if (index < 0) return (null, null);

        return (cards.ElementAtOrDefault(index - 1), cards.ElementAtOrDefault(index + 1));
    }
}
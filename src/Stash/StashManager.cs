using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Stash;

/// <summary>
///     暂存区管理器
///     管理玩家暂存的卡牌，下回合开始时自动返回手牌
/// </summary>
public static class StashManager
{
    private static readonly Dictionary<Player, CardPile> _stashPiles = new();

    /// <summary>
    ///     获取或创建玩家的暂存区
    /// </summary>
    public static CardPile GetOrCreateStash(Player player)
    {
        if (!_stashPiles.TryGetValue(player, out var pile))
        {
            // 使用 PileType.None 创建不在正常循环中的卡牌堆
            pile = new CardPile(PileType.None);
            _stashPiles[player] = pile;
        }

        return pile;
    }

    /// <summary>
    ///     添加卡牌到暂存区
    /// </summary>
    public static void AddCardToStash(Player player, CardModel card)
    {
        var pile = GetOrCreateStash(player);

        GD.Print($"[MaxwellMod] StashManager: Adding card to stash (current count: {pile.Cards.Count})");

        // 添加到牌堆
        pile.AddInternal(card, -1, false);

        // 关键：手动触发 CardAddFinished 事件
        pile.InvokeCardAddFinished();

        // 手动订阅 StateTracker（因为 PileType.None 不是战斗牌堆）
        if (CombatManager.Instance.IsInProgress) CombatManager.Instance.StateTracker.Subscribe(card);

        GD.Print($"[MaxwellMod] StashManager: Card added (new count: {pile.Cards.Count})");
    }

    /// <summary>
    ///     从暂存区移除卡牌
    /// </summary>
    public static void RemoveCardFromStash(Player player, CardModel card)
    {
        var pile = GetOrCreateStash(player);

        if (pile.Cards.Contains(card))
        {
            pile.RemoveInternal(card, false);

            // 关键：手动触发 CardRemoveFinished 事件
            pile.InvokeCardRemoveFinished();

            // 手动取消订阅 StateTracker
            if (CombatManager.Instance.IsInProgress) CombatManager.Instance.StateTracker.Unsubscribe(card);
        }
    }

    /// <summary>
    ///     将暂存区的卡牌返回到手牌
    /// </summary>
    public static async Task ReturnAllCardsToHandAsync(Player player)
    {
        var pile = GetOrCreateStash(player);

        if (pile.Cards.Count == 0) return;

        GD.Print($"[MaxwellMod] StashManager: Returning {pile.Cards.Count} cards to hand");

        // 复制列表，因为我们在迭代时会修改原列表
        var cardsToReturn = pile.Cards.ToList();

        // 先从暂存区移除所有卡牌
        foreach (var card in cardsToReturn) RemoveCardFromStash(player, card);

        // 使用 CardPileCmd.Add 正确添加到手牌（包括 UI）
        await CardPileCmd.Add(cardsToReturn, PileType.Hand);

        GD.Print($"[MaxwellMod] StashManager: Returned {cardsToReturn.Count} cards to hand");
    }

    /// <summary>
    ///     将暂存区的卡牌返回到手牌（同步版本）
    /// </summary>
    public static void ReturnAllCardsToHand(Player player)
    {
        var pile = GetOrCreateStash(player);

        if (pile.Cards.Count == 0) return;

        GD.Print($"[MaxwellMod] StashManager: Returning {pile.Cards.Count} cards to hand");

        // 复制列表，因为我们在迭代时会修改原列表
        var cardsToReturn = pile.Cards.ToList();

        foreach (var card in cardsToReturn)
        {
            RemoveCardFromStash(player, card);

            // 添加到手牌
            var handPile = PileType.Hand.GetPile(player);
            handPile.AddInternal(card, -1, false);
            handPile.InvokeCardAddFinished();

            // 重新订阅 StateTracker
            if (CombatManager.Instance.IsInProgress) CombatManager.Instance.StateTracker.Subscribe(card);

            GD.Print($"[MaxwellMod] StashManager: Returned card {card.Id.Entry} to hand");
        }
    }

    /// <summary>
    ///     清理指定玩家的暂存区
    /// </summary>
    public static void ClearStash(Player player)
    {
        if (_stashPiles.TryGetValue(player, out var pile))
        {
            // 正确取消所有卡牌的订阅
            foreach (var card in pile.Cards.ToList())
                if (CombatManager.Instance.IsInProgress)
                    CombatManager.Instance.StateTracker.Unsubscribe(card);

            pile.Clear();
            _stashPiles.Remove(player);
        }
    }

    /// <summary>
    ///     清理所有暂存区
    /// </summary>
    public static void ClearAll()
    {
        foreach (var kvp in _stashPiles)
        {
            foreach (var card in kvp.Value.Cards.ToList())
                if (CombatManager.Instance.IsInProgress)
                    CombatManager.Instance.StateTracker.Unsubscribe(card);

            kvp.Value.Clear();
        }

        _stashPiles.Clear();
    }
}
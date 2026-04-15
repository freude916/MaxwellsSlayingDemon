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

        // 添加到牌堆
        pile.AddInternal(card);

        // 关键：手动触发 CardAddFinished 事件
        pile.InvokeCardAddFinished();
    }

    /// <summary>
    ///     从暂存区移除卡牌
    /// </summary>
    public static void RemoveCardFromStash(Player player, CardModel card)
    {
        var pile = GetOrCreateStash(player);

        if (!pile.Cards.Contains(card)) return;

        pile.RemoveInternal(card);

        // 关键：手动触发 CardRemoveFinished 事件
        pile.InvokeCardRemoveFinished();
    }
}
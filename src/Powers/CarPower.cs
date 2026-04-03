using System.Linq;
using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Powers;

/// <summary>
///     车：每当你打出热牌时，弃掉最右手牌并抽牌
/// </summary>
public class CarPower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (!cardPlay.Card.Keywords.Contains(MaxwellKeywords.HeatKeyword)) return;

        Flash();
        var discardCount = Math.Max(0, Amount);
        if (discardCount == 0) return;

        // 先丢弃右边 n 张
        var discarded = await DiscardRightmostCards(context, discardCount);
        // 再抽 n 张
        if (Owner.Player != null && discarded > 0)
        {
            await CardPileCmd.Draw(context, discarded, Owner.Player);
        }
    }

    private async Task<int> DiscardRightmostCards(PlayerChoiceContext context, int count)
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState == null) return 0;

        var cardsInHand = player.PlayerCombatState.Hand.Cards.ToList();
        if (cardsInHand.Count == 0) return 0;

        var toDiscard = Math.Min(count, cardsInHand.Count);
        // 从右边开始丢弃
        for (var i = 0; i < toDiscard; i++)
        {
            var rightmostCard = cardsInHand[cardsInHand.Count - 1 - i];
            await CardCmd.Discard(context, rightmostCard);
        }
        return toDiscard;
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Powers;

/// <summary>
///     势阱：你的回合内，置入弃牌堆的状态牌改为消耗
/// </summary>
public class PotentialTrapPower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (Owner.Player == null) return;
        if (card.Owner != Owner.Player) return;
        if (card.Type != CardType.Status) return;
        if (card.Pile?.Type != PileType.Discard) return;
        if (CombatState?.CurrentSide != Owner.Side) return;

        Flash();
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);
    }
}

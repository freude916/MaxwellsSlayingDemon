using MaxwellMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     势阱：本回合置入弃牌堆的状态牌会被消耗
/// </summary>
public class PotentialTrap : AbstractMaxwellCard
{
    public PotentialTrap() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<PotentialTrapPower>(Owner.Creature, 1, Owner.Creature, this);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Cards;

/// <summary>
///     冷墙：按手牌中冷牌数量获得格挡，然后这些牌变为常温
/// </summary>
public class ColdWall : AbstractMaxwellCard
{
    public ColdWall() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4m, ValueProp.Move)
    ];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        var coldCards = handCards.Where(card => TemperatureManager.GetCardTemperature(card) < 0).ToList();

        for (var i = 0; i < coldCards.Count; i++)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay, fast: true);

        foreach (var coldCard in coldCards)
        {
            var currentTemperature = TemperatureManager.GetCardTemperature(coldCard);
            TemperatureManager.ModifyCardTemperature(coldCard, -currentTemperature, choiceContext);
        }
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

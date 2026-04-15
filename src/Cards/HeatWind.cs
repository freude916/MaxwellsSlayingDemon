using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     热风：使手牌全部升温 1
/// </summary>
public class HeatWind : AbstractMaxwellCard
{
    public HeatWind() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        await TemperatureManager.ApplyTemperatureBatchAsync(
            handCards,
            +1,
            choiceContext,
            TemperatureCause.CardEffect
        );
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
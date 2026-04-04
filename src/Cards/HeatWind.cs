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

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in handCards) TemperatureManager.ModifyCardTemperature(card, +1, choiceContext);

        await Task.CompletedTask;
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

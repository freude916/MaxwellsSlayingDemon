using MaxwellMod.Stash;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Cards;

/// <summary>
///     通缩
///     - 所有手牌降温 1, 本回合费用 -1, 态被去除
/// </summary>
public class Deflation : AbstractMaxwellCard
{
    public Deflation() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in handCards)
        {
            card.EnergyCost.AddThisTurn(-1, reduceOnly: true);
            TemperatureManager.ModifyCardTemperature(card, -1, choiceContext);
            TemperatureManager.SetCardState(card, StateType.None);
        }

        PileType.Hand.GetPile(Owner).Cards.ToList().ForEach(card => TemperatureManager.SetCardState(card, StateType.None));
        PileType.Discard.GetPile(Owner).Cards.ToList().ForEach(card => TemperatureManager.SetCardState(card, StateType.None));

        await Task.CompletedTask;
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

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

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in handCards)
            card.EnergyCost.AddThisTurn(-1, reduceOnly: true);

        await TemperatureManager.ApplyTemperatureBatchAsync(
            handCards,
            -1,
            choiceContext,
            TemperatureCause.CardEffect
        );

        foreach (var card in handCards)
            TemperatureManager.SetCardReactivity(card, ReactivityType.None);

        foreach (var card in PileType.Discard.GetPile(Owner).Cards)
            TemperatureManager.SetCardReactivity(card, ReactivityType.None);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     回溯
///     - 消耗 1 张手牌
///     - 将消耗堆中所有与该牌温度相同的牌置入手牌
/// </summary>
public class Retrospect : AbstractMaxwellCard
{
    public Retrospect() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            null,
            this
        )).FirstOrDefault();

        if (selectedCard == null) return;

        var targetTemperature = TemperatureManager.GetCardTemperature(selectedCard);
        await CardCmd.Exhaust(choiceContext, selectedCard);

        var cardsToReturn = PileType.Exhaust.GetPile(Owner).Cards
            .Where(card => TemperatureManager.GetCardTemperature(card) == targetTemperature)
            .ToList();

        if (cardsToReturn.Count > 0) await CardPileCmd.Add(cardsToReturn, PileType.Hand);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MaxwellMod.Cards;

/// <summary>
///     凝固：使一张手牌降温 1，并获得保留
/// </summary>
public class Solidification : AbstractMaxwellCard
{
    public Solidification() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
            null,
            this
        )).FirstOrDefault();

        if (selectedCard == null) return;

        await TemperatureManager.ApplyTemperatureDeltaAsync(
            selectedCard,
            -1,
            choiceContext,
            TemperatureCause.CardEffect
        );
        selectedCard.AddKeyword(CardKeyword.Retain);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
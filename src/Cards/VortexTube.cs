using MaxwellMod._Utils;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     涡流管：
///     - 抽 1 张牌
///     - 使一张手牌降温
///     - 使其相邻牌升温
/// </summary>
public class VortexTube : AbstractMaxwellCard
{
    public VortexTube() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        await CardPileCmd.Draw(choiceContext, 1, Owner);

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

        var (left, right) = CardAdjacencySnapshotStore.GetAdjacentCardsInHand(selectedCard);
        if (left != null)
            await TemperatureManager.ApplyTemperatureDeltaAsync(
                left,
                +1,
                choiceContext,
                TemperatureCause.CardEffect
            );
        if (right != null)
            await TemperatureManager.ApplyTemperatureDeltaAsync(
                right,
                +1,
                choiceContext,
                TemperatureCause.CardEffect
            );
    }

    protected override void OnUpgrade()
    {
    }
}
using MaxwellMod._Utils;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MaxwellMod.Cards;

/// <summary>
///     绝热膨胀：两侧手牌降温，并提供下回合抽牌
/// </summary>
public class AdiabaticExpansion : AbstractMaxwellCard
{
    public AdiabaticExpansion() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CoolAdjacentCards(choiceContext, cardPlay.Card);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    private async Task CoolAdjacentCards(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (!CardAdjacencySnapshotStore.TryGetPlayAdjacencySnapshot(sourceCard, out var left, out var right)) return;

        List<CardModel> targets = [];
        if (left != null) targets.Add(left);
        if (right != null) targets.Add(right);
        if (targets.Count == 0) return;

        await TemperatureManager.ApplyTemperatureBatchAsync(
            targets,
            -1,
            choiceContext,
            TemperatureCause.CardEffect
        );
    }
}
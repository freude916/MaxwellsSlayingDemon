using System.Linq;
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

    public override HashSet<CardTag> CanonicalTags => [];

    public override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        CoolAdjacentCards(choiceContext, cardPlay.Card);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    public override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    private void CoolAdjacentCards(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (Owner.PlayerCombatState == null) return;

        var hand = Owner.PlayerCombatState.Hand.Cards.ToList();
        var recordedIndex = TemperatureManager.HandIndexField.Get(sourceCard);

        if (recordedIndex < 0) return;

        if (recordedIndex > 0 && recordedIndex - 1 < hand.Count)
            TemperatureManager.ModifyCardTemperature(hand[recordedIndex - 1], -1, choiceContext);

        if (recordedIndex < hand.Count)
            TemperatureManager.ModifyCardTemperature(hand[recordedIndex], -1, choiceContext);
    }
}

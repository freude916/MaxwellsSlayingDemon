using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Cards;

/// <summary>
///     推拉：将任意张手牌洗回牌库并抽等量牌，每抽1张造成伤害
/// </summary>
public class PushPull : AbstractMaxwellCard
{
    public PushPull() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    public override HashSet<CardTag> CanonicalTags => [];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 999),
            null,
            this
        )).ToList();

        if (selectedCards.Count == 0) return;

        await CardPileCmd.Add(selectedCards, PileType.Draw, CardPilePosition.Random);
        await CardPileCmd.Shuffle(choiceContext, Owner);

        var drawnCards = (await CardPileCmd.Draw(choiceContext, selectedCards.Count, Owner)).ToList();
        foreach (var _ in drawnCards)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
    }

    public override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}

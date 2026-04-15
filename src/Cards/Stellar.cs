using MaxwellMod._Utils;
using MaxwellMod.Keywords;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Cards;

/// <summary>
///     恒星：
///     - 保留、热、绝缘
///     - 打出时造成伤害
///     - 回合结束留在手中时，使两侧卡牌升温
/// </summary>
public class Stellar : AbstractMaxwellCard
{
    private const string TempDeltaKey = "TempDelta";

    public Stellar() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain,
        MaxwellKeywords.HeatKeyword,
        MaxwellKeywords.IsothermalKeyword
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar(TempDeltaKey, 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var tempDelta = (int)DynamicVars[TempDeltaKey].BaseValue;
        if (tempDelta == 0) return;

        var (left, right) = CardAdjacencySnapshotStore.GetAdjacentCardsInHand(this);
        List<CardModel> targets = [];
        if (left != null) targets.Add(left);
        if (right != null) targets.Add(right);
        if (targets.Count == 0) return;

        await TemperatureManager.ApplyTemperatureBatchAsync(
            targets,
            tempDelta,
            choiceContext,
            TemperatureCause.EndTurnAdjacency
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
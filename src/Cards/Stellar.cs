using System.Linq;
using MaxwellMod.Keywords;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar(TempDeltaKey, 1m)
    ];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        var index = handCards.IndexOf(this);
        if (index < 0) return Task.CompletedTask;

        var tempDelta = (int)DynamicVars[TempDeltaKey].BaseValue;
        if (tempDelta == 0) return Task.CompletedTask;

        if (index > 0) TemperatureManager.ModifyCardTemperature(handCards[index - 1], tempDelta, choiceContext);
        if (index < handCards.Count - 1) TemperatureManager.ModifyCardTemperature(handCards[index + 1], tempDelta, choiceContext);

        return Task.CompletedTask;
    }

    public override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

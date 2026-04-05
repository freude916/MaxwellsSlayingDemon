using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MaxwellMod.Cards;

/// <summary>
///     喷冷气：
///     - 若环境温度 < 0，对所有敌人施加虚弱
/// </summary>
public class ColdSpray : AbstractMaxwellCard
{
    public ColdSpray() : base(0, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        if (TemperatureManager.GetGlobalTemperature(Owner) >= 0) return;
        if (CombatState == null) return;

        await PowerCmd.Apply<WeakPower>(CombatState.HittableEnemies, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}

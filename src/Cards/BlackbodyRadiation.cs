using MaxwellMod.Powers;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Cards;

/// <summary>
///     黑体辐射：
///     - 按当前环境温度获得格挡
///     - 本回合内，被攻击时按当前环境温度反伤攻击者
/// </summary>
public class BlackbodyRadiation : AbstractMaxwellCard
{
    private const string DamagePerTempKey = "DamagePerTemp";

    public BlackbodyRadiation() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BlackbodyRadiationPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(1m, ValueProp.Move),
        new DynamicVar(DamagePerTempKey, 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var temp = Math.Max(0, TemperatureManager.GetGlobalTemperature(Owner));

        for (var i = 0; i < temp; i++)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay, fast: true);

        var retaliationLayers = DynamicVars[DamagePerTempKey].BaseValue;
        if (retaliationLayers > 0)
            await PowerCmd.Apply<BlackbodyRadiationPower>(Owner.Creature, retaliationLayers, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}

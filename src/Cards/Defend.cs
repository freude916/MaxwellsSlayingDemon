using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Cards;

/// <summary>
/// 初始防御牌
/// </summary>
public class Defend : AbstractMaxwellCard
{
    public Defend() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }
    
    /// <summary>
    /// 卡牌标签 (Defend)
    /// </summary>
    public override HashSet<CardTag> CanonicalTags =>
    [
        CardTag.Defend
    ];
    
    /// <summary>
    /// 动态变量 (5点格挡)
    /// </summary>
    public override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];
    
    /// <summary>
    /// 打出效果
    /// </summary>
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, base.DynamicVars.Block, cardPlay);
    }
    
    /// <summary>
    /// 升级效果
    /// </summary>
    public override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellsSlayingDemon.Cards;

/// <summary>
/// 初始打击牌
/// </summary>
public class Strike : AbstractMaxwellCard
{
    public Strike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }
    
    /// <summary>
    /// 卡牌标签 (Strike)
    /// </summary>
    public override HashSet<CardTag> CanonicalTags =>
    [
        CardTag.Strike
    ];
    
    /// <summary>
    /// 动态变量 (6点伤害)
    /// </summary>
    public override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];
    
    /// <summary>
    /// 打出效果
    /// </summary>
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
    
    /// <summary>
    /// 升级效果
    /// </summary>
    public override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

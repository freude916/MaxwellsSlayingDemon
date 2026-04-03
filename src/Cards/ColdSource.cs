using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MaxwellMod.Cards;

/// <summary>
///     Maxwell 的初始攻击牌
/// </summary>
public class ColdSource : AbstractMaxwellCard
{
    public ColdSource() : base(1, CardType.Attack, CardRarity.Basic, TargetType.None)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [MaxwellKeywords.ColdKeyword];

    /// <summary>
    ///     卡牌标签 (Strike)
    /// </summary>
    public override HashSet<CardTag> CanonicalTags =>
    [
    ];

    /// <summary>
    ///     动态变量 (能量变化)
    /// </summary>
    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(0) // 升级后能耗
    ];
    
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        // 理论上热已经被 Keyword 解决了
        await Task.CompletedTask;
    }

    /// <summary>
    ///     升级效果
    /// </summary>
    public override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(-1m);
    }
}
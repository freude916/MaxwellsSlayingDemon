using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     Maxwell 的初始攻击牌
/// </summary>
public class HeatSource : AbstractMaxwellCard
{
    public HeatSource() : base(0, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [MaxwellKeywords.HeatKeyword, MaxwellKeywords.DeflectionKeyword];

    /// <summary>
    ///     卡牌标签 (Strike)
    /// </summary>
    protected override HashSet<CardTag> CanonicalTags =>
    [
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        // 理论上热已经被 Keyword 解决了
        await Task.CompletedTask;
    }

    /// <summary>
    ///     升级效果
    /// </summary>
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

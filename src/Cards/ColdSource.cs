using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     冷源
/// </summary>
public class ColdSource : AbstractMaxwellCard
{
    public ColdSource() : base(0, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [MaxwellKeywords.ColdKeyword, MaxwellKeywords.DeflectionKeyword];

    /// <summary>
    ///     卡牌标签 (Strike)
    /// </summary>
    protected override HashSet<CardTag> CanonicalTags =>
    [
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        // 理论上已经被 Keyword 解决了
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
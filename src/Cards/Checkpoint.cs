using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     卡关：
///     - 为所有手牌添加 Retain 关键词
///     - 结束你的回合
/// </summary>
public class Checkpoint : AbstractMaxwellCard
{
    public Checkpoint() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in handCards)
            card.AddKeyword(CardKeyword.Retain);

        PlayerCmd.EndTurn(Owner, false);
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}
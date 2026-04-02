using BaseLib.Abstracts;
using BaseLib.Utils;
using MaxwellMod.PatchesNModels;
using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
/// 活塞卡牌
/// - 无法被打出
/// - 升温时，丢弃一张牌
/// - 降温时，抽取一张牌
/// </summary>
[Pool(typeof(MaxwellCardPool))]
public class Piston : CustomCardModel, ICardTemperatureListener
{
    public Piston() : base(0, CardType.Status, CardRarity.Rare, TargetType.None, false)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public async Task OnCardTemperatureChanged(int oldTemp, int newTemp, int delta, PlayerChoiceContext choiceContext)
    {
        if (CombatState == null) return;

        switch (delta)
        {
            case > 0:
            {
                // 升温：丢弃一张牌
                var toDiscard = await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
                    new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this);
                await CardCmd.Discard(choiceContext, toDiscard);
                break;
            }
            case < 0:
                // 降温：抽取一张牌
                await CardPileCmd.Draw(choiceContext, 1, Owner);
                break;
        }
    }
}

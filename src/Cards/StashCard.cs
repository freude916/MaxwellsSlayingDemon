using BaseLib.Abstracts;
using BaseLib.Utils;
using MaxwellMod.Stash;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace MaxwellMod.Cards;

/// <summary>
///     暂存：将至多 2 张牌放入暂存区，下回合开始时放回手牌
///     升级后可暂存 3 张牌
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
[Pool(typeof(TokenCardPool))]
public class StashCard() : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override HashSet<CardTag> CanonicalTags => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public static async Task<StashCard?> CreateInHand(Player owner, CombatState combatState)
    {
        return (await CreateInHand(owner, 1, combatState)).FirstOrDefault();
    }

    public static async Task<IEnumerable<StashCard>> CreateInHand(Player owner, int count, CombatState combatState)
    {
        ArgumentNullException.ThrowIfNull(combatState);
        if (count <= 0 || CombatManager.Instance.IsOverOrEnding) return [];

        List<StashCard> cards = [];
        for (var i = 0; i < count; i++) cards.Add(combatState.CreateCard<StashCard>(owner));

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, true);
        return cards;
    }

    /// <summary>
    ///     打出效果：选择手牌放入暂存区
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var prefs = new CardSelectorPrefs(
            SelectionScreenPrompt,
            0,
            DynamicVars.Cards.IntValue
        );

        var selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            null,
            this
        )).ToList();

        // 如果没有选择卡牌，直接返回
        if (selectedCards.Count == 0) return;

        // 将选中的卡牌移动到暂存区
        foreach (var card in selectedCards)
        {
            await CardPileCmd.Add(card, StashPile.StashPileType);
        }

        // 添加暂存 Power，用于下回合开始时返回卡牌
        await PowerCmd.Apply<StashPower>(Owner.Creature, 1, Owner.Creature, this);
    }

    /// <summary>
    ///     升级效果：可暂存 3 张牌（原 2 张）
    /// </summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
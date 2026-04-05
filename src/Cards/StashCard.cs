using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MaxwellMod.Stash;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MaxwellMod.Cards;

/// <summary>
///     暂存：将至多 2 张牌放入暂存区，下回合开始时放回手牌
///     升级后可暂存 3 张牌
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global

[Pool(typeof(ColorlessCardPool))]
public class StashCard(): CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
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

        // 使用 DynamicVars.Cards 获取可暂存的卡牌数量
        var maxCards = (int)DynamicVars.Cards.BaseValue;

        GD.Print($"[MaxwellMod] StashCard: OnPlay started, maxCards = {maxCards}");

        // 直接使用 CardSelectCmd
        var handPile = PileType.Hand.GetPile(Owner);
        var prefs = new CardSelectorPrefs(
            new LocString("card_selection", "TO_STASH"),
            0,
            maxCards
        );

        var selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            null,
            this
        )).ToList();

        Entry.Logger.Info($"[MaxwellMod] [Stash] StashCard: Selected {selectedCards.Count} cards");

        // 如果没有选择卡牌，直接返回
        if (selectedCards.Count == 0) return;

        // 将选中的卡牌移动到暂存区
        foreach (var card in selectedCards)
        {
            GD.Print($"[MaxwellMod] StashCard: Moving card {card.Id.Entry} to stash");

            // 从手牌 UI 移除（使用 NPlayerHand.Remove 方法）
            var hand = NCombatRoom.Instance!.Ui.Hand; // 战斗外不可能有 OnPlay 吧
            var cardHolder = hand.GetCardHolder(card);
            if (cardHolder != null) hand.RemoveCardHolder(cardHolder);

            // 从手牌数据移除
            handPile.RemoveInternal(card);
            handPile.InvokeCardRemoveFinished();

            // 添加到暂存区
            StashManager.AddCardToStash(Owner, card);
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
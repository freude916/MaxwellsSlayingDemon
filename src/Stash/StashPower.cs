using Godot;
using MaxwellMod.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Stash;

/// <summary>
///     暂存效果，负责回合开始时将暂存区的卡牌返回手牌
/// </summary>
public class StashPower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    ///     在抽牌前将暂存的卡牌返回手牌
    /// </summary>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != Owner.Player) return;

        var stashPile = StashPile.StashPileType.GetPile(player);

        if (stashPile.Cards.Count == 0) return;

        GD.Print($"[MaxwellMod] StashPower: Returning {stashPile.Cards.Count} cards to hand");

        var cardsToReturn = stashPile.Cards.ToList();

        await CardPileCmd.Add(cardsToReturn, PileType.Hand);

        await PowerCmd.Remove(this);

        GD.Print($"[MaxwellMod] StashPower: Returned {cardsToReturn.Count} cards to hand");
    }
}
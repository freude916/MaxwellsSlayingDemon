using MaxwellMod.Cards;
using MaxwellMod.Stash;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Relics;

/// <summary>
///     Maxwell 的初始遗物
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global

public class RecordingEye : AbstractMaxwellRelic
{
    /// <summary>
    ///     遗物稀有度
    /// </summary>
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == Owner && combatState.RoundNumber <= 1)
        {
            Flash();
            await StashCard.CreateInHand(Owner, 1, combatState);
        }
    }

    /// <summary>
    ///     升级后的遗物 (如果有)
    /// </summary>
    public override RelicModel? GetUpgradeReplacement()
    {
        return null;
    }
}
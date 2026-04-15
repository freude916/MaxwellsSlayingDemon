using HarmonyLib;
using MaxwellMod.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     态对防御的修改：稳定 +2 * 层数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
public static class ReactivityBlockPatch
{
    public static void Postfix(ref decimal __result, CardModel? cardSource)
    {
        if (cardSource == null) return;

        if (cardSource is AbstractMaxwellCard maxwellCard) __result += maxwellCard.MaxwellMod_PermBlock;

        if (TemperatureManager.GetCardReactivity(cardSource) != ReactivityType.Stable) return;

        var stacks = TemperatureManager.GetCardReactivityStacks(cardSource);
        if (stacks <= 0) return;

        __result += 2 * stacks;
    }
}
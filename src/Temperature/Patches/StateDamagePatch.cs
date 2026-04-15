using BaseLib.Extensions;
using HarmonyLib;
using MaxwellMod.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     态对伤害的修改：活泼 +2 * 层数
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class ReactivityDamagePatch
{
    public static void Postfix(ref decimal __result, CardModel? cardSource, ValueProp props)
    {
        if (cardSource == null) return;
        if (!props.IsPoweredAttack_()) return;

        if (cardSource is AbstractMaxwellCard maxwellCard) __result += maxwellCard.MaxwellMod_PermDamage;

        if (TemperatureManager.GetCardReactivity(cardSource) != ReactivityType.Potent) return;

        var stacks = TemperatureManager.GetCardReactivityStacks(cardSource);
        if (stacks <= 0) return;

        __result += 2 * stacks;
    }
}
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     将卡牌态以额外文本的形式追加到卡牌描述底部（类似 extraCardText）
/// </summary>
[HarmonyPatch]
public static class CardReactivityExtraCardTextPatch
{
    private static MethodBase TargetMethod()
    {
        var previewType = AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType")
                          ?? throw new MissingMemberException(typeof(CardModel).FullName, "DescriptionPreviewType");

        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile",
                   [typeof(PileType), previewType, typeof(Creature)])
               ?? throw new MissingMethodException(typeof(CardModel).FullName,
                   "GetDescriptionForPile(PileType, DescriptionPreviewType, Creature)");
    }

    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance.CombatState == null) return;

        var extraText = BuildReactivityExtraCardText(__instance);
        if (string.IsNullOrEmpty(extraText)) return;

        __result = string.IsNullOrWhiteSpace(__result)
            ? extraText
            : $"{__result}\n{extraText}";
    }

    private static string? BuildReactivityExtraCardText(CardModel card)
    {
        var reactivity = TemperatureManager.GetCardReactivity(card);
        var stacks = TemperatureManager.GetCardReactivityStacks(card);
        if (reactivity == ReactivityType.None || stacks <= 0) return null;

        var key = reactivity switch
        {
            ReactivityType.Potent => "MAXWELLMOD-REACTIVITY_POTENT.extraCardText",
            ReactivityType.Stable => "MAXWELLMOD-REACTIVITY_STABLE.extraCardText",
            _ => null
        };
        if (key == null) return null;

        var text = LocString.GetIfExists("static_hover_tips", key);
        if (text != null)
        {
            text.Add("Amount", stacks);
            text.Add("Bonus", stacks * 2);
            return text.GetFormattedText();
        }

        Entry.Logger.Warn($"[TemperaturePatch] Missing loc key static_hover_tips/{key}, using fallback text");
        return reactivity switch
        {
            ReactivityType.Potent => $"[gold]活泼[/gold]{stacks}层 (伤害+{stacks * 2})",
            ReactivityType.Stable => $"[gold]稳定[/gold]{stacks}层 (格挡+{stacks * 2})",
            _ => null
        };
    }
}
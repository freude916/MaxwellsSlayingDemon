using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     手牌标题背景增强（热=红底，冷=蓝底，常温恢复默认）。
///     通过 Label 原生 stylebox 背景实现，不走阴影通道，避免被 outline 覆盖。
/// </summary>
[HarmonyPatch]
public static class HandTitleBackgroundPatch
{
    private const bool EnableTemperatureTitleBackground = true;

    private static readonly StyleBoxFlat HotStyle = CreateStyle(new Color(0.62f, 0.10f, 0.08f, 0.72f));
    private static readonly StyleBoxFlat ColdStyle = CreateStyle(new Color(0.08f, 0.20f, 0.60f, 0.72f));

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
    public static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        RefreshTitleBackground(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.Flash))]
    public static void FlashPostfix(NHandCardHolder __instance)
    {
        RefreshTitleBackground(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    public static void UpdateVisualsPostfix(NCard __instance, PileType pileType)
    {
        RefreshTitleBackground(__instance, pileType == PileType.Hand);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCardHolder), nameof(NCardHolder.ReassignToCard))]
    public static void ReassignToCardPostfix(NCardHolder __instance)
    {
        if (__instance is NHandCardHolder handHolder)
        {
            RefreshTitleBackground(handHolder);
        }
    }

    private static void RefreshTitleBackground(NHandCardHolder holder)
    {
        if (!GodotObject.IsInstanceValid(holder)) return;
        RefreshTitleBackground(holder.CardNode, inHandPile: true);
    }

    private static void RefreshTitleBackground(NCard? cardNode, bool inHandPile)
    {
        if (!GodotObject.IsInstanceValid(cardNode)) return;

        var model = cardNode?.Model;
        var titleLabel = cardNode?.GetNodeOrNull<Label>("%TitleLabel");
        if (titleLabel == null) return;

        if (!EnableTemperatureTitleBackground || !inHandPile || model?.CombatState == null)
        {
            RestoreDefault(titleLabel);
            return;
        }

        var temperature = TemperatureManager.GetCardTemperature(model);
        switch (temperature)
        {
            case > 0:
                ApplyBackground(titleLabel, HotStyle);
                break;
            case < 0:
                ApplyBackground(titleLabel, ColdStyle);
                break;
            default:
                RestoreDefault(titleLabel);
                break;
        }
    }

    private static void ApplyBackground(Label titleLabel, StyleBoxFlat style)
    {
        titleLabel.AddThemeStyleboxOverride("normal", style);
    }

    private static void RestoreDefault(Label titleLabel)
    {
        titleLabel.RemoveThemeStyleboxOverride("normal");
    }

    private static StyleBoxFlat CreateStyle(Color bgColor)
    {
        return new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 2,
            ContentMarginBottom = 2
        };
    }
}

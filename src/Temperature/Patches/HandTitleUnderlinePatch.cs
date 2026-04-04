using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     手牌标题粗下划线（热=红，冷=蓝，常温不显示）。
///     独立于原生 CardHighlight，不与可打出/金色高亮冲突。
/// </summary>
[HarmonyPatch]
public static class HandTitleUnderlinePatch
{
    private const string UnderlineNodeName = "MaxwellTempTitleUnderline";
    private const float UnderlineHeight = 6f;
    private const float HorizontalInset = 8f;
    private const float BottomOffset = 5f;

    private static readonly Color HotColor = new(0.93f, 0.28f, 0.24f, 0.95f);
    private static readonly Color ColdColor = new(0.24f, 0.57f, 1f, 0.95f);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
    public static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        RefreshUnderline(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.Flash))]
    public static void FlashPostfix(NHandCardHolder __instance)
    {
        RefreshUnderline(__instance);
    }

    private static void RefreshUnderline(NHandCardHolder holder)
    {
        if (!GodotObject.IsInstanceValid(holder)) return;

        var cardNode = holder.CardNode;
        var model = cardNode?.Model;
        if (model?.CombatState == null)
        {
            HideUnderline(cardNode);
            return;
        }

        var temperature = TemperatureManager.GetCardTemperature(model);
        if (temperature == 0)
        {
            HideUnderline(cardNode);
            return;
        }

        var underline = GetOrCreateUnderline(cardNode);
        if (underline == null) return;

        UpdateUnderlineLayout(underline, cardNode);
        underline.Color = temperature > 0 ? HotColor : ColdColor;
        underline.Visible = true;
    }

    private static void HideUnderline(Node? cardNode)
    {
        var underline = cardNode?.GetNodeOrNull<ColorRect>(UnderlineNodeName);
        if (underline != null) underline.Visible = false;
    }

    private static ColorRect? GetOrCreateUnderline(Node? cardNode)
    {
        if (cardNode == null) return null;

        var existing = cardNode.GetNodeOrNull<ColorRect>(UnderlineNodeName);
        if (existing != null) return existing;

        var titleLabel = cardNode.GetNodeOrNull<Label>("%TitleLabel");
        var titleParent = titleLabel?.GetParent();
        if (titleLabel == null || titleParent == null) return null;

        var underline = new ColorRect
        {
            Name = UnderlineNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            ZIndex = titleLabel.ZIndex + 1
        };

        titleParent.AddChild(underline);
        titleParent.MoveChild(underline, titleParent.GetChildCount() - 1);
        return underline;
    }

    private static void UpdateUnderlineLayout(ColorRect underline, Node? cardNode)
    {
        if (cardNode == null) return;
        var titleLabel = cardNode.GetNodeOrNull<Label>("%TitleLabel");
        if (titleLabel == null) return;

        var width = Mathf.Max(24f, titleLabel.Size.X - HorizontalInset * 2f);
        var x = titleLabel.Position.X + HorizontalInset;
        var y = titleLabel.Position.Y + titleLabel.Size.Y - BottomOffset;

        underline.Position = new Vector2(x, y);
        underline.Size = new Vector2(width, UnderlineHeight);
    }
}


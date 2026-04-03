using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MaxwellMod.Stash.Patches;

/// <summary>
///     Harmony Patch 注入暂存区 UI 到战斗界面
/// </summary>
[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
public static class StashUiPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance, CombatState state)
    {
        try
        {
            var me = state.Players[0];
            var pilesContainer = __instance.GetNode<NCombatPilesContainer>("%CombatPileContainer");
            InjectStashUI(pilesContainer, me);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MaxwellMod] Error injecting Stash UI: {ex}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InjectStashUI(NCombatPilesContainer pilesContainer, Player player)
    {
        var stashScene = GD.Load<PackedScene>("res://MaxwellMod/scenes/stash.tscn");
        if (stashScene == null)
        {
            GD.PrintErr("[MaxwellMod] Failed to load stash.tscn!");
            return;
        }

        var node = stashScene.Instantiate();
        var stashButton = node as NStashButton;
        if (stashButton == null)
        {
            GD.PrintErr($"[MaxwellMod] Failed to cast to NStashButton! Actual type: {node.GetType().FullName}");
            node.QueueFree();
            return;
        }

        var stash = StashManager.GetOrCreateStash(player);
        stashButton.Initialize(player, stash);

        stashButton.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        stashButton.OffsetLeft = -90;
        stashButton.OffsetTop = -370;
        stashButton.OffsetRight = -10;
        stashButton.OffsetBottom = -290;

        pilesContainer.AddChild(stashButton);
    }
}
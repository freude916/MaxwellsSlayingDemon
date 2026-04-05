using MaxwellMod.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace MaxwellMod.Relics;

/// <summary>
///     战斗内点击 RecordingEye：切换玩家身上的偏转层数（0/1）。
/// </summary>
[HarmonyLib.HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
public static class RecordingEyeClickPatch
{
    public static bool Prefix(RelicModel model)
    {
        if (model is not RecordingEye recordingEye) return true;
        if (!CombatManager.Instance.IsInProgress) return true;

        _ = TaskHelper.RunSafely(ToggleDeflection(recordingEye));
        return false;
    }

    private static async Task ToggleDeflection(RecordingEye relic)
    {
        var creature = relic.Owner.Creature;
        var existing = creature.GetPower<DeflectionPower>();

        relic.Flash();
        if (existing is not { Amount: > 0 })
        {
            await PowerCmd.Apply<DeflectionPower>(creature, 1, creature, null);
            return;
        }

        await PowerCmd.Remove(existing);
    }
}

using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MaxwellsSlayingDemon;

[ModInitializer(nameof(Initialize))]
public partial class Entry : Node
{
    private const string ModId = "MaxwellsSlayingDemon";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("MaxwellsSlayingDemon initializing...");
        
        Harmony harmony = new(ModId);
        harmony.PatchAll(typeof(Entry).Assembly);
        
        // 注册脚本（包括 localization 等）
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        
        Logger.Info("MaxwellsSlayingDemon initialized!");
    }
}

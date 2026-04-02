using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MaxwellMod;

[ModInitializer(nameof(Initialize))]
public partial class Entry : Node
{
    private const string ModId = "MaxwellMod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("MaxwellMod initializing...");
        
        Harmony harmony = new(ModId);
        harmony.PatchAll(typeof(Entry).Assembly);
        
        // 注册脚本（包括 localization 等）
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        
        Logger.Info("MaxwellMod initialized!");
    }
}

using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellsSlayingDemon.Relics;

/// <summary>
/// Maxwell 的初始遗物
/// TODO: 根据人物机制设计具体效果
/// </summary>
public class MaxwellStarterRelic : AbstractMaxwellRelic
{
    /// <summary>
    /// 遗物稀有度
    /// </summary>
    public override RelicRarity Rarity => RelicRarity.Starter;
    
    /// <summary>
    /// 动态变量
    /// TODO: 根据遗物效果添加变量
    /// </summary>
    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Power", 1m)
    ];
    
    /// <summary>
    /// TODO: 实现遗物效果
    /// 例如：在回合开始时获得某种效果
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        // TODO: 实现遗物效果
        await base.AfterPlayerTurnStart(context, player);
    }
    
    /// <summary>
    /// 升级后的遗物 (如果有)
    /// </summary>
    public override RelicModel? GetUpgradeReplacement()
    {
        // TODO: 如果有升级版本，返回对应的遗物
        return null;
    }
}
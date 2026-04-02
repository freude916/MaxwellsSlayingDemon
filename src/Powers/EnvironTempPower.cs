using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Powers;


/// 温度环境 Power <br/>
/// 
/// - 温度 &gt; 3：麦妖受到的伤害 +1 <br/>
/// - 温度 &lt; -3：冷牌费用 +1
public sealed class EnvironTempPower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Counter;

    private const int HotThreshold = 3;
    private const int ColdThreshold = -3;

    /// <summary>
    /// 当前全局温度 (正数=热，负数=冷)
    /// </summary>
    public int GlobalTemperature => Amount;

    /// <summary>
    /// 修改费用：温度 &lt; -3 时，冷牌费用 +1
    /// </summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        ArgumentNullException.ThrowIfNull(card);
        modifiedCost = originalCost;

        if (GlobalTemperature >= ColdThreshold) return false;
        if (card.Owner.Creature != base.Owner) return false;

        // 检查是否为冷牌（温度 < 0）
        if (TemperatureManager.GetCardTemperature(card) < 0)
        {
            modifiedCost = originalCost + 1;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 修改受到的伤害：温度 > 3 时，受伤 +1
    /// 注意：这是 ModifyDamageAdditive，当 target == Owner 时表示 Owner 受到伤害
    /// </summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // target 是伤害承受者，当 target == Owner 时表示我们受到伤害
        if (target != base.Owner) return 0m;
        if (GlobalTemperature <= HotThreshold) return 0m;
        if (!props.IsPoweredAttack()) return 0m;

        return 1m;
    }
}

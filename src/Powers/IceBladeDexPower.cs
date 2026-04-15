using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MaxwellMod.Powers;

/// <summary>
///     冰刀提供的临时敏捷：
///     当环境温度 > 0 时，移除对应敏捷。
/// </summary>
public class IceBladeDexPower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (power is not EnvironTempPower environTempPower) return;
        if (power.Owner != Owner) return;

        var newTemp = environTempPower.Amount;

        if (newTemp > 0)
        {
            Flash();

            await PowerCmd.Apply<DexterityPower>(Owner, -Amount, Owner, null, true);
            await PowerCmd.Remove(this);
        }
    }
}
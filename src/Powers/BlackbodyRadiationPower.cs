using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaxwellMod.Powers;

/// <summary>
///     黑体辐射提供的临时反伤：
///     本回合内被攻击时，对攻击者造成 Amount * 当前环境温度 点伤害。
/// </summary>
public class BlackbodyRadiationPower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult _,
        ValueProp props, Creature? dealer, CardModel? __)
    {
        if (target != Owner) return;
        if (dealer == null) return;
        if (Amount <= 0) return;
        if (!props.IsPoweredAttack()) return;

        var temp = Math.Max(0, TemperatureManager.GetGlobalTemperature(Owner.Player));
        var retaliation = Amount * temp;
        if (retaliation <= 0) return;

        Flash();
        await CreatureCmd.Damage(choiceContext, dealer, retaliation, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner, null);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner.Side != side) await PowerCmd.Remove(this);
    }
}

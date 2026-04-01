using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace MaxwellsSlayingDemon.Powers;

/// <summary>
/// Maxwell 人物能力(Power)的基类
/// </summary>
public abstract class AbstractMaxwellPower : CustomPowerModel
{
    /// <summary>
    /// 能力类型 (Buff/Debuff)
    /// </summary>
    public abstract override PowerType Type { get; }
    
    /// <summary>
    /// 叠加类型
    /// </summary>
    public abstract override PowerStackType StackType { get; }
    
    /// <summary>
    /// 小图标路径 (64x64)
    /// </summary>
    public override string? CustomPackedIconPath => $"res://MaxwellsSlayingDemon/images/powers/{Id.Entry.ToLowerInvariant()}.png";
    
    /// <summary>
    /// 大图标路径 (256x256)
    /// </summary>
    public override string? CustomBigIconPath => $"res://MaxwellsSlayingDemon/images/powers/{Id.Entry.ToLowerInvariant()}.png";
    
    /// <summary>
    /// 大图标Beta路径 (256x256)
    /// </summary>
    public override string? CustomBigBetaIconPath => $"res://MaxwellsSlayingDemon/images/powers/{Id.Entry.ToLowerInvariant()}.png";
}

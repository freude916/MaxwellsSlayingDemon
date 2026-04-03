using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace MaxwellMod.Powers;

/// <summary>
///     Maxwell 人物能力(Power)的基类
/// </summary>
public abstract class AbstractMaxwellPower : CustomPowerModel
{
    /// <summary>
    ///     能力类型 (Buff/Debuff)
    /// </summary>
    public abstract override PowerType Type { get; }

    /// <summary>
    ///     叠加类型
    /// </summary>
    public abstract override PowerStackType StackType { get; }

#pragma warning disable CA1308 // hard encoded file path
    /// <summary>
    ///     小图标路径 (64x64)
    /// </summary>
    public override string? CustomPackedIconPath => $"res://MaxwellMod/images/powers/{Id.Entry.ToLowerInvariant()}.png";

    /// <summary>
    ///     大图标路径 (256x256)
    /// </summary>
    public override string? CustomBigIconPath => $"res://MaxwellMod/images/powers/{Id.Entry.ToLowerInvariant()}.png";

    /// <summary>
    ///     大图标Beta路径 (256x256)
    /// </summary>
    public override string? CustomBigBetaIconPath =>
        $"res://MaxwellMod/images/powers/{Id.Entry.ToLowerInvariant()}.png";
}
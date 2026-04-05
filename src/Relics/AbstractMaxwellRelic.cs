using BaseLib.Abstracts;
using BaseLib.Utils;
using MaxwellMod.PatchesNModels;

namespace MaxwellMod.Relics;

/// <summary>
///     Maxwell 人物遗物的基类
/// </summary>
[Pool(typeof(MaxwellRelicPool))]
public abstract class AbstractMaxwellRelic : CustomRelicModel
{
    /// <summary>
    ///     小图标路径 (64x64)
    /// </summary>
    public override string PackedIconPath => $"res://MaxwellMod/images/relics/{Id.Entry.ToLowerInvariant()}.png";

    /// <summary>
    ///     轮廓图标路径
    /// </summary>
    protected override string PackedIconOutlinePath => $"res://MaxwellMod/images/relics/{Id.Entry.ToLowerInvariant()}.png";

    /// <summary>
    ///     大图标路径 (256x256)
    /// </summary>
    protected override string BigIconPath => $"res://MaxwellMod/images/relics/{Id.Entry.ToLowerInvariant()}.png";
}
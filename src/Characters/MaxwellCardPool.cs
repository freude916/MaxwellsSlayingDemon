using BaseLib.Abstracts;
using Godot;

namespace MaxwellMod.PatchesNModels;

/// <summary>
/// Maxwell 人物的卡池定义
/// </summary>
public class MaxwellCardPool : CustomCardPoolModel
{
    public override string Title => "Maxwell";

    /// <summary>
    /// 卡牌框架颜色 (深蓝色调)
    /// </summary>
    public override Color DeckEntryCardColor => new("1a237e");
    
    /// <summary>
    /// 能量轮廓颜色
    /// </summary>
    public override Color EnergyOutlineColor => new("1a237e");
    
    /// <summary>
    /// 是否为无色卡池
    /// </summary>
    public override bool IsColorless => false;
    
    /// <summary>
    /// 大能量图标路径
    /// </summary>
    public override string? BigEnergyIconPath => "res://MaxwellMod/images/ui/cardOrb.png";
    
    /// <summary>
    /// 文本能量图标路径
    /// </summary>
    public override string? TextEnergyIconPath => "res://MaxwellMod/images/ui/energyOrb-lighter.png";
    
    // HSV 颜色调整参数
    public override float H => 0.65f;
    public override float S => 0.59f;
    public override float V => 0.69f;
}
using BaseLib.Abstracts;
using BaseLib.Utils;
using MaxwellMod.PatchesNModels;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MaxwellMod.Cards;

/// <summary>
/// Maxwell 人物卡牌的基类
/// </summary>
[Pool(typeof(MaxwellCardPool))]
public abstract class AbstractMaxwellCard : CustomCardModel
{
    /// <summary>
    /// 卡牌肖像图片路径
    /// </summary>
    public override string PortraitPath => $"res://MaxwellMod/images/cards/{Id.Entry.ToLowerInvariant()}.png";

    protected AbstractMaxwellCard(int energyCost, CardType type, CardRarity rarity, TargetType targetType, 
        bool shouldShowInCardLibrary = true, bool autoAdd = true) 
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary, autoAdd)
    {
    }
}
using BaseLib.Abstracts;
using BaseLib.Utils;
using MaxwellMod.PatchesNModels;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MaxwellMod.Cards;

/// <summary>
///     Maxwell 人物卡牌的基类
/// </summary>
[Pool(typeof(MaxwellCardPool))]
public abstract class AbstractMaxwellCard(
    int energyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true,
    bool autoAdd = true)
    : CustomCardModel(energyCost, type, rarity, targetType, shouldShowInCardLibrary, autoAdd)
{
    private int _maxwellModPermBlock;
    private int _maxwellModPermDamage;

    /// <summary>
    ///     卡牌肖像图片路径
    /// </summary>
    public override string PortraitPath => $"res://MaxwellMod/images/cards/{Id.Entry.ToLowerInvariant()}.png";

    /// <summary>
    ///     Green 固化后的永久伤害加成（run 内持久化）
    /// </summary>
    [SavedProperty]
    public int MaxwellMod_PermDamage
    {
        get => _maxwellModPermDamage;
        set
        {
            AssertMutable();
            _maxwellModPermDamage = value;
        }
    }

    /// <summary>
    ///     Green 固化后的永久防御加成（run 内持久化）
    /// </summary>
    [SavedProperty]
    public int MaxwellMod_PermBlock
    {
        get => _maxwellModPermBlock;
        set
        {
            AssertMutable();
            _maxwellModPermBlock = value;
        }
    }

    /// <summary>
    ///     叠加 Green 固化后的永久攻防加成
    /// </summary>
    public void ApplyPermanentReactivityBonus(int damageBonus, int blockBonus)
    {
        if (damageBonus == 0 && blockBonus == 0) return;
        MaxwellMod_PermDamage += damageBonus;
        MaxwellMod_PermBlock += blockBonus;
    }
}
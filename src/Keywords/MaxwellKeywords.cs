using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MaxwellMod.Keywords;

/// <summary>
///     Maxwell Mod 自定义关键词
/// </summary>
#pragma warning disable CA2211
// ReSharper disable UnassignedField.Global
public static class MaxwellKeywords
{
    /// <summary>
    ///     热词缀 - 打出时升温，影响周围卡牌
    /// </summary>
    [CustomEnum(nameof(HeatKeyword))] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword HeatKeyword;

    /// <summary>
    ///     冷词缀 - 打出时降温，影响周围卡牌
    /// </summary>
    [CustomEnum(nameof(ColdKeyword))] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword ColdKeyword;

    /// <summary>
    ///     绿词缀 - 虚无、消耗，态转化
    /// </summary>
    [CustomEnum(nameof(GreenKeyword))] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword GreenKeyword;

    /// <summary>
    ///     恒温词缀 - 不会被相邻传热影响
    /// </summary>
    [CustomEnum(nameof(IsothermalKeyword))] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword IsothermalKeyword;
    
    /// <summary>
    ///     绝缘词缀 - 不会被相邻传热影响，不会把热传给别人，不会引起环境温度变化
    /// </summary>
    [CustomEnum(nameof(InsulationKeyword))] [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword InsulationKeyword;
}

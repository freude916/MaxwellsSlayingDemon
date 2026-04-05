using MaxwellMod.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Powers;

/// <summary>
///     偏转：当玩家打出带“偏转”关键词的牌时，将其结果牌堆改为抽牌堆顶。
/// </summary>
public class DeflectionPower : AbstractMaxwellPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None; // 不可堆叠不可重复

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay,
        ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        if (Amount <= 0) return (pileType, position);
        if (card.Owner.Creature != Owner) return (pileType, position);
        if (!card.Keywords.Contains(MaxwellKeywords.DeflectionKeyword)) return (pileType, position);
        if (pileType == PileType.None) return (pileType, position);

        Flash();
        return (PileType.Draw, CardPilePosition.Top);
    }
}

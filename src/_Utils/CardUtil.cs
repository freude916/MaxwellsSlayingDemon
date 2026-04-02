using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod._Utils;

public static class CardUtil
{
    public static bool RemoveKeywordIfExist(CardModel card, CardKeyword keyword)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (!card.Keywords.Contains(keyword)) return false;
        card.RemoveKeyword(keyword);
        return true;
    }
}
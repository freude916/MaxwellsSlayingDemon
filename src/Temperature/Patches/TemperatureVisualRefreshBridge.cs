using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace MaxwellMod.Temperature.Patches;

/// <summary>
///     规则层发出的刷新请求在这里落地为 UI 更新
/// </summary>
public static class TemperatureVisualRefreshBridge
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        TemperatureManager.CardVisualRefreshRequested += RefreshCardVisual;
        _initialized = true;
    }

    private static void RefreshCardVisual(CardModel card)
    {
        var nCard = NCard.FindOnTable(card);
        nCard?.UpdateVisuals(card.Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
    }
}
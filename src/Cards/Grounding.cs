using MaxwellMod.Temperature;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Cards;

/// <summary>
///     接地：
///     - 将环境温度变为 0
///     - 每获得 1 点温度抽 1 张牌
///     - 每失去 1 点温度，本回合手牌费用 +1
/// </summary>
public class Grounding : AbstractMaxwellCard
{
    public Grounding() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        var globalTemperature = TemperatureManager.GetGlobalTemperature(Owner);
        if (globalTemperature == 0) return;

        if (globalTemperature < 0)
        {
            var gainedTemperature = -globalTemperature;
            await TemperatureManager.ModifyGlobalTemperature(Owner, gainedTemperature);
            await CardPileCmd.Draw(choiceContext, gainedTemperature, Owner);
            return;
        }

        var lostTemperature = globalTemperature;
        await TemperatureManager.ModifyGlobalTemperature(Owner, -lostTemperature);

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in handCards)
            card.EnergyCost.AddThisTurn(lostTemperature);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
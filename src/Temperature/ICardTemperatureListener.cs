using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MaxwellMod.Temperature;

/// <summary>
///     卡牌温度变化监听接口
///     实现此接口的卡牌会在自己温度变化时收到通知
/// </summary>
public interface ICardTemperatureListener
{
    /// <summary>
    ///     当卡牌自己的温度发生变化时调用
    /// </summary>
    /// <param name="oldTemp">变化前的温度</param>
    /// <param name="newTemp">变化后的温度</param>
    /// <param name="delta">变化量 (正数=升温，负数=降温) (不一定是冗余参数，可能温度有上限)</param>
    /// <param name="choiceContext">选择上下文，方便抽牌弃牌</param>
    Task OnCardTemperatureChanged(int oldTemp, int newTemp, int delta, PlayerChoiceContext choiceContext);
}
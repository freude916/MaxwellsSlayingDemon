using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature;

/// <summary>
///     卡牌打出前记录的相邻快照（左右各一）
/// </summary>
public readonly record struct PlayAdjacencySnapshot(CardModel? Left, CardModel? Right);
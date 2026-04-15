using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Temperature;

/// <summary>
///     一次温度变化请求的处理结果
/// </summary>
public readonly record struct TemperatureChangeResult(
    CardModel Card,
    TemperatureCause Cause,
    int RequestedDelta,
    int AppliedDelta,
    int OldTemperature,
    int NewTemperature,
    bool ReactivityChanged,
    bool Skipped
);
using System;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

public sealed partial class SalvageDifficultyPrototype : IPrototype
{
    [DataField]
    public string LootPrototype = "SalvageLoot";

    /// <summary>
    /// Minimum round time after which this difficulty can appear.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.Zero;

    /// <summary>
    /// Weighted chance for this difficulty when generating offers.
    /// </summary>
    [DataField]
    public float Probability = 1f;

    /// <summary>
    /// Длительность экспедиции этой сложности. Ноль — берётся из CVar salvage.expedition_duration.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.Zero;
}

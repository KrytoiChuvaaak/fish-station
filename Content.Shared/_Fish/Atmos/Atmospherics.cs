namespace Content.Shared.Atmos;

public static partial class Atmospherics
{
    /// <summary>
    ///     Fish-edit: множитель тепловыделения при горении трития и плазмы.
    ///     Снижает перегрев замкнутых объёмов (например, сабфрейма) — при ванильных
    ///     значениях температура уходила за 100 000 K.
    /// </summary>
    public const float FireEnergyReleasedMultiplier = 0.2f;

    /// <summary>
    ///     Fish-edit: максимальная доля молей газовой смеси, которую одна реакция может
    ///     превратить за один тик (10%).
    /// </summary>
    /// <remarks>
    ///     Без этого ограничения реакции синтеза/разложения выедали до 100% лимитирующего
    ///     реагента за тик, и газ превращался целиком сразу после достижения нужных условий,
    ///     хотя должен расходоваться постепенно.
    /// </remarks>
    public const float MaxConversionFractionPerTick = 0.1f;
}

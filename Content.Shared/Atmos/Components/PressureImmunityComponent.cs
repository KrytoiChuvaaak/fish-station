namespace Content.Shared.Atmos.Components;

/// <summary>
/// Marker component: the entity is immune to pressure (barotrauma).
/// </summary>
/// <remarks>
/// Раньше компонент лежал в Content.Server, из-за чего предсказанный метаболизм на клиенте
/// падал с UnknownComponentException, когда реагент выдавал статус-эффект PressureImmunity.
/// </remarks>
[RegisterComponent]
public sealed partial class PressureImmunityComponent : Component
{
}

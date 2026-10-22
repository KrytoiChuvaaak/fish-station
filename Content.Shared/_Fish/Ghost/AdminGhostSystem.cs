using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;

namespace Content.Shared._Fish.Ghost;

/// <summary>
/// Система <see cref="AdminGhostComponent"/>.
/// Даёт админскому призраку возможность запретить себе взаимодействие с миром.
/// </summary>
public sealed partial class AdminGhostSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdminGhostComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AdminGhostComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<AdminGhostComponent, ToggleAghostInteractionActionEvent>(OnToggleInteraction);

        // Тот же набор действий, который запрещён обычным призракам через GhostComponent.CanGhostInteract.
        SubscribeLocalEvent<AdminGhostComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<AdminGhostComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AdminGhostComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AdminGhostComponent, DropAttemptEvent>(OnAttempt);
    }

    private void OnMapInit(Entity<AdminGhostComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.InteractionToggleActionEntity, ent.Comp.InteractionToggleAction);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<AdminGhostComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.InteractionToggleActionEntity);
    }

    private void OnToggleInteraction(Entity<AdminGhostComponent> ent, ref ToggleAghostInteractionActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetInteractionRestricted((ent.Owner, ent.Comp), !ent.Comp.InteractionRestricted);
    }

    private void OnInteractionAttempt(Entity<AdminGhostComponent> ent, ref InteractionAttemptEvent args)
    {
        if (ent.Comp.InteractionRestricted)
            args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, AdminGhostComponent component, CancellableEntityEventArgs args)
    {
        if (component.InteractionRestricted)
            args.Cancel();
    }

    /// <summary>
    /// Включает или выключает самозапрет на взаимодействие с миром.
    /// </summary>
    public void SetInteractionRestricted(Entity<AdminGhostComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.InteractionRestricted == value)
            return;

        ent.Comp.InteractionRestricted = value;
        Dirty(ent.Owner, ent.Comp);

        _actions.SetToggled(ent.Comp.InteractionToggleActionEntity, value);

        var message = Loc.GetString(value
            ? "admin-ghost-interaction-restricted-on"
            : "admin-ghost-interaction-restricted-off");

        _popup.PopupEntity(message, ent.Owner, ent.Owner);
    }
}

#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Fish.Ghost;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Fish.Ghost;

[TestFixture]
public sealed class AdminGhostTests : GameTest
{
    /// <summary>
    /// Самозапрет на взаимодействие должен быть включён сразу при входе в агоста,
    /// переключение кнопки — без задержки, но с кулдауном на кнопке.
    /// </summary>
    [Test]
    public async Task AghostInteractionRestrictedByDefault()
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var serverConsole = server.ResolveDependency<IServerConsoleHost>();

        var player = playerMan.Sessions.Single();

        // У игрока должно быть сознание и тело, иначе команда aghost не сработает.
        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.System<SharedMindSystem>();

            var body = entMan.SpawnEntity(null, new MapCoordinates());
            entMan.EnsureComponent<MindContainerComponent>(body);

            var mind = mindSystem.CreateMind(player.UserId, "Aghost Tester");
            mindSystem.TransferTo(mind, body, mind: mind);
        });

        await pair.RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            serverConsole.ExecuteCommand(player, "aghost");
        });

        await pair.RunTicksSync(20);

        EntityUid ghost = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.Null);
            ghost = player.AttachedEntity!.Value;

            var comp = entMan.GetComponent<AdminGhostComponent>(ghost);

            // Запрет включён сразу, без дополнительных действий.
            Assert.That(comp.InteractionRestricted, Is.True);

            var actionUid = comp.InteractionToggleActionEntity;
            Assert.That(actionUid, Is.Not.Null);

            var action = entMan.GetComponent<ActionComponent>(actionUid!.Value);

            // Кнопка сразу отображается во включённом состоянии.
            Assert.That(action.Toggled, Is.True);

            // Кнопка должна работать даже при включённом запрете, иначе его нельзя снять.
            Assert.That(action.CheckCanInteract, Is.False);
        });

        await server.WaitAssertion(() =>
        {
            // Собственные кнопки агоста запрет блокировать не должен.
            var ownAttempt = new InteractionAttemptEvent(ghost, null);
            entMan.EventBus.RaiseLocalEvent(ghost, ref ownAttempt);
            Assert.That(ownAttempt.Cancelled, Is.False);

            // А вот взаимодействие с объектами мира — должно блокироваться.
            var worldAttempt = new InteractionAttemptEvent(ghost, entMan.SpawnEntity("ClothingBackpack", MapCoordinates.Nullspace));
            entMan.EventBus.RaiseLocalEvent(ghost, ref worldAttempt);
            Assert.That(worldAttempt.Cancelled, Is.True);
        });

        // Нажатие переключает состояние сразу, но кнопка уходит в кулдаун.
        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<AdminGhostComponent>(ghost);
            var actionUid = comp.InteractionToggleActionEntity!.Value;
            var action = new Entity<ActionComponent>(actionUid, entMan.GetComponent<ActionComponent>(actionUid));
            var performer = new Entity<ActionsComponent>(ghost, entMan.GetComponent<ActionsComponent>(ghost));

            entMan.System<SharedActionsSystem>().PerformAction(performer, action);

            // Состояние изменилось сразу, без задержки.
            Assert.That(entMan.GetComponent<AdminGhostComponent>(ghost).InteractionRestricted, Is.False);

            // Кнопка ушла в кулдаун на несколько секунд после нажатия.
            Assert.That(entMan.GetComponent<ActionComponent>(actionUid).Cooldown, Is.Not.Null);
        });
    }
}

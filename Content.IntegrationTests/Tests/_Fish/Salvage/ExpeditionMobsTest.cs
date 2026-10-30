#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server._Fish.Salvage;
using Content.Server._Sunrise.Misc.ShiftedAsciiTableAccent;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Fish.Salvage;

[TestFixture]
public sealed class ExpeditionMobsTest : GameTest
{
    private const string Goliath = "MobGoliath";
    private const string Pirate = "MobPirateT1Knife";

    /// <summary>
    /// Мобы экспедиций никогда не становятся гост-ролью и не получают AnomalyAccent.
    /// </summary>
    [Test]
    public async Task ExpeditionPiratesHaveNoGhostRoleAndNoAnomalyAccent()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var testMap = await Pair.CreateTestMap();

        var pirates = new List<EntityUid>();

        await server.WaitPost(() =>
        {
            // Раньше шанс гост-роли у пиратов был 10%, поэтому берём много, чтобы тест был надёжным.
            for (var i = 0; i < 60; i++)
            {
                pirates.Add(entMan.SpawnEntity(Pirate, testMap.GridCoords));
            }
        });

        // Гост-роль с вероятностью убирается отложенно, при инициализации карты.
        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            foreach (var pirate in pirates)
            {
                Assert.That(entMan.HasComponent<GhostRoleComponent>(pirate), Is.False);
                Assert.That(entMan.HasComponent<AnomalyAccentComponent>(pirate), Is.False);
            }
        });
    }

    /// <summary>
    /// Радиусы обзора и агра мобов на карте экспедиции увеличиваются, а мобы на других картах не затрагиваются.
    /// </summary>
    [Test]
    public async Task BoostMobVisionScalesRadiiOnlyOnExpeditionMap()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var testMap = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var goliath = entMan.SpawnEntity(Goliath, testMap.GridCoords);
            var pirate = entMan.SpawnEntity(Pirate, testMap.GridCoords);
            var large = entMan.SpawnEntity(Pirate, testMap.GridCoords);
            var elsewhere = entMan.SpawnEntity(Goliath, MapCoordinates.Nullspace);

            // У голиафа радиусы заданы в прототипе: обзор 6, агр 10. У пирата стандартные: 10 и 10.
            AssertRadii(entMan, goliath, 6f, 10f);
            AssertRadii(entMan, pirate, 10f, 10f);

            // Мобу с уже большими радиусами задаём их вручную, чтобы проверить предел.
            var largeBlackboard = entMan.GetComponent<HTNComponent>(large).Blackboard;
            largeBlackboard.SetValue("VisionRadius", 30f);
            largeBlackboard.SetValue("AggroVisionRadius", 40f);

            entMan.System<ExpeditionMobVisionSystem>().BoostMobVision(testMap.MapUid);

            AssertRadii(entMan, goliath, 9f, 15f);
            AssertRadii(entMan, pirate, 15f, 15f);
            // Меньший радиус поднимается только до предела, а тот, что уже выше предела, остаётся прежним.
            AssertRadii(entMan, large, ExpeditionMobVisionSystem.RadiusCap, 40f);
            // Моб вне карты экспедиции не затронут.
            AssertRadii(entMan, elsewhere, 6f, 10f);
        });
    }

    private static void AssertRadii(IEntityManager entMan, EntityUid mob, float vision, float aggro)
    {
        var blackboard = entMan.GetComponent<HTNComponent>(mob).Blackboard;
        Assert.That(blackboard.GetValueOrDefault<float>("VisionRadius", entMan), Is.EqualTo(vision));
        Assert.That(blackboard.GetValueOrDefault<float>("AggroVisionRadius", entMan), Is.EqualTo(aggro));
    }
}

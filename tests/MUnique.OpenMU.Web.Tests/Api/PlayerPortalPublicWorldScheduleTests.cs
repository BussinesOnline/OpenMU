// <copyright file="PlayerPortalPublicWorldScheduleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Web.PlayerPortal.Api;

/// <summary>Tests the read-only schedule projection against persisted OpenMU event configuration.</summary>
[TestFixture]
public sealed class PlayerPortalPublicWorldScheduleTests
{
    /// <summary>Only active, allow-listed event plugin timetables produce public events.</summary>
    [Test]
    public void UpcomingEventsUseActiveOpenMuTimetableAndServerTimeZone()
    {
        var now = new DateTimeOffset(2026, 11, 13, 0, 30, 0, TimeSpan.Zero);
        var plugIns = new[]
        {
            new PlugInConfiguration
            {
                TypeId = Guid.Parse("95e68c14-ad87-4b3c-af46-45b8f1c3bc2a"),
                IsActive = true,
                CustomConfiguration = "{\"Timetable\":[\"01:00:00\",\"13:00:00\"]}",
            },
            new PlugInConfiguration
            {
                TypeId = Guid.Parse("3ad96a70-ed24-4979-80b8-169e461e548f"),
                IsActive = false,
                CustomConfiguration = "{\"Timetable\":[\"02:00:00\"]}",
            },
            new PlugInConfiguration
            {
                TypeId = Guid.Parse("6542e452-9780-45b8-85ae-4036422e9a6e"),
                IsActive = true,
                CustomConfiguration = "{\"Timetable\":[\"00:45:00\"]}",
            },
        };

        var events = PlayerPortalPublicWorldSchedule.GetUpcoming(plugIns, null, now, TimeZoneInfo.Utc);

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Key, Is.EqualTo("bloodCastle"));
        Assert.That(events[0].StartsAtUtc, Is.EqualTo(new DateTimeOffset(2026, 11, 13, 1, 0, 0, TimeSpan.Zero)));
        Assert.That(PlayerPortalPublicWorldSchedule.ResolveServerTimeZone(null), Is.EqualTo(TimeZoneInfo.Utc));
    }

    /// <summary>Daily plugin times and Castle Siege are interpreted in the configured server time zone.</summary>
    [Test]
    public void ConfiguredServerTimeZoneIsUsedForUpcomingEventTimes()
    {
        var timeZone = PlayerPortalPublicWorldSchedule.ResolveServerTimeZone("America/Sao_Paulo");
        var now = new DateTimeOffset(2026, 11, 13, 23, 0, 0, TimeSpan.Zero);
        var plugIns = new[]
        {
            new PlugInConfiguration
            {
                TypeId = Guid.Parse("95e68c14-ad87-4b3c-af46-45b8f1c3bc2a"),
                IsActive = true,
                CustomConfiguration = "{\"Timetable\":[\"21:00:00\"]}",
            },
        };
        var events = PlayerPortalPublicWorldSchedule.GetUpcoming(plugIns, null, now, timeZone);

        Assert.That(timeZone.Id, Is.EqualTo("America/Sao_Paulo"));
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Key, Is.EqualTo("bloodCastle"));
        Assert.That(events[0].StartsAtUtc, Is.EqualTo(new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero)));
        Assert.That(PlayerPortalPublicWorldSchedule.ResolveServerTimeZone("OpenMU/InvalidZone"), Is.EqualTo(TimeZoneInfo.Utc));
    }

    /// <summary>Castle Siege uses the configured start-state weekday and time only when enabled.</summary>
    [Test]
    public async Task CastleSiegeUsesPersistedStartStateAsync()
    {
        var persistence = new InMemoryPersistenceContextProvider();
        GameConfiguration gameConfiguration;
        using (var context = persistence.CreateNewContext())
        {
            gameConfiguration = context.CreateNew<GameConfiguration>();
            var castleSiege = context.CreateNew<CastleSiegeConfiguration>();
            castleSiege.Enabled = true;
            gameConfiguration.CastleSiegeConfiguration = castleSiege;
            var start = context.CreateNew<CastleSiegeStateScheduleEntry>();
            start.State = CastleSiegeState.Start;
            start.DayOfWeek = DayOfWeek.Saturday;
            start.Hour = 20;
            start.Minute = 0;
            castleSiege.StateSchedule.Add(start);
            var ready = context.CreateNew<CastleSiegeStateScheduleEntry>();
            ready.State = CastleSiegeState.Ready;
            ready.DayOfWeek = DayOfWeek.Saturday;
            ready.Hour = 18;
            castleSiege.StateSchedule.Add(ready);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var now = new DateTimeOffset(2026, 11, 13, 23, 0, 0, TimeSpan.Zero);
        var timeZone = PlayerPortalPublicWorldSchedule.ResolveServerTimeZone("America/Sao_Paulo");
        var events = PlayerPortalPublicWorldSchedule.GetUpcoming([], gameConfiguration.CastleSiegeConfiguration, now, timeZone);

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Key, Is.EqualTo("castleSiege"));
        Assert.That(events[0].StartsAtUtc, Is.EqualTo(new DateTimeOffset(2026, 11, 14, 23, 0, 0, TimeSpan.Zero)));
    }
}

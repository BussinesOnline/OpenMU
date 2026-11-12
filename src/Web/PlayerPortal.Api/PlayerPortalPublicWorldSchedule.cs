// <copyright file="PlayerPortalPublicWorldSchedule.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

using System.Globalization;
using System.Text.Json;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>Builds the public schedule from persisted built-in event configuration.</summary>
public static class PlayerPortalPublicWorldSchedule
{
    private static readonly IReadOnlyDictionary<Guid, string> ScheduledEventKeys = new Dictionary<Guid, string>
    {
        [Guid.Parse("95e68c14-ad87-4b3c-af46-45b8f1c3bc2a")] = "bloodCastle",
        [Guid.Parse("3ad96a70-ed24-4979-80b8-169e461e548f")] = "chaosCastle",
        [Guid.Parse("61c61a58-211e-4d6a-9ea1-d25e0c4a47c5")] = "devilSquare",
        [Guid.Parse("a8f3c2d1-9e74-4ecb-8963-08a3697278c4")] = "kanturu",
        [Guid.Parse("06d18a9e-2919-4c17-9dbc-6e4f7756495c")] = "goldenInvasion",
        [Guid.Parse("548a76cc-242c-441c-bc9d-6c22745a2d72")] = "redDragonInvasion",
        [Guid.Parse("4b5d0f55-5b26-4447-b9c0-c272e5d0a141")] = "whiteWizardInvasion",
    };

    /// <summary>Gets the effective event time zone used by the current OpenMU runtime.</summary>
    /// <remarks><see cref="MUnique.OpenMU.GameLogic.GameContext.ServerTimeZone"/> defaults to UTC and has no override in the PlayerPortal host.</remarks>
    public const string TimeZoneId = "UTC";

    /// <summary>Gets one upcoming occurrence for each enabled built-in scheduled event.</summary>
    /// <param name="configuration">The persisted game configuration.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The next configured occurrence per event, ordered by UTC start time.</returns>
    public static IReadOnlyList<PlayerPortalScheduledEvent> GetUpcoming(
        IEnumerable<PlugInConfiguration> plugInConfigurations,
        CastleSiegeConfiguration? castleSiegeConfiguration,
        DateTimeOffset utcNow)
    {
        var now = utcNow.ToUniversalTime();
        var horizon = now.AddDays(8);
        var events = new List<PlayerPortalScheduledEvent>();

        foreach (var plugIn in plugInConfigurations)
        {
            if (!plugIn.IsActive || !ScheduledEventKeys.TryGetValue(plugIn.TypeId, out var key)
                || !TryReadTimetable(plugIn.CustomConfiguration, out var timetable))
            {
                continue;
            }

            var nextStart = timetable
                .Select(time => GetNextDailyStart(time, now))
                .Where(start => start > now && start <= horizon)
                .Order()
                .FirstOrDefault();
            if (nextStart != default)
            {
                events.Add(new PlayerPortalScheduledEvent(key, nextStart));
            }
        }

        if (castleSiegeConfiguration is { Enabled: true } castleSiege)
        {
            var nextStart = (castleSiege.StateSchedule ?? [])
                .Where(entry => entry.State == CastleSiegeState.Start)
                .Select(entry => GetNextWeeklyStart(entry.DayOfWeek, entry.Hour, entry.Minute, now))
                .Where(start => start > now && start <= horizon)
                .Order()
                .FirstOrDefault();
            if (nextStart != default)
            {
                events.Add(new PlayerPortalScheduledEvent("castleSiege", nextStart));
            }
        }

        return events
            .GroupBy(gameEvent => gameEvent.Key, StringComparer.Ordinal)
            .Select(group => group.MinBy(gameEvent => gameEvent.StartsAtUtc)!)
            .OrderBy(gameEvent => gameEvent.StartsAtUtc)
            .ToArray();
    }

    private static bool TryReadTimetable(string? customConfiguration, out IReadOnlyList<TimeOnly> timetable)
    {
        timetable = [];
        if (string.IsNullOrWhiteSpace(customConfiguration))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(customConfiguration);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !TryGetProperty(document.RootElement, "Timetable", out var times)
                || times.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var parsedTimes = new List<TimeOnly>();
            foreach (var value in times.EnumerateArray())
            {
                if (value.ValueKind == JsonValueKind.String
                    && TimeOnly.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                {
                    parsedTimes.Add(time);
                }
            }

            timetable = parsedTimes.Distinct().Order().ToArray();
            return timetable.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static DateTimeOffset GetNextDailyStart(TimeOnly time, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var start = new DateTimeOffset(today.ToDateTime(time, DateTimeKind.Utc));
        return start > now ? start : start.AddDays(1);
    }

    private static DateTimeOffset GetNextWeeklyStart(DayOfWeek dayOfWeek, byte hour, byte minute, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var dayOffset = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var date = today.AddDays(dayOffset);
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(hour, minute), DateTimeKind.Utc));
        return start > now ? start : start.AddDays(7);
    }
}

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
    private static readonly Guid CrywolfPlugInTypeId = Guid.Parse("3e8b5c27-9d41-4a6f-b2c8-7f1d6e0a9b34");

    /// <summary>Resolves the same configured time zone used by the OpenMU GameServer, falling back to UTC.</summary>
    /// <param name="timeZoneId">The persisted system configuration value.</param>
    public static TimeZoneInfo ResolveServerTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            // Keep this behavior in sync with Startup.Program.ResolveServerTimeZone.
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>Gets one upcoming occurrence for each enabled built-in scheduled event.</summary>
    /// <param name="plugInConfigurations">The persisted plugin configurations.</param>
    /// <param name="castleSiegeConfiguration">The persisted Castle Siege configuration, if enabled.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="serverTimeZone">The persisted OpenMU server time zone.</param>
    /// <returns>The next configured occurrence per event, ordered by UTC start time.</returns>
    public static IReadOnlyList<PlayerPortalScheduledEvent> GetUpcoming(
        IEnumerable<PlugInConfiguration> plugInConfigurations,
        CastleSiegeConfiguration? castleSiegeConfiguration,
        DateTimeOffset utcNow,
        TimeZoneInfo serverTimeZone)
    {
        var now = utcNow.ToUniversalTime();
        var horizon = now.AddDays(8);
        var events = new List<PlayerPortalScheduledEvent>();

        foreach (var plugIn in plugInConfigurations)
        {
            if (!plugIn.IsActive || GetScheduledEventKey(plugIn.TypeId) is not { } key)
            {
                continue;
            }

            if (plugIn.TypeId == CrywolfPlugInTypeId)
            {
                if (!TryReadCrywolfSchedule(plugIn.CustomConfiguration, out var startDays, out var startTimes))
                {
                    continue;
                }

                var crywolfStart = startDays
                    .SelectMany(day => startTimes.Select(time => GetNextWeeklyStart(day, time, now, serverTimeZone)))
                    .Where(start => start > now && start <= horizon)
                    .Order()
                    .FirstOrDefault();
                if (crywolfStart != default)
                {
                    events.Add(new PlayerPortalScheduledEvent(key, crywolfStart));
                }

                continue;
            }

            if (!TryReadTimetable(plugIn.CustomConfiguration, out var timetable))
            {
                continue;
            }

            var nextStart = timetable
                .Select(time => GetNextDailyStart(time, now, serverTimeZone))
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
                .Select(entry => GetNextWeeklyStart(entry.DayOfWeek, entry.Hour, entry.Minute, now, serverTimeZone))
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

    private static string? GetScheduledEventKey(Guid plugInTypeId)
    {
        return plugInTypeId.ToString("D", CultureInfo.InvariantCulture) switch
        {
            "3e8b5c27-9d41-4a6f-b2c8-7f1d6e0a9b34" => "crywolf",
            "95e68c14-ad87-4b3c-af46-45b8f1c3bc2a" => "bloodCastle",
            "3ad96a70-ed24-4979-80b8-169e461e548f" => "chaosCastle",
            "61c61a58-211e-4d6a-9ea1-d25e0c4a47c5" => "devilSquare",
            "a8f3c2d1-9e74-4ecb-8963-08a3697278c4" => "kanturu",
            "06d18a9e-2919-4c17-9dbc-6e4f7756495c" => "goldenInvasion",
            "548a76cc-242c-441c-bc9d-6c22745a2d72" => "redDragonInvasion",
            "4b5d0f55-5b26-4447-b9c0-c272e5d0a141" => "whiteWizardInvasion",
            _ => null,
        };
    }

    private static bool TryReadCrywolfSchedule(
        string? customConfiguration,
        out IReadOnlyList<DayOfWeek> startDays,
        out IReadOnlyList<TimeOnly> startTimes)
    {
        startDays = [];
        startTimes = [];
        if (string.IsNullOrWhiteSpace(customConfiguration))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(customConfiguration);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !TryGetProperty(document.RootElement, "StartDays", out var configuredDays)
                || !TryReadCollection(configuredDays, out var days)
                || !TryGetProperty(document.RootElement, "StartTimes", out var configuredTimes)
                || !TryReadCollection(configuredTimes, out var times))
            {
                return false;
            }

            var parsedDays = new List<DayOfWeek>();
            foreach (var value in days.EnumerateArray())
            {
                if (value.ValueKind == JsonValueKind.Number
                    && value.TryGetInt32(out var dayNumber)
                    && Enum.IsDefined(typeof(DayOfWeek), dayNumber))
                {
                    parsedDays.Add((DayOfWeek)dayNumber);
                }
                else if (value.ValueKind == JsonValueKind.String
                    && Enum.TryParse<DayOfWeek>(value.GetString(), true, out var day)
                    && Enum.IsDefined(typeof(DayOfWeek), day))
                {
                    parsedDays.Add(day);
                }
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

            startDays = parsedDays.Distinct().Order().ToArray();
            startTimes = parsedTimes.Distinct().Order().ToArray();
            return startDays.Count > 0 && startTimes.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadCollection(JsonElement value, out JsonElement collection)
    {
        collection = value;
        if (collection.ValueKind == JsonValueKind.Object
            && TryGetProperty(collection, "$values", out var wrappedValues))
        {
            collection = wrappedValues;
        }

        return collection.ValueKind == JsonValueKind.Array;
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
                || !TryGetProperty(document.RootElement, "Timetable", out var configuredTimes)
                || !TryReadCollection(configuredTimes, out var times))
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

    private static DateTimeOffset GetNextDailyStart(TimeOnly time, DateTimeOffset now, TimeZoneInfo serverTimeZone)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, serverTimeZone).DateTime);
        var localStart = DateTime.SpecifyKind(today.ToDateTime(time), DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, serverTimeZone);
        var start = new DateTimeOffset(startUtc, TimeSpan.Zero);
        if (start > now)
        {
            return start;
        }

        localStart = DateTime.SpecifyKind(today.AddDays(1).ToDateTime(time), DateTimeKind.Unspecified);
        startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, serverTimeZone);
        return new DateTimeOffset(startUtc, TimeSpan.Zero);
    }

    private static DateTimeOffset GetNextWeeklyStart(DayOfWeek dayOfWeek, byte hour, byte minute, DateTimeOffset now, TimeZoneInfo serverTimeZone)
    {
        return GetNextWeeklyStart(dayOfWeek, new TimeOnly(hour, minute), now, serverTimeZone);
    }

    private static DateTimeOffset GetNextWeeklyStart(DayOfWeek dayOfWeek, TimeOnly time, DateTimeOffset now, TimeZoneInfo serverTimeZone)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, serverTimeZone).DateTime);
        var dayOffset = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var date = today.AddDays(dayOffset);
        var localStart = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, serverTimeZone);
        var start = new DateTimeOffset(startUtc, TimeSpan.Zero);
        if (start > now)
        {
            return start;
        }

        localStart = DateTime.SpecifyKind(date.AddDays(7).ToDateTime(time), DateTimeKind.Unspecified);
        startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, serverTimeZone);
        return new DateTimeOffset(startUtc, TimeSpan.Zero);
    }
}

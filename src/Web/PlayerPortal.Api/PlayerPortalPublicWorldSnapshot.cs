// <copyright file="PlayerPortalPublicWorldSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

/// <summary>A bounded, read-only snapshot of public rankings and event times.</summary>
public sealed record PlayerPortalPublicWorldSnapshot(
    IReadOnlyList<PlayerPortalCharacterRankingEntry> CharacterRanking,
    IReadOnlyList<PlayerPortalGuildRankingEntry> GuildRanking,
    IReadOnlyList<PlayerPortalScheduledEvent> Events,
    string TimeZoneId,
    DateTimeOffset GeneratedAtUtc);

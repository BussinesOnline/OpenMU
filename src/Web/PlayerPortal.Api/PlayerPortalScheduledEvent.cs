// <copyright file="PlayerPortalScheduledEvent.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

/// <summary>A configured OpenMU event and its next start time.</summary>
public sealed record PlayerPortalScheduledEvent(string Key, DateTimeOffset StartsAtUtc);

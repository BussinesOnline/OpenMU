// <copyright file="PlayerPortalGuildRankingEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

/// <summary>A public guild ranking row with only fields intended for public display.</summary>
public sealed record PlayerPortalGuildRankingEntry(string Name, int Score, int MemberCount);

// <copyright file="PlayerPortalCharacterRankingEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

/// <summary>A public character ranking row without account identifiers or private account data.</summary>
public sealed record PlayerPortalCharacterRankingEntry(string Name, string ClassName, int Level, int MasterLevel);

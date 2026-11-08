// <copyright file="PlayerAccountVaultBalances.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

/// <summary>Detailed cash shop balances and pending grants for the authenticated player account.</summary>
/// <param name="WCoinC">Available WCoin (C) balance.</param>
/// <param name="WCoinP">Available WCoin (P) balance.</param>
/// <param name="GoblinPoints">Available Goblin Points balance.</param>
/// <param name="PendingWCoinC">Pending WCoin (C) grants not yet applied by the game server.</param>
/// <param name="PendingWCoinP">Pending WCoin (P) grants not yet applied by the game server.</param>
/// <param name="PendingGoblinPoints">Pending Goblin Points grants not yet applied by the game server.</param>
public sealed record PlayerAccountVaultBalances(
    int WCoinC,
    int WCoinP,
    int GoblinPoints,
    int PendingWCoinC,
    int PendingWCoinP,
    int PendingGoblinPoints);


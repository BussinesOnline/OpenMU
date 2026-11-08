// <copyright file="PlayerAccountSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

/// <summary>A persisted, owner-authorized player account snapshot with real balances.</summary>
/// <param name="LoginName">The account's login name.</param>
/// <param name="Characters">The account's characters.</param>
/// <param name="WCoinC">Available WCoin (C) balance.</param>
/// <param name="WCoinP">Available WCoin (P) balance.</param>
/// <param name="GoblinPoints">Available Goblin Points balance.</param>
/// <param name="PendingWCoinC">Pending WCoin (C) grants awaiting GameServer application.</param>
/// <param name="PendingWCoinP">Pending WCoin (P) grants awaiting GameServer application.</param>
/// <param name="PendingGoblinPoints">Pending Goblin Points grants awaiting GameServer application.</param>
/// <param name="VaultBalances">Structured vault balances breakdown.</param>
public sealed record PlayerAccountSnapshot(
    string LoginName,
    IReadOnlyList<PlayerCharacterSnapshot> Characters,
    int WCoinC = 0,
    int WCoinP = 0,
    int GoblinPoints = 0,
    int PendingWCoinC = 0,
    int PendingWCoinP = 0,
    int PendingGoblinPoints = 0,
    PlayerAccountVaultBalances? VaultBalances = null);

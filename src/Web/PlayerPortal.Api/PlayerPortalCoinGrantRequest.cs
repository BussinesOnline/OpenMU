// <copyright file="PlayerPortalCoinGrantRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>Request to grant cash shop coins to a player account.</summary>
/// <param name="LoginName">The target account login name.</param>
/// <param name="CoinType">The coin type.</param>
/// <param name="Amount">The coin amount.</param>
/// <param name="Reference">The unique idempotency reference.</param>
/// <param name="Reason">The optional reason.</param>
public sealed record PlayerPortalCoinGrantRequest(
    string LoginName,
    CashShopCoinType CoinType,
    int Amount,
    string Reference,
    string? Reason);


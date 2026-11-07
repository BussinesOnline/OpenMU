// <copyright file="AccountTermsAcceptance.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Operations;

/// <summary>Server-recorded acceptance metadata for a newly registered player portal account.</summary>
/// <param name="Version">The terms revision accepted by the user.</param>
/// <param name="AcceptedAtUtc">The server-generated UTC acceptance timestamp.</param>
public sealed record AccountTermsAcceptance(string Version, DateTime AcceptedAtUtc);

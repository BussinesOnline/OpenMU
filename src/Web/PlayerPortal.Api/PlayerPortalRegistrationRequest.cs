// <copyright file="PlayerPortalRegistrationRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

using System.ComponentModel.DataAnnotations;

/// <summary>Player portal registration data, including explicit acceptance of the required terms revision.</summary>
public sealed class PlayerPortalRegistrationRequest
{
    /// <summary>Gets or sets the OpenMU login name, limited to the Season 6 login packet size.</summary>
    [Required]
    [MinLength(3)]
    [MaxLength(10)]
    [RegularExpression(@"^[\x21-\x7E]+$")]
    public string LoginName { get; set; } = string.Empty;

    /// <summary>Gets or sets the OpenMU password, limited to the Season 6 login packet size.</summary>
    [Required]
    [MinLength(3)]
    [MaxLength(20)]
    [RegularExpression(@"^[\x20-\x7E]+$")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional e-mail address.</summary>
    [EmailAddress]
    public string? EMail { get; set; }

    /// <summary>Gets or sets whether the user explicitly accepted the required terms revision.</summary>
    [Required]
    public bool? TermsAccepted { get; set; }

    /// <summary>Gets or sets the immutable terms revision shown to the user.</summary>
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string TermsVersion { get; set; } = string.Empty;

    /// <summary>Creates the native OpenMU account request after portal-only acceptance checks pass.</summary>
    /// <returns>The account credentials and optional e-mail for native account persistence.</returns>
    internal MUnique.OpenMU.Web.AdminPanel.API.AccountRegistration ToAccountRegistration()
        => new() { LoginName = this.LoginName, Password = this.Password, EMail = this.EMail };
}

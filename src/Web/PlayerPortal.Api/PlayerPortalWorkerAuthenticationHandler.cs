// <copyright file="PlayerPortalWorkerAuthenticationHandler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.PlayerPortal.Api;

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

/// <summary>Authenticates the trusted Cloudflare Worker identities, distinguishing between common portal worker and privileged grant service.</summary>
public sealed class PlayerPortalWorkerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly byte[] _expectedWorkerToken;
    private readonly byte[] _expectedGrantToken;

    /// <summary>Initializes a new instance of the <see cref="PlayerPortalWorkerAuthenticationHandler"/> class.</summary>
    /// <param name="options">The authentication scheme options monitor.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="workerCredential">The configured common Worker service credential.</param>
    /// <param name="grantCredential">The configured privileged grant service credential.</param>
    public PlayerPortalWorkerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        PlayerPortalWorkerCredential workerCredential,
        PlayerPortalGrantCredential grantCredential)
        : base(options, logger, encoder)
    {
        this._expectedWorkerToken = Encoding.UTF8.GetBytes(workerCredential.Value);
        this._expectedGrantToken = Encoding.UTF8.GetBytes(grantCredential.Value);
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = this.Request.Headers[HeaderNames.Authorization];
        if (authorization.Count != 1 || !AuthenticationHeaderValue.TryParse(authorization[0], out var parsed)
            || !string.Equals(parsed.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(parsed.Parameter))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var candidate = Encoding.UTF8.GetBytes(parsed.Parameter);

        // 1. Check if candidate matches the dedicated privileged grant credential
        if (candidate.Length == this._expectedGrantToken.Length && CryptographicOperations.FixedTimeEquals(candidate, this._expectedGrantToken))
        {
            var grantIdentity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "cloudflare-worker-grant-service"),
                    new Claim(ClaimTypes.Role, PlayerPortalApiDefaults.PrivilegedServiceRole),
                    new Claim(ClaimTypes.Role, PlayerPortalApiDefaults.CommonWorkerRole),
                ],
                this.Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(grantIdentity), this.Scheme.Name)));
        }

        // 2. Check if candidate matches the common portal worker credential (sessions, me, login)
        if (candidate.Length == this._expectedWorkerToken.Length && CryptographicOperations.FixedTimeEquals(candidate, this._expectedWorkerToken))
        {
            var workerIdentity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "cloudflare-worker"),
                    new Claim(ClaimTypes.Role, PlayerPortalApiDefaults.CommonWorkerRole),
                ],
                this.Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(workerIdentity), this.Scheme.Name)));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid service credentials."));
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        this.Response.StatusCode = StatusCodes.Status401Unauthorized;
        this.Response.Headers.Append(HeaderNames.WWWAuthenticate, "Bearer");
        return Task.CompletedTask;
    }
}

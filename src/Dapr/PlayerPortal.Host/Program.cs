// <copyright file="Program.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MUnique.OpenMU.Dapr.Common;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.EntityFramework;
using MUnique.OpenMU.Web.PlayerPortal.Api;

var builder = DaprService.CreateBuilder("PlayerPortal", args);

// Keep local execution on loopback; container deployments must opt in to their bind address.
var bindUrl = Environment.GetEnvironmentVariable("PLAYER_PORTAL_BIND_URL");
builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(bindUrl) ? "http://127.0.0.1:8080" : bindUrl);

var directConnectionString = Environment.GetEnvironmentVariable("OPENMU_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(directConnectionString))
{
    builder.Services.AddSingleton<IDatabaseConnectionSettingProvider>(new DirectDatabaseConnectionSettingProvider(directConnectionString));
}

builder.Services.AddPeristenceProvider()
    .AddSingleton<IDataSource<GameConfiguration>, PlayerPortalGameConfigurationDataSource>();
builder.Services.AddPlayerPortalApi(builder.Configuration);
builder.Services.AddControllers().AddApplicationPart(typeof(PlayerPortalController).Assembly);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.ConfigureDaprService();

await app.WaitForDatabaseConnectionInitializationAsync().ConfigureAwait(false);
await app.RunAsync().ConfigureAwait(false);

internal sealed class DirectDatabaseConnectionSettingProvider(string connectionString) : IDatabaseConnectionSettingProvider
{
    public Task? Initialization => Task.CompletedTask;

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        ConnectionConfigurator.Initialize(this);
        return Task.CompletedTask;
    }

    public ConnectionSetting GetConnectionSetting<TContextType>()
        where TContextType : DbContext =>
        this.GetConnectionSetting(typeof(TContextType));

    public ConnectionSetting GetConnectionSetting(Type contextType) =>
        new() { DatabaseEngine = DatabaseEngine.Npgsql, ConnectionString = connectionString };
}


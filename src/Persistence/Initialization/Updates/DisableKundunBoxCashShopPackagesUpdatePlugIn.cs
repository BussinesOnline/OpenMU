// <copyright file="DisableKundunBoxCashShopPackagesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Disables Box of Kundun packages (461-465) from the Cash Shop on existing Season 6 configurations
/// to enforce the server policy prohibiting equipment sales.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7F2E648B-A53B-49C7-8B56-2CE467B9F10A")]
public class DisableKundunBoxCashShopPackagesUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug-in name.
    /// </summary>
    internal const string PlugInName = "Disable Box of Kundun cash shop packages";

    /// <summary>
    /// The plug-in description.
    /// </summary>
    internal const string PlugInDescription = "Disables Box of Kundun packages (461-465) in existing Season 6 cash shop configurations.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 8, 8, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.CashShopConfiguration is { } cashShop)
        {
            foreach (var package in cashShop.Packages)
            {
                if (package.PackageSequence is >= 461 and <= 465
                    || package.Products.Any(p => p.ItemDefinition is { Group: 14, Number: 11 }))
                {
                    package.IsForSale = false;
                }
            }
        }

        return ValueTask.CompletedTask;
    }
}

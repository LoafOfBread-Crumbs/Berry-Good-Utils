using System.Windows.Controls;

namespace BerryGoodUtils.Modules;

public interface IUtilityModule
{
    /// <summary>
    /// Display name shown on the dashboard tile.
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Short description shown on the dashboard tile.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Optional icon or symbol displayed on the tile.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// The control that hosts the module UI when launched from the dashboard.
    /// </summary>
    UserControl View { get; }
}

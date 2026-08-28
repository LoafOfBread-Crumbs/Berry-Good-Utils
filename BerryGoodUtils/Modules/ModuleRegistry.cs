using BerryGoodUtils.Modules.PartRequest;
using BerryGoodUtils.Modules.QuoteGenerator;

namespace BerryGoodUtils.Modules;

/// <summary>
/// Central registry of utility modules.
/// To add a new utility:
/// 1. Create a UserControl in Modules/&lt;YourModule&gt;/&lt;YourModule&gt;Module.xaml
///    that implements IUtilityModule.
/// 2. Add a new instance of your module to the Modules list below.
/// The dashboard will automatically display a tile for it.
/// </summary>
public static class ModuleRegistry
{
    public static IReadOnlyList<IUtilityModule> Modules { get; } = new List<IUtilityModule>
    {
        new QuoteGeneratorModule(),
        new PartRequestModule()
    };
}

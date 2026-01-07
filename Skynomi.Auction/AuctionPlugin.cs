using Skynomi.Modules;
using TShockAPI.Hooks;

namespace Skynomi.Auction
{
    public class AuctionPlugin : IModule, IDependent, IReloadable, IDisposable
    {
        public string Name => "Auction System";
        public string Description => "Auction system module for Skynomi";
        public Version Version => new(1, 0, 0);
        public string Author => "folvrix";

        public IReadOnlyList<Type> RequiredModules => new[]
        {
            typeof(Utils.UtilsModule),
            typeof(Database.DatabaseModule),
            typeof(Economy.EconomyModule)
        };

        public static AuctionPlugin Instance { get; private set; } = null!;
        private Config? _config;

        public void Initialize()
        {
            Instance = this;
            _config = Config.Read();
            AuctionManager.Initialize(_config);
            Commands.Initialize(_config);
        }

        public void Reload(ReloadEventArgs args)
        {
            _config = Config.Read();
            AuctionManager.Reload(_config);
            Commands.Reload(_config);
        }

        public void Dispose()
        {
            AuctionManager.Dispose();
        }
    }
}
using Skynomi.Utils;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.Auction
{
    public class AuctionPlugin : Loader.ISkynomiExtension, Loader.ISkynomiExtensionReloadable, Loader.ISkynomiExtensionDisposable
    {
        public string Name => "Auction System";
        public string Description => "Auction system extension for Skynomi";
        public Version Version => new(1, 0, 0);
        public string Author => "folvrix";
        
        public static AuctionPlugin Instance { get; private set; }
        private Config _config;

        public AuctionPlugin()
        {
            Instance = this;
        }

        public void Initialize()
        {
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

using System.Timers;
using Skynomi.Utils;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.Shop
{
    public class Shop : Loader.ISkynomiExtension, Loader.ISkynomiExtensionReloadable, Loader.ISkynomiExtensionPostInit
    {
        public string Name => "Shop System";
        public string Description => "Shop system extension for Skynomi";
        public Version Version => new(1, 3, 0);
        public string Author => "Keyou";

        private static Config shopConfig;
        private static System.Timers.Timer broadcastTimer;
        public void Initialize()
        {
            shopConfig = Config.Read();

            Commands.Initialize();
        }

        public void Reload(ReloadEventArgs args)
        {
            if (shopConfig.AutoBroadcastShop)
            {
                broadcastTimer.Stop();
            }

            shopConfig = Config.Read();

            Commands.Reload();

            if (shopConfig.ProtectedByRegion && string.IsNullOrEmpty(shopConfig.ShopRegion))
            {
                Log.Warn(Messages.EmptyNEnableProtectedRegion);
            }

            if (shopConfig.AutoBroadcastShop && _List() != "No items available")
            {
                broadcastTimer = new System.Timers.Timer(shopConfig.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }
        }

        public void PostInitialize(EventArgs args)
        {
            if (shopConfig.AutoBroadcastShop && _List() != "No items available")
            {
                broadcastTimer = new System.Timers.Timer(shopConfig.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }

            if (shopConfig.ProtectedByRegion && string.IsNullOrEmpty(shopConfig.ShopRegion))
            {
                Log.Warn(Messages.EmptyNEnableProtectedRegion);
            }
        }

        private static string _List()
        {
            string message = "Shop Items";
            int i = 0;
            foreach (var item in shopConfig.ShopItems)
            {
                i++;
                message += $"\n{i}. [i:{item.Key}] ({item.Key}) - B: {Util.CurrencyFormat(item.Value.buyPrice)} | S: {Util.CurrencyFormat(item.Value.sellPrice)}";
            }

            if (message == "Shop Items")
            {
                message = "No items available";
            }

            return message;
        }

        private static void OnBroadcastTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TSPlayer.All.SendInfoMessage(_List());
        }
    }
}

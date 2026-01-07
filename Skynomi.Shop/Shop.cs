using System.Timers;
using Skynomi.Modules;
using TShockAPI;
using TShockAPI.Hooks;
using Timer = System.Timers.Timer;

namespace Skynomi.Shop
{
    public class ShopModule : IModule, IReloadable, IDependent
    {
        public string Name => "Shop System";
        public string Description => "Shop system module for Skynomi";
        public Version Version => new(1, 3, 0);
        public string Author => "Keyou";

        public IReadOnlyList<Type> RequiredModules => new[]
        {
            typeof(Utils.UtilsModule),
            typeof(Database.DatabaseModule),
            typeof(Economy.EconomyModule)
        };

        private static Config _shopConfig = null!;
        private static Timer? _broadcastTimer;

        public void Initialize()
        {
            _shopConfig = Config.Read();
            Commands.Initialize();
            
            StartBroadcastTimer();
        }

        public void Reload(ReloadEventArgs args)
        {
            _broadcastTimer?.Stop();
            _shopConfig = Config.Read();
            Commands.Reload();
            
            StartBroadcastTimer();

            if (_shopConfig.ProtectedByRegion && string.IsNullOrEmpty(_shopConfig.ShopRegion))
            {
                Log.Warn(Messages.EmptyNEnableProtectedRegion);
            }
        }

        private void StartBroadcastTimer()
        {
            if (_shopConfig.AutoBroadcastShop && _List() != "No items available")
            {
                _broadcastTimer = new Timer(_shopConfig.BroadcastIntervalInSeconds * 1000);
                _broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                _broadcastTimer.AutoReset = true;
                _broadcastTimer.Start();
            }
        }

        private static string _List()
        {
            var utils = ModuleManager.Get<Utils.UtilsModule>();
            string message = "Shop Items";
            int i = 0;
            foreach (var item in _shopConfig.ShopItems)
            {
                i++;
                message += $"\n{i}. [i:{item.Key}] ({item.Key}) - B: {utils.CurrencyFormat(item.Value.buyPrice)} | S: {utils.CurrencyFormat(item.Value.sellPrice)}";
            }

            if (message == "Shop Items")
            {
                message = "No items available";
            }

            return message;
        }

        private static void OnBroadcastTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            TSPlayer.All.SendInfoMessage(_List());
        }
    }
}

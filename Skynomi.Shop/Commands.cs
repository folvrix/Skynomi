using Skynomi.Modules;
using Terraria;
using TShockAPI;

namespace Skynomi.Shop
{
    public static class Commands
    {
        private static Config _shopConfig = null!;

        public static void Initialize()
        {
            _shopConfig = Config.Read();

            TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.Shop, Shop, "shop")
            {
                AllowServer = false,
                HelpText = "Shop commands:\nbuy <item> [amount] - Buy an item\nsell <item> [amount] - Sell an item\nlist [page] - List all items in the shop"
            });
        }

        public static void Reload()
        {
            _shopConfig = Config.Read();
        }

        private static void Shop(CommandArgs args)
        {
            var utils = ModuleManager.Get<Utils.UtilsModule>();
            var economy = ModuleManager.Get<Economy.EconomyModule>();

            string shopUsage = "Usage: /shop <buy/sell/list>";
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage(shopUsage);
                return;
            }

            if (_shopConfig.ProtectedByRegion)
            {
                var region = TShock.Regions.GetRegionByName(_shopConfig.ShopRegion);
                if (region == null || !region.InArea((int)(args.Player.X / 16), (int)(args.Player.Y / 16)))
                {
                    args.Player.SendErrorMessage("You can only use the shop command in the shop region.");
                    return;
                }
            }

            #region Buy
            if (args.Parameters[0] == "buy")
            {
                if (!utils.CheckPermission(Permissions.Buy, args)) return;

                try
                {
                    string usage = "Usage: /shop buy <item> [amount]";

                    int itemAmount = 1;
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage(usage);
                        return;
                    }
                    else if (args.Parameters.Count >= 3 && !int.TryParse(args.Parameters[2], out itemAmount))
                    {
                        args.Player.SendErrorMessage("Invalid amount");
                        return;
                    }

                    var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).FirstOrDefault(x =>
                        x.Name.Equals(args.Parameters[1], StringComparison.OrdinalIgnoreCase) ||
                        (int.TryParse(args.Parameters[1], out int parsedId) && x.netID == parsedId));

                    if (item == null || item.netID == 0)
                    {
                        args.Player.SendErrorMessage("Item not found!");
                        return;
                    }

                    if (itemAmount < 1)
                    {
                        args.Player.SendErrorMessage("Amount must be greater than 0.");
                        return;
                    }

                    if (!_shopConfig.ShopItems.TryGetValue(item.netID.ToString(), out var shopItem))
                    {
                        args.Player.SendErrorMessage("Item not found in shop");
                        return;
                    }

                    if (TShock.Utils.GetItemById(item.netID).maxStack == 1 && itemAmount > 1)
                    {
                        args.Player.SendErrorMessage("This item can only be bought one at a time.");
                        return;
                    }

                    long totalPrice = (long)shopItem.buyPrice * itemAmount;
                    long balance = economy.Db.GetWalletBalance(args.Player.Account.Name) ?? 0;

                    if (balance < totalPrice)
                    {
                        args.Player.SendErrorMessage($"You do not have enough currency to buy this item. (Need {utils.CurrencyFormat(totalPrice - balance)} more)");
                        return;
                    }

                    int itemPrefix = shopItem.prefix;
                    if (!TShock.Utils.GetItemById(item.netID).CanApplyPrefix(itemPrefix))
                        itemPrefix = 0;

                    args.Player.SendInfoMessage($"You have bought [i/s{itemAmount},p{itemPrefix}:{item.netID}] for {utils.CurrencyFormat(totalPrice)}");
                    args.Player.GiveItem(item.netID, itemAmount, itemPrefix);
                    economy.Db.UpdateWalletBalance(args.Player.Account.Name, w => w.Balance -= totalPrice);

                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            #endregion

            #region Sell
            else if (args.Parameters[0] == "sell")
            {
                if (!utils.CheckPermission(Permissions.Sell, args)) return;

                string usage = "Usage: /shop sell <item> [amount]";

                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).FirstOrDefault(x =>
                    x.Name.Equals(args.Parameters[1], StringComparison.OrdinalIgnoreCase) ||
                    (int.TryParse(args.Parameters[1], out int parsedId) && x.netID == parsedId));

                if (item == null || item.netID == 0)
                {
                    args.Player.SendErrorMessage("Item not found!");
                    return;
                }

                int amount = 1;
                if (args.Parameters.Count > 2 && !int.TryParse(args.Parameters[2], out amount))
                {
                    args.Player.SendErrorMessage("Invalid amount");
                    return;
                }

                if (amount <= 0)
                {
                    args.Player.SendErrorMessage("Amount must be greater than 0.");
                    return;
                }

                if (!_shopConfig.ShopItems.TryGetValue(item.netID.ToString(), out var shopItem))
                {
                    args.Player.SendErrorMessage("Item not sellable");
                    return;
                }

                int totalOwned = 0;
                foreach (var i in args.Player.TPlayer.inventory)
                {
                    if (i.type == item.netID)
                    {
                        totalOwned += i.stack;
                    }
                }

                if (totalOwned < amount)
                {
                    args.Player.SendErrorMessage($"You don't have {amount} of this item.");
                    return;
                }

                int remainingToRemove = amount;
                for (int i = 0; i < args.Player.TPlayer.inventory.Length; i++)
                {
                    if (args.Player.TPlayer.inventory[i].type == item.netID)
                    {
                        if (args.Player.TPlayer.inventory[i].stack > remainingToRemove)
                        {
                            args.Player.TPlayer.inventory[i].stack -= remainingToRemove;
                            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, args.Player.Index, i);
                            break;
                        }

                        remainingToRemove -= args.Player.TPlayer.inventory[i].stack;
                        args.Player.TPlayer.inventory[i].netDefaults(0);
                        NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, args.Player.Index, i);
                    }
                    if (remainingToRemove <= 0) break;
                }

                long totalGain = (long)shopItem.sellPrice * amount;
                args.Player.SendInfoMessage($"You have sold [i/s{amount}:{item.netID}] for {utils.CurrencyFormat(totalGain)}");
                economy.Db.UpdateWalletBalance(args.Player.Account.Name, w => w.Balance += totalGain);
            }
            #endregion

            #region List
            else if (args.Parameters[0] == "list")
            {
                if (!utils.CheckPermission(Permissions.List, args)) return;

                int pageSize = _shopConfig.ListLength;
                int currentPage = 1;
                int totalPages = (int)Math.Ceiling(_shopConfig.ShopItems.Count / (double)pageSize);

                if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int parsedPage))
                {
                    if (totalPages == 0)
                    {
                        args.Player.SendErrorMessage("No items available");
                        return;
                    }
                    currentPage = Math.Clamp(parsedPage, 1, totalPages);
                }

                var itemsToDisplay = _shopConfig.ShopItems
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize);

                string message = $"Shop Items (Page {currentPage}/{totalPages})";
                int index = (currentPage - 1) * pageSize + 1;
                foreach (var item in itemsToDisplay)
                {
                    int itemId = Convert.ToInt32(item.Key);
                    int prefix = 0;
                    string prefixName = "";
                    if (itemId != 0)
                    {
                        if (TShock.Utils.GetItemById(itemId).CanApplyPrefix(item.Value.prefix))
                        {
                            prefix = item.Value.prefix;
                            prefixName = TShock.Utils.GetPrefixById(item.Value.prefix);
                        }
                    }

                    message += $"\n{index}. [i/p{prefix}:{item.Key}] {TShock.Utils.GetItemById(itemId).Name} ({item.Key}) {(!string.IsNullOrWhiteSpace(prefixName) ? "[" + prefixName + "] " : "")}- B: {utils.CurrencyFormat(item.Value.buyPrice)} | S: {utils.CurrencyFormat(item.Value.sellPrice)}";
                    index++;
                }

                args.Player.SendInfoMessage(message);
            }
            else
            {
                args.Player.SendErrorMessage(shopUsage);
            }
            #endregion
        }
    }
}

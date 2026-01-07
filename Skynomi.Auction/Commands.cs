using Skynomi.Utils;
using Terraria;
using TShockAPI;

namespace Skynomi.Auction
{
    public class Commands
    {
        private static Config _config;

        public static void Initialize(Config config)
        {
            _config = config;
            TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.Create, AuctionCmd, "auction"));
        }

        public static void Reload(Config config)
        {
            _config = config;
        }

        private static void AuctionCmd(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("Usage: /auction <create/bid/list/info/cancel/claim/settings/admin>");
                return;
            }

            string sub = args.Parameters[0].ToLower();
            switch (sub)
            {
                case "create":
                    {
                        if (!Util.CheckPermission(Permissions.Create, args)) return;
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Usage: /auction create <price>");
                            return;
                        }

                        if (!long.TryParse(args.Parameters[1], out long price) || price <= 0)
                        {
                            args.Player.SendErrorMessage("Invalid price.");
                            return;
                        }

                        if (AuctionManager.AwaitingDrops.ContainsKey(args.Player.Name))
                        {
                            args.Player.SendErrorMessage("You already have a pending auction creation. Drop the item!");
                            return;
                        }

                        AuctionManager.AwaitingDrops[args.Player.Name] = price;
                        args.Player.SendSuccessMessage("Throw the item you want to auction!");
                        break;
                    }
                case "bid":
                    {
                        if (!Util.CheckPermission(Permissions.Bid, args)) return;
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Usage: /auction bid <amount>");
                            return;
                        }
                        if (!long.TryParse(args.Parameters[1], out long amount) || amount <= 0)
                        {
                            args.Player.SendErrorMessage("Invalid amount.");
                            return;
                        }

                        AuctionManager.Bid(args.Player, amount);
                        break;
                    }
                case "list":
                    {
                        var auctions = AuctionManager.GetActiveAuctions();
                        if (auctions.Count == 0)
                        {
                            args.Player.SendInfoMessage("No active auctions.");
                            return;
                        }

                        args.Player.SendInfoMessage("Active Auctions:");
                        foreach (var a in auctions)
                        {
                            args.Player.SendInfoMessage($"#{a.Id}: {a.SellerName} selling [i/s{a.Stack}:{a.ItemId}] - Current Bid: {Util.CurrencyFormat(a.CurrentBid)} (Ends in {a.EndTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds()}s)");
                        }
                        break;
                    }
                case "info":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Usage: /auction info <id>");
                            return;
                        }
                        if (!int.TryParse(args.Parameters[1], out int id))
                        {
                            args.Player.SendErrorMessage("Invalid ID.");
                            return;
                        }
                        var a = AuctionManager.GetAuction(id);
                        if (a == null)
                        {
                            args.Player.SendErrorMessage("Auction not found.");
                            return;
                        }
                        args.Player.SendInfoMessage($"Auction #{a.Id}");
                        args.Player.SendInfoMessage($"Item: [i/s{a.Stack}:{a.ItemId}] (Prefix: {a.Prefix})");
                        args.Player.SendInfoMessage($"Seller: {a.SellerName}");
                        args.Player.SendInfoMessage($"Price: {Util.CurrencyFormat(a.StartingPrice)}");
                        args.Player.SendInfoMessage($"High Bid: {Util.CurrencyFormat(a.CurrentBid)} by {(a.HighBidderName ?? "None")}");
                        args.Player.SendInfoMessage($"Time Left: {a.EndTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds()}s");
                        break;
                    }
                case "cancel":
                    {
                         if (!Util.CheckPermission(Permissions.Cancel, args)) return;
                         if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int id))
                         {
                             AuctionManager.CancelAuction(args.Player, id);
                         }
                         else
                         {
                             AuctionManager.CancelAuction(args.Player);
                         }
                         break;
                    }
                case "claim":
                    {
                        AuctionManager.Claim(args.Player);
                        break;
                    }
                case "settings":
                    {
                        if (!Util.CheckPermission(Permissions.Settings, args)) return;
                        if (args.Parameters.Count < 3 || args.Parameters[1] != "broadcast")
                        {
                             args.Player.SendErrorMessage("Usage: /auction settings broadcast <on/off>");
                             return;
                        }
                        
                        if (args.Parameters[2] == "on") _config.BroadcastAuction = true;
                        else if (args.Parameters[2] == "off") _config.BroadcastAuction = false;
                        else { args.Player.SendErrorMessage("Invalid option."); return; }
                        
                        args.Player.SendSuccessMessage($"Broadcast set to {_config.BroadcastAuction}");
                        break;
                    }
                case "admin":
                    {
                        if (!Util.CheckPermission(Permissions.Admin, args)) return;
                         if (args.Parameters.Count < 2)
                        {
                             args.Player.SendErrorMessage("Usage: /auction admin <cancel/end/list/reload>");
                             return;
                        }
                        
                        string adminSub = args.Parameters[1].ToLower();
                        if (adminSub == "cancel")
                        {
                            if (args.Parameters.Count < 3 || !int.TryParse(args.Parameters[2], out int id))
                            {
                                args.Player.SendErrorMessage("Usage: /auction admin cancel <id>");
                                return;
                            }
                            AuctionManager.CancelAuction(args.Player, id);
                        }
                        else if (adminSub == "list")
                        {
                             var auctions = AuctionManager.GetActiveAuctions();
                             args.Player.SendInfoMessage($"Total Active: {auctions.Count}");
                             foreach (var a in auctions)
                                args.Player.SendInfoMessage($"#{a.Id}: {a.SellerName} - {a.ItemId}");
                        }
                        else if (adminSub == "reload")
                        {
                            _config = Config.Read();
                            AuctionManager.Reload(_config);
                            args.Player.SendSuccessMessage("Auction config reloaded.");
                        }
                        break;
                    }
                default:
                    args.Player.SendErrorMessage("Invalid subcommand.");
                    break;
            }
        }
    }

    public static class Permissions
    {
        public const string Create = "auction.create";
        public const string Bid = "auction.bid";
        public const string Cancel = "auction.cancel";
        public const string Settings = "auction.settings";
        public const string Admin = "auction.admin";
    }
}

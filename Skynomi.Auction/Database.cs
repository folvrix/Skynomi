using System.Data;
using Microsoft.Data.Sqlite;
using Skynomi.Utils;
using TShockAPI;
using TShockAPI.DB;

namespace Skynomi.Auction
{
    public class Database
    {
        private readonly string _dbPath;

        public Database()
        {
            _dbPath = Path.Combine(TShock.SavePath, "Skynomi", "Auction.sqlite");
            Initialize();
        }

        private void Initialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            
            using var conn = GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Auctions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SellerName TEXT NOT NULL,
                    ItemId INTEGER NOT NULL,
                    Stack INTEGER NOT NULL,
                    Prefix INTEGER NOT NULL,
                    StartingPrice INTEGER NOT NULL,
                    CurrentBid INTEGER NOT NULL,
                    HighBidderName TEXT,
                    EndTime INTEGER NOT NULL,
                    IsActive INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS DeliveryQueue (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayerName TEXT NOT NULL,
                    ItemId INTEGER NOT NULL,
                    Stack INTEGER NOT NULL,
                    Prefix INTEGER NOT NULL,
                    Amount INTEGER NOT NULL DEFAULT 0
                );
            ";
            cmd.ExecuteNonQuery();
        }

        private SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={_dbPath}");
        }

        public int CreateAuction(string seller, int itemId, int stack, int prefix, long price, long endTime)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Auctions (SellerName, ItemId, Stack, Prefix, StartingPrice, CurrentBid, EndTime, IsActive) VALUES (@seller, @itemId, @stack, @prefix, @price, @price, @endTime, 1); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@seller", seller);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            cmd.Parameters.AddWithValue("@stack", stack);
            cmd.Parameters.AddWithValue("@prefix", prefix);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@endTime", endTime);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateBid(int auctionId, string bidder, long bidAmount, long newEndTime)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Auctions SET HighBidderName = @bidder, CurrentBid = @bid, EndTime = @endTime WHERE Id = @id";
            cmd.Parameters.AddWithValue("@bidder", bidder);
            cmd.Parameters.AddWithValue("@bid", bidAmount);
            cmd.Parameters.AddWithValue("@endTime", newEndTime);
            cmd.Parameters.AddWithValue("@id", auctionId);
            cmd.ExecuteNonQuery();
        }

        public void EndAuction(int auctionId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Auctions SET IsActive = 0 WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", auctionId);
            cmd.ExecuteNonQuery();
        }

        public void AddToDeliveryQueue(string player, int itemId, int stack, int prefix, long money = 0)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO DeliveryQueue (PlayerName, ItemId, Stack, Prefix, Amount) VALUES (@player, @itemId, @stack, @prefix, @money)";
            cmd.Parameters.AddWithValue("@player", player);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            cmd.Parameters.AddWithValue("@stack", stack);
            cmd.Parameters.AddWithValue("@prefix", prefix);
            cmd.Parameters.AddWithValue("@money", money);
            cmd.ExecuteNonQuery();
        }

        public List<AuctionItem> LoadActiveAuctions()
        {
            var list = new List<AuctionItem>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Auctions WHERE IsActive = 1";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AuctionItem
                {
                    Id = reader.GetInt32(0),
                    SellerName = reader.GetString(1),
                    ItemId = reader.GetInt32(2),
                    Stack = reader.GetInt32(3),
                    Prefix = reader.GetInt32(4),
                    StartingPrice = reader.GetInt64(5),
                    CurrentBid = reader.GetInt64(6),
                    HighBidderName = reader.IsDBNull(7) ? null : reader.GetString(7),
                    EndTime = reader.GetInt64(8)
                });
            }
            return list;
        }

        public List<DeliveryItem> GetDeliveries(string player)
        {
            var list = new List<DeliveryItem>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ItemId, Stack, Prefix, Amount FROM DeliveryQueue WHERE PlayerName = @player";
            cmd.Parameters.AddWithValue("@player", player);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DeliveryItem
                {
                    Id = reader.GetInt32(0),
                    ItemId = reader.GetInt32(1),
                    Stack = reader.GetInt32(2),
                    Prefix = reader.GetInt32(3),
                    Money = reader.GetInt64(4)
                });
            }
            return list;
        }

        public void RemoveDelivery(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DeliveryQueue WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }

    public class AuctionItem
    {
        public int Id { get; set; }
        public string SellerName { get; set; }
        public int ItemId { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public long StartingPrice { get; set; }
        public long CurrentBid { get; set; }
        public string? HighBidderName { get; set; }
        public long EndTime { get; set; }
    }

    public class DeliveryItem
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public long Money { get; set; }
    }
}

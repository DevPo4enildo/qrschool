using Npgsql;
using qrschool.Models;

namespace qrschool.Service
{
    public class InventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<InventoryItemDto?> GetByCodeAsync(string code)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var computer = await FindComputerAsync(conn, code);
            if (computer != null)
                return computer;

            var monitor = await FindMonitorAsync(conn, code);
            if (monitor != null)
                return monitor;

            var per = await FindPeripheralAsync(conn, code);
            if (per != null)
                return per;

            return null;
        }

        private static async Task<InventoryItemDto?> FindComputerAsync(NpgsqlConnection conn, string code)
        {
            const string sql = @"
            SELECT c.code,
                   c.inventory_no,
                   c.cpu,
                   c.ram_gb,
                   c.storage,
                   c.status,
                   r.name AS room_name
            FROM computers c
            LEFT JOIN rooms r ON r.id = c.room_id
            WHERE c.code = @code";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var cpu = reader["cpu"] as string;
            var ram = reader["ram_gb"] as int?;
            var storage = reader["storage"] as string;

            var descParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(cpu)) descParts.Add(cpu);
            if (ram.HasValue) descParts.Add($"{ram.Value} GB RAM");
            if (!string.IsNullOrWhiteSpace(storage)) descParts.Add(storage);

            return new InventoryItemDto
            {
                ObjectType = "computer",
                Code = reader["code"].ToString() ?? string.Empty,
                InventoryNo = reader["inventory_no"] as string,
                RoomName = reader["room_name"] as string,
                Status = reader["status"].ToString() ?? string.Empty,
                Description = descParts.Count > 0 ? string.Join(", ", descParts) : null
            };
        }

        private static async Task<InventoryItemDto?> FindMonitorAsync(NpgsqlConnection conn, string code)
        {
            const string sql = @"
            SELECT m.code,
                   m.inventory_no,
                   m.brand,
                   m.model,
                   m.diagonal_inch,
                   m.status,
                   r.name AS room_name
            FROM monitors m
            LEFT JOIN rooms r ON r.id = m.room_id
            WHERE m.code = @code";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var brand = reader["brand"] as string;
            var model = reader["model"] as string;
            var diagonal = reader["diagonal_inch"] as decimal?;

            var descParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(brand)) descParts.Add(brand);
            if (!string.IsNullOrWhiteSpace(model)) descParts.Add(model);
            if (diagonal.HasValue) descParts.Add($"{diagonal.Value}\"");

            return new InventoryItemDto
            {
                ObjectType = "monitor",
                Code = reader["code"].ToString() ?? string.Empty,
                InventoryNo = reader["inventory_no"] as string,
                RoomName = reader["room_name"] as string,
                Status = reader["status"].ToString() ?? string.Empty,
                Description = descParts.Count > 0 ? string.Join(" ", descParts) : null
            };
        }

        private static async Task<InventoryItemDto?> FindPeripheralAsync(NpgsqlConnection conn, string code)
        {
            const string sql = @"
            SELECT p.code,
                   p.inventory_no,
                   p.type,
                   p.brand,
                   p.model,
                   p.status,
                   r.name AS room_name
            FROM peripherals p
            LEFT JOIN rooms r ON r.id = p.room_id
            WHERE p.code = @code";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var type = reader["type"] as string;
            var brand = reader["brand"] as string;
            var model = reader["model"] as string;

            var descParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(type)) descParts.Add(type);
            if (!string.IsNullOrWhiteSpace(brand)) descParts.Add(brand);
            if (!string.IsNullOrWhiteSpace(model)) descParts.Add(model);

            return new InventoryItemDto
            {
                ObjectType = "peripheral",
                Code = reader["code"].ToString() ?? string.Empty,
                InventoryNo = reader["inventory_no"] as string,
                RoomName = reader["room_name"] as string,
                Status = reader["status"].ToString() ?? string.Empty,
                Description = descParts.Count > 0 ? string.Join(" ", descParts) : null
            };
        }
    }
}

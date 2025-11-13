using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace qrschool.Models
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

            // 1) Компьютер
            var computer = await FindComputerAsync(conn, code);
            if (computer != null)
                return computer;

            // 2) Монитор
            var monitor = await FindMonitorAsync(conn, code);
            if (monitor != null)
                return monitor;

            // 3) Периферия
            var peripheral = await FindPeripheralAsync(conn, code);
            if (peripheral != null)
                return peripheral;

            return null;
        }

        private static async Task<InventoryItemDto?> FindComputerAsync(NpgsqlConnection conn, string code)
        {
            const string sql = @"
            SELECT c.code, c.inventory_no, c.cpu, c.ram_gb, c.storage,
                   c.status, r.name AS room_name
            FROM computers c
            LEFT JOIN rooms r ON r.id = c.room_id
            WHERE c.code = @code";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new InventoryItemDto
            {
                ObjectType = "computer",
                Code = reader["code"].ToString(),
                InventoryNo = reader["inventory_no"] as string,
                Status = reader["status"].ToString(),
                RoomName = reader["room_name"] as string,
                Description = $"{reader["cpu"]}, {reader["ram_gb"]}GB RAM, {reader["storage"]}"
            };
        }

        private static async Task<InventoryItemDto?> FindMonitorAsync(NpgsqlConnection conn, string code)
        {
            const string sql = @"
            SELECT m.code, m.inventory_no, m.brand, m.model,
                   m.diagonal_inch, m.status, r.name AS room_name
            FROM monitors m
            LEFT JOIN rooms r ON r.id = m.room_id
            WHERE m.code = @code";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new InventoryItemDto
            {
                ObjectType = "monitor",
                Code = reader["code"].ToString(),
                InventoryNo = reader["inventory_no"] as string,
                Status = reader["status"].ToString(),
                RoomName = reader["room_name"] as string,
                Description = $"{reader["brand"]} {reader["model"]} {reader["diagonal_inch"]}\""
            };
        }

        private static async Task<InventoryItemDto?> FindPeripheralAsync(NpgsqlConnection conn, string code)
        {
            const string sql = @"
            SELECT p.code, p.inventory_no, p.type, p.brand, p.model,
                   p.status, r.name AS room_name
            FROM peripherals p
            LEFT JOIN rooms r ON r.id = p.room_id
            WHERE p.code = @code";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new InventoryItemDto
            {
                ObjectType = "peripheral",
                Code = reader["code"].ToString(),
                InventoryNo = reader["inventory_no"] as string,
                Status = reader["status"].ToString(),
                RoomName = reader["room_name"] as string,
                Description = $"{reader["type"]} {reader["brand"]} {reader["model"]}"
            };
        }
    }
}

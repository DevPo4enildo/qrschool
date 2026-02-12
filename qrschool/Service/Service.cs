using Npgsql;
using qrschool.Models;

namespace qrschool.Services;

public interface IEquipmentService
{
    Task<bool> AddEquipmentAsync(Equipment equipment);
    Task<List<Equipment>> GetAllEquipmentAsync();
    Task<Equipment?> GetEquipmentByIdAsync(int id);
    Task<bool> UpdateEquipmentAsync(Equipment equipment);
    Task<bool> DeleteEquipmentAsync(int id);
    Task<List<string>> GetEquipmentTypesAsync();
    Task<List<string>> GetOfficesAsync();
    Task<List<string>> GetStatusOptionsAsync();
}

public class EquipmentService : IEquipmentService
{
    private readonly string _connectionString;
    private bool _initialized;

    public EquipmentService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> AddEquipmentAsync(Equipment equipment)
    {
        await EnsureInitializedAsync();

        const string sql = @"
            INSERT INTO equipment (type, office, status, description, created_date)
            VALUES (@type, @office, @status, @description, @createdDate)
            RETURNING id;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("type", equipment.Type);
        cmd.Parameters.AddWithValue("office", equipment.Office);
        cmd.Parameters.AddWithValue("status", equipment.Status);
        cmd.Parameters.AddWithValue("description", (object?)equipment.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("createdDate", equipment.CreatedDate == default ? DateTime.UtcNow : equipment.CreatedDate);

        var id = await cmd.ExecuteScalarAsync();
        if (id == null)
        {
            return false;
        }

        equipment.Id = Convert.ToInt32(id);
        if (equipment.CreatedDate == default)
        {
            equipment.CreatedDate = DateTime.UtcNow;
        }

        return true;
    }

    public async Task<List<Equipment>> GetAllEquipmentAsync()
    {
        await EnsureInitializedAsync();

        const string sql = @"
            SELECT id, type, office, status, description, created_date
            FROM equipment
            ORDER BY created_date DESC, id DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var result = new List<Equipment>();
        while (await reader.ReadAsync())
        {
            result.Add(new Equipment
            {
                Id = reader.GetInt32(0),
                Type = reader.GetString(1),
                Office = reader.GetString(2),
                Status = reader.GetString(3),
                Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                CreatedDate = reader.GetDateTime(5)
            });
        }

        return result;
    }

    public async Task<Equipment?> GetEquipmentByIdAsync(int id)
    {
        await EnsureInitializedAsync();

        const string sql = @"
            SELECT id, type, office, status, description, created_date
            FROM equipment
            WHERE id = @id;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Equipment
        {
            Id = reader.GetInt32(0),
            Type = reader.GetString(1),
            Office = reader.GetString(2),
            Status = reader.GetString(3),
            Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            CreatedDate = reader.GetDateTime(5)
        };
    }

    public async Task<bool> UpdateEquipmentAsync(Equipment equipment)
    {
        await EnsureInitializedAsync();

        const string sql = @"
            UPDATE equipment
            SET type = @type,
                office = @office,
                status = @status,
                description = @description
            WHERE id = @id;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", equipment.Id);
        cmd.Parameters.AddWithValue("type", equipment.Type);
        cmd.Parameters.AddWithValue("office", equipment.Office);
        cmd.Parameters.AddWithValue("status", equipment.Status);
        cmd.Parameters.AddWithValue("description", (object?)equipment.Description ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteEquipmentAsync(int id)
    {
        await EnsureInitializedAsync();

        const string sql = "DELETE FROM equipment WHERE id = @id;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public Task<List<string>> GetEquipmentTypesAsync() => Task.FromResult(new List<string>
    {
        "Компьютер", "Монитор", "Принтер", "Сканер", "Ноутбук", "Проектор", "ИБП", "Сетевое оборудование", "Телефон", "МФУ"
    });

    public async Task<List<string>> GetOfficesAsync()
    {
        await EnsureInitializedAsync();

        const string sql = "SELECT DISTINCT office FROM equipment ORDER BY office;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var offices = new List<string>();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                offices.Add(reader.GetString(0));
            }
        }

        if (offices.Count == 0)
        {
            offices.AddRange(new[] { "101", "102", "201", "202", "301" });
        }

        return offices;
    }

    public Task<List<string>> GetStatusOptionsAsync() => Task.FromResult(new List<string>
    {
        "Рабочий", "В ремонте", "Списано", "Резерв", "Новый", "Требует настройки"
    });

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        const string sql = @"
            CREATE TABLE IF NOT EXISTS equipment
            (
                id SERIAL PRIMARY KEY,
                type TEXT NOT NULL,
                office TEXT NOT NULL,
                status TEXT NOT NULL,
                description TEXT NULL,
                created_date TIMESTAMP NOT NULL DEFAULT NOW()
            );";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();

        _initialized = true;
    }
}

using System.Data;
using Microsoft.Data.SqlClient;
using App.Core;

namespace App.BLL.SP;

public class SpService : IService
{
    private readonly string _connectionString;

    public SpService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var results = new List<User>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("GetUsers", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new User
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return results;
    }
}

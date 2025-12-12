using App.Core;
using Microsoft.Data.SqlClient;
using System.Data;

namespace App.BLL.SP;

public class SpOperatorService : IOperatorService
{
    private readonly string _connectionString;

    public SpOperatorService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Operator?> GetOperatorDetailsAsync(string operatorID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetOperatorDetails", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OperatorID", operatorID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new Operator
        {
            OperatorID = reader.GetString(reader.GetOrdinal("OperatorID")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
            RouteID = reader.IsDBNull(reader.GetOrdinal("RouteID"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("RouteID")),
            WarehouseID = reader.IsDBNull(reader.GetOrdinal("WarehouseID"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("WarehouseID")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            Route = reader.IsDBNull(reader.GetOrdinal("RouteName"))
                ? null
                : new Route
                {
                    RouteID = reader.GetInt32(reader.GetOrdinal("RouteID")),
                    RouteName = reader.GetString(reader.GetOrdinal("RouteName")),
                    AreaID = reader.GetInt32(reader.GetOrdinal("AreaID"))
                },
            Warehouse = reader.IsDBNull(reader.GetOrdinal("WarehouseName"))
                ? null
                : new Warehouse
                {
                    WarehouseID = reader.GetInt32(reader.GetOrdinal("WarehouseID")),
                    WarehouseName = reader.GetString(reader.GetOrdinal("WarehouseName"))
                }
        };
    }

    public async Task<List<OperatorCollectionPointView>> GetMyCollectionPointsAsync(string operatorID)
    {
        var points = new List<OperatorCollectionPointView>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "SELECT * FROM WasteManagement.vw_OperatorCollectionPoints WHERE OperatorID = @OperatorID AND Status = 'Pending'",
            conn);

        cmd.Parameters.AddWithValue("@OperatorID", operatorID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            points.Add(new OperatorCollectionPointView
            {
                OperatorID = reader.GetString(reader.GetOrdinal("OperatorID")),
                OperatorName = reader.GetString(reader.GetOrdinal("OperatorName")),
                RouteID = reader.IsDBNull(reader.GetOrdinal("RouteID"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("RouteID")),
                RouteName = reader.IsDBNull(reader.GetOrdinal("RouteName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("RouteName")),
                ListingID = reader.GetInt32(reader.GetOrdinal("ListingID")),
                CitizenID = reader.GetString(reader.GetOrdinal("CitizenID")),
                CitizenName = reader.GetString(reader.GetOrdinal("CitizenName")),
                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Address")),
                AreaName = reader.GetString(reader.GetOrdinal("AreaName")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                Weight = reader.GetDecimal(reader.GetOrdinal("Weight")),
                EstimatedPrice = reader.IsDBNull(reader.GetOrdinal("EstimatedPrice"))
                    ? null
                    : reader.GetDecimal(reader.GetOrdinal("EstimatedPrice")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("VerificationCode"))
            });
        }

        return points;
    }

    public async Task<int> CollectWasteAsync(CollectionDto dto)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_PerformCollection", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OperatorID", dto.OperatorID);
        cmd.Parameters.AddWithValue("@ListingID", dto.ListingID);
        cmd.Parameters.AddWithValue("@CollectedWeight", dto.CollectedWeight);
        cmd.Parameters.AddWithValue("@WarehouseID", dto.WarehouseID);

        var collectionIDParam = new SqlParameter("@CollectionID", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(collectionIDParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return (int)collectionIDParam.Value;
    }

    public async Task<bool> DepositWasteAsync(WarehouseDepositDto dto)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            @"IF EXISTS (SELECT 1 FROM WasteManagement.WarehouseStock
                         WHERE WarehouseID = @WarehouseID AND CategoryID = @CategoryID)
              BEGIN
                  UPDATE WasteManagement.WarehouseStock
                  SET CurrentWeight = CurrentWeight + @Quantity, LastUpdated = GETDATE()
                  WHERE WarehouseID = @WarehouseID AND CategoryID = @CategoryID;
              END
              ELSE
              BEGIN
                  INSERT INTO WasteManagement.WarehouseStock (WarehouseID, CategoryID, CurrentWeight, LastUpdated)
                  VALUES (@WarehouseID, @CategoryID, @Quantity, GETDATE());
              END",
            conn);

        cmd.Parameters.AddWithValue("@WarehouseID", dto.WarehouseID);
        cmd.Parameters.AddWithValue("@CategoryID", dto.CategoryID);
        cmd.Parameters.AddWithValue("@Quantity", dto.Quantity);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return true;
    }

    public async Task<List<Collection>> GetMyCollectionHistoryAsync(string operatorID)
    {
        var collections = new List<Collection>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetOperatorCollectionHistory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OperatorID", operatorID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            collections.Add(new Collection
            {
                CollectionID = reader.GetInt32(reader.GetOrdinal("CollectionID")),
                OperatorID = reader.GetString(reader.GetOrdinal("OperatorID")),
                ListingID = reader.GetInt32(reader.GetOrdinal("ListingID")),
                CollectedDate = reader.GetDateTime(reader.GetOrdinal("CollectedDate")),
                CollectedWeight = reader.GetDecimal(reader.GetOrdinal("CollectedWeight")),
                PhotoProof = reader.IsDBNull(reader.GetOrdinal("PhotoProof"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("PhotoProof")),
                IsVerified = reader.GetBoolean(reader.GetOrdinal("IsVerified")),
                WarehouseID = reader.GetInt32(reader.GetOrdinal("WarehouseID"))
            });
        }

        return collections;
    }

    public async Task<OperatorPerformanceView?> GetMyPerformanceAsync(string operatorID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "SELECT * FROM WasteManagement.vw_OperatorPerformance WHERE OperatorID = @OperatorID",
            conn);

        cmd.Parameters.AddWithValue("@OperatorID", operatorID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new OperatorPerformanceView
        {
            OperatorID = reader.GetString(reader.GetOrdinal("OperatorID")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
            RouteID = reader.IsDBNull(reader.GetOrdinal("RouteID"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("RouteID")),
            WarehouseID = reader.IsDBNull(reader.GetOrdinal("WarehouseID"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("WarehouseID")),
            TotalPickups = reader.GetInt32(reader.GetOrdinal("TotalPickups")),
            TotalCollectedWeight = reader.GetDecimal(reader.GetOrdinal("TotalCollectedWeight")),
            TotalCollectedAmount = reader.GetDecimal(reader.GetOrdinal("TotalCollectedAmount"))
        };
    }
}

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

    public async Task<CollectionResultDto> CollectWasteAsync(CollectionDto dto)
    {
        Console.WriteLine($"[SP] CollectWasteAsync - Using STORED PROCEDURE: sp_PerformCollection");
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

        var transactionIDParam = new SqlParameter("@TransactionID", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(transactionIDParam);

        var paymentAmountParam = new SqlParameter("@PaymentAmount", SqlDbType.Decimal)
        {
            Direction = ParameterDirection.Output,
            Precision = 10,
            Scale = 2
        };
        cmd.Parameters.Add(paymentAmountParam);

        var verificationCodeParam = new SqlParameter("@VerificationCode", SqlDbType.VarChar, 50)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(verificationCodeParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        var collectionId = (int)collectionIDParam.Value;
        var transactionId = transactionIDParam.Value == DBNull.Value ? (int?)null : (int)transactionIDParam.Value;
        var paymentAmount = paymentAmountParam.Value == DBNull.Value ? (decimal?)null : (decimal)paymentAmountParam.Value;
        var verificationCode = verificationCodeParam.Value == DBNull.Value ? null : (string)verificationCodeParam.Value;
        
        return new CollectionResultDto
        {
            Success = true,
            CollectionID = collectionId,
            TransactionID = transactionId,
            PaymentAmount = paymentAmount,
            VerificationCode = verificationCode,
            Message = "Collection recorded successfully"
        };
    }

    public async Task<bool> DepositWasteAsync(WarehouseDepositDto dto)
    {
        Console.WriteLine($"[SP] DepositWasteAsync - Using STORED PROCEDURE: sp_WarehouseDeposit");
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_WarehouseDeposit", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

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

    // ============================================
    // COMPLAINTS
    // ============================================
    public async Task<List<ActiveComplaintView>> GetMyComplaintsAsync(string operatorID)
    {
        Console.WriteLine($"[SP] GetMyComplaintsAsync - Using STORED PROCEDURE: sp_GetOperatorComplaints");
        var complaints = new List<ActiveComplaintView>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetOperatorComplaints", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OperatorID", operatorID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            complaints.Add(new ActiveComplaintView
            {
                ComplaintID = reader.GetInt32(reader.GetOrdinal("ComplaintID")),
                ComplaintType = reader.GetString(reader.GetOrdinal("ComplaintType")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CitizenID = reader.GetString(reader.GetOrdinal("CitizenID")),
                CitizenName = reader.GetString(reader.GetOrdinal("CitizenName")),
                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                OperatorID = reader.IsDBNull(reader.GetOrdinal("OperatorID"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OperatorID")),
                OperatorName = reader.IsDBNull(reader.GetOrdinal("OperatorName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OperatorName")),
                RouteName = reader.IsDBNull(reader.GetOrdinal("RouteName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("RouteName")),
                AreaName = reader.GetString(reader.GetOrdinal("AreaName")),
                DaysOpen = reader.GetInt32(reader.GetOrdinal("DaysOpen"))
            });
        }

        return complaints;
    }

    public async Task<bool> UpdateComplaintStatusAsync(int complaintID, string newStatus)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_UpdateComplaintStatus", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ComplaintID", complaintID);
        cmd.Parameters.AddWithValue("@NewStatus", newStatus);
        
        var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(rowsAffectedParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
        
        int rowsAffected = (int)rowsAffectedParam.Value;
        return rowsAffected > 0;
    }
}

using App.Core;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace App.BLL.SP;

public class SpGovernmentService : IGovernmentService
{
    private readonly string _connectionString;

    public SpGovernmentService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<WarehouseInventoryView>> GetWarehouseInventoryAsync(int? warehouseID = null)
    {
        var inventory = new List<WarehouseInventoryView>();

        using var conn = new SqlConnection(_connectionString);
        var sql = warehouseID.HasValue
            ? "SELECT * FROM WasteManagement.vw_WarehouseInventory WHERE WarehouseID = @WarehouseID"
            : "SELECT * FROM WasteManagement.vw_WarehouseInventory";

        using var cmd = new SqlCommand(sql, conn);
        if (warehouseID.HasValue)
        {
            cmd.Parameters.AddWithValue("@WarehouseID", warehouseID.Value);
        }

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            inventory.Add(new WarehouseInventoryView
            {
                WarehouseID = reader.GetInt32(reader.GetOrdinal("WarehouseID")),
                WarehouseName = reader.GetString(reader.GetOrdinal("WarehouseName")),
                AreaName = reader.GetString(reader.GetOrdinal("AreaName")),
                City = reader.GetString(reader.GetOrdinal("City")),
                Capacity = reader.GetFloat(reader.GetOrdinal("Capacity")),
                CurrentInventory = reader.GetFloat(reader.GetOrdinal("CurrentInventory")),
                CapacityUsedPercent = reader.GetDouble(reader.GetOrdinal("CapacityUsedPercent")),
                AvailableCapacity = reader.GetFloat(reader.GetOrdinal("AvailableCapacity")),
                CategoryCount = reader.GetInt32(reader.GetOrdinal("CategoryCount"))
            });
        }

        return inventory;
    }

    public async Task<List<Warehouse>> GetAllWarehousesAsync()
    {
        var warehouses = new List<Warehouse>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllWarehouses", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            warehouses.Add(new Warehouse
            {
                WarehouseID = reader.GetInt32(reader.GetOrdinal("WarehouseID")),
                WarehouseName = reader.GetString(reader.GetOrdinal("WarehouseName")),
                AreaID = reader.GetInt32(reader.GetOrdinal("AreaID")),
                Address = reader.GetString(reader.GetOrdinal("Address")),
                Capacity = reader.GetFloat(reader.GetOrdinal("Capacity")),
                CurrentInventory = reader.GetFloat(reader.GetOrdinal("CurrentInventory"))
            });
        }

        return warehouses;
    }

    public async Task<List<HighYieldAreaReport>> AnalyzeHighYieldAreasAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var reports = new List<HighYieldAreaReport>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_AnalyzeHighYieldAreas", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            reports.Add(new HighYieldAreaReport
            {
                AreaID = reader.GetInt32(0),
                AreaName = reader.GetString(1),
                City = reader.GetString(2),
                TotalListings = reader.GetInt32(3),
                TotalWeight = reader.GetDecimal(4),
                TotalRevenue = reader.GetDecimal(5),
                RevenueRank = reader.GetInt32(6)
            });
        }

        return reports;
    }

    public async Task<List<OperatorPerformanceReport>> GetOperatorPerformanceReportAsync()
    {
        var reports = new List<OperatorPerformanceReport>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_OperatorPerformance", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            reports.Add(new OperatorPerformanceReport
            {
                OperatorID = reader.GetString(0),
                FullName = reader.GetString(1),
                TotalCollections = reader.GetInt32(2),
                TotalWeightKg = reader.GetDecimal(3),
                Complaints = reader.GetInt32(4),
                Rating = reader.GetString(5)
            });
        }

        return reports;
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        var categories = new List<Category>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllCategories", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                BasePricePerKg = reader.GetDecimal(reader.GetOrdinal("BasePricePerKg")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description"))
            });
        }

        return categories;
    }

    public async Task<int> CreateCategoryAsync(CreateCategoryDto dto)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            @"INSERT INTO WasteManagement.Category (CategoryName, BasePricePerKg, Description)
              VALUES (@CategoryName, @BasePricePerKg, @Description);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            conn);

        cmd.Parameters.AddWithValue("@CategoryName", dto.CategoryName);
        cmd.Parameters.AddWithValue("@BasePricePerKg", dto.BasePricePerKg);
        cmd.Parameters.AddWithValue("@Description", dto.Description ?? (object)DBNull.Value);

        await conn.OpenAsync();
        var categoryID = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(categoryID);
    }

    public async Task<bool> UpdateCategoryPriceAsync(int categoryID, decimal newPrice)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_UpdateCategoryPrice", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CategoryID", categoryID);
        cmd.Parameters.AddWithValue("@NewPrice", newPrice);

        var successParam = new SqlParameter("@Success", SqlDbType.Bit)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(successParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return (bool)successParam.Value;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_DeleteCategory", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CategoryID", categoryID);

        var successParam = new SqlParameter("@Success", SqlDbType.Bit)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(successParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return (bool)successParam.Value;
    }

    public async Task<string> CreateOperatorAsync(CreateOperatorDto dto)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_CreateOperator", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Name", dto.FullName);
        cmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber);
        cmd.Parameters.AddWithValue("@CNIC", dto.CNIC);

        var resultParam = new SqlParameter("@ResultMessage", SqlDbType.VarChar, 255)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(resultParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        var result = resultParam.Value as string;
        if (result?.Contains("successfully") == true)
        {
            // Assign to route and warehouse if provided
            if (dto.RouteID.HasValue && dto.WarehouseID.HasValue)
            {
                await AssignOperatorToRouteAsync(dto.CNIC, dto.RouteID.Value, dto.WarehouseID.Value);
            }

            return dto.CNIC;
        }

        throw new InvalidOperationException(result ?? "Failed to create operator");
    }

    public async Task<bool> AssignOperatorToRouteAsync(string operatorID, int routeID, int warehouseID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_AssignOperatorToRoute", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OperatorID", operatorID);
        cmd.Parameters.AddWithValue("@WarehouseID", warehouseID);
        cmd.Parameters.AddWithValue("@RouteID", routeID);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return true;
    }

    public async Task<bool> DeactivateOperatorAsync(string operatorID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_DeactivateOperator", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@OperatorID", operatorID);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return true;
    }

    public async Task<List<Operator>> GetAllOperatorsAsync()
    {
        var operators = new List<Operator>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllOperators", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            operators.Add(new Operator
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
                Status = reader.GetString(reader.GetOrdinal("Status"))
            });
        }

        return operators;
    }

    public async Task<List<Complaint>> GetAllComplaintsAsync(string? status = null)
    {
        var complaints = new List<Complaint>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllComplaints", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            complaints.Add(new Complaint
            {
                ComplaintID = reader.GetInt32(reader.GetOrdinal("ComplaintID")),
                CitizenID = reader.GetString(reader.GetOrdinal("CitizenID")),
                OperatorID = reader.IsDBNull(reader.GetOrdinal("OperatorID"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OperatorID")),
                ComplaintType = reader.GetString(reader.GetOrdinal("ComplaintType")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
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

        var successParam = new SqlParameter("@Success", SqlDbType.Bit)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(successParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return (bool)successParam.Value;
    }

    public async Task<List<Route>> GetAllRoutesAsync()
    {
        var routes = new List<Route>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllRoutes", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            routes.Add(new Route
            {
                RouteID = reader.GetInt32(reader.GetOrdinal("RouteID")),
                RouteName = reader.GetString(reader.GetOrdinal("RouteName")),
                AreaID = reader.GetInt32(reader.GetOrdinal("AreaID"))
            });
        }

        return routes;
    }

    public async Task<List<Area>> GetAllAreasAsync()
    {
        var areas = new List<Area>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllAreas", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            areas.Add(new Area
            {
                AreaID = reader.GetInt32(reader.GetOrdinal("AreaID")),
                AreaName = reader.GetString(reader.GetOrdinal("AreaName")),
                City = reader.GetString(reader.GetOrdinal("City"))
            });
        }

        return areas;
    }
}

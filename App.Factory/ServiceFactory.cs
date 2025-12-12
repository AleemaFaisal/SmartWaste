using App.Core;
using App.BLL.EF;
using App.BLL.SP;
using Microsoft.EntityFrameworkCore;
using System;

namespace App.Factory;

/// <summary>
/// Factory for creating service implementations (EF or SP) at runtime
/// </summary>
public static class ServiceFactory
{
    /// <summary>
    /// Create Authentication Service
    /// </summary>
    public static IAuthenticationService CreateAuthService(bool useEf, string connectionString)
    {
        if (useEf)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new EfAuthenticationService(new AppDbContext(options));
        }
        else
        {
            return new SpAuthenticationService(connectionString);
        }
    }

    /// <summary>
    /// Create Citizen Service
    /// </summary>
    public static ICitizenService CreateCitizenService(bool useEf, string connectionString)
    {
        if (useEf)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new EfCitizenService(new AppDbContext(options));
        }
        else
        {
            return new SpCitizenService(connectionString);
        }
    }

    /// <summary>
    /// Create Operator Service (NOT IMPLEMENTED YET)
    /// </summary>
    public static IOperatorService CreateOperatorService(bool useEf, string connectionString)
    {
        if (useEf)
        {
            // Creates the EF version you provided
            var dbContext = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new EfOperatorService(new AppDbContext(dbContext));
        }
        
        // Fallback to SP version (ensure SpOperatorService exists in App.BLL.SP)
        return new SpOperatorService(connectionString);
    }

    /// <summary>
    /// Create Government Service (NOT IMPLEMENTED YET)
    /// </summary>
    public static IGovernmentService CreateGovernmentService(bool useEf, string connectionString)
    {
        throw new NotImplementedException("Government portal is not yet implemented. Only Citizen portal is available.");
    }
}

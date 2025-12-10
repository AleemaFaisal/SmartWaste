using App.Core;
using App.BLL.EF;
using App.BLL.SP;
using Microsoft.EntityFrameworkCore;

namespace App.Factory;

public static class ServiceFactory
{
    public static IService Create(bool useEf, string connectionString)
    {
        if (useEf)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new EfService(new AppDbContext(options));
        }
        else
        {
            return new SpService(connectionString);
        }
    }
}

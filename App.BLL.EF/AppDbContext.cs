using Microsoft.EntityFrameworkCore;
using App.Core;

namespace App.BLL.EF;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}

using Microsoft.EntityFrameworkCore;
using App.Core;

namespace App.BLL.EF;

public class EfService : IService
{
    private readonly AppDbContext _db;

    public EfService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _db.Users.ToListAsync();
    }
}

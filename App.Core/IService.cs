namespace App.Core;

public interface IService
{
    Task<List<User>> GetUsersAsync();
}

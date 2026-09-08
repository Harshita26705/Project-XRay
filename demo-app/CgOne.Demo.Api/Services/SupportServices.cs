using CgOne.Demo.Api.Models;

namespace CgOne.Demo.Api.Services;

public interface IInventoryService
{
    bool IsAvailable(string sku);
}

public class InventoryService : IInventoryService
{
    public bool IsAvailable(string sku)
    {
        return !string.IsNullOrWhiteSpace(sku);
    }
}

public interface IUserService
{
    User GetProfile(int userId);
}

public class UserService : IUserService
{
    public User GetProfile(int userId)
    {
        return new User { Id = userId, Email = "demo@example.com", DisplayName = "Demo User" };
    }
}

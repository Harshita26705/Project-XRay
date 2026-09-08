using CgOne.Demo.Api.Data;
using CgOne.Demo.Api.Models;

namespace CgOne.Demo.Api.Repositories;

public interface IOrderRepository
{
    Task<int> CreateAsync(Order order);
}

public class OrderRepository : IOrderRepository
{
    private readonly CgOneDbContext _context;

    public OrderRepository(CgOneDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order.Id;
    }
}

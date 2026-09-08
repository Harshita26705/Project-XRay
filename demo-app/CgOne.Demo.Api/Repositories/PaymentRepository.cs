using CgOne.Demo.Api.Data;
using CgOne.Demo.Api.Models;

namespace CgOne.Demo.Api.Repositories;

public interface IPaymentRepository
{
    Task<int> SaveAsync(PaymentTransaction transaction);

    Task<PaymentTransaction?> GetAsync(int id);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly CgOneDbContext _context;

    public PaymentRepository(CgOneDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveAsync(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction.Id;
    }

    public async Task<PaymentTransaction?> GetAsync(int id)
    {
        return await _context.PaymentTransactions.FindAsync(id);
    }
}

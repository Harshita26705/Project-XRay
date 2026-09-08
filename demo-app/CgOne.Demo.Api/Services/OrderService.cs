using CgOne.Demo.Api.Models;
using CgOne.Demo.Api.Repositories;

namespace CgOne.Demo.Api.Services;

public interface IOrderService
{
    Task<int> PlaceOrderAsync(OrderRequest request);
}

public class OrderService : IOrderService
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;

    public OrderService(IPaymentService paymentService, IOrderRepository orderRepository, IInventoryService inventoryService)
    {
        _paymentService = paymentService;
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
    }

    public async Task<int> PlaceOrderAsync(OrderRequest request)
    {
        if (!_inventoryService.IsAvailable(request.Sku))
        {
            return 0;
        }

        var order = new Order { UserId = request.UserId, Total = request.Amount, Status = "PENDING" };
        var orderId = await _orderRepository.CreateAsync(order);

        var payment = await _paymentService.ProcessAsync(new PaymentRequest
        {
            OrderId = orderId,
            Amount = request.Amount,
            CardNumber = "4111111111111111"
        });

        return payment.Accepted ? orderId : 0;
    }
}

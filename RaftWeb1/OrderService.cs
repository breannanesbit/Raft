using Newtonsoft.Json;
using RaftElection;

namespace RaftWeb1
{
    public class OrderService
    {
        private readonly GatewayService gateway;

        public OrderService(GatewayService gateway)
        {
            this.gateway = gateway;
        }
        public async Task CreateANewOrderAsync(Cart cart)
        {
            cart.OrderId = Guid.NewGuid();

            var productsAsStringArray = new List<string>();

            foreach (var p in cart.ShoppingItems)
            {
                productsAsStringArray.Add(p.ProductItem);
            }

            var serializedCart = JsonConvert.SerializeObject(cart);


            await gateway.NewValue($"order-status {cart.OrderId}", "pendiing");
            await gateway.NewValue($"order-info {cart.OrderId}", $"purchaser: {cart.Username}, list: {serializedCart}");

            var pending_orders = await gateway.GetInfor("pending-orders");

            var pendingOrders = JsonConvert.DeserializeObject<List<Guid>>(pending_orders.Item1);

            // Add the new order to the pending orders list
            pendingOrders.Add(cart.OrderId);

            // Serialize the updated list back to JSON
            var updatedPendingOrdersJson = JsonConvert.SerializeObject(pendingOrders);

            var newCompareandSwap = new SwapInfo
            {
                Key = "pending-orders",
                ExpectedIndex = pending_orders.Item2,
                NewValue = updatedPendingOrdersJson,
            };

            await gateway.CompareAndSwap(newCompareandSwap);

        }
    }
}

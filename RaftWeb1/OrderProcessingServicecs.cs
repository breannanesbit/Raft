using Newtonsoft.Json;
using RaftElection;

namespace RaftWeb1
{
    public class OrderProcessingService
    {
        public OrderProcessingService(GatewayService service)
        {
            this.service = service;
        }
        private Queue<string> _actionLog = new Queue<string>();
        private readonly GatewayService service;

        public async Task ProcessOrder(PendingOrders order)
        {
            try
            {
                _actionLog.Clear();
                // Perform Saga pattern steps here
                // For example:
                // 1. Reduce user balance
                await ChangeBalance(order.OrderInfo, true);
                LogAction("step 1: Reduced user balance");


                // 2. Reduce product stock
                foreach (var p in order.OrderInfo.ShoppingItems)
                {
                    await ChangeProductStock(p, true);

                }
                LogAction($"Step 2: Reduced product stock");

                // 3. Increase vendor balance
                var amount = CalcuateAmountTotal(order.OrderInfo);
                await ChangeVendorAmount(amount, true);
                LogAction($"Step 3");

                // 4. Update order status
                var updatedOrderStatus = new SwapInfo
                {
                    Key = $"order-status {order.OrderId}",
                    NewValue = $"processed-by {Guid.NewGuid()}",
                    ExpectedIndex = order.StatusIndex,

                };
                await service.CompareAndSwap(updatedOrderStatus);
                LogAction("Step 4");

                // 5. Remove order from pending orders
                await ChangePendingOrder(order);
                LogAction("Step 5");
                // You can call appropriate methods from GatewayService or other services for each step
                // Handle retries and undo op

            }
            catch (Exception ex)
            {
                await RollbackActions(order);
            }
        }

        public async Task ChangeVendorAmount(double amount, bool AddingToVendor)
        {
            var vendorBalance = await service.GetInfor("balance-of vendor");
            double.TryParse(vendorBalance.Item1, out double oldAmount);

            double newAmount = 0;
            if (AddingToVendor)
            {
                newAmount = oldAmount + amount;
            }
            else
            {
                newAmount = oldAmount - amount;
            }

            var updateVendorsBalance = new SwapInfo
            {
                Key = "balance-of vendor",
                ExpectedIndex = vendorBalance.Item2,
                NewValue = $"{newAmount}"
            };

            await service.CompareAndSwap(updateVendorsBalance);
        }

        public async Task ChangePendingOrder(PendingOrders order)
        {
            var pending_orders = await service.GetInfor("pending-orders");

            var pendingOrders = JsonConvert.DeserializeObject<List<Guid>>(pending_orders.Item1);
            var newOrder = pendingOrders.FindAll((op) => op != order.OrderId);
            var updatedPendingOrdersJson = JsonConvert.SerializeObject(newOrder);

            var updatePendingOrders = new SwapInfo
            {
                Key = "pending-orders",
                ExpectedIndex = pending_orders.Item2,
                NewValue = updatedPendingOrdersJson,
            };
            await service.CompareAndSwap(updatePendingOrders);
        }

        public async Task ChangeProductStock(Product changeProduct, bool decreaseStock)
        {
            var getProduct = await service.GetInfor($"stock-of-{changeProduct.ProductItem}");
            int.TryParse(getProduct.Item1, out int amount);

            var newStockNum = 0;
            if (decreaseStock)
            {
                newStockNum = amount - changeProduct.Quanity;
            }
            else
            {
                newStockNum = amount + changeProduct.Quanity;
            }

            var updateProductStock = new SwapInfo
            {
                Key = $"stock-of-{changeProduct.ProductItem}",
                ExpectedIndex = getProduct.Item2,
                NewValue = $"{newStockNum}"
            };
            await service.CompareAndSwap(updateProductStock);
        }

        public double CalcuateAmountTotal(Cart cart)
        {
            double totalAmount = 0;
            foreach (var c in cart.ShoppingItems)
            {
                totalAmount += (c.Cost * c.Quanity);
            }
            return totalAmount;
        }

        public async Task ChangeBalance(Cart cart, bool subtractFromAmount)
        {
            var totalAmount = CalcuateAmountTotal(cart);

            var balance = await service.GetInfor($"balance-of {cart.Username}");
            double.TryParse(balance.Item1, out double amount);

            double newTotal = 0;
            if (subtractFromAmount)
            {
                newTotal = totalAmount - amount;
            }
            else
            {
                newTotal = totalAmount + amount;
            }

            var changeUserBalance = new SwapInfo
            {
                Key = $"balance-of {cart.Username}",
                ExpectedIndex = balance.Item2,
                NewValue = $"{newTotal}",
            };

            await service.CompareAndSwap(changeUserBalance);
        }

        private void LogAction(string action)
        {
            _actionLog.Enqueue(action);
        }

        private async Task RollbackActions(PendingOrders order)
        {
            while (_actionLog.Count > 0)
            {
                var action = _actionLog.Dequeue();
                await UndoAction(action, order);
            }
        }

        private async Task UndoAction(string action, PendingOrders order)
        {
            switch (action)
            {
                case "Step 5":
                    // Undo action 5: Add the order back to pending orders
                    await AddOrderToPendingOrders(order.OrderId);
                    break;
                case "Step 4":
                    // Undo action 4: Rollback the order status update
                    var updatedOrderStatus = new SwapInfo
                    {
                        Key = $"order-status {order.OrderId}",
                        NewValue = $" rejected-by {Guid.NewGuid()}",
                        ExpectedIndex = order.StatusIndex,

                    };
                    await service.CompareAndSwap(updatedOrderStatus);
                    break;
                case "Step 3":
                    // Undo action 3: Decrease the vendor balance
                    var amount = CalcuateAmountTotal(order.OrderInfo);
                    await ChangeVendorAmount(amount, false);
                    break;
                case "Step 2":
                    // Undo action 2: Increase the product stock

                    foreach (var p in order.OrderInfo.ShoppingItems)
                    {
                        await ChangeProductStock(p, false);
                    }
                    break;
                case "Step 1":
                    // Undo action 1: Increase the user balance
                    await ChangeBalance(order.OrderInfo, false);
                    break;
                default:
                    // Handle unknown action
                    break;
            }
        }

        public async Task AddOrderToPendingOrders(Guid order)
        {
            var pending_orders = await service.GetInfor("pending-orders");

            var pendingOrders = JsonConvert.DeserializeObject<List<Guid>>(pending_orders.Item1);

            // Add the new order to the pending orders list
            pendingOrders.Add(order);

            // Serialize the updated list back to JSON
            var updatedPendingOrdersJson = JsonConvert.SerializeObject(pendingOrders);

            var newCompareandSwap = new SwapInfo
            {
                Key = "pending-orders",
                ExpectedIndex = pending_orders.Item2,
                NewValue = updatedPendingOrdersJson,
            };

            await service.CompareAndSwap(newCompareandSwap);
        }

    }

}

namespace ScheduleIMegaExpansion.Core;

/// <summary>
/// Persistent order lifecycle for NPC-generated orders and phone-shop purchases.
/// The host is the only peer allowed to commit transitions for shared orders.
/// </summary>
public enum OrderStatus
{
    Offered,
    Accepted,
    Preparing,
    Ready,
    Delivered,
    Collected,
    Completed,
    Expired,
    Cancelled,
    Failed,
    Disputed
}

public static class OrderState
{
    public static bool CanTransition(OrderStatus current, OrderStatus next) =>
        current switch
        {
            OrderStatus.Offered =>
                next is OrderStatus.Accepted or OrderStatus.Expired or OrderStatus.Cancelled,

            OrderStatus.Accepted =>
                next is OrderStatus.Preparing or OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.Failed,

            OrderStatus.Preparing =>
                next is OrderStatus.Ready or OrderStatus.Expired or OrderStatus.Failed,

            OrderStatus.Ready =>
                next is OrderStatus.Delivered or OrderStatus.Collected or OrderStatus.Expired or OrderStatus.Failed,

            OrderStatus.Delivered or OrderStatus.Collected =>
                next is OrderStatus.Completed or OrderStatus.Disputed,

            OrderStatus.Disputed =>
                next is OrderStatus.Completed or OrderStatus.Failed,

            OrderStatus.Completed or OrderStatus.Expired or OrderStatus.Cancelled or OrderStatus.Failed =>
                false,

            _ => false
        };
}

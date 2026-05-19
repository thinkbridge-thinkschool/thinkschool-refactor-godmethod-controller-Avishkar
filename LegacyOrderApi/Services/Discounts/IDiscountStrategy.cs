namespace LegacyOrderApi.Services.Discounts
{
    public interface IDiscountStrategy
    {
        bool IsApplicable(decimal totalAmount);
        decimal ApplyDiscount(decimal totalAmount);
    }

    public class HighValueDiscountStrategy : IDiscountStrategy
    {
        public bool IsApplicable(decimal totalAmount) => totalAmount > 1000;
        
        public decimal ApplyDiscount(decimal totalAmount) => totalAmount * 0.9m; // 10% discount
    }

    public class MidValueDiscountStrategy : IDiscountStrategy
    {
        public bool IsApplicable(decimal totalAmount) => totalAmount > 500 && totalAmount <= 1000;
        
        public decimal ApplyDiscount(decimal totalAmount) => totalAmount * 0.95m; // 5% discount
    }
}

using System;

namespace Minimarket
{
    internal abstract class EntityBase
    {
        public int Id { get; set; }
    }

    internal sealed class Product : EntityBase
    {
        public int ProductId
        {
            get { return Id; }
            set { Id = value; }
        }

        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountPercent { get; set; }
        public int Stock { get; set; }

        public decimal FinalPrice
        {
            get { return Money.Round(Price - (Price * DiscountPercent / 100m)); }
        }
    }

    internal sealed class CartItem
    {
        public Product Product { get; set; }
        public int Qty { get; set; }

        public decimal Subtotal
        {
            get { return Money.Round(Product.FinalPrice * Qty); }
        }
    }

    internal sealed class SalesTransaction : EntityBase
    {
        public int TransactionId
        {
            get { return Id; }
            set { Id = value; }
        }

        public string InvoiceNo { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
    }

    internal sealed class SaleItem
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Qty { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    internal enum ReportKind
    {
        Sales,
        Transaction,
        User,
        Inventory
    }

    internal static class Money
    {
        public static decimal Round(decimal value)
        {
            return Math.Round(value, 0, MidpointRounding.AwayFromZero);
        }
    }
}

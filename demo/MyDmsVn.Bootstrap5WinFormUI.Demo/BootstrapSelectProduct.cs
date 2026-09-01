namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class BootstrapSelectProduct
{
    public BootstrapSelectProduct(int id, string name, string unit, decimal unitPrice, int stockQuantity)
    {
        Id = id;
        Name = name;
        Unit = unit;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    public int Id { get; }

    public string Name { get; }

    public string Unit { get; }

    public decimal UnitPrice { get; }

    public int StockQuantity { get; }
}

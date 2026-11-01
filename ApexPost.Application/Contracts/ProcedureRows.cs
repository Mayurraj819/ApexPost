namespace ApexPost.Application.Contracts;
public sealed class DailySalesProcedureRow
{
    public Guid BranchId { get; set; }
    public DateTime Day { get; set; }
    public decimal Retail { get; set; }
    public decimal Services { get; set; }
    public string Currency { get; set; } = "";
}

public sealed class LowStockProcedureRow
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public string Product { get; set; } = "";
    public int Quantity { get; set; }
}

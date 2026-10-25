using ApexPost.Domain.Enums;

namespace ApexPost.Web.Models;

public sealed record ShippingLabelViewModel(
    Guid TransactionId,
    Guid BranchId,
    DateTimeOffset TransactionDate,
    string ServiceCode,
    ShippingServiceLevel? ServiceLevel,
    decimal Amount,
    string Currency,
    int LineNumber);

using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Contracts;

public sealed record TransactionReport(
    Guid BranchId,
    DateTimeOffset From,
    DateTimeOffset To,
    int TransactionCount,
    int PaidCount,
    int VoidedCount,
    int RefundedCount,
    Money PaidTotal);

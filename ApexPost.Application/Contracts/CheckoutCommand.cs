namespace ApexPost.Application.Contracts;
public sealed record CheckoutLine(Guid ProductId, int Quantity);
public sealed record CheckoutCommand(Guid BranchId, Guid CounterId, Guid StaffId, DateTimeOffset OccurredAt, IReadOnlyList<CheckoutLine> Lines);

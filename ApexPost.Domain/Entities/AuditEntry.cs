namespace ApexPost.Domain.Entities;

public sealed record AuditEntry(
    DateTimeOffset OccurredAt,
    string Controller,
    string Action,
    string? StaffId,
    string? Role,
    string? BranchId,
    string CorrelationId,
    int StatusCode,
    double DurationMs,
    string RouteValues,
    string Result,
    string? Details = null);

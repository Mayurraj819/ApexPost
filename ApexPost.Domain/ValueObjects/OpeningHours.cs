namespace ApexPost.Domain.ValueObjects;

public sealed record OpeningHours
{
    public OpeningHours(TimeOnly opensAt, TimeOnly closesAt)
    {
        if (opensAt >= closesAt)
        {
            throw new ArgumentException("Opening time must be before closing time.", nameof(opensAt));
        }

        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    public TimeOnly OpensAt { get; }
    public TimeOnly ClosesAt { get; }

    public bool Contains(TimeOnly time) => time >= OpensAt && time < ClosesAt;
}

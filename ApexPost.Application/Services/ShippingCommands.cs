using ApexPost.Application.Abstractions.Persistence;
using ApexPost.Application.Abstractions.Services;
using ApexPost.Application.Contracts;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;

namespace ApexPost.Application.Services;
public sealed class ShippingCommands(ITransactionRepository transactions, IShipmentStore shipments, IUnitOfWork unitOfWork) : IShippingCommands
{
    public async Task AddAsync(AddShipmentCommand command, CancellationToken ct)
    {
        if (command.Weight.Grams <= 0 || !command.Weight.IsWithinParcelLimit || !Enum.IsDefined(command.ServiceLevel))
            throw new ArgumentException("Select a valid service and weight up to 30000 grams.");
        if (string.IsNullOrWhiteSpace(command.Recipient) || string.IsNullOrWhiteSpace(command.Street) || string.IsNullOrWhiteSpace(command.City))
            throw new ArgumentException("Recipient and address are required.");
        var transaction = await transactions.GetByIdAsync(command.TransactionId, ct) ?? throw new KeyNotFoundException("Transaction not found.");
        if (transaction.BranchId != command.BranchId)
            throw new ArgumentException("Transaction does not belong to this branch.");
        var rate = await shipments.GetRateAsync(command.ZoneId, command.ServiceLevel, ct) ?? throw new ArgumentException("Shipping rate not found.");
        var charge = ShippingCharge.Calculate(command.Weight, rate.BaseRate, rate.PerKilogram);
        try
        {
            transaction.AddService("SHIP-" + command.ServiceLevel, TransactionLineType.PostalService, charge, command.ServiceLevel);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApexPost.Application.Exceptions.DomainRuleException(ex.Message, ex);
        }

        await shipments.AddAsync(command, charge, ct);
        await transactions.SaveAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

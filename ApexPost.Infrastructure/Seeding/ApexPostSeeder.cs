using ApexPost.Domain.Entities;
using ApexPost.Domain.Enums;
using ApexPost.Domain.ValueObjects;
using ApexPost.Infrastructure.Persistence;
using ApexPost.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure.Seeding;
public sealed class ApexPostSeeder(ApexPostDbContext db)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Set<Branch>().AnyAsync(ct))
            return;
        // Explicit command, never run implicitly on every web request. Fictional fixtures only.
        var regions = Enumerable.Range(1, 3).Select(i => new RegionRecord { Id = Guid.NewGuid(), Code = $"R{i}", Name = $"Region {i}" }).ToArray();
        db.AddRange(regions);
        db.AddRange(Enum.GetValues<StaffRole>().Select(r => new RoleRecord { Id = r, Name = r.ToString() }));
        db.AddRange(Enum.GetValues<ProductCategory>().Select(c => new ProductCategoryRecord { Id = c, Name = c.ToString() }));
        var branchIds = new Guid[]
        {
            Guid.Parse("4d1e86ae-8b8b-4f1d-9d6f-4d4d7817d0d6"),
            Guid.Parse("fda2be0e-5d9a-44fb-bf10-af339c8f1b39"),
            Guid.Parse("7b3f41b1-1776-4389-8d3f-4d30241b9d48"),
            Guid.Parse("0d89ca33-fd1d-49d0-9d86-5007bc3748e4"),
            Guid.Parse("bdd5981e-69d8-4b8c-9b63-5e5c7d888a15")
        };
        var products = Enumerable.Range(1, 30).Select(i => new Product(Guid.NewGuid(), new Sku($"SKU-{i:D3}"), $"Demo product {i}", new Money(10 + i), ProductCategory.OfficeSupplies)).ToArray();
        db.AddRange(products);
        var zones = Enumerable.Range(1, 10).Select(i => new ShippingZoneRecord { Id = Guid.NewGuid(), Code = $"ZONE-{i}", Name = $"Zone {i}" }).ToArray();
        db.AddRange(zones);
        foreach (var z in zones)
            foreach (var level in Enum.GetValues<ShippingServiceLevel>())
            {
                var rates = level switch
                {
                    ShippingServiceLevel.Standard => (50m, 20m),
                    ShippingServiceLevel.Express => (85m, 35m),
                    _ => (70m, 25m)};
                db.Add(new ShippingRateRecord { Id = Guid.NewGuid(), ZoneId = z.Id, ServiceLevel = level, BaseRate = new Money(rates.Item1), PerKilogram = rates.Item2 });
            }

        for (var b = 0; b < 5; b++)
        {
            var branch = new Branch(branchIds[b], new BranchCode($"BR-{b + 1:D2}"), $"Demo branch {b + 1}", $"{b + 1} Demo Avenue");
            db.Add(branch);
            db.Entry(branch).Property("RegionId").CurrentValue = regions[b % 3].Id;
            var counter = new Counter(Guid.NewGuid(), branch.Id, 1, "Counter 1");
            db.Add(counter);
            var actors = new List<Staff>();
            if (b == 0)
            {
                var person2 = new Staff(Guid.Parse("c64d0a5a-c63b-49e3-8e98-8fe0d0a048d1"), branch.Id, "EMP-001", "Demo EMP-001", new EmailAddress("alya.reyes@example.com"), (StaffRole)0);
                actors.Add(person2);
                db.Add(person2);
                var person4 = new Staff(Guid.Parse("ad6b7860-d246-46e8-b233-1f2d13fd5d94"), branch.Id, "EMP-002", "Demo EMP-002", new EmailAddress("ben.santos@example.com"), (StaffRole)0);
                actors.Add(person4);
                db.Add(person4);
                var person6 = new Staff(Guid.Parse("2a7d5f34-9fd3-4ae0-8008-6a219a1eae29"), branch.Id, "EMP-003", "Demo EMP-003", new EmailAddress("chen.delacruz@example.com"), (StaffRole)1);
                actors.Add(person6);
                db.Add(person6);
                var person8 = new Staff(Guid.Parse("3d7d9e0d-0b8a-4d5d-ae8d-937d4c5b9a1a"), branch.Id, "EMP-004", "Demo EMP-004", new EmailAddress("diana.flores@example.com"), (StaffRole)2);
                actors.Add(person8);
                db.Add(person8);
                var person10 = new Staff(Guid.Parse("d38887e4-d2fd-4ca5-a8f7-64d5d1ad1c7f"), branch.Id, "EMP-005", "Demo EMP-005", new EmailAddress("enzo.garcia@example.com"), (StaffRole)3);
                actors.Add(person10);
                db.Add(person10);
            }

            if (b == 1)
            {
                var person14 = new Staff(Guid.Parse("b4ef7c31-9d04-452b-b3ef-6b4d47939581"), branch.Id, "EMP-006", "Demo EMP-006", new EmailAddress("farah.gomez@example.com"), (StaffRole)0);
                actors.Add(person14);
                db.Add(person14);
                var person16 = new Staff(Guid.Parse("6d7ab791-86ca-4073-b4da-296febd0fe30"), branch.Id, "EMP-007", "Demo EMP-007", new EmailAddress("gabriel.lim@example.com"), (StaffRole)0);
                actors.Add(person16);
                db.Add(person16);
                var person18 = new Staff(Guid.Parse("bb31a9f2-4b1e-4875-8b0e-485f1fb7e30b"), branch.Id, "EMP-008", "Demo EMP-008", new EmailAddress("hana.tan@example.com"), (StaffRole)1);
                actors.Add(person18);
                db.Add(person18);
                var person20 = new Staff(Guid.Parse("9d0a3db7-41dd-4e39-9b7f-5285ff17f8de"), branch.Id, "EMP-009", "Demo EMP-009", new EmailAddress("isaac.navarro@example.com"), (StaffRole)2);
                actors.Add(person20);
                db.Add(person20);
                var person22 = new Staff(Guid.Parse("65b37c63-2d6a-4dfe-b07d-f05a93013f0d"), branch.Id, "EMP-010", "Demo EMP-010", new EmailAddress("jasmine.bautista@example.com"), (StaffRole)3);
                actors.Add(person22);
                db.Add(person22);
            }

            if (b == 2)
            {
                var person26 = new Staff(Guid.Parse("5f0420af-ad5d-49fb-9c7a-1a4d66b8fcd2"), branch.Id, "EMP-011", "Demo EMP-011", new EmailAddress("kai.ortega@example.com"), (StaffRole)0);
                actors.Add(person26);
                db.Add(person26);
                var person28 = new Staff(Guid.Parse("4e3f62e8-9cb1-4f93-8d2e-a01df06d3e05"), branch.Id, "EMP-012", "Demo EMP-012", new EmailAddress("liana.mercado@example.com"), (StaffRole)0);
                actors.Add(person28);
                db.Add(person28);
                var person30 = new Staff(Guid.Parse("06837c3d-4d31-4d91-9fc1-c350e6d7d179"), branch.Id, "EMP-013", "Demo EMP-013", new EmailAddress("marco.santos@example.com"), (StaffRole)1);
                actors.Add(person30);
                db.Add(person30);
                var person32 = new Staff(Guid.Parse("d779eb76-9c29-40e6-bc2d-d45e89ece747"), branch.Id, "EMP-014", "Demo EMP-014", new EmailAddress("nadia.cruz@example.com"), (StaffRole)2);
                actors.Add(person32);
                db.Add(person32);
            }

            if (b == 3)
            {
                var person36 = new Staff(Guid.Parse("8fe86a5d-3f75-4d94-9ddc-870a4a49d7dd"), branch.Id, "EMP-015", "Demo EMP-015", new EmailAddress("oscar.bennett@example.com"), (StaffRole)0);
                actors.Add(person36);
                db.Add(person36);
                var person38 = new Staff(Guid.Parse("c3c82d44-e5d8-4ca0-9d0b-31936f97af9e"), branch.Id, "EMP-016", "Demo EMP-016", new EmailAddress("paula.montoya@example.com"), (StaffRole)0);
                actors.Add(person38);
                db.Add(person38);
                var person40 = new Staff(Guid.Parse("f08370ce-2b17-46d0-a1fd-5b8b4f23f4f5"), branch.Id, "EMP-017", "Demo EMP-017", new EmailAddress("quinn.romero@example.com"), (StaffRole)1);
                actors.Add(person40);
                db.Add(person40);
                var person42 = new Staff(Guid.Parse("7c0dbeae-aaa4-401e-ab30-aa160e4d7e7d"), branch.Id, "EMP-018", "Demo EMP-018", new EmailAddress("rina.ishida@example.com"), (StaffRole)2);
                actors.Add(person42);
                db.Add(person42);
            }

            if (b == 4)
            {
                var person46 = new Staff(Guid.Parse("c1de8d44-28aa-4d37-a6d0-530dc7741b7f"), branch.Id, "EMP-019", "Demo EMP-019", new EmailAddress("samuel.yu@example.com"), (StaffRole)0);
                actors.Add(person46);
                db.Add(person46);
                var person48 = new Staff(Guid.Parse("d2b1718f-f4cc-473d-9bcb-8f0253d8fa41"), branch.Id, "EMP-020", "Demo EMP-020", new EmailAddress("tessa.walker@example.com"), (StaffRole)1);
                actors.Add(person48);
                db.Add(person48);
            }

            foreach (var product in products)
                db.Add(new BranchStock(Guid.NewGuid(), branch.Id, product.Id, new Quantity(100)));
            for (var day = 0; day < 40; day++)
            {
                var occurred = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-day).AddHours(10), TimeSpan.Zero);
                var sale = Transaction.Start(branch, counter, actors[0], occurred);
                sale.AddProduct(products[day % products.Length], new Quantity(1 + day % 3));
                if (day % 2 == 0)
                {
                    sale.AddService("POST-STD", TransactionLineType.PostalService, new Money(70), ShippingServiceLevel.Standard);
                    db.Add(new ShipmentRecord { Id = Guid.NewGuid(), TransactionId = sale.Id, ZoneId = zones[day % 10].Id, ServiceLevel = ShippingServiceLevel.Standard, Charge = new Money(70), Weight = new Weight(1000), RecipientName = "Demo recipient", Address = new AddressRecord { Street = "1 Demo Street", City = "Demo City", PostalCode = "1000" } });
                }

                sale.Pay(actors[0], branch);
                db.Add(sale);
                db.Add(new Payment(sale.Id, sale.Id, sale.Total, occurred));
            }
        }

        // Stage once; preserve Added state until the transaction is known to have committed.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(
            token => db.SaveChangesAsync(acceptAllChangesOnSuccess: false, token),
            token => db.Set<Branch>().AsNoTracking().AnyAsync(b => b.Id == branchIds[0], token),
            ct);
        db.ChangeTracker.AcceptAllChanges();
    }
}

using System.Collections.Concurrent;
using ApexPost.Courier.Offline.Models;
using ApexPost.Courier.Offline.Persistence;
using Couchbase.Lite;

namespace ApexPost.IntegrationTests.Couchbase;

[Trait("Category", "Integration")]
[Trait("Storage", "Couchbase")]
public sealed class CouchbaseRepositoryTests
{
    private static readonly DateTimeOffset RecordedAt =
        new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidScan_PersistsEveryFieldAsPendingWithUtcTimestamp()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var scan = Scan(1) with
        {
            ScannedAtUtc = new DateTimeOffset(2026, 1, 1, 15, 30, 0, TimeSpan.FromHours(5.5)),
            SyncStatus = SyncStatus.Synced
        };

        // Act
        var created = store.Repository.Create(scan);
        var loaded = store.Repository.GetById(scan.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(scan with { ScannedAtUtc = RecordedAt, SyncStatus = SyncStatus.Pending }, loaded);
        Assert.Equal(created, loaded);
        Assert.Equal(TimeSpan.Zero, loaded.ScannedAtUtc.Offset);
        using var document = store.Database.Scans.GetDocument(scan.Id);
        Assert.NotNull(document);
        Assert.Equal("Pickup", document.GetString("scanType"));
        Assert.Equal("Pending", document.GetString("syncStatus"));
    }

    [Fact]
    public void Create_NullNotes_PersistsEmptyString()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var scan = Scan(1) with { Notes = null! };

        // Act
        var created = store.Repository.Create(scan);
        var loaded = store.Repository.GetById(scan.Id);

        // Assert
        Assert.Equal(string.Empty, created.Notes);
        Assert.NotNull(loaded);
        Assert.Equal(string.Empty, loaded.Notes);
    }

    [Fact]
    public void Create_ExistingDocumentId_DoesNotOverwriteStoredScan()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var original = store.Repository.Create(Scan(1));
        var different = original with { Location = "Different fictional location" };

        // Act
        Action duplicateCreate = () => store.Repository.Create(different);

        // Assert
        Assert.Throws<InvalidOperationException>(duplicateCreate);
        Assert.Equal(original, store.Repository.GetById(original.Id));
        Assert.Single(store.Repository.ListAll());
    }

    [Fact]
    public void Update_ExistingScan_PersistsNewStateAndPreservesOtherFields()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var original = store.Repository.Create(Scan(1));
        var changed = original with { SyncStatus = SyncStatus.Synced, Notes = "Server acknowledged" };

        // Act
        store.Repository.Update(changed);
        var loaded = store.Repository.GetById(original.Id);

        // Assert
        Assert.Equal(changed, loaded);
        Assert.Single(store.Repository.ListAll());
        Assert.Empty(store.Repository.ListPending());
    }

    [Fact]
    public void Delete_ExistingAndUnknownIds_ReturnsCorrectResultsAndRemovesDocument()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var scan = store.Repository.Create(Scan(1));

        // Act
        var firstDelete = store.Repository.Delete(scan.Id);
        var loaded = store.Repository.GetById(scan.Id);
        var secondDelete = store.Repository.Delete(scan.Id);

        // Assert
        Assert.True(firstDelete);
        Assert.Null(loaded);
        Assert.False(secondDelete);
        Assert.Empty(store.Repository.ListAll());
    }

    [Fact]
    public void Reopen_PreviouslySavedScan_RetainsContentsAndSyncState()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var saved = store.Repository.Create(Scan(1));
        var acknowledged = saved with { SyncStatus = SyncStatus.Synced };
        store.Repository.Update(acknowledged);

        // Act
        store.Reopen();
        var loaded = store.Repository.GetById(saved.Id);

        // Assert
        Assert.Equal(acknowledged, loaded);
        Assert.Empty(store.Repository.ListPending());
        Assert.Single(store.Repository.ListAll());
    }

    [Fact]
    public void FindByTrackingNumber_MixedData_ReturnsOnlyMatchingScansInStableUtcOrder()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var first = Scan(1);
        var tie = Scan(2);
        var later = Scan(3) with { ScannedAtUtc = RecordedAt.AddMinutes(1) };
        var unrelated = Scan(4) with { TrackingNumber = "APX-10000002" };
        foreach (var scan in new[] { later, tie, unrelated, first })
            store.Repository.Create(scan);

        // Act
        var found = store.Repository.FindByTrackingNumber(first.TrackingNumber);
        var injectionAttempt = store.Repository.FindByTrackingNumber("' OR 1=1 --");

        // Assert
        Assert.Equal(new[] { first.Id, tie.Id, later.Id }, found.Select(s => s.Id));
        Assert.Empty(injectionAttempt);
    }

    [Fact]
    public void ListPending_MixedSyncStates_ExcludesSyncedAndFailedDocuments()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var pending = store.Repository.Create(Scan(1));
        var synced = store.Repository.Create(Scan(2));
        var failed = store.Repository.Create(Scan(3));
        store.Repository.Update(synced with { SyncStatus = SyncStatus.Synced });
        store.Repository.Update(failed with { SyncStatus = SyncStatus.Failed });

        // Act
        var results = store.Repository.ListPending();

        // Assert
        Assert.Equal(pending, Assert.Single(results));
        Assert.Equal(3, store.Repository.ListAll().Count);
    }

    [Theory]
    [InlineData("COURIER-01", 2L, 1L)]
    [InlineData("UNKNOWN-COURIER", 0L, 0L)]
    public void CountByScanType_MixedCouriers_ReturnsOwnerCountsAndZeroForMissingTypes(
        string courierId, long pickupCount, long transitCount)
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        foreach (var scan in new[]
        {
            Scan(1), Scan(2),
            Scan(3) with { ScanType = ScanType.InTransit },
            Scan(4) with { CourierId = "OTHER-COURIER", ScanType = ScanType.Delivered }
        })
            store.Repository.Create(scan);

        // Act
        var counts = store.Repository.CountByScanType(courierId);

        // Assert
        Assert.Equal(4, counts.Count);
        Assert.Equal(pickupCount, counts[ScanType.Pickup]);
        Assert.Equal(transitCount, counts[ScanType.InTransit]);
        Assert.Equal(0L, counts[ScanType.Delivered]);
        Assert.Equal(0L, counts[ScanType.Failed]);
    }

    [Fact]
    public void Open_NewDatabase_CreatesRequiredValueIndexes()
    {
        // Arrange
        using var store = new TemporaryCourierStore();

        // Act
        var indexes = store.Database.Scans.GetIndexes();

        // Assert
        Assert.Contains("idx_trackingNumber", indexes);
        Assert.Contains("idx_syncStatus", indexes);
    }

    [Theory]
    [InlineData("scanType")]
    [InlineData("syncStatus")]
    public void GetById_InvalidStoredEnum_RejectsCorruptDocument(string field)
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var scan = Scan(1);
        using var document = new MutableDocument(scan.Id);
        ShipmentScanMapper.Write(document, scan);
        document.SetString(field, "UnknownValue");
        Assert.True(store.Database.Scans.Save(document, ConcurrencyControl.FailOnConflict));

        // Act
        Action load = () => store.Repository.GetById(scan.Id);

        // Assert
        Assert.Throws<InvalidDataException>(load);
    }

    [Fact]
    public async Task ChangeListener_AfterCreate_ReportsDocumentIdAndPendingCount()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var scan = Scan(1);
        var notification = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var messages = new ConcurrentQueue<string>();
        store.Database.StartChangeListener(message =>
        {
            messages.Enqueue(message);
            if (message.Contains(scan.Id, StringComparison.Ordinal))
                notification.TrySetResult(message);
        });

        // Act
        store.Repository.Create(scan);
        var change = await notification.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        Assert.Contains(scan.Id, change);
        Assert.Contains("Pending scans: 1", change);
        Assert.Contains(messages, message => message.Contains("Started | Pending scans: 0", StringComparison.Ordinal));
        Assert.Single(store.Repository.ListPending());
    }

    [Fact]
    public async Task Dispose_ThenReopen_AllowsNewListenerAndContinuedDatabaseUse()
    {
        // Arrange
        using var store = new TemporaryCourierStore();
        var previous = store.Database;
        var oldMessages = new ConcurrentQueue<string>();
        previous.StartChangeListener(oldMessages.Enqueue);
        store.Repository.Create(Scan(1));

        // Act
        previous.Dispose();
        previous.Dispose();
        store.Reopen();
        var scan = Scan(2);
        var notification = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        store.Database.StartChangeListener(message =>
        {
            if (message.Contains(scan.Id, StringComparison.Ordinal))
                notification.TrySetResult(message);
        });
        store.Repository.Create(scan);
        var change = await notification.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        Assert.Throws<ObjectDisposedException>(() => previous.StartChangeListener(_ => { }));
        Assert.Contains("Pending scans: 2", change);
        Assert.Equal(2, store.Repository.ListPending().Count);
        Assert.DoesNotContain(oldMessages, message => message.Contains(scan.Id, StringComparison.Ordinal));
    }

    private static ShipmentScan Scan(int id) => new()
    {
        Id = $"scan::{id:D8}-0000-4000-8000-000000000000",
        TrackingNumber = "APX-10000001",
        ScanType = ScanType.Pickup,
        CourierId = "COURIER-01",
        Location = "Fictional Pune Depot",
        ScannedAtUtc = RecordedAt,
        Notes = "Fictional integration-test scan",
        SyncStatus = SyncStatus.Pending
    };

    private sealed class TemporaryCourierStore : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(), "ApexPost_Week5_Couchbase_" + Guid.NewGuid().ToString("N"));
        private bool _disposed;

        public CourierDatabase Database { get; private set; }
        public CouchbaseScanRepository Repository { get; private set; }

        public TemporaryCourierStore()
        {
            try
            {
                Database = new CourierDatabase(_directory);
                Repository = new CouchbaseScanRepository(Database);
            }
            catch
            {
                if (Directory.Exists(_directory))
                    Directory.Delete(_directory, recursive: true);
                throw;
            }
        }

        public void Reopen()
        {
            Database.Dispose();
            Database = new CourierDatabase(_directory);
            Repository = new CouchbaseScanRepository(Database);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try
            {
                Database.Dispose();
            }
            finally
            {
                if (Directory.Exists(_directory))
                    Directory.Delete(_directory, recursive: true);
            }
        }
    }
}

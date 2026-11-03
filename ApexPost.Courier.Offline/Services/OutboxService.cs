using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;

namespace ApexPost.Courier.Offline.Services;

public sealed record SyncSummary(int Synced, int Failed);

public sealed class OutboxService(
    IScanRepository repository,
    IShipmentUploader uploader)
{
    public async Task<SyncSummary> SyncPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var pending = repository.ListPending();
        var synced = 0;
        var failed = 0;

        foreach (var scan in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool uploaded;

            try
            {
                uploaded = await uploader.UploadAsync(
                    scan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancellation must not mark a scan as failed.
                throw;
            }
            catch (Exception)
            {
                uploaded = false;
            }

            // Persist the result locally after the upload attempt.
            repository.Update(scan with
            {
                SyncStatus = uploaded
                    ? SyncStatus.Synced
                    : SyncStatus.Failed
            });

            if (uploaded)
                synced++;
            else
                failed++;
        }

        return new SyncSummary(synced, failed);
    }
}
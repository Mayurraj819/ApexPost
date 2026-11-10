using ApexPost.Courier.Offline.Abstractions;
using ApexPost.Courier.Offline.Models;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ApexPost.Courier.Integration;
public sealed class CourierSync(IScanRepository repository, ApexApiClient client, CourierState state) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task SyncAsync(string courierId, CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct))
            return;
        try
        {
            var failed = 0;
            string? message = null;
            var groups = repository.ListPending().Where(s => s.CourierId == courierId).GroupBy(s => s.TrackingNumber);
            foreach (var group in groups)
                foreach (var batch in group.Chunk(20))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var result = await client.UploadBatchAsync(group.Key, batch, ct);
                        message ??= result.Rejections?.FirstOrDefault()?.Reason;
                        var success = result.AcceptedScanIds!.Concat(result.DuplicateScanIds!).ToHashSet();
                        var rejected = result.Rejections!.Select(r => r.Id).ToHashSet();
                        foreach (var scan in batch)
                        {
                            if (repository.GetById(scan.Id) != scan)
                                continue;
                            if (success.Contains(scan.Id))
                                repository.Update(scan with { SyncStatus = SyncStatus.Synced });
                            else if (rejected.Contains(scan.Id))
                            {
                                repository.Update(scan with { SyncStatus = SyncStatus.Failed });
                                failed++;
                            }
                        }
                    }
                    catch (CourierApiException ex)when (ex.Status == 401)
                    {
                        state.Report("Your session expired. Sign in again.");
                        return;
                    }
                    catch (CourierApiException ex)when (ex.Status is 400 or 403 or 404 or 415)
                    {
                        foreach (var scan in batch)
                            if (repository.GetById(scan.Id) == scan)
                                repository.Update(scan with { SyncStatus = SyncStatus.Failed });
                        failed += batch.Length;
                        message = ex.Message;
                    }
                    catch (CourierApiException)
                    {
                        state.Report("Server unavailable – working offline", true);
                        return;
                    }
                }

            state.Completed(failed);
            if (message is not null)
                state.Report(message);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            state.Report("Server unavailable – working offline", true);
        }
        catch (Exception ex)when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException or CourierProtocolException)
        {
            state.Report("Server unavailable – working offline", true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}

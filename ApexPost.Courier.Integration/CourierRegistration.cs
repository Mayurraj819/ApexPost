using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace ApexPost.Courier.Integration;
public static class CourierRegistration
{
    public static IHttpClientBuilder AddCourierApi(this IServiceCollection services, Uri baseAddress, Action<HttpStandardResilienceOptions>? tune = null)
    {
        if (baseAddress.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("The courier API requires HTTPS.");
        services.AddSingleton<CourierState>();
        services.AddTransient<AuthHeaderHandler>();
        var client = services.AddHttpClient<ApexApiClient>(http =>
        {
            http.BaseAddress = baseAddress;
            http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }).RedactLoggedHeaders(_ => true).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false }).AddHttpMessageHandler<AuthHeaderHandler>();
        client.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.ShouldHandle = args => ValueTask.FromResult(Safe(args.Outcome.Result?.RequestMessage ?? args.Context.GetRequestMessage()) && Transient(args.Outcome.Result, args.Outcome.Exception));
            options.CircuitBreaker.ShouldHandle = args => ValueTask.FromResult(Safe(args.Outcome.Result?.RequestMessage ?? args.Context.GetRequestMessage()) && Transient(args.Outcome.Result, args.Outcome.Exception));
            options.CircuitBreaker.MinimumThroughput = 3;
            options.CircuitBreaker.FailureRatio = .5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(10);
            tune?.Invoke(options);
        }).Configure((options, provider) =>
        {
            var state = provider.GetRequiredService<CourierState>();
            var logger = provider.GetRequiredService<ILogger<ApexApiClient>>();
            options.Retry.OnRetry = args =>
            {
                logger.LogWarning("Courier retry {Attempt}; status {Status}; delay {DelayMs}ms", args.AttemptNumber + 1, (int? )args.Outcome.Result?.StatusCode, args.RetryDelay.TotalMilliseconds);
                return default;
            };
            options.CircuitBreaker.OnOpened = args =>
            {
                state.SetCircuit("Open");
                logger.LogWarning("Courier circuit opened");
                return default;
            };
            options.CircuitBreaker.OnHalfOpened = args =>
            {
                state.SetCircuit("HalfOpen");
                logger.LogInformation("Courier circuit half-open");
                return default;
            };
            options.CircuitBreaker.OnClosed = args =>
            {
                state.SetCircuit("Closed");
                logger.LogInformation("Courier circuit closed");
                return default;
            };
        });
        return client;
    }

    private static bool Safe(HttpRequestMessage? request) => request is not null && (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head || request.Options.TryGetValue(ApexApiClient.IdempotentScanUpload, out var value) && value);
    private static bool Transient(HttpResponseMessage? response, Exception? exception) => exception is HttpRequestException or TimeoutRejectedException || response is not null && (response.StatusCode == HttpStatusCode.RequestTimeout || (int)response.StatusCode >= 500);
}

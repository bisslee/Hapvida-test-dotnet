using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Hapvida.ExternalIntegration.CrossCutting.DependencyInjection;

public static class HttpResilienceExtension
{
    /// <summary>
    /// Configura políticas de resiliência para HttpClient (Retry, Circuit Breaker, Timeout)
    /// Timeout: 3s por tentativa
    /// Retry: 3 tentativas com backoff exponencial + jitter
    /// Circuit Breaker: abre após 50% de falhas em 30s (mínimo 5 requisições), mantém aberto por 30s
    /// </summary>
    public static IHttpClientBuilder AddResiliencePolicies(
        this IHttpClientBuilder builder,
        string clientName)
    {
        builder
            .AddStandardResilienceHandler(options =>
            {
                // Timeout: 3 segundos por tentativa
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(3);

                // Retry: 3 tentativas com backoff exponencial + jitter
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(0.5);
                options.Retry.UseJitter = true;

                // Circuit Breaker: abre após 5 falhas consecutivas em 30 segundos
                options.CircuitBreaker.FailureRatio = 0.5; // 50% de falhas
                options.CircuitBreaker.MinimumThroughput = 5; // Mínimo de 5 requisições
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30); // Mantém aberto por 30s
            });
        
        return builder;
    }

    /// <summary>
    /// Configura políticas de resiliência simplificadas para provedores de CEP (timeout menor)
    /// Timeout: 2s por tentativa
    /// Retry: 3 tentativas com backoff exponencial + jitter
    /// Circuit Breaker: abre após 50% de falhas em 20s (mínimo 3 requisições), mantém aberto por 20s
    /// </summary>
    public static IHttpClientBuilder AddCepProviderResilience(
        this IHttpClientBuilder builder,
        string clientName)
    {
        builder
            .AddStandardResilienceHandler(options =>
            {
                // Timeout: 2 segundos por tentativa (mais rápido para CEP)
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(2);

                // Retry: 3 tentativas
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(0.3);
                options.Retry.UseJitter = true;

                // Circuit Breaker: abre após 3 falhas consecutivas em 20 segundos
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 3;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);
            });
        
        return builder;
    }
}

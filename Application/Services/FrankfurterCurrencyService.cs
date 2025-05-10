using Application.Interfaces;
using OpenTelemetry.Trace;
using Polly;
using Serilog;
using Shared.CurrencyProviders;
using Shared.Models;
using System.Net.Http.Json;

namespace Application.Services
{
    public class FrankfurterCurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _retryPolicy;
        private readonly IAsyncPolicy _circuitBreakerPolicy;
        private readonly Tracer _tracer;

        public FrankfurterCurrencyService(HttpClient httpClient, TracerProvider tracerProvider)
        {
            _httpClient = httpClient;
            _tracer = tracerProvider.GetTracer("CurrencyService");

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Log.Warning("Retry {RetryCount} for {ExceptionMessage} after {TimeSpanTotalSeconds}s",
                            retryCount, exception.Message, timeSpan.TotalSeconds);
                    });

            _circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
                    onBreak: (exception, timespan) =>
                    {
                        Log.Error("Circuit breaker triggered: {ExceptionMessage} at {TimespanTotalSeconds} seconds",
                            exception.Message, timespan.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        Log.Information("Circuit breaker reset.");
                    });
        }

        public async Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency)
        {
            using var span = _tracer.StartActiveSpan("GetLatestRatesAsync", SpanKind.Internal);

            try
            {
                span.SetAttribute("baseCurrency", baseCurrency);

                var url = $"latest?base={baseCurrency}";

                var response = await _retryPolicy.WrapAsync(_circuitBreakerPolicy)
                    .ExecuteAsync(async () =>
                    {
                        span.AddEvent("Calling Frankfurter API");

                        var result = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(url);
                        if (result == null)
                        {
                            span.SetStatus(Status.Error.WithDescription("Null response from API"));
                            Log.Error("Failed to retrieve exchange rates for {BaseCurrency}.", baseCurrency);
                            throw new Exception("Failed to retrieve exchange rates.");
                        }

                        return result;
                    });

                span.SetStatus(Status.Ok);
                span.AddEvent("Successfully retrieved exchange rates");

                return new ExchangeRate
                {
                    BaseCurrency = response.Base,
                    Date = response.Date,
                    Rates = response.Rates
                };
            }
            catch (Exception ex)
            {
                span.RecordException(ex);
                span.SetStatus(Status.Error.WithDescription(ex.Message));
                Log.Error(ex, "Error occurred while retrieving exchange rates.");
                throw;
            }
        }
        public async Task<ConversionResult> ConvertCurrencyAsync(string from, string to, decimal amount)
        {
            using var span = _tracer.StartActiveSpan("ConvertCurrencyAsync", SpanKind.Internal);

            span.SetAttribute("currency.from", from);
            span.SetAttribute("currency.to", to);
            span.SetAttribute("currency.amount", (double)amount);

            var url = $"latest?amount={amount}&from={from}&to={to}";

            try
            {
                var response = await _retryPolicy.WrapAsync(_circuitBreakerPolicy)
                    .ExecuteAsync(async () =>
                    {
                        span.AddEvent("Calling Frankfurter API for currency conversion");

                        var result = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(url);
                        if (result == null || !result.Rates.TryGetValue(to, out var rate))
                        {
                            var error = "Missing or invalid conversion result";
                            span.SetStatus(Status.Error.WithDescription(error));
                            span.AddEvent("Conversion failed: missing rate");
                            Log.Error("Failed to convert currency from {FromCurrency} to {ToCurrency}.", from, to);
                            throw new Exception(error);
                        }

                        return result;
                    });

                var rate = response.Rates[to];

                span.SetStatus(Status.Ok);
                span.AddEvent("Conversion successful");

                Log.Information("Conversion successful: {Amount} {FromCurrency} to {ToCurrency} at rate {Rate}",
                    amount, from, to, rate);

                return new ConversionResult
                {
                    FromCurrency = from,
                    ToCurrency = to,
                    OriginalAmount = amount,
                    Rate = rate,
                    ConvertedAmount = amount * rate
                };
            }
            catch (Exception ex)
            {
                span.RecordException(ex);
                span.SetStatus(Status.Error.WithDescription(ex.Message));
                Log.Error(ex, "Conversion failed with exception.");
                throw;
            }
        }
        public async Task<List<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end)
        {
            using var span = _tracer.StartActiveSpan("GetHistoricalRatesAsync", SpanKind.Internal);

            span.SetAttribute("currency.base", baseCurrency);
            span.SetAttribute("date.start", start.ToString("yyyy-MM-dd"));
            span.SetAttribute("date.end", end.ToString("yyyy-MM-dd"));

            var url = $"https://api.frankfurter.app/{start:yyyy-MM-dd}..{end:yyyy-MM-dd}?base={baseCurrency}";

            try
            {
                var response = await _retryPolicy.WrapAsync(_circuitBreakerPolicy)
                    .ExecuteAsync(async () =>
                    {
                        span.AddEvent("Calling Frankfurter API for historical data");

                        var result = await _httpClient.GetFromJsonAsync<ExchangeRateMultiDayResponse>(url);
                        if (result == null || result.Rates == null)
                        {
                            var error = "Invalid historical response or no rates found";
                            span.SetStatus(Status.Error.WithDescription(error));
                            span.AddEvent("Failed to retrieve historical rates");
                            Log.Error("Failed to fetch historical data for {BaseCurrency} from {Start} to {End}.",
                                baseCurrency, start.ToShortDateString(), end.ToShortDateString());
                            throw new Exception(error);
                        }

                        return result;
                    });

                span.SetStatus(Status.Ok);
                span.AddEvent("Successfully received historical data");

                Log.Information("Successfully fetched historical rates for {BaseCurrency} from {Start} to {End}.",
                    baseCurrency, start.ToShortDateString(), end.ToShortDateString());

                return response.Rates.Select(r => new ExchangeRate
                {
                    BaseCurrency = response.Base,
                    Date = r.Key,
                    Rates = r.Value
                }).ToList();
            }
            catch (Exception ex)
            {
                span.RecordException(ex);
                span.SetStatus(Status.Error.WithDescription(ex.Message));
                Log.Error(ex, "Failed to fetch historical rates due to an error.");
                throw;
            }
        }
    }
}

using Shared.Models;

namespace Application.Interfaces
{
    public interface ICurrencyService
    {
        Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency);
        Task<ConversionResult> ConvertCurrencyAsync(string from, string to, decimal amount);
        Task<List<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end);
    }
}

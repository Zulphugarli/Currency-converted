namespace Shared.CurrencyProviders
{
    public class ExchangeRateMultiDayResponse
    {
        public string Base { get; set; }
        public Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; } = new();
    }
}

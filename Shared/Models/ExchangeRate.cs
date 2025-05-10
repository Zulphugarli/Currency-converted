namespace Shared.Models
{
    public class ExchangeRate
    {
        public string BaseCurrency { get; set; } = default!;
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}

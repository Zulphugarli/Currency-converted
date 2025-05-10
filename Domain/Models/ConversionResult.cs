namespace Core.Models
{
    public class ConversionResult
    {
        public string FromCurrency { get; set; } = default!;
        public string ToCurrency { get; set; } = default!;
        public decimal OriginalAmount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal Rate { get; set; }
    }
}

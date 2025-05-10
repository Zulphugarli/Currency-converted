using Application.Interfaces;
using Application.Middlewares;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.CurrencyProviders;

namespace CurrencyConvertorApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly HashSet<string> _blockedCurrencies;

        public CurrencyController(ICurrencyService currencyService, IOptions<CurrencySettings> currencyOptions)
        {
            _currencyService = currencyService;
            _blockedCurrencies = currencyOptions.Value.BlockedCurrencies.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private bool IsCurrencyBlocked(string currencyCode) =>
            _blockedCurrencies.Contains(currencyCode);

        [HttpGet("latest")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetLatest([FromQuery] string baseCurrency)
        {
            if (IsCurrencyBlocked(baseCurrency))
                throw new ValidationException("Conversion with blocked currencies is not allowed.",
                    new Dictionary<string, string[]> { { "Currency", new[] { "Base currency is blocked." } } });

            var rates = await _currencyService.GetLatestRatesAsync(baseCurrency.ToUpper());

            foreach (var blocked in _blockedCurrencies)
                rates.Rates.Remove(blocked);

            return Ok(rates);
        }

        [HttpGet("convert")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Convert([FromQuery] string from, [FromQuery] string to, [FromQuery] decimal amount)
        {
            if (IsCurrencyBlocked(from) || IsCurrencyBlocked(to))
                throw new ValidationException("Conversion with blocked currencies is not allowed.",
                    new Dictionary<string, string[]> { { "Currency", new[] { "One or both currencies are blocked." } } });

            var result = await _currencyService.ConvertCurrencyAsync(from.ToUpper(), to.ToUpper(), amount);
            return Ok(result);
        }

        [HttpGet("history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetHistory([FromQuery] string baseCurrency, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (IsCurrencyBlocked(baseCurrency))
                throw new ValidationException("Conversion with blocked currencies is not allowed.",
                    new Dictionary<string, string[]> { { "Currency", new[] { "Base currency is blocked." } } });

            var data = await _currencyService.GetHistoricalRatesAsync(baseCurrency.ToUpper(), start, end);
            return Ok(data);
        }
    }
}

using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConvertorApi.Controllers.v2
{
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpGet("convert")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> ConvertV2([FromQuery] string from, [FromQuery] string to, [FromQuery] decimal amount, [FromQuery] DateTime? date = null)
        {
            var result = await _currencyService.ConvertCurrencyAsync(from.ToUpper(), to.ToUpper(), amount);
            return Ok(result);
        }
    }
}

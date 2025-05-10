using Application.Interfaces;
using Application.Middlewares;
using CurrencyConvertorApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Shared.CurrencyProviders;
using Shared.Models;

namespace Test
{
    public class CurrencyControllerTests
    {
        private readonly Mock<ICurrencyService> _mockCurrencyService;
        private readonly Mock<IOptions<CurrencySettings>> _mockCurrencySettings;
        private readonly CurrencyController _controller;

        public CurrencyControllerTests()
        {
            _mockCurrencyService = new Mock<ICurrencyService>();

            _mockCurrencySettings = new Mock<IOptions<CurrencySettings>>();
            _mockCurrencySettings.Setup(options => options.Value).Returns(new CurrencySettings
            {
                BlockedCurrencies = new List<string> { "TRY", "XBT" }
            });

            _controller = new CurrencyController(_mockCurrencyService.Object, _mockCurrencySettings.Object);
        }
        [Fact]
        public async Task GetLatest_ThrowsValidationException_ForBlockedCurrency()
        {
            var baseCurrency = "TRY";

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                _controller.GetLatest(baseCurrency));

            Assert.Equal("Conversion with blocked currencies is not allowed.", exception.Message);
            Assert.True(exception.Errors.ContainsKey("Currency"));
        }
       
        [Fact]
        public async Task GetLatest_ReturnsOk_ForValidCurrency()
        {
            var baseCurrency = "USD";
            var expectedRates = new ExchangeRate
            {
                BaseCurrency = baseCurrency,
                Date = System.DateTime.Now,
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.75m } }
            };

            _mockCurrencyService.Setup(service => service.GetLatestRatesAsync(baseCurrency.ToUpper()))
                .ReturnsAsync(expectedRates);

            var result = await _controller.GetLatest(baseCurrency);

            var actionResult = Assert.IsType<OkObjectResult>(result);
            var rates = Assert.IsType<ExchangeRate>(actionResult.Value);
            Assert.Equal(baseCurrency, rates.BaseCurrency);
            Assert.Contains(rates.Rates, r => r.Key == "EUR");
        }

        [Fact]
        public async Task Convert_ThrowsValidationException_ForBlockedCurrency()
        {
            var from = "TRY";
            var to = "USD";
            var amount = 100m;

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                _controller.Convert(from, to, amount));

            Assert.Equal("Conversion with blocked currencies is not allowed.", exception.Message);
            Assert.True(exception.Errors.ContainsKey("Currency"));
        }

        [Fact]
        public async Task Convert_ReturnsOk_ForValidConversion()
        {
            var fromCurrency = "USD";
            var toCurrency = "EUR";
            var amount = 100m;
            var conversionResult = new ConversionResult
            {
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                OriginalAmount = amount,
                Rate = 0.85m,
                ConvertedAmount = amount * 0.85m
            };

            _mockCurrencyService.Setup(service => service.ConvertCurrencyAsync(fromCurrency.ToUpper(), toCurrency.ToUpper(), amount))
                .ReturnsAsync(conversionResult);

            var result = await _controller.Convert(fromCurrency, toCurrency, amount);

            var actionResult = Assert.IsType<OkObjectResult>(result);
            var convertedResult = Assert.IsType<ConversionResult>(actionResult.Value);
            Assert.Equal(conversionResult.ConvertedAmount, convertedResult.ConvertedAmount);
        }

        [Fact]
        public async Task GetHistory_ThrowsValidationException_ForBlockedCurrency()
        {
            var baseCurrency = "TRY";
            var start = DateTime.UtcNow.AddDays(-7);
            var end = DateTime.UtcNow;

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                _controller.GetHistory(baseCurrency, start, end));

            Assert.Equal("Conversion with blocked currencies is not allowed.", exception.Message);
            Assert.True(exception.Errors.ContainsKey("Currency"));
        }

        [Fact]
        public async Task GetHistory_ReturnsOk_ForValidCurrency()
        {
            var baseCurrency = "USD";
            var start = DateTime.Now.AddDays(-30);
            var end = DateTime.Now;
            var expectedRates = new List<ExchangeRate>
            {
                new ExchangeRate
                {
                    BaseCurrency = baseCurrency,
                    Date = DateTime.Now.AddDays(-1),
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
                }
            };

            _mockCurrencyService.Setup(service => service.GetHistoricalRatesAsync(baseCurrency.ToUpper(), start, end))
                .ReturnsAsync(expectedRates);

            var result = await _controller.GetHistory(baseCurrency, start, end);

            var actionResult = Assert.IsType<OkObjectResult>(result);
            var rates = Assert.IsType<List<ExchangeRate>>(actionResult.Value);
            Assert.NotEmpty(rates);
        }
    }
}
using Application.Interfaces;
using Core.Models;
using CurrencyConvertorApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Shared.CurrencyProviders;

namespace Tests
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
        public async Task GetLatest_ReturnsBadRequest_ForBlockedCurrency()
        {
            var blockedCurrency = "TRY";
            var baseCurrency = blockedCurrency;

            var result = await _controller.GetLatest(baseCurrency);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Conversion with blocked currencies is not allowed.", actionResult.Value);
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
        public async Task Convert_ReturnsBadRequest_ForBlockedCurrency()
        {
            var blockedCurrency = "TRY";
            var fromCurrency = blockedCurrency;
            var toCurrency = "USD";
            var amount = 100m;

            var result = await _controller.Convert(fromCurrency, toCurrency, amount);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Conversion with blocked currencies is not allowed.", actionResult.Value);
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
        public async Task GetHistory_ReturnsBadRequest_ForBlockedCurrency()
        {
            var blockedCurrency = "TRY";
            var baseCurrency = blockedCurrency;
            var start = DateTime.Now.AddDays(-30);
            var end = DateTime.Now;

            var result = await _controller.GetHistory(baseCurrency, start, end);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Conversion with blocked currencies is not allowed.", actionResult.Value);
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
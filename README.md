💱 Currency Converter API
A robust, scalable, and maintainable currency conversion API built with C# and ASP.NET Core. This API provides real-time and historical currency exchange rates using the Frankfurter API, focusing on performance, security, and resilience.

🚀 Features
✅ Latest Exchange Rates – Retrieve current exchange rates for a specified base currency.
✅ Currency Conversion – Convert amounts between currencies, excluding TRY, PLN, THB, and MXN.
✅ Historical Exchange Rates – Get paginated historical exchange rate data for a date range.
✅ In-Memory Caching – Minimizes external API calls and improves performance.
✅ Resilience – Retry policies and circuit breaker patterns ensure fault tolerance.
✅ Security – JWT authentication and role-based access control.
✅ Rate Limiting – Prevents abuse by throttling excessive requests.
✅ Observability – Structured logging (Serilog), OpenTelemetry for distributed tracing.
✅ Testing – Includes unit and integration tests with 100% test coverage.
✅ API Versioning – Future-proof design with support for versioned endpoints.

Installation
1.Clone the Repository
git clone https://github.com/Zulphugarli/Currency-converted.git
cd Currency-converted
2.Configure Environment Settings
Create or update appsettings.Development.json to include:
JWT secret
Logging configuration
Caching (optional: uses in-memory by default)
3.Run the Application
dotnet restore
dotnet run

The API will be available at:
 https://localhost:7153
 http://localhost:5084

📚 API Endpoints
1. Retrieve Latest Exchange Rates
GET /api/v1/exchange-rates/latest?base=EUR
2. Currency Conversion
POST /api/v1/exchange-rates/convert
Request Example:
{
  "fromCurrency": "USD",
  "toCurrency": "EUR",
  "amount": 100
}
⚠️ Returns 400 Bad Request if the request involves TRY, PLN, THB, or MXN.
3. Historical Exchange Rates with Pagination
GET /api/v1/exchange-rates/history?base=EUR&startDate=2020-01-01&endDate=2020-01-31

🔐 Security & Access Control
JWT Authentication – Authenticate using bearer tokens.
RBAC – Role-based access control at the endpoint level.
Rate Limiting – Restricts excessive requests to prevent abuse.

📈 Observability & Monitoring
Structured Logging – via Serilog (optionally with Seq)
Distributed Tracing – via OpenTelemetry
Logs contain:
Client IP
ClientId from JWT
HTTP method and path
Status code and response time
External API correlation ID

🧪 Testing
Tests are located in CurrencyConvertorApi.Tests
Includes:
✅ Unit Tests 
✅ Integration Tests (verifying actual behavior and flow)



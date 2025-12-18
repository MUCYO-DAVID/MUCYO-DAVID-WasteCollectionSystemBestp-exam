using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace WasteCollectionSystem.Services
{
    /// <summary>
    /// Production-ready MTN Mobile Money service for Sandbox environment.
    /// Handles authentication, payment requests, and status checking.
    /// </summary>
    public class MtnMomoService : IMomoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _apiUser;
        private readonly string _apiKey;
        private readonly string _subscriptionKey;
        private readonly string _targetEnvironment;

        public MtnMomoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            // Load configuration from appsettings.json
            _baseUrl = _configuration["MtnMomo:BaseUrl"] ?? "https://sandbox.momodeveloper.mtn.com";
            _apiUser = _configuration["MtnMomo:ApiUser"] ?? throw new InvalidOperationException("MtnMomo:ApiUser not configured");
            _apiKey = _configuration["MtnMomo:ApiKey"] ?? throw new InvalidOperationException("MtnMomo:ApiKey not configured");
            _subscriptionKey = _configuration["MtnMomo:SubscriptionKey"] ?? throw new InvalidOperationException("MtnMomo:SubscriptionKey not configured");
            _targetEnvironment = _configuration["MtnMomo:TargetEnvironment"] ?? "sandbox";
        }

        /// <summary>
        /// Gets an access token from MTN MoMo API using Basic Authentication.
        /// </summary>
        /// <returns>Bearer access token</returns>
        public async Task<string> GetTokenAsync()
        {
            // Encode APIUser:APIKey to Base64 for Basic Auth
            var credentials = $"{_apiUser}:{_apiKey}";
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            // Clear previous headers
            _httpClient.DefaultRequestHeaders.Clear();

            // Add required headers for token request
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {base64Credentials}");
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

            // Make POST request to collection token endpoint
            var response = await _httpClient.PostAsync($"{_baseUrl}/collection/token/", null);
            response.EnsureSuccessStatusCode();

            // Deserialize response to extract access_token
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic? jsonResponse = JsonConvert.DeserializeObject(responseBody);

            return jsonResponse?.access_token ?? throw new InvalidOperationException("Failed to retrieve access token");
        }

        /// <summary>
        /// Initiates a payment request to MTN MoMo.
        /// </summary>
        /// <param name="phone">Payer's phone number (MSISDN format, e.g., 46733123450)</param>
        /// <param name="amount">Amount to charge</param>
        /// <returns>Transaction Reference ID (X-Reference-Id) for status polling</returns>
        public async Task<string> RequestToPayAsync(string phone, decimal amount)
        {
            // Get access token
            var accessToken = await GetTokenAsync();

            // Generate unique reference ID for this transaction
            var referenceId = Guid.NewGuid().ToString();

            // Clear previous headers
            _httpClient.DefaultRequestHeaders.Clear();

            // Add required headers for payment request
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("X-Reference-Id", referenceId);
            _httpClient.DefaultRequestHeaders.Add("X-Target-Environment", _targetEnvironment);
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

            // Build request body (EUR currency required for Sandbox)
            var requestBody = new
            {
                amount = amount.ToString("F2"),
                currency = "EUR", // Sandbox requires EUR
                externalId = Guid.NewGuid().ToString(),
                payer = new
                {
                    partyIdType = "MSISDN",
                    partyId = phone
                },
                payerMessage = "Payment for waste collection service",
                payeeNote = "Waste collection payment"
            };

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            // Make POST request to requesttopay endpoint
            var response = await _httpClient.PostAsync($"{_baseUrl}/collection/v1_0/requesttopay", jsonContent);
            response.EnsureSuccessStatusCode();

            // Return the reference ID for status polling
            return referenceId;
        }

        /// <summary>
        /// Gets the current status of a payment transaction.
        /// </summary>
        /// <param name="transactionId">Transaction Reference ID from RequestToPayAsync</param>
        /// <returns>Status string: "PENDING", "SUCCESSFUL", "FAILED", etc.</returns>
        public class MomoTransactionResult
        {
            public string Status { get; set; } = "UNKNOWN";
            public decimal Amount { get; set; }
            public string Currency { get; set; } = "EUR";
        }

        /// <summary>
        /// Gets the current status of a payment transaction.
        /// </summary>
        /// <param name="transactionId">Transaction Reference ID from RequestToPayAsync</param>
        /// <returns>MomoTransactionResult object with status and amount</returns>
        public async Task<MomoTransactionResult> GetTransactionStatusAsync(string transactionId)
        {
            // Get access token
            var accessToken = await GetTokenAsync();

            // Clear previous headers
            _httpClient.DefaultRequestHeaders.Clear();

            // Add required headers for status check
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("X-Target-Environment", _targetEnvironment);
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

            // Make GET request to check transaction status
            var response = await _httpClient.GetAsync($"{_baseUrl}/collection/v1_0/requesttopay/{transactionId}");
            response.EnsureSuccessStatusCode();

            // Deserialize response to extract status
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic? jsonResponse = JsonConvert.DeserializeObject(responseBody);

            return new MomoTransactionResult
            {
                Status = jsonResponse?.status ?? "UNKNOWN",
                Amount = decimal.TryParse((string?)jsonResponse?.amount, out var amount) ? amount : 0,
                Currency = jsonResponse?.currency ?? "EUR"
            };
        }
    }
}

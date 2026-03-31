using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FNS.Services
{
    public interface IRazorpayService
    {
        Task<Dictionary<string, object>> CreateOrderAsync(decimal amount, string description, string customerId);
        bool VerifyPayment(string orderId, string paymentId, string signature);
    }

    public class RazorpayService : IRazorpayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RazorpayService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.razorpay.com/v1";

        public RazorpayService(IConfiguration configuration, ILogger<RazorpayService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, object>> CreateOrderAsync(decimal amount, string description, string customerId)
        {
            try
            {
                string keyId = _configuration["Razorpay:KeyId"];
                string keySecret = _configuration["Razorpay:KeySecret"];

                // Validate credentials are configured
                if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret))
                {
                    _logger.LogError("Razorpay credentials are not configured. Please update appsettings.json with your Key ID and Key Secret");
                    throw new InvalidOperationException("Razorpay credentials are not configured. Please visit https://dashboard.razorpay.com/app/keys to get your test/live keys and update appsettings.json");
                }

                if (keyId.Contains("YOUR_") || keySecret.Contains("YOUR_"))
                {
                    _logger.LogError("Razorpay credentials are still placeholder values");
                    throw new InvalidOperationException("Razorpay credentials are placeholder values. Please update with real credentials from https://dashboard.razorpay.com/app/keys");
                }

                // Create basic auth header
                string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                var orderData = new
                {
                    amount = (int)(amount * 100), // Amount in paise
                    currency = "INR",
                    receipt = customerId,
                    description = description,
                    notes = new
                    {
                        customer_id = customerId,
                        payment_type = "UPI"
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(orderData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/orders", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var order = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    _logger.LogInformation($"Order created: {order.GetProperty("id")}");

                    return new Dictionary<string, object>
                    {
                        { "orderId", order.GetProperty("id").GetString() },
                        { "amount", order.GetProperty("amount").GetInt32() },
                        { "currency", order.GetProperty("currency").GetString() },
                        { "status", order.GetProperty("status").GetString() }
                    };
                }
                else
                {
                    _logger.LogError($"Failed to create order: {responseContent}");
                    throw new Exception($"Failed to create order: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                throw;
            }
        }

        public bool VerifyPayment(string orderId, string paymentId, string signature)
        {
            try
            {
                string keySecret = _configuration["Razorpay:KeySecret"];

                // Verify signature
                string text = orderId + "|" + paymentId;
                System.Security.Cryptography.HMACSHA256 hmac = new System.Security.Cryptography.HMACSHA256(
                    System.Text.Encoding.UTF8.GetBytes(keySecret)
                );
                byte[] computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));
                string computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLower();

                bool isValid = computedSignature == signature.ToLower();
                
                if (isValid)
                {
                    _logger.LogInformation($"Payment verified for Order: {orderId}, Payment: {paymentId}");
                }
                else
                {
                    _logger.LogWarning($"Payment verification failed for Order: {orderId}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying payment: {ex.Message}");
                return false;
            }
        }
    }
}

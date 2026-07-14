using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PazarYeriOtomasyon.API.Models;

namespace PazarYeriOtomasyon.API.Services
{
    public class HepsiburadaService : IMarketplaceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<HepsiburadaService> _logger;

        public string PlatformName => "Hepsiburada";

        public HepsiburadaService(HttpClient httpClient, IConfiguration config, ILogger<HepsiburadaService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Hepsiburada OAuth2 token al (client_credentials grant)
        /// POST https://api.hepsiburada.com/v3/auth
        /// </summary>
        private async Task<string?> GetAccessTokenAsync(string merchantId, string secretKey)
        {
            try
            {
                var authPayload = new Dictionary<string, string>
                {
                    { "client_id", merchantId },
                    { "client_secret", secretKey },
                    { "grant_type", "client_credentials" }
                };

                var authContent = new FormUrlEncodedContent(authPayload);
                
                // Auth endpoint ayrı bir base URL'de
                var authRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.hepsiburada.com/v3/auth")
                {
                    Content = authContent
                };
                authRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Hepsiburada OAuth2 token alınıyor...");
                var response = await _httpClient.SendAsync(authRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Hepsiburada token alınamadı. Status: {(int)response.StatusCode}. Body: {responseBody}");
                    return null;
                }

                _logger.LogDebug($"Hepsiburada auth yanıtı: {responseBody}");

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // Token'ı çeşitli olası alanlardan dene
                string? token = null;
                string[] tokenFields = { "access_token", "token", "accessToken", "data" };
                foreach (var field in tokenFields)
                {
                    if (root.TryGetProperty(field, out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String)
                    {
                        token = tokenProp.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            _logger.LogInformation($"Hepsiburada token başarıyla alındı (alan: {field}).");
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError($"Hepsiburada auth yanıtında token bulunamadı. Yanıt: {responseBody}");
                }

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hepsiburada OAuth2 token alınırken hata oluştu.");
                return null;
            }
        }

        public async Task<List<Order>> GetDeliveredOrdersAsync(int daysBack = 7)
        {
            var merchantId = _config["Hepsiburada:MerchantId"];
            var secretKey = _config["Hepsiburada:ApiKey"]; // Servis Anahtarı (Secret Key)

            if (string.IsNullOrEmpty(merchantId) || string.IsNullOrEmpty(secretKey))
            {
                _logger.LogWarning("Hepsiburada API bilgileri eksik (MerchantId veya ApiKey/SecretKey), servis çalıştırılamıyor.");
                return new List<Order>();
            }

            // 1) OAuth2 token al
            var accessToken = await GetAccessTokenAsync(merchantId, secretKey);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Hepsiburada erişim token'ı alınamadığı için siparişler çekilemiyor.");
                return new List<Order>();
            }

            var ordersList = new List<Order>();
            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
            int offset = 0;
            int limit = 50;
            bool hasMoreData = true;

            while (hasMoreData)
            {
                try
                {
                    // 2) Siparişleri çek - Bearer token ile
                    var endpoint = $"https://oms-external.hepsiburada.com/packages/merchantid/{merchantId}?offset={offset}&limit={limit}";
                    
                    var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    _logger.LogInformation($"Hepsiburada API çağrısı: {endpoint}");

                    var response = await _httpClient.SendAsync(request);

                    // Hata durumunda detaylı loglama
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Hepsiburada API hatası. Status: {(int)response.StatusCode} {response.StatusCode}. Body: {errorBody}");
                        hasMoreData = false;
                        break;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Hepsiburada API yanıtı (offset={offset}): {content.Substring(0, Math.Min(content.Length, 500))}...");

                    using var document = JsonDocument.Parse(content);
                    var root = document.RootElement;

                    // Toplam sayıyı al
                    int totalCount = 0;
                    if (root.TryGetProperty("totalCount", out var totalProp) && totalProp.ValueKind == JsonValueKind.Number)
                    {
                        totalCount = totalProp.GetInt32();
                        _logger.LogInformation($"Hepsiburada toplam paket sayısı: {totalCount}");
                    }

                    // Paketler "items" dizisi içinde gelir
                    if (root.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                    {
                        int itemCount = 0;
                        foreach (var item in itemsArray.EnumerateArray())
                        {
                            itemCount++;

                            // Status kontrolü - sadece "Delivered" olanları al
                            var status = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "";
                            if (!string.Equals(status, "Delivered", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // Tarih kontrolü
                            DateTime deliveredDate = DateTime.UtcNow;
                            if (item.TryGetProperty("deliveredDate", out var ddProp) && ddProp.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(ddProp.GetString(), out var parsed))
                                    deliveredDate = parsed;
                            }
                            else if (item.TryGetProperty("orderDate", out var odProp2) && odProp2.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(odProp2.GetString(), out var parsed))
                                    deliveredDate = parsed;
                            }

                            if (deliveredDate < cutoffDate) continue;

                            // Order Number
                            var orderNumber = item.TryGetProperty("orderNumber", out var onProp) ? onProp.GetString() : null;
                            if (string.IsNullOrEmpty(orderNumber))
                                orderNumber = item.TryGetProperty("packageNumber", out var pnProp) ? pnProp.GetString() : Guid.NewGuid().ToString();

                            // Müşteri adı
                            var customerName = "Bilinmiyor";
                            if (item.TryGetProperty("customer", out var cust))
                            {
                                if (cust.TryGetProperty("name", out var custName))
                                    customerName = custName.GetString() ?? "Bilinmiyor";
                                else if (cust.TryGetProperty("fullName", out var custFullName))
                                    customerName = custFullName.GetString() ?? "Bilinmiyor";
                            }
                            else if (item.TryGetProperty("customerName", out var cnProp))
                            {
                                customerName = cnProp.GetString() ?? "Bilinmiyor";
                            }

                            // Toplam tutar
                            decimal totalAmount = 0;
                            string[] priceFields = { "totalPrice", "grossAmount", "price", "totalAmount" };
                            foreach (var pf in priceFields)
                            {
                                if (item.TryGetProperty(pf, out var pVal) && pVal.ValueKind == JsonValueKind.Number)
                                {
                                    totalAmount = pVal.GetDecimal();
                                    break;
                                }
                            }

                            // Sipariş tarihi
                            var orderDate = deliveredDate;
                            if (item.TryGetProperty("orderDate", out var orderDateProp) && orderDateProp.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(orderDateProp.GetString(), out var parsedDate))
                                    orderDate = parsedDate;
                            }

                            // Fatura adresi
                            var isCorporate = false;
                            var companyName = string.Empty;
                            var taxNumber = "11111111111";
                            var taxOffice = string.Empty;
                            var addressStr = "Adres Yok";

                            JsonElement addressElement = default;
                            bool hasAddress = item.TryGetProperty("invoiceAddress", out addressElement) && addressElement.ValueKind != JsonValueKind.Null;
                            if (!hasAddress)
                                hasAddress = item.TryGetProperty("shippingAddress", out addressElement) && addressElement.ValueKind != JsonValueKind.Null;

                            if (hasAddress)
                            {
                                isCorporate = addressElement.TryGetProperty("isCorporate", out var corpProp) && corpProp.ValueKind == JsonValueKind.True;
                                
                                companyName = addressElement.TryGetProperty("companyName", out var cProp) ? cProp.GetString() ?? "" : "";
                                if (string.IsNullOrEmpty(companyName))
                                    companyName = addressElement.TryGetProperty("company", out var cProp2) ? cProp2.GetString() ?? "" : "";
                                
                                taxNumber = addressElement.TryGetProperty("taxNumber", out var tProp) ? tProp.GetString() ?? "" : "";
                                if (string.IsNullOrEmpty(taxNumber))
                                    taxNumber = addressElement.TryGetProperty("tcIdentityNumber", out var tcProp) ? tcProp.GetString() ?? "11111111111" : "11111111111";
                                if (string.IsNullOrEmpty(taxNumber)) taxNumber = "11111111111";

                                taxOffice = addressElement.TryGetProperty("taxOffice", out var toProp) ? toProp.GetString() ?? "" : "";

                                string[] addrFields = { "address", "address1", "fullAddress" };
                                foreach (var af in addrFields)
                                {
                                    if (addressElement.TryGetProperty(af, out var aProp) && !string.IsNullOrEmpty(aProp.GetString()))
                                    {
                                        addressStr = aProp.GetString()!;
                                        break;
                                    }
                                }
                            }

                            // Ürün bilgileri
                            var productNames = new List<string>();
                            var items2 = new List<object>();
                            string[] lineItemProps = { "lineItems", "items", "orderItems", "products" };

                            foreach (var prop in lineItemProps)
                            {
                                if (item.TryGetProperty(prop, out var lineItemsArray) && lineItemsArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var line in lineItemsArray.EnumerateArray())
                                    {
                                        var pName = line.TryGetProperty("productName", out var pn) ? pn.GetString() :
                                                   (line.TryGetProperty("name", out var n) ? n.GetString() : null);
                                        
                                        var qty = 1;
                                        if (line.TryGetProperty("quantity", out var qProp) && qProp.ValueKind == JsonValueKind.Number)
                                            qty = qProp.GetInt32();
                                        
                                        decimal unitPrice = 0;
                                        string[] upFields = { "price", "unitPrice", "amount" };
                                        foreach (var upf in upFields)
                                        {
                                            if (line.TryGetProperty(upf, out var upVal) && upVal.ValueKind == JsonValueKind.Number)
                                            {
                                                unitPrice = upVal.GetDecimal();
                                                break;
                                            }
                                        }

                                        if (totalAmount == 0 && unitPrice > 0)
                                            totalAmount += unitPrice * qty;

                                        if (!string.IsNullOrEmpty(pName))
                                        {
                                            productNames.Add(qty > 1 ? $"{pName} x{qty}" : pName);
                                            items2.Add(new { Name = pName, Quantity = qty, UnitPrice = unitPrice });
                                        }
                                    }
                                    break;
                                }
                            }

                            var productName = productNames.Count > 0
                                ? string.Join(", ", productNames)
                                : "E-Ticaret Satışı";
                            var itemsJson = JsonSerializer.Serialize(items2);

                            ordersList.Add(new Order
                            {
                                OrderNumber = orderNumber ?? Guid.NewGuid().ToString(),
                                Platform = PlatformName,
                                CustomerName = customerName ?? "Bilinmiyor",
                                TotalAmount = totalAmount,
                                OrderDate = orderDate,
                                Status = "Delivered",
                                IsCorporate = isCorporate,
                                CompanyName = companyName ?? string.Empty,
                                TaxNumber = taxNumber ?? "11111111111",
                                TaxOffice = taxOffice ?? string.Empty,
                                InvoiceAddress = addressStr,
                                ProductName = productName,
                                ItemsJson = itemsJson,
                                IsInvoiced = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        _logger.LogInformation($"Hepsiburada offset={offset}: {itemCount} paket okundu, {ordersList.Count} adet Delivered sipariş toplandı.");

                        if (itemCount < limit)
                            hasMoreData = false;
                        else
                        {
                            offset += limit;
                            if (totalCount > 0 && offset >= totalCount)
                                hasMoreData = false;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Hepsiburada API yanıtında 'items' dizisi bulunamadı. Root keys: {string.Join(", ", GetRootKeys(root))}");
                        hasMoreData = false;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, $"Hepsiburada HTTP isteği başarısız oldu. Offset: {offset}");
                    hasMoreData = false;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Hepsiburada API yanıtı JSON olarak parse edilemedi. Offset: {offset}");
                    hasMoreData = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Hepsiburada siparişleri çekilirken beklenmeyen hata oluştu. Offset: {offset}");
                    hasMoreData = false;
                }
            }

            _logger.LogInformation($"Hepsiburada: Toplam {ordersList.Count} adet teslim edilmiş sipariş çekildi (Son {daysBack} gün).");
            return ordersList;
        }

        private static IEnumerable<string> GetRootKeys(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                    yield return prop.Name;
            }
        }
    }
}

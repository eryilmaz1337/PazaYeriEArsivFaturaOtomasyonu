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
    public class TrendyolService : IMarketplaceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<TrendyolService> _logger;

        public string PlatformName => "Trendyol";

        public TrendyolService(HttpClient httpClient, IConfiguration config, ILogger<TrendyolService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri("https://api.trendyol.com/sapigw/suppliers/");
        }

        public async Task<List<Order>> GetDeliveredOrdersAsync(int daysBack = 7)
        {
            var supplierId = _config["Trendyol:SupplierId"];
            var apiKey = _config["Trendyol:ApiKey"];
            var apiSecret = _config["Trendyol:ApiSecret"];

            if (string.IsNullOrEmpty(supplierId) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Trendyol API bilgileri eksik, servis çalıştırılamıyor.");
                return new List<Order>();
            }

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var ordersList = new List<Order>();
            var chunks = (int)Math.Ceiling(daysBack / 14.0);
            
            for (int i = 0; i < chunks; i++)
            {
                var endDaysBack = i * 14;
                var startDaysBack = Math.Min((i + 1) * 14, daysBack);
                
                var startDate = DateTimeOffset.UtcNow.AddDays(-startDaysBack).ToUnixTimeMilliseconds();
                var endDate = DateTimeOffset.UtcNow.AddDays(-endDaysBack).ToUnixTimeMilliseconds();
                
                int page = 0;
                int size = 100; // Maksimum limit
                bool hasMoreData = true;

                while (hasMoreData)
                {
                    var endpoint = $"{supplierId}/orders?status=Delivered&startDate={startDate}&endDate={endDate}&page={page}&size={size}";

                    try
                    {
                        var response = await _httpClient.GetAsync(endpoint);
                        response.EnsureSuccessStatusCode();

                        var content = await response.Content.ReadAsStringAsync();
                        using var document = JsonDocument.Parse(content);
                        var root = document.RootElement;
                        
                        if (root.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                        {
                            var itemCount = 0;
                            foreach (var item in contentArray.EnumerateArray())
                            {
                                itemCount++;
                                var orderNumber = item.TryGetProperty("orderNumber", out var onProp) ? onProp.GetString() : Guid.NewGuid().ToString();
                                
                                var customerName = "Bilinmiyor";
                                if (item.TryGetProperty("shipmentAddress", out var shipAddr) && shipAddr.TryGetProperty("fullName", out var fn))
                                {
                                    customerName = fn.GetString();
                                }
                                
                                decimal totalAmount = 0;
                                if (item.TryGetProperty("totalPrice", out var tp)) totalAmount = tp.GetDecimal();
                                else if (item.TryGetProperty("totalDiscountedPrice", out var tdp)) totalAmount = tdp.GetDecimal();
                                else if (item.TryGetProperty("grossAmount", out var ga)) totalAmount = ga.GetDecimal();

                                long orderDateUnix = item.TryGetProperty("orderDate", out var od) ? od.GetInt64() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                var orderDate = DateTimeOffset.FromUnixTimeMilliseconds(orderDateUnix).UtcDateTime;

                                var companyName = string.Empty;
                                var taxNumber = string.Empty;
                                var taxOffice = string.Empty;
                                var fullAddress = "Adres Bilinmiyor";

                                if (item.TryGetProperty("invoiceAddress", out var invoiceAddress) && invoiceAddress.ValueKind != JsonValueKind.Null)
                                {
                                    companyName = invoiceAddress.TryGetProperty("company", out var companyProp) ? companyProp.GetString() : string.Empty;
                                    taxNumber = invoiceAddress.TryGetProperty("taxNumber", out var taxNumProp) ? taxNumProp.GetString() : string.Empty;
                                    if (string.IsNullOrEmpty(taxNumber)) {
                                         taxNumber = invoiceAddress.TryGetProperty("tcIdentityNumber", out var tcProp) ? tcProp.GetString() : "11111111111";
                                    }
                                    taxOffice = invoiceAddress.TryGetProperty("taxOffice", out var tOutOffice) ? tOutOffice.GetString() : string.Empty;
                                    fullAddress = invoiceAddress.TryGetProperty("address1", out var addrProp) ? addrProp.GetString() : "Adres Bilinmiyor";
                                }
                                
                                bool isCorporate = !string.IsNullOrEmpty(companyName) && taxNumber != null && taxNumber.Length >= 10;

                                var productNames = new List<string>();
                                var items = new List<object>();
                                if (item.TryGetProperty("lines", out var linesArray) && linesArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var line in linesArray.EnumerateArray())
                                    {
                                        var pName = line.TryGetProperty("productName", out var pnProp) ? pnProp.GetString() : 
                                                    (line.TryGetProperty("name", out var n) ? n.GetString() : null);
                                        var qty = line.TryGetProperty("quantity", out var qProp) && qProp.ValueKind == JsonValueKind.Number ? qProp.GetInt32() : 1;
                                        
                                        decimal unitPrice = 0;
                                        if (line.TryGetProperty("price", out var pProp)) unitPrice = qty > 0 ? pProp.GetDecimal() / qty : pProp.GetDecimal();
                                        else if (line.TryGetProperty("totalDiscountedPrice", out var tdpProp)) unitPrice = qty > 0 ? tdpProp.GetDecimal() / qty : tdpProp.GetDecimal();
                                        
                                        if (!string.IsNullOrEmpty(pName))
                                        {
                                            productNames.Add(qty > 1 ? $"{pName} x{qty}" : pName);
                                            items.Add(new { Name = pName, Quantity = qty, UnitPrice = unitPrice });
                                        }
                                    }
                                }
                                var productName = productNames.Count > 0 
                                    ? string.Join(", ", productNames) 
                                    : "E-Ticaret Satışı";
                                var itemsJson = JsonSerializer.Serialize(items);

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
                                    InvoiceAddress = fullAddress,
                                    ProductName = productName,
                                    ItemsJson = itemsJson,
                                    IsInvoiced = false,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                            
                            if (itemCount < size) {
                                hasMoreData = false;
                            } else {
                                page++;
                            }
                        }
                        else
                        {
                            hasMoreData = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Trendyol siparişleri çekilirken hata oluştu. Sayfa: {page}");
                        hasMoreData = false; // Hata durumunda sonsuz döngüden çık
                    }
                }
            }

            return ordersList;
        }
    }
}

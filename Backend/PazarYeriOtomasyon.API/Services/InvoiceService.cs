using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PazarYeriOtomasyon.API.Models;

namespace PazarYeriOtomasyon.API.Services
{
    public class InvoiceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<InvoiceService> _logger;

        private string _token = string.Empty;

        public InvoiceService(HttpClient httpClient, IConfiguration config, ILogger<InvoiceService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri("https://earsivportal.efatura.gov.tr/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("Origin", "https://earsivportal.efatura.gov.tr");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://earsivportal.efatura.gov.tr/intragiris.html");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"146\", \"Not-A.Brand\";v=\"24\", \"Google Chrome\";v=\"146\"");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Linux\"");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        private async Task AuthenticateAsync()
        {
            var userCode = _config["GibPortal:UserCode"]?.Trim();
            var password = _config["GibPortal:Password"]?.Trim();
            
            if (string.IsNullOrEmpty(userCode) || string.IsNullOrEmpty(password))
            {
                throw new Exception("GİB Portal bilgileri eksik (.env kontrol ediniz).");
            }

            var payload = $"assoscmd=anologin&rtype=json&userid={Uri.EscapeDataString(userCode)}&sifre={Uri.EscapeDataString(password)}&sifre2={Uri.EscapeDataString(password)}&parola=1&";
            var content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync("earsiv-services/assos-login", content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            // GİB Portal genelde response içinde "token" döner
            using var doc = JsonDocument.Parse(responseContent);
            if (doc.RootElement.TryGetProperty("token", out var tokenElement))
            {
                _token = tokenElement.GetString() ?? "";
                _logger.LogInformation("GİB E-Arşiv portalına başarıyla giriş yapıldı.");
            }
            else
            {
                _logger.LogError($"GİB Hatası: Sunucudan dönen yanıt: {responseContent}");
                throw new Exception($"GİB Portal token alınamadı, giriş/şifre hatalı. Yanıt: {responseContent}");
            }
        }

        private async Task LogoutAsync()
        {
            if (string.IsNullOrEmpty(_token)) return;

            try
            {
                var callId = Guid.NewGuid().ToString();
                var content = new StringContent(
                    $"cmd=EARSIV_PORTAL_LOGOUT&callId={callId}&token={_token}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"
                );

                await _httpClient.PostAsync("earsiv-services/dispatch", content);
                _logger.LogInformation("GİB Portalından güvenli bir şekilde çıkış (Logout) yapıldı.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GİB Logout işlemi sırasında hata oluştu, oturum kendiliğinden düşecek.");
            }
            finally
            {
                _token = string.Empty; // Token'ı temizle
            }
        }

        public async Task<bool> CreateInvoiceAsync(Order order)
        {
            try
            {
                if (string.IsNullOrEmpty(_token))
                {
                    await AuthenticateAsync();
                }

                string aliciAdi = order.CustomerName;
                string aliciSoyadi = "";
                
                // Müşteri adını ad ve soyad olarak ayırma (Bireysel fatura için GİB soyad bekler)
                if (!order.IsCorporate && !string.IsNullOrEmpty(order.CustomerName))
                {
                    var nameParts = order.CustomerName.Split(' ');
                    if (nameParts.Length > 1) {
                        aliciSoyadi = nameParts[nameParts.Length - 1];
                        aliciAdi = string.Join(" ", nameParts.Take(nameParts.Length - 1));
                    }
                }

                // KDV Hesaplaması (%20 standart oran)
                decimal kdvOrani = 20m;
                var malHizmetListesi = new List<Dictionary<string, object>>();
                decimal toplamMatrah = 0m;
                decimal toplamKdv = 0m;

                if (!string.IsNullOrEmpty(order.ItemsJson) && order.ItemsJson != "[]")
                {
                    try
                    {
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var items = JsonSerializer.Deserialize<List<OrderItem>>(order.ItemsJson, jsonOptions);
                        if (items != null && items.Count > 0)
                        {
                            foreach (var item in items)
                            {
                                decimal itemTotalAmount = item.UnitPrice * item.Quantity;
                                decimal itemMatrah = Math.Round(itemTotalAmount / (1 + kdvOrani / 100), 2);
                                decimal itemKdv = Math.Round(itemTotalAmount - itemMatrah, 2);
                                decimal birimFiyatMatrah = item.Quantity > 0 ? Math.Round(itemMatrah / item.Quantity, 2) : 0;

                                malHizmetListesi.Add(new Dictionary<string, object>
                                {
                                    { "malHizmet", item.Name },
                                    { "miktar", item.Quantity },
                                    { "birim", "C62" },
                                    { "birimFiyat", birimFiyatMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                                    { "fiyat", itemMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                                    { "iskontoOrani", 0 },
                                    { "iskontoTutari", "0" },
                                    { "iskontoNedeni", "" },
                                    { "malHizmetTutari", itemMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                                    { "kdvOrani", kdvOrani.ToString("F0") },
                                    { "kdvTutari", itemKdv.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                                    { "vergiOrani", 0 },
                                    { "vergiTutari", "0" }
                                });

                                toplamMatrah += itemMatrah;
                                toplamKdv += itemKdv;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ItemsJson deserialize edilirken hata oluştu. Varsayılan tek kalem moduna geçiliyor.");
                        malHizmetListesi.Clear();
                    }
                }

                // Fallback: Eğer liste boşsa (Hata veya eski veri) tek kalem olarak oluştur
                if (malHizmetListesi.Count == 0)
                {
                    toplamMatrah = Math.Round(order.TotalAmount / (1 + kdvOrani / 100), 2);
                    toplamKdv = Math.Round(order.TotalAmount - toplamMatrah, 2);

                    var malHizmetText = !string.IsNullOrEmpty(order.ProductName) ? order.ProductName : "E-Ticaret Satışı";
                    malHizmetListesi.Add(new Dictionary<string, object>
                    {
                        { "malHizmet", malHizmetText },
                        { "miktar", 1 },
                        { "birim", "C62" },
                        { "birimFiyat", toplamMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                        { "fiyat", toplamMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                        { "iskontoOrani", 0 },
                        { "iskontoTutari", "0" },
                        { "iskontoNedeni", "" },
                        { "malHizmetTutari", toplamMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                        { "kdvOrani", kdvOrani.ToString("F0") },
                        { "kdvTutari", toplamKdv.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                        { "vergiOrani", 0 },
                        { "vergiTutari", "0" }
                    });
                }

                // Toplam Yuvarlama Kontrolü (GİB kuruş farklarını sevmez)
                decimal hesaplananGenelToplam = toplamMatrah + toplamKdv;
                if (hesaplananGenelToplam != order.TotalAmount && malHizmetListesi.Count > 0)
                {
                    decimal fark = order.TotalAmount - hesaplananGenelToplam;
                    toplamKdv += fark;
                    // Son kalemin KDV'sine farkı ekle/çıkar
                    var sonKalem = malHizmetListesi[malHizmetListesi.Count - 1];
                    decimal eskiKdv = decimal.Parse(sonKalem["kdvTutari"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    sonKalem["kdvTutari"] = (eskiKdv + fark).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                }

                // GİB Portal'a Fatura payload'u
                // NOT: GİB 2026 güncellemesi - faturaUuid BOŞ gönderilmeli, ETTN'yi GİB kendisi üretiyor.
                var faturaPayload = new Dictionary<string, object>
                {
                    { "faturaUuid", "" },
                    { "belgeNumarasi", "" },
                    { "faturaTarihi", DateTime.Now.ToString("dd/MM/yyyy") },
                    { "saat", DateTime.Now.ToString("HH:mm:ss") },
                    { "paraBirimi", "TRY" },
                    { "dovzTLkur", "0" },
                    { "faturaTipi", "SATIS" },
                    { "hangiTip", "5000/30000" },
                    { "vknTckn", order.IsCorporate ? order.TaxNumber : "11111111111" },
                    { "aliciUnvan", order.IsCorporate ? order.CompanyName : "" },
                    { "aliciAdi", order.IsCorporate ? "" : aliciAdi },
                    { "aliciSoyadi", order.IsCorporate ? "" : aliciSoyadi },
                    { "binaAdi", "" },
                    { "binaNo", "" },
                    { "kapiNo", "" },
                    { "kasabaKoy", "" },
                    { "vergiDairesi", order.IsCorporate ? order.TaxOffice : "" },
                    { "ulke", "Türkiye" },
                    { "bulvarcaddesokak", string.IsNullOrEmpty(order.InvoiceAddress) ? "Adres Bilinmiyor" : order.InvoiceAddress },
                    { "mahalleSemtIlce", "" },
                    { "sehir", "" },
                    { "postaKodu", "" },
                    { "tel", "" },
                    { "fax", "" },
                    { "eposta", "" },
                    { "websitesi", "" },
                    { "iadeTable", Array.Empty<object>() },
                    { "ozelMatrahTutari", "0" },
                    { "ozelMatrahOrani", "0" },
                    { "ozelMatrahVergiTutari", "0" },
                    { "vergiCesidi", " " },
                    { "malHizmetTable", malHizmetListesi },
                    { "tip", "İskonto" },
                    { "matrah", toplamMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                    { "malhizmetToplamTutari", toplamMatrah.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                    { "toplamIskonto", "0" },
                    { "hesaplanankdv", toplamKdv.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                    { "vergilerToplami", toplamKdv.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                    { "vergilerDahilToplamTutar", order.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                    { "odenecekTutar", order.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                    { "not", $"Sipariş No: {order.OrderNumber} numaralı e-ticaret satışı" },
                    { "siparisNumarasi", "" },
                    { "siparisTarihi", "" },
                    { "irsaliyeNumarasi", "" },
                    { "irsaliyeTarihi", "" },
                    { "fisNo", "" },
                    { "fisTarihi", "" },
                    { "fisSaati", "" },
                    { "fisTipi", "" },
                    { "zRaporNo", "" },
                    { "okcSeriNo", "" }
                };

                var callId = Guid.NewGuid().ToString();
                var jpJson = JsonSerializer.Serialize(faturaPayload);
                _logger.LogInformation($"GİB Fatura Payload: {jpJson}");

                var content = new StringContent(
                    $"cmd=EARSIV_PORTAL_FATURA_OLUSTUR&callid={callId}&token={_token}&pageName=RG_BASITFATURA&jp={Uri.EscapeDataString(jpJson)}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"
                );

                var response = await _httpClient.PostAsync("earsiv-services/dispatch", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"GİB Yanıt: {responseString}");

                // GİB başarı yanıtı: {"data":"Faturanız başarıyla oluşturulmuştur...","metadata":{...}}
                // GİB hata yanıtı: {"data":"Hata mesajı...","metadata":{...}} veya {"error":"..."}
                using var responseDoc = JsonDocument.Parse(responseString);
                var root = responseDoc.RootElement;
                
                // Hata kontrolü
                if (root.TryGetProperty("error", out _))
                {
                    _logger.LogError($"GİB Portal hata döndü: {responseString}");
                    return false;
                }

                if (root.TryGetProperty("data", out var dataElement))
                {
                    var dataStr = dataElement.GetString() ?? "";
                    if (dataStr.Contains("başarı", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"Fatura başarıyla oluşturuldu: Sipariş {order.OrderNumber}");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"GİB fatura oluşturulamadı. Data: {dataStr}");
                        return false;
                    }
                }

                _logger.LogError($"GİB'den beklenmeyen yanıt: {responseString}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fatura kesim servisinde hata: Sipariş {order.OrderNumber}");
                return false;
            }
            finally
            {
                // İşlem başarılı da olsa hatalı da olsa GİB Portal'ı meşgul etmemek için hemen çıkış (Logout) yap.
                await LogoutAsync();
            }
        }
    }

    public class OrderItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
        
        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }
    }
}

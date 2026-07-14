using System;
using System.ComponentModel.DataAnnotations;

namespace PazarYeriOtomasyon.API.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        
        public string OrderNumber { get; set; } = string.Empty;
        
        public string Platform { get; set; } = string.Empty; // Trendyol, Hepsiburada vs.
        
        public string CustomerName { get; set; } = string.Empty;
        
        public decimal TotalAmount { get; set; }
        
        public DateTime OrderDate { get; set; }
        
        public string Status { get; set; } = string.Empty; // Delivered vs.
        
        // Fatura Detayları
        public bool IsCorporate { get; set; } = false;
        public string TaxNumber { get; set; } = "11111111111"; // VKN veya TCKN
        public string TaxOffice { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string InvoiceAddress { get; set; } = string.Empty;
        
        public string ProductName { get; set; } = string.Empty; // API'den gelen ürün adı
        
        public string ItemsJson { get; set; } = "[]"; // Ürün kalemleri detayı (JSON)
        
        public bool IsInvoiced { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PazarYeriOtomasyon.API.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public string GibEttn { get; set; } = string.Empty; // GİB üzerinden dönen ETTN
        
        public DateTime InvoicedDate { get; set; } = DateTime.UtcNow;
        
        public string ErrorMessage { get; set; } = string.Empty; // Hata varsa kaydedilir
        
        public bool IsSuccess { get; set; } = false;
    }
}

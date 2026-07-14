using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PazarYeriOtomasyon.API.Data;
using PazarYeriOtomasyon.API.Models;
using PazarYeriOtomasyon.API.Services;

namespace PazarYeriOtomasyon.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly InvoiceService _invoiceService;

        public OrdersController(AppDbContext context, InvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(100)
                .Select(o => new {
                    o.Id,
                    o.OrderNumber,
                    o.Platform,
                    o.CustomerName,
                    o.TotalAmount,
                    o.OrderDate,
                    o.Status,
                    o.IsCorporate,
                    o.TaxNumber,
                    o.TaxOffice,
                    o.CompanyName,
                    o.InvoiceAddress,
                    o.ProductName,
                    o.IsInvoiced,
                    o.CreatedAt,
                    // Eğer fatura kesilmişse veya denenmişse faturadaki hata durumunu da getirelim
                    InvoiceDetails = _context.Invoices.Where(i => i.OrderId == o.Id).OrderByDescending(i => i.InvoicedDate).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost("{id}/invoice")]
        public async Task<IActionResult> CreateManualInvoice(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("Sipariş bulunamadı.");
            
            if (order.IsInvoiced) return BadRequest("Bu siparişe zaten fatura kesilmiş.");

            // Kurumsal veya bireysel ayrımı model üzerinden otomatik yapılır.
            var isSuccess = await _invoiceService.CreateInvoiceAsync(order);
            
            if (isSuccess)
            {
                order.IsInvoiced = true;
                _context.Invoices.Add(new Invoice
                {
                    Order = order,
                    InvoiceNumber = "GIB-" + System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    IsSuccess = true,
                    InvoicedDate = System.DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Fatura başarıyla kesildi.", order = order });
            }
            
            _context.Invoices.Add(new Invoice
            {
                Order = order,
                IsSuccess = false,
                ErrorMessage = "GİB Portal fatura kesim reddi/hatası",
                InvoicedDate = System.DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return BadRequest(new { success = false, message = "Fatura kesimi başarısız oldu." });
        }
    }
}

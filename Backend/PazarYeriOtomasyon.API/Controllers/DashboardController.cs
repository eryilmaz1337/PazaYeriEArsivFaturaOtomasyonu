using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PazarYeriOtomasyon.API.Data;

namespace PazarYeriOtomasyon.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var invoicedOrders = await _context.Orders.CountAsync(o => o.IsInvoiced);
            var pendingInvoices = await _context.Orders.CountAsync(o => !o.IsInvoiced && o.Status == "Delivered");
            var failedInvoices = await _context.Invoices.CountAsync(i => !i.IsSuccess);

            var revenue = await _context.Orders.SumAsync(o => (double)o.TotalAmount); // SQLite dec to double issue prevention or just decimal

            return Ok(new
            {
                TotalOrders = totalOrders,
                InvoicedOrders = invoicedOrders,
                PendingInvoices = pendingInvoices,
                FailedInvoices = failedInvoices,
                TotalRevenue = revenue
            });
        }
    }
}

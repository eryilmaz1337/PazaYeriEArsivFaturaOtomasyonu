using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PazarYeriOtomasyon.API.Data;
using PazarYeriOtomasyon.API.Models;
using PazarYeriOtomasyon.API.Services;

namespace PazarYeriOtomasyon.API.Workers
{
    public class OrderSyncWorker : BackgroundService
    {
        private readonly ILogger<OrderSyncWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public OrderSyncWorker(ILogger<OrderSyncWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Sync Worker başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker çalışırken krtik bir hata oluştu.");
                }

                // 2 saatte bir çalışacak şekilde beklentidir (Örn: 2 * 60 * 60 * 1000)
                await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
            }
        }

        private async Task ProcessOrdersAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var marketplaces = scope.ServiceProvider.GetRequiredService<IEnumerable<IMarketplaceService>>();

            foreach (var mp in marketplaces)
            {
                _logger.LogInformation($"{mp.PlatformName} -  Teslim edilen siparişler kontrol ediliyor (Son 90 gün)...");
                var deliveredOrders = await mp.GetDeliveredOrdersAsync(daysBack: 90);
                
                foreach (var remoteOrder in deliveredOrders)
                {
                    var existingOrder = await dbContext.Orders
                        .FirstOrDefaultAsync(o => o.OrderNumber == remoteOrder.OrderNumber, stoppingToken);

                    if (existingOrder == null)
                    {
                        dbContext.Orders.Add(remoteOrder);
                        existingOrder = remoteOrder;
                    }
                }
            }
            
            await dbContext.SaveChangesAsync(stoppingToken);

            /* OTOMATİK FATURA KESİMİ KULLANICI İSTEĞİ ÜZERİNE İPTAL EDİLMİŞTİR
            var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();
            var uninvoicedOrders = await dbContext.Orders
                .Where(o => !o.IsInvoiced && o.Status == "Delivered")
                .ToListAsync(stoppingToken);

            foreach (var order in uninvoicedOrders)
            {
                _logger.LogInformation($"{order.OrderNumber} numaralı sipariş için fatura kesimi başlatılıyor...");
                var isSuccess = await invoiceService.CreateInvoiceAsync(order, "11111111111", order.CustomerName, "Örnek Adres, Türkiye");
                if (isSuccess)
                {
                    order.IsInvoiced = true;
                    dbContext.Invoices.Add(new Invoice
                    {
                        Order = order,
                        InvoiceNumber = "GIB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        IsSuccess = true,
                        InvoicedDate = DateTime.UtcNow
                    });
                }
                else
                {
                    dbContext.Invoices.Add(new Invoice
                    {
                        Order = order,
                        IsSuccess = false,
                        ErrorMessage = "GİB Portal fatura kesim reddi/hatası",
                        InvoicedDate = DateTime.UtcNow
                    });
                }
            }
            await dbContext.SaveChangesAsync(stoppingToken);
            */
            
            _logger.LogInformation("Periyodik senkronizasyon tamamlandı (Manuel Fatura).");
        }
    }
}

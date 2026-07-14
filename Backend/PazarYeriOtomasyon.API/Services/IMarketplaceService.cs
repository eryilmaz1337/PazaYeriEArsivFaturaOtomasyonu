using System.Collections.Generic;
using System.Threading.Tasks;
using PazarYeriOtomasyon.API.Models;

namespace PazarYeriOtomasyon.API.Services
{
    public interface IMarketplaceService
    {
        string PlatformName { get; }
        
        /// <summary>
        /// "Delivered" statüsündeki siparişleri çeker.
        /// </summary>
        Task<List<Order>> GetDeliveredOrdersAsync(int daysBack = 7);
    }
}

using Microsoft.EntityFrameworkCore;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;
using TechStore.Domain.Models;
using TechStore.Repository.Data;

namespace TechStore.Service.Service
{
    /// <summary>
    /// Truy van tong hop cho dashboard Admin. Inject thang ApplicationDbContext
    /// (tien le OrderService) vi IGenericRepository materialize ngay, khong
    /// aggregate/GroupBy o tang DB duoc.
    /// </summary>
    public class AdminService : IService.IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminStatsResponse> GetStatsAsync(DateTime from, DateTime to)
        {
            var inRange = _context.Orders.Where(o => o.OrderDate >= from && o.OrderDate < to);

            // "Counted revenue" — khop luat man Doanh thu Staff phia FE:
            // don khong huy, khong that bai, va (VNPay da tra) hoac (COD da giao).
            var counted = inRange.Where(o =>
                o.Status != "Cancelled" && o.PaymentStatus != "Failed" &&
                ((o.PaymentMethod == "VNPay" && o.PaymentStatus == "Paid") ||
                 (o.PaymentMethod != "VNPay" && o.Status == "Delivered")));

            var totalRevenue = await counted.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var orderCount = await counted.CountAsync();
            var shipTotal = await counted.SumAsync(o => (decimal?)o.ShippingFee) ?? 0m;

            var statusCounts = await inRange
                .GroupBy(o => o.Status)
                .Select(g => new StatusCountDTO { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var paymentSplit = await counted
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new PaymentSplitDTO
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var series = await counted
                .GroupBy(o => o.OrderDate.Date) // Npgsql -> date_trunc('day')
                .Select(g => new RevenuePointDTO
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Orders = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var newCustomers = await _context.Users
                .CountAsync(u => u.CreatedAt >= from && u.CreatedAt < to && u.Role == Role.Customer);
            var lowStock = await _context.Products.CountAsync(p => p.StockQuantity <= 10);

            return new AdminStatsResponse
            {
                From = from,
                To = to,
                TotalRevenue = totalRevenue,
                OrderCount = orderCount,
                ShippingFeeTotal = shipTotal,
                NewCustomers = newCustomers,
                LowStockCount = lowStock,
                StatusCounts = statusCounts,
                PaymentSplit = paymentSplit,
                RevenueSeries = series
            };
        }

        public async Task<IEnumerable<LoginSession>> GetSessionsAsync()
        {
            return await _context.LoginSessions
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .Take(200)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserDevice>> GetDevicesAsync()
        {
            return await _context.UserDevices
                .OrderByDescending(d => d.UpdatedAt)
                .Take(200)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity <= threshold)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
        }

        public async Task<Product?> UpdateProductStockAsync(Guid id, int stock)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return null;
            product.StockQuantity = stock < 0 ? 0 : stock;
            await _context.SaveChangesAsync();
            return product;
        }
    }
}

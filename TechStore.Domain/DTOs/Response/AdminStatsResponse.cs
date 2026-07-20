using System;
using System.Collections.Generic;

namespace TechStore.Domain.DTOs.Response
{
    /// <summary>
    /// Thong ke tong hop cho dashboard Admin (GET /api/admin/stats?from=&to=).
    /// Doanh thu "counted" khop luat man Doanh thu cua Staff phia FE.
    /// </summary>
    public class AdminStatsResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal ShippingFeeTotal { get; set; }
        public int NewCustomers { get; set; }
        public int LowStockCount { get; set; }
        public List<StatusCountDTO> StatusCounts { get; set; } = new();
        public List<PaymentSplitDTO> PaymentSplit { get; set; } = new();
        public List<RevenuePointDTO> RevenueSeries { get; set; } = new();
    }

    public class StatusCountDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PaymentSplitDTO
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RevenuePointDTO
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }
}

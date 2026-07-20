using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Domain.DTOs.Response
{
    public class ShippingCalculationResponse
    {
        public double DistanceKm { get; set; }
        public double DurationMinutes { get; set; }
        public decimal ShippingFee { get; set; }
        public string RoutePolyline { get; set; } = string.Empty;

    }
}
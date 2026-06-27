using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Domain.DTOs.Request
{
    public class ShippingCalculationRequest
    {
        public double DestinationLat { get; set; }
        public double DestinationLng { get; set; }
    }
}
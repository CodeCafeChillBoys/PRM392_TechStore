using System;
using System.Collections.Concurrent;
using TechStore.Domain.DTOs.Request;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class TrackingService : ITrackingService
    {
        private readonly ConcurrentDictionary<Guid, TrackingLocation> _locations = new();
        private readonly ConcurrentDictionary<Guid, TrackingLocation> _orderLocations = new();

        public void UpdateLocation(Guid shipperId, double lat, double lng)
        {
            var tracking = new TrackingLocation
            {
                ShipperId = shipperId,
                Lat = lat,
                Lng = lng,
                UpdatedAt = DateTime.UtcNow
            };
            _locations.AddOrUpdate(shipperId, tracking, (key, oldValue) => tracking);
        }

        public TrackingLocation? GetLatestLocation(Guid shipperId)
        {
            _locations.TryGetValue(shipperId, out var location);
            return location;
        }

        public void UpdateLocationForOrder(Guid orderId, Guid shipperId, double lat, double lng)
        {
            var tracking = new TrackingLocation
            {
                ShipperId = shipperId,
                Lat = lat,
                Lng = lng,
                UpdatedAt = DateTime.UtcNow
            };
            _orderLocations.AddOrUpdate(orderId, tracking, (key, oldValue) => tracking);
        }

        public TrackingLocation? GetLatestLocationByOrder(Guid orderId)
        {
            _orderLocations.TryGetValue(orderId, out var location);
            return location;
        }
    }
}
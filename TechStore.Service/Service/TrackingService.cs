using System;
using System.Collections.Concurrent;
using TechStore.Domain.DTOs;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class TrackingService : ITrackingService
    {
        private readonly ConcurrentDictionary<Guid, TrackingLocation> _locations = new();

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
    }
}
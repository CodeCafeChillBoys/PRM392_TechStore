using System;
using TechStore.Domain.DTOs.Request;

namespace TechStore.Service.IService
{
    public interface ITrackingService
    {
        void UpdateLocation(Guid shipperId, double lat, double lng);
        TrackingLocation? GetLatestLocation(Guid shipperId);

        void UpdateLocationForOrder(Guid orderId, Guid shipperId, double lat, double lng);
        TrackingLocation? GetLatestLocationByOrder(Guid orderId);
    }
}
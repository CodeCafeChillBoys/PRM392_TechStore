using System;
using TechStore.Domain.DTOs;

namespace TechStore.Service.IService
{
    public interface ITrackingService
    {
        void UpdateLocation(Guid shipperId, double lat, double lng);
        TrackingLocation? GetLatestLocation(Guid shipperId);
    }
}
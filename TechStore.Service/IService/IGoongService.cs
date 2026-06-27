using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Service.IService
{
    public interface IGoongService
    {
        public Task<string> GetRouteAsync(
        double originLat,
        double originLng,
        double destLat,
        double destLng);

    }
}
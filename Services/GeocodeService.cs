using Geocode.Models;
using Geocode.Interfaces;
using Geocode.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Geocode.Services
{
    public class GeocodeService : IGeocode
    {
        readonly IServiceScopeFactory<GeoDataContext> _context;
        private ILogger<GeocodeService> _log;
        public GeocodeService(IServiceScopeFactory<GeoDataContext> context, ILogger<GeocodeService> log)
        {
            _context = context;
            _log = log;
        }
        public async Task<string> GetStateByZip(int zipcode)
        {
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();
            _log.LogInformation("Attempting to get required service at GetStateByZip");

            var state = await db.GeoData.Where(x => x.Zip == zipcode).Select(x => x.StateName).FirstOrDefaultAsync();

            return state;
        }

        public async Task<IEnumerable<StateResponse>> GetStates()
        {
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();
            _log.LogInformation("Attempting to get required service at GetStateByZip");

            var states = db.GeoData.Select(x => new StateResponse() { StateName = x.StateName, StateId = x.StateId }).Distinct();

            return states.ToList();
        }

        public async Task<GeocodeLookupResponse> KeywordLookup(string Keyword)
        {
            try
            {
                using var scope = _context.CreateScope();
                _log.LogInformation("Attempting to get required service at KeywordLookup");
                var db = scope.GetRequiredService();
                int keywordInt = 0;
                int.TryParse(Keyword, out keywordInt);

                var data = await db.GeoData
                   .Where(x => x.City.Contains(Keyword) ||
                     x.StateName == Keyword ||
                     x.CountyName == Keyword ||
                     x.Zip == keywordInt)
                   .Take(10)
                   .ToListAsync();
                return new GeocodeLookupResponse()
                {
                    Data = data,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new GeocodeLookupResponse()
                {
                    Data = null,
                    Success = false
                };
            }
            
        }

        public async Task<GeocodeLookupResponse> LatLongLookup(double lat, double lng)
        {
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();
            
            //consider redis for this
            var cites = await db.GeoData
               .Select(x => new { Id = x.Id, Lat = x.Lat, Lng = x.Lng })
               .ToListAsync();

            var found = cites.Where(x => ArePointsNear(lat, lng, x.Lat, x.Lng, 4)).Take(10);

            var data = await db.GeoData
               .Where(x => found.Select(y => y.Id).Contains(x.Id))
               .ToListAsync();
            return new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };
        }

        private bool ArePointsNear(double lat, double lng, double db_lat, double db_lng, int miles)
        {
            var km = miles * 1.609344;
            var ky = 40000 / 360;
            var kx = Math.Cos(Math.PI * lat / 180.0) * ky;
            var dx = Math.Abs(lng - db_lng) * kx;
            var dy = Math.Abs(lat - db_lat) * ky;
            return Math.Sqrt(dx * dx + dy * dy) <= km;
        }

        public async Task<GeocodeLookupResponse> ZipcodeLookup(int zipcode)
        {
            using var scope = _context.CreateScope();
            _log.LogInformation("Attempting to get required service at ZipcodeLookup");
            var db = scope.GetRequiredService();

            var data = await db.GeoData.Where(x => x.Zip == zipcode).ToListAsync();
            return new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };
        }

        public async Task<GeocodeLookupResponse> StatecodeLookup(string stateLookup)
        {
            using var scope = _context.CreateScope();
            _log.LogInformation("Attempting to get required service at ZipcodeLookup");
            var db = scope.GetRequiredService();

            var data = await db.GeoData.Where(x => x.StateId == stateLookup).ToListAsync();
            return new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };
        }

        public async Task<GeocodeLookupResponse> GetAllGeoData()
        {
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();
            var data = await db.GeoData
                .Select(x=> new GeoData()
                {
                    Id = x.Id,
                    Lat = x.Lat,
                    Lng = x.Lng,
                    City = x.City,
                    StateId = x.StateId,
                    Zip = x.Zip,
                    StateName = x.StateName
                })
                .ToListAsync();

            return new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };
        }
    }
}

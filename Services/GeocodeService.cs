using Geocode.Models;
using Geocode.Interfaces;
using Geocode.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Geocode.Services
{
    public class GeocodeService : IGeocode
    {
        readonly IServiceScopeFactory<GeoDataContext> _context;
        private ILogger<GeocodeService> _log;
        private readonly IDistributedCache _cache;
        public GeocodeService(IServiceScopeFactory<GeoDataContext> context, ILogger<GeocodeService> log, IDistributedCache cache)
        {
            _context = context;
            _log = log;
            _cache = cache;
        }
        public async Task<string> GetStateByZip(int zipcode)
        {
            _log.LogInformation("Attempting to get state by zipcode at GetStateByZip {@zipcode}", zipcode);

            var cacheKey = $"GetStateByZip_{zipcode}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return cached;
            }
            
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();
            _log.LogInformation("Attempting to get required service at GetStateByZip");

            var state = await db.GeoData.Where(x => x.Zip == zipcode).Select(x => x.StateName).FirstOrDefaultAsync();

            _cache.SetString(cacheKey, state);

            return state;
        }

        public async Task<IEnumerable<StateResponse>> GetStates()
        {
            _log.LogInformation("Attempting to get all states");

            var cacheKey = "GetStates";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonConvert.DeserializeObject<IEnumerable<StateResponse>>(cached);
            }

            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();
            _log.LogInformation("Attempting to get all states");

            var states = db.GeoData.Select(x => new StateResponse() { StateName = x.StateName, StateId = x.StateId }).Distinct();

            var r = states.ToList();

            _cache.SetString(cacheKey, JsonConvert.SerializeObject(r));

            return r;
        }

        public async Task<GeocodeLookupResponse> KeywordLookup(string keyword)
        {
            try
            {
                _log.LogInformation("getting KeywordLookup_{@Keyword}", keyword);

                var cacheKey = $"KeywordLookup_{keyword}";

                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    return JsonConvert.DeserializeObject<GeocodeLookupResponse>(cached);
                }

                using var scope = _context.CreateScope();
                _log.LogInformation("Attempting to get required service at KeywordLookup");
                var db = scope.GetRequiredService();
                int keywordInt = 0;
                int.TryParse(keyword, out keywordInt);

                var data = await db.GeoData
                   .Where(x => x.City.Contains(keyword) ||
                     x.StateName == keyword ||
                     x.CountyName == keyword ||
                     x.Zip == keywordInt)
                   .Take(10)
                   .ToListAsync();
                var r = new GeocodeLookupResponse()
                {
                    Data = data,
                    Success = true
                };

                _cache.SetString(cacheKey, JsonConvert.SerializeObject(r));
                return r;
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
            _log.LogInformation("getting LatLongLookup_{lat}_{lng}");

            var cacheKey = $"LatLongLookup_{lat}_{lng}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonConvert.DeserializeObject<GeocodeLookupResponse>(cached);
            }

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
            var r = new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };

            _cache.SetString(cacheKey, JsonConvert.SerializeObject(r));
            return r;
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
            var cacheKey = $"ZipcodeLookup_{zipcode}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonConvert.DeserializeObject<GeocodeLookupResponse>(cached);
            }

            using var scope = _context.CreateScope();
            _log.LogInformation("Attempting to get required service at ZipcodeLookup");
            var db = scope.GetRequiredService();

            var data = await db.GeoData.Where(x => x.Zip == zipcode).ToListAsync();
            var r = new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };

            _cache.SetString(cacheKey, JsonConvert.SerializeObject(r));

            return r;
        }

        public async Task<GeocodeLookupResponse> StatecodeLookup(string stateLookup)
        {
            var cacheKey = $"StatecodeLookup_{stateLookup}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonConvert.DeserializeObject<GeocodeLookupResponse>(cached);
            }

            using var scope = _context.CreateScope();
            _log.LogInformation("Attempting to get required service at StatecodeLookup");
            var db = scope.GetRequiredService();

            var data = await db.GeoData.Where(x => x.StateId == stateLookup).ToListAsync();
            var r = new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };

            _cache.SetString(cacheKey, JsonConvert.SerializeObject(r));
            return r;
        }

        //TODO: WE SHOULD NEVER RETURN ALL DATA FROM A TABLE
        public async Task<GeocodeLookupResponse> GetAllGeoData()
        {
            _log.LogInformation("Attempting to all data from entire geocode database at GetAllGeoData");
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

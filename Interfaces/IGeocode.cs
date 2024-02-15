using Geocode.Models;

namespace Geocode.Interfaces
{
    public interface IGeocode
    {
        Task<GeocodeLookupResponse> KeywordLookup(string Keyword);
        Task<GeocodeLookupResponse> ZipcodeLookup(int zipcode);
        Task<string> GetStateByZip(int zipcode);
        Task<GeocodeLookupResponse> LatLongLookup(double lat, double lng);
        Task<GeocodeLookupResponse> GetAllGeoData();

    }
}

namespace Geocode.Models
{
    public class GeocodeLookupResponse
    {
        public List<GeoData> Data { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class StateResponse
    {
        public string StateId { get; set; }
        public string StateName { get; set; }
    }
}
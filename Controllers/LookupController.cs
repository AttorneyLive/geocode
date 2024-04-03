using Azure.Core;
using Geocode.Interfaces;
using Geocode.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Emit;


namespace Geocode.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    [ApiController]
    public class LookupController : ControllerBase
    {
        private readonly IGeocode _geocode;
        readonly ILogger<LookupController> _log;

        public LookupController(IGeocode geocode, ILogger<LookupController> log)
        {
            _geocode = geocode;
            _log = log;
        }

        /// <summary>
        /// Returns all data matching provided zipcode
        /// </summary>
        /// <param name="zipcode"></param>
        /// <returns></returns>
        [HttpGet("Zipcode")]
        public async Task<GeocodeLookupResponse> ZipcodeLookup(int zipcode)
        {
            _log.LogInformation("ZipcodeLookup attempting for zipcode: {@ZipCode}", zipcode);
            return await _geocode.ZipcodeLookup(zipcode);
        }

        /// <summary>
        /// Returns all data matching provided keyword
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [HttpGet("Keyword")]
        public async Task<GeocodeLookupResponse> KeywordLookup(string keyword)
        {
            _log.LogInformation("KeywordLookup attempting for keyword: {@KeyWord}", keyword);
            return await _geocode.KeywordLookup(keyword);
        }

        /// <summary>
        /// Returns all data matching provided keyword
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [HttpGet("LatLong")]
        public async Task<GeocodeLookupResponse> LatLongLookup(double lat, double lng)
        {
            return await _geocode.LatLongLookup(lat, lng);
        }

        [HttpGet("All")]
        public async Task<GeocodeLookupResponse> GetAllGeoCodeData()
        {
            return await _geocode.GetAllGeoData();
        }

        [HttpGet("States")]
        public async Task<IEnumerable<string>> GetAllStates()
        {
            return await _geocode.GetStates();
        }
    }
}

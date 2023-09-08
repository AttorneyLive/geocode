﻿using Geocode.Models;
using Geocode.Interfaces;
using Geocode.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Geocode.Services
{
    public class Geocode : IGeocode
    {
        readonly IServiceScopeFactory<GeoDataContext> _context;
        public Geocode(IServiceScopeFactory<GeoDataContext> context)
        {
            _context = context;
        }
        public async Task<string> GetStateByZip(int zipcode)
        {
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();

            var state = await db.GeoData.Where(x => x.Zip == zipcode).Select(x => x.StateName).FirstOrDefaultAsync();

            return state;
        }

        public async Task<GeocodeLookupResponse> KeywordLookup(string Keyword)
        {
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();

            var data = await db.GeoData
                .Where(x => x.City == Keyword)
                .Where(x => x.StateName == Keyword)
                .Where(x => x.CountyName == Keyword)
                .Where(x => x.Zip.ToString() == Keyword)
                .ToListAsync();
            return new GeocodeLookupResponse()
            {
                Data = data,
                Success = true
            };
        }

        public async Task<GeocodeLookupResponse> ZipcodeLookup(int zipcode)
        {
            using var scope = _context.CreateScope();
            var db = scope.GetRequiredService();

            var data = await db.GeoData.Where(x => x.Zip == zipcode).ToListAsync();
            return new GeocodeLookupResponse()
            {
               Data = data,
               Success = true
            };
        }
    }
}

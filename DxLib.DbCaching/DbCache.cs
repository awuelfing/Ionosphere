﻿using DXLib.HamQTH;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DxLib.DbCaching
{
    public class DbCache : QthLookup
    {
        private readonly DbCacheOptions _options;
        private MongoClientSettings? _settings;
        private MongoClient? _mongoClient;
        private IMongoDatabase? _mongoDatabase;
        private IMongoCollection<HamQTHResult>? _mongoCollection;
        private bool initialized = false;
        public DbCache(IOptions<DbCacheOptions> options)
        {
            _options = options.Value;
        }
        static DbCache()
        {
            BsonClassMap.RegisterClassMap<HamQTHResult>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }
        public void Initialize() // moved this stuff out of the constructor
        {
            _settings = MongoClientSettings.FromConnectionString(_options.ConnectionString);
            _settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };
            _mongoClient = new MongoClient(_settings);

            _mongoDatabase = _mongoClient.GetDatabase(_options.Database);
            _mongoCollection = _mongoDatabase.GetCollection<HamQTHResult>(_options.Collection);
            initialized= true;
        }

        public async Task StoreResult(HamQTHResult result)
        {
            if (!this.initialized)
            {
                this.Initialize();
            }
            await _mongoCollection!.InsertOneAsync(result);

            return;
        }

        public override async Task<HamQTHResult?> GetGeo(string callsign)
        {
            if (!this.initialized)
            {
                this.Initialize();
            }

            DateTime start = DateTime.UtcNow;
            var filter = Builders<HamQTHResult>.Filter.Eq("callsign", callsign.ToUpper());
            var results = await _mongoCollection.FindAsync(filter);
            var result = await results.FirstOrDefaultAsync();
            Debug.WriteLine($"DB cache checked in {(start - DateTime.Now).TotalMilliseconds}ms");

            if (result != null)
            {
                Debug.WriteLine("DB cache hit");
                return result;
            }

            if (base.Lower == null)
            {
                throw new Exception("GeoGeo() - no match and no lower configuration available");
            }
            result = await base.Lower.GetGeo(callsign);

            if(result != null)
            {
                start= DateTime.Now;
                await StoreResult(result);
                Debug.WriteLine($"Stored result from lower provider in {(DateTime.Now - start).TotalMilliseconds}ms");
                return result;
            }

            return result;
        }
    }
}
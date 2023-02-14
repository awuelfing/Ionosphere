﻿using DXLib.RBN;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLib.DbCaching
{
    public class DbSpots : DbCommon<Spot>
    {
        public DbSpots(IOptions<DbCacheOptions> options) : base(options)
        {
        }

        public DbSpots(DbCacheOptions options) : base(options)
        {
        }

    }
}
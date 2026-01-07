using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FNS.Models;

namespace FNS.Repository
{
    public interface IImprove
    {
        public bool SaveHealthInfoAsync(JsonElement healthInfo, string email);
    }
}
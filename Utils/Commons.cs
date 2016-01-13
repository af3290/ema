using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Commons
    {
        public static void SaveFileAsJson(string filePath, object data)
        {
            var settings = new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.All
            };
            var jsonsx = JsonConvert.SerializeObject(data, settings);
            File.WriteAllText(filePath, jsonsx);
        }
    }
}

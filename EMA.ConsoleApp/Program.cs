using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.WeatherUnderground;

namespace EMA.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Read();

            Console.ReadLine();            
        }

        static void Read()
        {
            var all = new List<WeatherObservation>();

            var from = new DateTime(2005, 4, 2);
            for (int i = 0; i < 11; i++)
            {
                var fromT = from.AddYears(i).ToString("yyyy-MM-dd");
                var data = File.ReadAllText(string.Format(@"D:\Projects\GitHub\ema\Data\WeatherJapan-{0}.json", fromT));
                var observations = JsonConvert.DeserializeObject<List<WeatherObservation>>(data);
                all.AddRange(observations);
            }

            //write to a huge CSV file
            var lines = all.Select(o=>o.ToString()).ToArray();
            File.WriteAllLines(@"D:\Projects\GitHub\ema\Data\WeatherJapan.csv", lines);
        }
        static void Download()
        {
            var from = new DateTime(2005, 4, 2);
            var to = new DateTime(2016, 4, 2);

            //multithreaded runner to download data faster...
            for (int i = 0; i < 11; i++)
            {
                var f = i == 0 ? from : from.AddYears(i);
                var t = i == 10 ? to : from.AddYears(i + 1).AddDays(-1);

                //var task = new Task(() => Run(f, t));
                //task.Start();
            }
        }
        static void Run(DateTime from, DateTime to)
        {
            var fromT = from.ToString("yyyy-MM-dd");
            Console.WriteLine("Starting {0} -> {1}", from, to);

            var wu = new Utils.WeatherUnderground.Downloader();
            var data = wu.WeatherData("Tokyo", from, to);

            var jsonsx = JsonConvert.SerializeObject(data);
            
            File.WriteAllText(string.Format(@"D:\Projects\GitHub\ema\Data\WeatherJapan-{0}.json", fromT), jsonsx);

            Console.WriteLine("Done year");
        }
    }
}

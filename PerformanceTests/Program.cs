using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;


namespace PerformanceTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var startDT = DateTime.UtcNow;

            IConfigurationRoot configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();

            string cfgADLSaccountName = configuration.GetValue<string>("ADLSaccountName");
            string cfgADLSaccountKey = configuration.GetValue<string>("ADLSaccountKey");
            string cfgADLSfileSystemName = configuration.GetValue<string>("ADLSfileSystemName");
            string cfgADLSworkingFolder = configuration.GetValue<string>("ADLSworkingFolder");

            var adlsListing = new ADLSListing(cfgADLSaccountName, cfgADLSfileSystemName, cfgADLSaccountKey);

            string path = cfgADLSworkingFolder + "/FilesAndFolders";

            //var items1 = adlsListing.ListSimple(path);  // fast
            //Console.WriteLine(items1.Count);

            //var items2 = await adlsListing.ListAsync(path);   // fast
            //Console.WriteLine(items2.Count);

            //var items3 = adlsListing.SimpleRecursive(path);     // very slow
            //Console.WriteLine(items3.Count);

            //var items4 = await adlsListing.RecursiveParallel(path);     // slow
            //Console.WriteLine(items4.Count);

            //var items5 = adlsListing.PartiallyRecursive(path, 0);   // fast
            //Console.WriteLine(items5.Count);

            Console.WriteLine(DateTime.UtcNow.Subtract(startDT).TotalSeconds);
        }
    }
}

using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace TestsDataProducer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddEnvironmentVariables()
                //.AddCommandLine(args)
                .Build();




            if (args.Length > 0)
            {
                if (args[0] == "FilesAndFolders") await FilesAndFolders.Run(configuration, 10, 100, 3);
            }

            await Task.CompletedTask;
        }
    }
}

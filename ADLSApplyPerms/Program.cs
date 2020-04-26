using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;


namespace ADLSApplyPerms
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Wrong number of parameters. Expected 1.");
                return;
            }

            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(args[0]));

            Console.WriteLine(new string('-', 60));
            Console.WriteLine(String.Join(Environment.NewLine, config.DumpToStringArray()));
            Console.WriteLine(new string('-', 60));

            new PermsApply(config,
                          (s) => Console.WriteLine($"{DateTime.UtcNow.ToString("o")} > {s}"))
                          .Run();
        }
    }
}

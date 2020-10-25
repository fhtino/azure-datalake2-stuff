using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PerformanceTests
{
    public class ADLSListing
    {

        private DataLakeServiceClient _serviceClient;
        private DataLakeFileSystemClient _fileSystemClient;


        public ADLSListing(string accountName, string fileSystem, string accountKey)
        {
            var sskc = new StorageSharedKeyCredential(accountName, accountKey);
            _serviceClient = new DataLakeServiceClient(new Uri("https://" + accountName + ".dfs.core.windows.net"), sskc);
            _fileSystemClient = _serviceClient.GetFileSystemClient(fileSystem);

            //DataLakeClientBuilderExtensions/
            //new DataLakeClientOptions() { Diagnostics = new Azure.Core.DiagnosticsOptions() { IsLoggingEnabled=true } }
        }


        public List<PathItem> ListSimple(string path)
        {
            var items = _fileSystemClient.GetPaths(path, true).ToList();
            return items;
        }


        public async Task<List<PathItem>> ListAsync(string path)
        {
            var outputList = new List<PathItem>();

            IAsyncEnumerator<PathItem> enumerator = _fileSystemClient.GetPathsAsync(path, true).GetAsyncEnumerator();

            while (await enumerator.MoveNextAsync())
            {
                PathItem item = enumerator.Current;
                outputList.Add(item);
            }

            return outputList;
        }


        public List<PathItem> SimpleRecursive(string path)
        {
            Console.WriteLine(path);

            var outputList = new List<PathItem>();

            var items = _fileSystemClient.GetPaths(path).ToList();
            outputList.AddRange(items);

            foreach (var subFolder in items.Where(x => x.IsDirectory == true))
            {
                outputList.AddRange(SimpleRecursive(subFolder.Name));
            }

            return outputList;
        }


        public async Task<List<PathItem>> RecursiveParallel(string path)
        {
            Console.WriteLine(path);
            await Task.Delay(1);

            var outputList = new List<PathItem>();

            var items = _fileSystemClient.GetPaths(path).ToList();
            outputList.AddRange(items);

            var tasks = items.Where(x => x.IsDirectory == true).Select(x => RecursiveParallel(x.Name));
            await Task.WhenAll(tasks);
            foreach (var task in tasks)
            {
                outputList.AddRange(task.Result);
            }

            return outputList;
        }


        public async Task RecursicePartiallyParallel()
        {
            await Task.CompletedTask;
        }



        public List<PathItem> PartiallyRecursive(string path, int parallelLevels)
        {
            Console.WriteLine($"{DateTime.UtcNow.ToString("o")} : {parallelLevels} : {path}");

            object listLocker = new object();
            List<PathItem> items = _fileSystemClient.GetPaths(path, false).ToList();
            var subDirList = items.Where(x => x.IsDirectory.HasValue && x.IsDirectory.Value == true).ToList();

            if (parallelLevels > 0)
            {
                Parallel.ForEach(
                    subDirList,
                    //new ParallelOptions() { MaxDegreeOfParallelism = 8},
                    (subDir) =>
                    {
                        Console.WriteLine($"{DateTime.UtcNow.ToString("o")} : {parallelLevels} : PAR --> {subDir.Name}");
                        var subItems = PartiallyRecursive(subDir.Name, parallelLevels - 1);
                        lock (listLocker)
                        {
                            items.AddRange(subItems);
                        }
                    });
            }
            else
            {
                foreach (var subDir in subDirList)
                {
                    //Console.WriteLine($"{DateTime.UtcNow.ToString("o")} : {parallelLevels} : SEQ --> {subDir.Name}");
                    Console.Write(".");
                    items.AddRange(_fileSystemClient.GetPaths(subDir.Name, true));
                }
            }

            return items;
        }

    }
}

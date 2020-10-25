using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestsDataProducer
{

    internal class FilesAndFolders
    {
        private static int _foldersPerFolder;
        private static int _filesPerFolder;
        private static int _levels;
        private static int _fileCounter;
        private static int _folderCounter;



        public static async Task Run(IConfigurationRoot configuration, int foldersPerFolder, int filesPerFolder, int levels)
        {
            _foldersPerFolder = foldersPerFolder;
            _filesPerFolder = filesPerFolder;
            _levels = levels;

            string cfgADLSaccountName = configuration.GetValue<string>("ADLSaccountName");
            string cfgADLSaccountKey = configuration.GetValue<string>("ADLSaccountKey");
            string cfgADLSfileSystemName = configuration.GetValue<string>("ADLSfileSystemName");
            string cfgADLSworkingFolder = configuration.GetValue<string>("ADLSworkingFolder");

            var ADLSBaseURL = "https://" + cfgADLSaccountName + ".dfs.core.windows.net";
            var ADLSClient = new DataLakeServiceClient(new Uri(ADLSBaseURL), new StorageSharedKeyCredential(cfgADLSaccountName, cfgADLSaccountKey));
            var ADLSFileSystemClient = ADLSClient.GetFileSystemClient(cfgADLSfileSystemName);
            var ADLSDirectoryClient = ADLSFileSystemClient.GetDirectoryClient(cfgADLSworkingFolder + "/FilesAndFolders");
            ADLSDirectoryClient.Create();


            _fileCounter = 0;
            _folderCounter = 0;
            await CreateFoldersAndFiles(ADLSFileSystemClient, cfgADLSworkingFolder + "/FilesAndFolders", levels);

            Console.WriteLine($"_folderCounter={_folderCounter} _fileCounter={_fileCounter}");
        }


        private static async Task CreateFoldersAndFiles(DataLakeFileSystemClient ADLSFileSystemClient, string workingPath, int level)
        {
            Console.WriteLine("START-folder: " + workingPath);

            // Unclear behavior: when files already exists, only ADLSFileSystemClient.GetPaths is called. 
            // This is sync and the tasks do not parallelize. But if I add Task.Delay() then tasks run in parallel as expected. 
            // ... investigate...
            await Task.Delay(1);

            var ADLSDirectoryClient = ADLSFileSystemClient.GetDirectoryClient(workingPath);
            List<string> existingItems = ADLSFileSystemClient.GetPaths(workingPath, recursive: false).Select(x => x.Name).ToList();

            // create files (if required)
            for (int i = 0; i < _filesPerFolder; i++)
            {
                Interlocked.Increment(ref _fileCounter);
                var adlsFileClient = ADLSDirectoryClient.GetFileClient(i.ToString("0000") + ".txt");
                if (!existingItems.Contains(adlsFileClient.Path))
                {
                    await adlsFileClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes("fake data " + DateTime.UtcNow.ToString("O"))), true);
                    Console.Write(".");
                }
            }

            // create sub-folders
            if (level > 0)
            {
                List<string> subFolders = Enumerable.Range(0, 10).Select(i => workingPath + "/" + i.ToString("000")).ToList();
                foreach (var item in subFolders)
                {
                    Interlocked.Increment(ref _folderCounter);
                    if (!existingItems.Contains(item))
                    {
                        ADLSFileSystemClient.GetDirectoryClient(item).Create();
                    }
                }

                // start the sub-tasks and let them run in parallel
                var tasks = subFolders.Select(subFolder => CreateFoldersAndFiles(ADLSFileSystemClient, subFolder, level - 1));
                await Task.WhenAll(tasks);
            }

            Console.WriteLine("END-folder: " + workingPath);
        }

    }

}

using Azure.Storage;
using Azure.Storage.Files.DataLake;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake.Models;
using System.Threading;


namespace ADLSApplyPerms
{

    public class PermsApply
    {
        private Config _cfg;
        private int _counterAll;
        private int _counterUpdated;

        private List<PathAccessControlItem> _newACL;


        public Action<string> Log { get; set; }
        public int CounterAll { get { return _counterAll; } }
        public int CounterUpdated { get { return _counterUpdated; } }


        public PermsApply(Config cfg, Action<string> log = null)
        {
            _cfg = cfg;
            Log = log;

            if (_cfg.ACL == null) _cfg.ACL = new string[0];
            if (_cfg.RemoveList == null) _cfg.RemoveList = new string[0];
            if (_cfg.ExitAfter == -1) _cfg.ExitAfter = null;

            _newACL = _cfg.ACL.Select(x => PathAccessControlItem.Parse(x)).ToList();
        }


        public void Run()
        {
            Log?.Invoke("ACL:");
            _newACL.ForEach((item) => Log?.Invoke($" - {item.ToString()}"));

            var serviceClient = new DataLakeServiceClient(
                  new Uri($"https://{_cfg.AccountName}.dfs.core.windows.net"),
                  new StorageSharedKeyCredential(_cfg.AccountName, _cfg.AccountKey));

            var fileSystemClient = serviceClient.GetFileSystemClient(_cfg.FileSystem);

            Log?.Invoke("ADLS listing items...");
            var allItems = fileSystemClient.GetPaths(_cfg.StartingPath, true).ToList();
            Log?.Invoke($"Items count: {allItems.Count}");


            var startDT = DateTime.UtcNow;
            _counterAll = 0;
            _counterUpdated = 0;

            Parallel.ForEach(
                allItems,
                new ParallelOptions() { MaxDegreeOfParallelism = _cfg.Parallelism },
                (pathItem, state) =>
                {
                    try
                    {
                        var a = pathItem;
                        Interlocked.Increment(ref _counterAll);
                        if (_cfg.LogVerbose)
                            Log?.Invoke($"Processing {pathItem.Name} {(pathItem.IsDirectory.Value ? " [D]" : "")}");

                        DataLakePathClient itemClient =
                          a.IsDirectory.Value ?
                          (DataLakePathClient)fileSystemClient.GetDirectoryClient(pathItem.Name) :
                          (DataLakePathClient)fileSystemClient.GetFileClient(pathItem.Name);

                        bool updated = ApplyACL(itemClient);  // <<<===

                        if (updated)
                            Interlocked.Increment(ref _counterUpdated);

                        if (_counterAll % 10 == 0)
                            Log?.Invoke($"Progress: {_counterAll}/{allItems.Count} - Updated: {_counterUpdated}");

                        if (_cfg.ExitAfter.HasValue && _counterAll > _cfg.ExitAfter)
                            state.Break();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"******* ITEM: {pathItem.Name} - EXCEPTION: {ex.ToString()}");
                        throw;
                    }
                });


            Log?.Invoke($"Elapsed time: {DateTime.UtcNow.Subtract(startDT).TotalSeconds}");
            Log?.Invoke($"counterAll={_counterAll} counterUpdated={_counterUpdated}");
        }


        private bool ApplyACL(DataLakePathClient itemClient)
        {
            PathAccessControl pac = itemClient.GetAccessControl(true).Value;
            var fileACLList = pac.AccessControlList.ToList();

            if (_cfg.LogVerbose)
            {
                Log?.Invoke($" - ACL before:");
                fileACLList.ForEach((item) => Log?.Invoke($"   - {item.ToString()}"));
            }

            bool changed = false;

            // new/updated permissions
            foreach (var newACLItem in _newACL)
            {
                var existingACLItem = fileACLList.SingleOrDefault(x => x.AccessControlType == newACLItem.AccessControlType &&
                                                                       x.EntityId == newACLItem.EntityId &&
                                                                       x.DefaultScope == newACLItem.DefaultScope);

                if (existingACLItem == null)
                {
                    if (!(newACLItem.DefaultScope && itemClient is DataLakeFileClient))
                    {
                        fileACLList.Add(newACLItem);
                        changed = true;
                    }
                }
                else
                {
                    if (existingACLItem.Permissions != newACLItem.Permissions)
                    {
                        existingACLItem.Permissions = newACLItem.Permissions;
                        changed = true;
                    }
                }
            }

            // remove users/groups
            var fileACLList2 = fileACLList.Where(x => !_cfg.RemoveList.Contains(x.EntityId)).ToList();
            if (fileACLList.Count() != fileACLList2.Count())
                changed = true;

            // apply
            if (changed)
            {
                itemClient.SetAccessControlList(fileACLList2);
            }

            if (_cfg.LogVerbose)
            {
                Log?.Invoke($" - ACL after:");
                fileACLList.ForEach((item) => Log?.Invoke($"   - {item.ToString()}"));
            }

            return changed;
        }

    }

}


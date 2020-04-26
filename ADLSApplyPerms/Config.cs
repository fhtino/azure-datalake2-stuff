using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace ADLSApplyPerms
{
    public class Config
    {
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string FileSystem { get; set; }
        public string StartingPath { get; set; }
        public int? ExitAfter { get; set; }
        public int Parallelism { get; set; }
        public string[] ACL { get; set; }
        public string[] RemoveList { get; set; }
        public bool LogVerbose { get; set; }


        public string[] DumpToStringArray()
        {
            var list = new List<string>();
            list.Add($"AccountName={AccountName}");
            list.Add($"AccountKey={AccountKey}");
            list.Add($"FileSystem={FileSystem}");
            list.Add($"StartingPath={StartingPath}");
            list.Add($"ExitAfter={ExitAfter}");
            list.Add($"Parallelism={Parallelism}");
            list.Add($"ACL:");
            ACL?.ToList().ForEach(x => list.Add($" - {x}"));
            list.Add($"RemoveList:");
            RemoveList?.ToList().ForEach(x => list.Add($" - {x}"));
            return list.ToArray();
        }
    }
}

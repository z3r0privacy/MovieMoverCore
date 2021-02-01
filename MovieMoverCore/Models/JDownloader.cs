using MovieMoverCore.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Security.Principal;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{

    public class JD_Error
    {
        [JsonPropertyName("src")]
        public string Source { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }
    public class JD_LoginResponse
    {
        [JsonPropertyName("sessiontoken")]
        public string SessionToken { get; set; }
        [JsonPropertyName("regaintoken")]
        public string RegainToken { get; set; }
        [JsonPropertyName("rid")]
        public int Rid { get; set; }
    }

    public class JD_ListDevice_Response
    {
        [JsonPropertyName("rid")]
        public int Rid { get; set; }
        [JsonPropertyName("list")]
        public List<JD_Device> Devices { get; set; }
    }

    public class JD_Device
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class JD_Request
    {
        [JsonPropertyName("apiVer")]
        public int ApiVer => 1;
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("rid")]
        public int Rid { get; set; }
        [JsonPropertyName("params")]
        public List<string> Params { get; set; }

        public JD_Request()
        {
            Params = new List<string>();
        }
    }

    public class JD_Response<TData>
    {
        [JsonPropertyName("rid")]
        public int Rid { get; set; }
        [JsonPropertyName("data")]
        public TData Data { get; set; }
    }

    public class JD_AddLinks_Request_Params
    {
        [JsonPropertyName("assignJobID")]
        public bool AssignJobId { get; set; } = true;
        [JsonPropertyName("autoExtract")]
        public bool AutoExtract { get; set; } = true;
        [JsonPropertyName("autostart")]
        public bool AutoStart { get; set; } = false;
        [JsonPropertyName("dataURLs")]
        public string[] DataURLs { get; set; }
        [JsonPropertyName("deepDecrypt")]
        public bool DeepDecrypt { get; set; }
        [JsonPropertyName("destinationFolder")]
        public string DestinationFolder { get; set; }
        [JsonPropertyName("downloadPassword")]
        public string DownloadPassword { get; set; }
        [JsonPropertyName("extractPassword")]
        public string ExtractPassword { get; set; }
        [JsonPropertyName("links")]
        public string Links { get; set; }
        [JsonPropertyName("overwritePackagizerRules")]
        public bool OverwritePackagizerRules { get; set; } = true;
        [JsonPropertyName("packageName")]
        public string PackageName { get; set; }
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "DEFAULT";
        [JsonPropertyName("sourceUrl")]
        public string SourceUrl { get; set; }

        public JD_AddLinks_Request_Params(string packageName, List<string> links)
        {
            PackageName = packageName;
            Links = links.Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
        }
    }

    public class JD_AddLinks_Response
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class JD_QueryDevices_Request_Params
    {
        //    myPackageQuery = 
        //              {
        //                "bytesLoaded"  = (boolean),
        //                "bytesTotal"   = (boolean),
        //                "childCount"   = (boolean),
        //                "comment"      = (boolean),
        //                "enabled"      = (boolean),
        //                "eta"          = (boolean),
        //                "finished"     = (boolean),
        //                "hosts"        = (boolean),
        //                "maxResults"   = (int),
        //                "packageUUIDs" = (long[]),
        //                "priority"     = (boolean),
        //                "running"      = (boolean),
        //                "saveTo"       = (boolean),
        //                "speed"        = (boolean),
        //                "startAt"      = (int),
        //                "status"       = (boolean)
        //}

        [JsonPropertyName("bytesLoaded")]
        public bool BytesLoaded { get; set; } = true;
        [JsonPropertyName("bytesTotal")]
        public bool BytesTotal { get; set; } = true;
        [JsonPropertyName("childCount")]
        public bool ChildCount { get; set; } = true;
        [JsonPropertyName("comment")]
        public bool Comment { get; set; } = true;
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        [JsonPropertyName("eta")]
        public bool Eta { get; set; } = true;
        [JsonPropertyName("finished")]
        public bool Finished { get; set; } = true;
        [JsonPropertyName("hosts")]
        public bool Hosts { get; set; } = true;
        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; } = int.MaxValue;
        [JsonPropertyName("packageUUIDs")]
        public long[] PackageUUIDs { get; set; }
        [JsonPropertyName("priority")]
        public bool Priority { get; set; } = true;
        [JsonPropertyName("running")]
        public bool Running { get; set; } = true;
        [JsonPropertyName("saveTo")]
        public bool SaveTo { get; set; } = true;
        [JsonPropertyName("speed")]
        public bool Speed { get; set; } = true;
        [JsonPropertyName("startAt")]
        public int StartAt { get; set; }
        [JsonPropertyName("status")]
        public bool Status { get; set; } = true;
    }

    public enum JD_Priority
    {
        [EnumMember(Value = "HIGHEST")]
        Highest,
        [EnumMember(Value = "HIGHER")]
        Higher,
        [EnumMember(Value = "HIGH")]
        High,
        [EnumMember(Value = "DEFAULT")]
        Default,
        [EnumMember(Value = "LOW")]
        Low,
        [EnumMember(Value = "LOWER")]
        Lower,
        [EnumMember(Value = "LOWEST")]
        Lowest
    }

    public enum JD_PackageState
    {
        Wait, Download, Decrypt, Extract, Finished, Error, Unknown
    }

    public class JD_FilePackage
    {
        [JsonPropertyName("activeTask")]
        public string ActiveTask { get; set; }
        [JsonPropertyName("bytesLoaded")]
        public long BytesLoaded { get; set; }
        [JsonPropertyName("bytesTotal")]
        public long BytesTotal { get; set; }
        [JsonPropertyName("childCount")]
        public int ChildCount { get; set; }
        [JsonPropertyName("comment")]
        public string Comment { get; set; }
        [JsonPropertyName("downloadPassword")]
        public string DownloadPassword { get; set; }
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        [JsonPropertyName("eta")]
        public long Eta { get; set; }
        [JsonPropertyName("finished")]
        public bool Finished { get; set; }
        [JsonPropertyName("hosts")]
        public string[] Hosts { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("priority")]
        [JsonConverter(typeof(JsonConverterNullableJDPriority))]
        public JD_Priority? Priority { get; set; }
        [JsonPropertyName("running")]
        public bool Running { get; set; }
        [JsonPropertyName("saveTo")]
        public string SaveTo { get; set; }
        [JsonPropertyName("speed")]
        public long Speed { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("statusIconKey")]
        public string StatusIconKey { get; set; }
        [JsonPropertyName("uuid")]
        public long UUID { get; set; }
        [JsonIgnore]
        public bool IsExtracting { get; set; }
        [JsonIgnore]
        public JD_PackageState PackageState
        {
            get
            {
                if (IsExtracting) return JD_PackageState.Extract;

                if (Finished) return JD_PackageState.Finished;

                if (Status == null && BytesLoaded == 0 && !Finished && !Running && Enabled) return JD_PackageState.Wait;

                if (Running && Enabled && !Finished && BytesLoaded > 0 && BytesLoaded < BytesTotal) return JD_PackageState.Download;

                var lowState = Status?.ToLower() ?? "";
                if (lowState.StartsWith("download")) return JD_PackageState.Download;
                if (lowState.StartsWith("decrypt")) return JD_PackageState.Decrypt;
                if (lowState.StartsWith("running")) return JD_PackageState.Download;
                if (lowState.StartsWith("invalid")) return JD_PackageState.Error;
                if (lowState.StartsWith("an error")) return JD_PackageState.Error;
                if (lowState.StartsWith("connection problem")) return JD_PackageState.Error;

                return JD_PackageState.Unknown;
            }
        }

        [JsonIgnore]
        public double DownloadPercentage => (100d * BytesLoaded / BytesTotal);
    }

    public enum JD_ControllerStatus
    {
        [EnumMember(Value ="RUNNING")]
        Running,
        [EnumMember(Value = "QUEUED")]
        Queued,
        [EnumMember(Value = "NA")]
        NA
    }

    public enum JD_ArchiveFileStatus
    {
        [EnumMember(Value = "COMPLETE")]
        Complete,
        [EnumMember(Value = "INCOMPLETE")]
        Incomplete,
        [EnumMember(Value = "MISSING")]
        Missing
    }

    public class JD_ArchiveStatus
    {
        [JsonPropertyName("archiveId")]
        public string ArchiveId { get; set; }
        [JsonPropertyName("archiveName")]
        public string ArchiveName { get; set; }
        [JsonPropertyName("controllerId")]
        public long ControllerId { get; set; }
        [JsonPropertyName("controllerStatus")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JD_ControllerStatus ControllerStatus { get; set; }
        [JsonPropertyName("states")]
        [JsonConverter(typeof(JsonConverterDictionaryEnum<JD_ArchiveFileStatus>))]
        public Dictionary<string, JD_ArchiveFileStatus> States { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class JD_QueryLinkCrawler_Request_Params
    {
        [JsonPropertyName("collectorInfo")]
        public bool CollectorInfo { get; set; } = true;
        [JsonPropertyName("jobIds")]
        public long[] JobIds { get; set; }

        public JD_QueryLinkCrawler_Request_Params(long id)
        {
            JobIds = new long[] { id };
        }
    }

    public class JD_JobLinkCrawler
    {
        [JsonPropertyName("broken")]
        public int Broken { get; set; }
        [JsonPropertyName("checking")]
        public bool Checking { get; set; }
        [JsonPropertyName("crawled")]
        public int Crawled { get; set; }
        [JsonPropertyName("crawlerId")]
        public long CrawlerId { get; set; }
        [JsonPropertyName("crawling")]
        public bool Crawling { get; set; }
        [JsonPropertyName("filtered")]
        public int Filtered { get; set; }
        [JsonPropertyName("jobId")]
        public long JobId { get; set; }
        [JsonPropertyName("unhandled")]
        public int Unhandled { get; set; }
    }

    public class JD_CrawledPackageQuery_Request_Params
    {
        [JsonPropertyName("availableOfflineCount")]
        public bool AvailableOfflineCount { get; set; } = true;
        [JsonPropertyName("availableOnlineCount")]
        public bool AvailableOnlineCount { get; set; } = true;
        [JsonPropertyName("availableTempUnknownCount")]
        public bool AvailableTempUnknownCount { get; set; } = true;
        [JsonPropertyName("availableUnknownCount")]
        public bool AvailblbeUnknownCount { get; set; } = true;
        [JsonPropertyName("bytesTotal")]
        public bool BytesTotal { get; set; } = true;
        [JsonPropertyName("childCount")]
        public bool ChildCount { get; set; }
        [JsonPropertyName("comment")]
        public bool Comment { get; set; }
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        [JsonPropertyName("hosts")]
        public bool Hosts { get; set; }
        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; } = int.MaxValue;
        [JsonPropertyName("packageUUIDs")]
        public long[] PackageUUIDs { get; set; }
        [JsonPropertyName("priority")]
        public bool Priority { get; set; }
        [JsonPropertyName("saveTo")]
        public bool SaveTo { get; set; }
        [JsonPropertyName("startAt")]
        public int StartAt { get; set; }
        [JsonPropertyName("status")]
        public bool Status { get; set; } = true;
    }

    public class JD_CrawledPackage
    {
        [JsonPropertyName("bytesTotal")]
        public long BytesTotal { get; set; }
        [JsonPropertyName("childCount")]
        public int ChildCount { get; set; }
        [JsonPropertyName("comment")]
        public string Comment { get; set; }
        [JsonPropertyName("downloadPassword")]
        public string DownloadPassword { get; set; }
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        [JsonPropertyName("hosts")]
        public string[] Hosts { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("offlineCount")]
        public int OfflineCount { get; set; }
        [JsonPropertyName("onlineCount")]
        public int OnlineCount { get; set; }
        [JsonPropertyName("priority")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JD_Priority Priority { get; set; }
        [JsonPropertyName("saveTo")]
        public string SaveTo { get; set; }
        [JsonPropertyName("tempUnknownCount")]
        public int TempUnknownCount { get; set; }
        [JsonPropertyName("unknownCount")]
        public int UnknownCount { get; set; }
        [JsonPropertyName("uuid")]
        public long UUID { get; set; }
    }
}

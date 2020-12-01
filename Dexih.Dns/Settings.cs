using Microsoft.Extensions.Logging;

namespace Dexih.Dns
{
    public class Settings
    {
        public AppSettingsSection AppSettings { get; set; } = new AppSettingsSection();
        public LoggingSection Logging { get; set; } = new LoggingSection();
    }
    
    public class AppSettingsSection
    {
        public string DnsTxtUrl { get; set; }
        public string RootIpAddress { get; set; }
        public string RootDomain { get; set; }
        public string[] DnsIpAddresses { get; set; }
        public string DnsEmail { get; set; }
        public int DnsTtl { get; set; } = 300;
        
        public string ListenIpAddress { get; set; }
        public int ListenPort { get; set; }
    }
    
    public class LoggingSection
    {
        public bool LogRequests { get; set; } = true;
        public bool LogErrors { get; set; } = true;

        public bool IncludeScopes { get; set; } = false;
        public LogLevelSection LogLevel { get; set; } = new LogLevelSection();
    }

    public class LogLevelSection
    {
        // [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel Default { get; set; } = LogLevel.Information;

        public LogLevel System { get; set; } = LogLevel.Information;

        public LogLevel Microsoft { get; set; } = LogLevel.Information;

    }

}
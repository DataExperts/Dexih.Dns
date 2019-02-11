using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dexih.Dns
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var txtUrl = Environment.GetEnvironmentVariable("DNS_TXT_URL");
            var rootIpAddress = Environment.GetEnvironmentVariable("ROOT_IP_ADDRESS");
            var rootDomain = Environment.GetEnvironmentVariable("ROOT_DOMAIN");
            var dnsIpAddresses = Environment.GetEnvironmentVariable("DNS_IP_ADDRESS")?.Split(',');
            var email = Environment.GetEnvironmentVariable("DNS_EMAIL");
            var timeStamp = long.Parse(DateTime.Now.ToString("yyyymmdd") + "00");
            var ttl = int.Parse(Environment.GetEnvironmentVariable("DNS_TTL")??"300");

            var logRequests = bool.Parse(Environment.GetEnvironmentVariable("LOG_REQUESTS")??"true");
            var logErrors = bool.Parse(Environment.GetEnvironmentVariable("LOG_ERRORS") ?? "true");

            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Information)
                .AddDebug(LogLevel.Trace);
            
            var logger = loggerFactory.CreateLogger("main");
            
            
            while(true)
            {
                try
                {
                    logger.Log(LogLevel.Information, "Starting the dexih dns server...");

                    var dns = new WildcardDns(logger, rootIpAddress, dnsIpAddresses, rootDomain, email, timeStamp, ttl, txtUrl);

                    await dns.Listen(logRequests, logErrors);
                    
                    logger.LogWarning("DNS has disconnected, waiting for 10 seconds to retry...");

                    await Task.Delay(10000);
                }
                catch(Exception ex)
                {
                    logger.LogError(ex,("Error occurred: " + ex.Message));
                    await Task.Delay(10000);
                }

            }
        }
        


    }
}

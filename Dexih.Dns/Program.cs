using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Dexih.Dns
{
    class Program
    {
        static void Main(string[] args)
        {
            var txtUrl = Environment.GetEnvironmentVariable("DNS_TXT_URL");
            var rootIpAddress = Environment.GetEnvironmentVariable("ROOT_IP_ADDRESS");
            var rootDomain = Environment.GetEnvironmentVariable("ROOT_DOMAIN");
            var dnsIpAddresses = Environment.GetEnvironmentVariable("DNS_IP_ADDRESS").Split(',');
            var email = Environment.GetEnvironmentVariable("DNS_EMAIL");
            var timeStamp = 2018040501; // long.Parse(DateTime.Now.ToString("yyyymmdd") + "00");
            var ttl = int.Parse(Environment.GetEnvironmentVariable("DNS_TTL")??"300");

            var logRequests = bool.Parse(Environment.GetEnvironmentVariable("LOG_REQUESTS")??"true");
            var logErrors = bool.Parse(Environment.GetEnvironmentVariable("LOG_ERRORS") ?? "true");

            Console.WriteLine("Starting the dns server...");
            
            while(true)
            {
                try
                {
                    var dns = new WildcardDns(rootIpAddress, dnsIpAddresses, rootDomain, email, timeStamp, ttl, txtUrl);
                    dns.Listen(logRequests, logErrors).Wait();
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine("Error occurred: " + ex.Message);
                }

            }
        }

    }
}

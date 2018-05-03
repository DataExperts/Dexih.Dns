using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dexih.Dns
{
    public class WildcardDns
    {
        private string _rootIpAddress;
        private string _rootDomain;
        private string[] _dnsIpAddresses;
        private string _email;
        private long _timeStamp;
        private int _ttl;
        private string _txtUrl;

        public WildcardDns(string rootIpAddress, string[] dnsIpAddresses, string rootDomain, string email, long timeStamp, int ttl, string txtUrl)
        {
            _rootIpAddress = rootIpAddress;
            _dnsIpAddresses = dnsIpAddresses;
            _rootDomain = rootDomain;
            _email = email;
            _timeStamp = timeStamp;
            _ttl = ttl;
            _txtUrl = txtUrl;
        }

        public async Task Listen(bool logRequests = true, bool logErrors = true)
        {
            // All dns requests received will be handled by the request resolver
            DnsServer server = new DnsServer(new RequestResolver(_rootIpAddress, _dnsIpAddresses, _rootDomain, _email, _timeStamp, _ttl, _txtUrl));

            if (logRequests)
            {
                // Log every request
                server.Requested += (request) => LogRequest(request);
                // On every successful request log the request and the response
                server.Responded += (request, response) => LogResponse(response);
            }

            if (logErrors)
            {
                // Log errors
                server.Errored += (e) => Console.WriteLine(e.Message);
            }

            await server.Listen();
        }

        public void LogRequest(IRequest request)
        {
            Console.WriteLine($"Request: Id:{request.Id}, Query: {request.Questions.Count()}.");
            foreach(var question in request.Questions)
            {
                Console.WriteLine($"  Question: {question.Name}\t{question.Type}");
            }
        }

        public void LogResponse(IResponse response)
        {
            Console.WriteLine($"Response: Id:{response.Id}, Query: {response.Questions.Count()}, Answers: {response.AnswerRecords.Count()}, Authority: {response.AuthorityRecords.Count()}, Additional: {response.AdditionalRecords.Count()}.");

            foreach (StartOfAuthorityResourceRecord auth in response.AuthorityRecords)
            {
                Console.WriteLine($"  SOA: {auth.Name}\t{auth.Type}\t{auth.MasterDomainName}\t{auth.ResponsibleDomainName}");
            }

            foreach (var record in response.AnswerRecords)
            {
                switch(record.Type)
                {
                    case RecordType.A:
                        IPAddressResourceRecord rec = (IPAddressResourceRecord) record;
                        Console.WriteLine($"  Answer: {record.Name}\t{record.Type}\t{rec.IPAddress}");
                        break;
                    case RecordType.NS:
                        NameServerResourceRecord nsrec = (NameServerResourceRecord)record;
                        Console.WriteLine($"  Answer: {record.Name}\t{record.Type}\t{nsrec.NSDomainName}");
                        break;
                    default:
                        Console.WriteLine($"  Answer: {record.Name}\t{record.Type}\t{record.Data}");
                        break;
                }
            }

            foreach (var record in response.AdditionalRecords)
            {
                Console.WriteLine($"  Additional: {record.Name}\t{record.Type}\t{Encoding.UTF8.GetString(record.Data)}");
            }

        }


    }
}

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
using Microsoft.Extensions.Logging;

namespace Dexih.Dns
{
    public class WildcardDns
    {
        private readonly string _rootIpAddress;
        private readonly string _rootDomain;
        private readonly string[] _dnsIpAddresses;
        private readonly string _email;
        private readonly long _timeStamp;
        private readonly int _ttl;
        private readonly string _txtUrl;
        private readonly ILogger _logger;
        

        public WildcardDns(ILogger logger, string rootIpAddress, string[] dnsIpAddresses, string rootDomain, string email, long timeStamp, int ttl, string txtUrl)
        {
            _logger = logger;
            
            if(string.IsNullOrEmpty(rootIpAddress) ||  !IPAddress.TryParse(rootIpAddress, out _))
            {
                throw new DnsException($"The Root IP Address {rootIpAddress} is invalid.");
            }

            if(dnsIpAddresses == null || dnsIpAddresses.Length == 0)
            {
                throw new DnsException($"There are no DNS IP Addresses set.");
            }

            foreach(var ipAddress in dnsIpAddresses)
            {
                if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
                {
                    throw new DnsException($"The DNS IP Address {ipAddress} is invalid.");
                }
            }

            if(string.IsNullOrEmpty(rootDomain) || Uri.CheckHostName(rootDomain) != UriHostNameType.Dns)
            {
                throw new DnsException($"The Root Domain {rootDomain} is invalid.");
            }

            if (string.IsNullOrEmpty(email) || Uri.CheckHostName(email) != UriHostNameType.Dns)
            {
                throw new DnsException($"The Email {email} is invalid.  Use the format username.gmail.com (rather than username@gmail.com)");
            }

            _rootIpAddress = rootIpAddress;
            _dnsIpAddresses = dnsIpAddresses;
            _rootDomain = rootDomain;
            _email = email;
            _timeStamp = timeStamp;
            _ttl = ttl;
            _txtUrl = txtUrl;
        }

        public async Task Listen(bool logRequests, bool logErrors)
        {
            // All dns requests received will be handled by the request resolver
            var server = new DnsServer(new RequestResolver(_logger, _rootIpAddress, _dnsIpAddresses, _rootDomain, _email, _timeStamp, _ttl, _txtUrl));

            if (logRequests)
            {
                // Log every request
                server.Requested += (sender, e) => LogRequest(e.Request);
                // On every successful request log the request and the response
                server.Responded += (sender, e) => LogResponse(e.Response);
            }

            if (logErrors)
            {
                // Log errors
                server.Errored += (sender, e) => _logger.LogError(e.Exception, $"Dns Server encountered error: {e.Exception?.Message} ");
            }

            await server.Listen();
        }

        public void LogRequest(IRequest request)
        {
            //Console.WriteLine($"Request: Id:{request.Id}, Operation: {request.OperationCode}, Query: {request.Questions.Count()}.");
            //foreach(var question in request.Questions)
            //{
            //    Console.WriteLine($"  Question: {question.Name}\t{question.Type}");
            //}
        }

        public void LogResponse(IResponse response)
        {
            _logger.LogInformation($"Response: Id:{response.Id}, Query: {response.Questions.Count()}, Answers: {response.AnswerRecords.Count()}, Authority: {response.AuthorityRecords.Count()}, Additional: {response.AdditionalRecords.Count()}.");

            foreach (var question in response.Questions)
            {
                _logger.LogInformation($"  Question: {question.Name}\t{question.Type}");
            }

            foreach (var auth in response.AuthorityRecords)
            {
                if (auth is StartOfAuthorityResourceRecord record)
                {
                    _logger.LogInformation($"  SOA: {record.Name}\t{record.Type}\t{record.MasterDomainName}\t{record.ResponsibleDomainName}");
                }
            }

            foreach (var record in response.AnswerRecords)
            {
                switch(record.Type)
                {
                    case RecordType.A:
                    case RecordType.AAAA:
                        IPAddressResourceRecord rec = (IPAddressResourceRecord) record;
                        _logger.LogInformation($"  Answer: {record.Name}\t{record.Type}\t{rec.IPAddress}");
                        break;
                    case RecordType.NS:
                        NameServerResourceRecord nsrec = (NameServerResourceRecord)record;
                        _logger.LogInformation($"  Answer: {record.Name}\t{record.Type}\t{nsrec.NSDomainName}");
                        break;
                    default:
                        _logger.LogInformation($"  Answer: {record.Name}\t{record.Type}\t{Encoding.UTF8.GetString(record.Data)}");
                        break;
                }
            }

            foreach (var record in response.AdditionalRecords)
            {
                _logger.LogInformation($"  Additional: {record.Name}\t{record.Type}\t{Encoding.UTF8.GetString(record.Data)}");
            }

        }


    }
}

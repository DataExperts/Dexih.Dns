using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dexih.Dns
{
    public class DnsService: IHostedService
    {
        private readonly ILogger<DnsService> _logger;
        private readonly Settings _settings;
        private readonly long _timeStamp;
        private readonly IHttpClientFactory _clientFactory;
        private DnsServer _server;

        public DnsService(IConfiguration configuration, ILogger<DnsService> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _settings = configuration.Get<Settings>();
            _timeStamp = long.Parse(DateTime.Now.ToString("yyyymmdd") + "00");
            _clientFactory = clientFactory;
            
            if(string.IsNullOrEmpty(_settings.AppSettings.RootIpAddress) ||  !IPAddress.TryParse(_settings.AppSettings.RootIpAddress, out _))
            {
                throw new DnsException($"The Root IP Address {_settings.AppSettings.RootIpAddress} is invalid.");
            }

            if(_settings.AppSettings.DnsIpAddresses == null || _settings.AppSettings.DnsIpAddresses.Length == 0)
            {
                throw new DnsException($"There are no DNS IP Addresses set.");
            }

            foreach(var ipAddress in _settings.AppSettings.DnsIpAddresses)
            {
                if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
                {
                    throw new DnsException($"The DNS IP Address {ipAddress} is invalid.");
                }
            }

            if(string.IsNullOrEmpty(_settings.AppSettings.RootDomain) || Uri.CheckHostName(_settings.AppSettings.RootDomain) != UriHostNameType.Dns)
            {
                throw new DnsException($"The Root Domain {_settings.AppSettings.RootIpAddress} is invalid.");
            }

            if (string.IsNullOrEmpty(_settings.AppSettings.DnsEmail) || Uri.CheckHostName(_settings.AppSettings.DnsEmail) != UriHostNameType.Dns)
            {
                throw new DnsException($"The Email {_settings.AppSettings.DnsEmail} is invalid.  Use the format username.gmail.com (rather than username@gmail.com)");
            }
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // All dns requests received will be handled by the request resolver
            _server = new DnsServer(new RequestResolver(
                _logger, _clientFactory,
                _settings.AppSettings.RootIpAddress, _settings.AppSettings.DnsIpAddresses, _settings.AppSettings.RootDomain, _settings.AppSettings.DnsEmail, _timeStamp, _settings.AppSettings.DnsTtl, _settings.AppSettings.DnsTxtUrl));

            if (_settings.Logging.LogRequests)
            {
                // Log every request
                _server.Requested += (sender, e) => LogRequest(e.Request);
                // On every successful request log the request and the response
                _server.Responded += (sender, e) => LogResponse(e.Response);
            }

            if (_settings.Logging.LogErrors)
            {
                // Log errors
                _server.Errored += (sender, e) => _logger.LogError(e.Exception, $"Dns Server encountered error: {e.Exception?.Message} ");
            }
            
            return _server.Listen();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Dispose();
            return Task.CompletedTask;
        }
        
        public void LogRequest(IRequest request)
        {
            _logger.LogDebug($"Request: Id:{request.Id}, Operation: {request.OperationCode}, Query: {request.Questions.Count()}.");
            foreach(var question in request.Questions)
            {
                _logger.LogDebug($"  Question: {question.Name}\t{question.Type}");
            }
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
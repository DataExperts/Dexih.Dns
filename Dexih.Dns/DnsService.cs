using System;
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
        private readonly DnsServer _server;
        private readonly IPEndPoint _ipEndPoint;
        
        private Task _listenTask;

        public DnsService(IConfiguration configuration, ILogger<DnsService> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            var settings = configuration.Get<Settings>();
            var timeStamp = long.Parse(DateTime.Now.ToString("yyyymmdd") + "00");

            var listenIpAddress = string.IsNullOrWhiteSpace(settings.AppSettings.ListenIpAddress)
                ? IPAddress.Any
                : IPAddress.Parse(settings.AppSettings.ListenIpAddress);

            _ipEndPoint = new IPEndPoint(listenIpAddress, settings.AppSettings.ListenPort);
            
            if(string.IsNullOrEmpty(settings.AppSettings.RootIpAddress) ||  !IPAddress.TryParse(settings.AppSettings.RootIpAddress, out _))
            {
                throw new DnsException($"The Root IP Address {settings.AppSettings.RootIpAddress} is invalid.");
            }

            if(settings.AppSettings.DnsIpAddresses == null || settings.AppSettings.DnsIpAddresses.Length == 0)
            {
                throw new DnsException($"There are no DNS IP Addresses set.");
            }

            foreach(var ipAddress in settings.AppSettings.DnsIpAddresses)
            {
                if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
                {
                    throw new DnsException($"The DNS IP Address {ipAddress} is invalid.");
                }
            }

            if(string.IsNullOrEmpty(settings.AppSettings.RootDomain) || Uri.CheckHostName(settings.AppSettings.RootDomain) != UriHostNameType.Dns)
            {
                throw new DnsException($"The Root Domain {settings.AppSettings.RootIpAddress} is invalid.");
            }

            if (string.IsNullOrEmpty(settings.AppSettings.DnsEmail) || Uri.CheckHostName(settings.AppSettings.DnsEmail) != UriHostNameType.Dns)
            {
                throw new DnsException($"The Email {settings.AppSettings.DnsEmail} is invalid.  Use the format username.gmail.com (rather than username@gmail.com)");
            }
            
            // All dns requests received will be handled by the request resolver
            _server = new DnsServer(new RequestResolver(
                _logger, clientFactory,
                settings.AppSettings.RootIpAddress, settings.AppSettings.DnsIpAddresses, settings.AppSettings.RootDomain, settings.AppSettings.DnsEmail, timeStamp, settings.AppSettings.DnsTtl, settings.AppSettings.DnsTxtUrl));

            if (settings.Logging.LogRequests)
            {
                // Log every request
                _server.Requested += (sender, e) => LogRequest(e.Request);
                // On every successful request log the request and the response
                _server.Responded += (sender, e) => LogResponse(e.Response);
            }

            if (settings.Logging.LogErrors)
            {
                // Log errors
                _server.Errored += (sender, e) => _logger.LogError(e.Exception, $"Dns Server encountered error: {e.Exception?.Message} ");
            }
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _listenTask = _server.Listen(_ipEndPoint);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(("Dns server is stopping."));
            _server.Dispose();
            return _listenTask;
        }

        private void LogRequest(IRequest request)
        {
            _logger.LogDebug($"Request: Id:{request.Id}, Operation: {request.OperationCode}, Query: {request.Questions.Count()}.");
            foreach(var question in request.Questions)
            {
                _logger.LogDebug($"  Question: {question.Name}\t{question.Type}");
            }
        }

        private void LogResponse(IResponse response)
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
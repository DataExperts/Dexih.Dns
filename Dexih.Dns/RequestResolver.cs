using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dexih.Dns
{
    class KeyValue
    {
        public string key { get; set; }
        public string value { get; set; }
    }
    
    public class RequestResolver : IRequestResolver
    {
        private readonly ConcurrentDictionary<string, IPAddress> _ipAddressRecords;
        private readonly string[] _rootDomainComponents;
        private readonly Domain[] _nsDomains;
        private readonly Domain _email;
        private readonly long _timeStamp;
        private readonly TimeSpan _ttl;
        private readonly string _txtUrl;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _clientFactory;

        public RequestResolver(ILogger logger, IHttpClientFactory clientFactory, string rootIpAddress, IReadOnlyList<string> dnsIpAddresses, string rootDomain, string email, long timeStamp, int ttl, string txtUrl)
        {
            _logger = logger;
            
            _ipAddressRecords = new ConcurrentDictionary<string, IPAddress>();

            if(!string.IsNullOrEmpty(rootIpAddress))
            {
                var rootIp = IPAddress.Parse(rootIpAddress);
                _ipAddressRecords.TryAdd("", rootIp);
                _ipAddressRecords.TryAdd("www", rootIp);
            }
            
            _rootDomainComponents = rootDomain.ToLower().Split('.');
            _nsDomains = new Domain[dnsIpAddresses.Count];

            for (var i = 0; i < dnsIpAddresses.Count; i++)
            {
                _nsDomains[i] = new Domain($"ns{i+1}.{rootDomain}");
                _ipAddressRecords.TryAdd($"ns{i + 1}", IPAddress.Parse(dnsIpAddresses[i]));
            }

            _email = new Domain(email.Replace('@', '.'));
            _timeStamp = timeStamp;
            _ttl = TimeSpan.FromSeconds(ttl);
            _txtUrl = txtUrl;
            _clientFactory = clientFactory;
        }

        // A request resolver that resolves all dns queries to localhost
        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                IResponse response = Response.FromRequest(request);

                foreach (Question question in response.Questions)
                {
                    if (question.Type == RecordType.SOA || question.Type == RecordType.NS)
                    {
                        var record = new StartOfAuthorityResourceRecord(question.Name, _nsDomains[0], _email,
                            _timeStamp, _ttl, _ttl, _ttl, _ttl, _ttl);
                        response.AuthorityRecords.Add(record);
                        
                        if (question.Type == RecordType.NS)
                        {
                            foreach (var t in _nsDomains)
                            {
                                response.AnswerRecords.Add(new NameServerResourceRecord(question.Name, t, _ttl));
                            }
                        }
                    }

                    if (question.Type == RecordType.TXT)
                    {
                       
                        if (!string.IsNullOrEmpty(_txtUrl))
                        {
                            var httpClient = _clientFactory.CreateClient();
                            // var requestMessage = new HttpRequestMessage(HttpMethod.Get, _txtUrl);
                            var txtResponse = await httpClient.GetAsync(_txtUrl, cancellationToken);
                            List<KeyValue> txtValues = null;
                            if (txtResponse.IsSuccessStatusCode)
                            {
                                // var jsonString = await txtResponse.Content.ReadAsStringAsync();
                                txtValues = await JsonSerializer.DeserializeAsync<List<KeyValue>>(await txtResponse.Content.ReadAsStreamAsync(), cancellationToken: cancellationToken);
                            }

                            if (txtValues != null)
                            {
                                foreach (var keyValue in txtValues)
                                {
                                    if (question.Name.ToString().ToLower().EndsWith(keyValue.key))
                                    {
                                        IList<CharacterString> characterStrings = new List<CharacterString> {new CharacterString(keyValue.value)};
                                        response.AnswerRecords.Add(new TextResourceRecord(question.Name, characterStrings, _ttl));
                                    }
                                    else
                                    {
                                        response.AnswerRecords.Add(new TextResourceRecord(question.Name, keyValue.key, keyValue.value, _ttl));
                                    }
                                }
                            }
                        }
                    }

                    var name = question.Name.ToString().ToLower().Split('.');

                    //check the base domain is the same.
                    if (name.Length >= _rootDomainComponents.Length && name
                            .Skip(name.Length - _rootDomainComponents.Length).SequenceEqual(_rootDomainComponents))
                    {
                        // match any static A records.
                        var key = string.Join('.', name.Take(name.Length - _rootDomainComponents.Length));
                        if (_ipAddressRecords.TryGetValue(key, out var ipAddress))
                        {
                            response.AnswerRecords.Add(new IPAddressResourceRecord(question.Name, ipAddress, _ttl));
                        }

                        // query the domain to determine ip.  Format 1-2-3-4.hash.dexih.com
                        var domain = question.Name.ToString().Split(".");
                        if (domain.Length == 4)
                        {
                            if (IPAddress.TryParse(domain[0].Replace('-', '.'), out var iPAddress))
                            {
                                var record = new IPAddressResourceRecord(question.Name, iPAddress, _ttl);
                                response.AnswerRecords.Add(record);
                            }
                        }
                    }
                }

                return response;
            }
            catch (Exception e)
            {
                 _logger?.LogError(e, $"The following error was encountered: {e.Message}.");
                 var response = new Response {ResponseCode = ResponseCode.NoError};
                 return response;
            }

        }
        
    }
}

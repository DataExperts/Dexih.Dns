using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dexih.Dns
{
    public class RequestResolver : IRequestResolver
    {
        private ConcurrentDictionary<string, IPAddress> _ipAddressRecords;
        private Domain _rootDomain;
        private string[] _rootDomainComponents;
        private Domain[] _nsDomains;
        private Domain _email;
        private IPAddress[] _dnsIpAddresses;
        private long _timeStamp;
        private TimeSpan _ttl;

        public RequestResolver(string rootIpAddress, string[] dnsIpAddresses, string rootDomain, string email, long timeStamp, int ttl)
        {
            _ipAddressRecords = new ConcurrentDictionary<string, IPAddress>();

            if(!string.IsNullOrEmpty(rootIpAddress))
            {
                var rootIp = IPAddress.Parse(rootIpAddress);
                _ipAddressRecords.TryAdd("", rootIp);
                _ipAddressRecords.TryAdd("www", rootIp);
            }
            
            _rootDomain = new Domain(rootDomain);
            _rootDomainComponents = rootDomain.Split('.');

            _dnsIpAddresses = dnsIpAddresses.Select(c => IPAddress.Parse(c)).ToArray();

            _nsDomains = new Domain[dnsIpAddresses.Length];
            for (var i = 0; i < dnsIpAddresses.Length; i++)
            {
                _nsDomains[i] = new Domain($"ns{i+1}.{rootDomain}");
                _ipAddressRecords.TryAdd($"ns{i + 1}", IPAddress.Parse(dnsIpAddresses[i]));
            }

            _email = new Domain(email.Replace('@', '.'));
            _timeStamp = timeStamp;
            _ttl = TimeSpan.FromSeconds(ttl);
        }

        // A request resolver that resolves all dns queries to localhost
        public Task<IResponse> Resolve(IRequest request)
        {
            IResponse response = Response.FromRequest(request);

            foreach (Question question in response.Questions)
            {
                if (question.Type == RecordType.SOA || question.Type == RecordType.NS)
                {
                    var record = new StartOfAuthorityResourceRecord(question.Name, _nsDomains[0], _email, _timeStamp, _ttl, _ttl, _ttl, _ttl, _ttl);
                    response.AuthorityRecords.Add(record);
                }

                if (question.Type == RecordType.NS)
                {
                    for (var i = 0; i < _nsDomains.Length; i++)
                    {
                        response.AnswerRecords.Add(new NameServerResourceRecord(_nsDomains[i], _nsDomains[i], _ttl));
                    }
                }

                var name = question.Name.ToString().Split('.');

                //check the base domain is the same.
                if (name.Length >= _rootDomainComponents.Length && name.Skip(name.Length - _rootDomainComponents.Length).SequenceEqual(_rootDomainComponents))
                {
                    // match any static A records.
                    var key = string.Join('.', name.Take(name.Length - _rootDomainComponents.Length));
                    if(_ipAddressRecords.TryGetValue(key, out var ipAddress))
                    {
                        response.AnswerRecords.Add(new IPAddressResourceRecord(new Domain(string.Join('.', name)), ipAddress, _ttl));
                    }

                    // query the domain to determine ip.  Format 1-2-3-4.hash.dexih.com
                    var domain = question.Name.ToString().Split(".");
                    if (domain.Length == 4)
                    {
                        if (IPAddress.TryParse(domain[0].Replace('-', '.'), out var iPAddress))
                        {
                            IResourceRecord record = new IPAddressResourceRecord(question.Name, iPAddress, _ttl);
                            response.AnswerRecords.Add(record);
                        }
                    }
                }
            }

            return Task.FromResult(response);
        }
    }
}

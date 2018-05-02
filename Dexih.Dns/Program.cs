using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Dexih.Dns
{
    class Program
    {
        static DnsClient Client;

        static void Main(string[] args)
        {
            Client = new DnsClient("8.8.8.8");

            // All dns requests received will be handled by the localhost request resolver
            DnsServer server = new DnsServer(new LocalRequestResolver());

            //MasterFile masterFile = new MasterFile();
            //DnsServer server = new DnsServer(masterFile, "8.8.8.8");
            //masterFile.Add(new StartOfAuthorityResourceRecord(new Domain("dexih.com"), new Domain("ns2.dataexpertsgroup.com"), new Domain("ns2.dataexpertsgroup.com")));

            // Log every request
            server.Requested += (request) => Console.WriteLine(request);
            // On every successful request log the request and the response
            server.Responded += (request, response) => Console.WriteLine("{0} => {1}", request, response);
            // Log errors
            server.Errored += (e) => Console.WriteLine(e.Message);

            server.Listen().Wait();
        }

        // A request resolver that resolves all dns queries to localhost
        public class LocalRequestResolver : IRequestResolver
        {
            public async Task<IResponse> Resolve(IRequest request)
            {
                IResponse response = Response.FromRequest(request);

                foreach (Question question in response.Questions)
                {
                    if (question.Type == RecordType.SOA || question.Type == RecordType.NS)
                    {
                        var record = new StartOfAuthorityResourceRecord(new Domain("dataexpertsgroup.com"), new Domain("dexih-ns1.dataexpertsgroup.com"), new Domain("gholland.dataexpertsgroup.com"), 2018040145, TimeSpan.FromSeconds(3000), TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(300));
                        response.AuthorityRecords.Add(record);
                    }
                    if (question.Type == RecordType.A)
                    {
                        var dynamic = false;
                        var name = question.Name.ToString().Split(".");

                        if (name.Length == 4 && name[3] == "com" && name[2] == "dexih")
                        {
                            if (IPAddress.TryParse(name[0].Replace('-', '.'), out var iPAddress))
                            {
                                IResourceRecord record = new IPAddressResourceRecord(question.Name, iPAddress);
                                response.AnswerRecords.Add(record);
                                dynamic = true;
                            }
                        }

                        if (!dynamic)
                        {
                            if (question.Name.ToString() == "dexih-ns1.dataexpertsgroup.com")
                            {
                                IResourceRecord record = new IPAddressResourceRecord(question.Name, IPAddress.Parse("60.224.193.14"));
                                response.AnswerRecords.Add(record);
                            }
                            else
                            {
                                var clientResponse = await Client.Resolve(question.Name, RecordType.A);
                                response.AnswerRecords.Add(clientResponse.AnswerRecords[0]);
                            }
                        }
                    }
                }

                return response;
            }
        }
    }
}

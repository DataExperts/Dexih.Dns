using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using Xunit;

namespace Dexih.Dns.Tests
{
    public class UnitTest1
    {
        private const string RootAddress = "10.10.10.10";
        
        [Fact]
        public void Test_Root_A_Request()
        {
            var requestResolver = new RequestResolver(null, null, RootAddress, new [] {"20.20.20.20"}, "dexih.com", "gholland@dataexpertsgroup.com", 123, 60, "abc");
            
            var question = new Question(new Domain("dexih.com"), RecordType.A, RecordClass.IN);

            var request = new Request(new Header(), new List<Question>() {question}, new List<IResourceRecord>());
            var resolve = requestResolver.Resolve(request).Result;
            
            Assert.Equal(1, resolve.AnswerRecords.Count);

            Assert.IsType<IPAddressResourceRecord>(resolve.AnswerRecords[0]);

            var record = (IPAddressResourceRecord) resolve.AnswerRecords[0];
            Assert.Equal(RootAddress, record.IPAddress.ToString());
        }
        
        [Fact]
        public void Test_Dynamic_A_Request()
        {
            var requestResolver = new RequestResolver(null, null,RootAddress, new [] {"20.20.20.20"}, "dexih.com", "gholland@dataexpertsgroup.com", 123, 60, "abc");
            
            var question = new Question(new Domain("127-0-0-1.abc.dexih.com"), RecordType.A, RecordClass.IN);

            var request = new Request(new Header(), new List<Question>() {question}, new List<IResourceRecord>());
            var resolve = requestResolver.Resolve(request).Result;
            
            Assert.Equal(1, resolve.AnswerRecords.Count);

            Assert.IsType<IPAddressResourceRecord>(resolve.AnswerRecords[0]);

            var record = (IPAddressResourceRecord) resolve.AnswerRecords[0];
            Assert.Equal("127.0.0.1", record.IPAddress.ToString());
        }

        // [Fact]
        // public async void Test_RunOne()
        // {
        //     var runOnce = new RunOnce<int>();
        //
        //     var count = 0;
        //     
        //     Parallel.For(0, 10, i =>
        //     {
        //         // should only increment the first time, and cache subsequent requests
        //         runOnce.RunAsync(async () =>
        //         {
        //             count++;
        //             await Task.Delay(1000);
        //             return count;
        //         });
        //         
        //         Assert.Equal(1, count);
        //     });
        //
        //     // delay to let the original task finish, and then run again
        //     await Task.Delay(2000);
        //     var value = await runOnce.RunAsync(async () =>
        //     {
        //         count++;
        //         await Task.Delay(1000);
        //         return count;
        //     });
        //     
        //     Assert.Equal(2, value);
        //     
        // }

        [Fact]
        public async void Test_Txt_Request()
        {
            var url = "https://dexih.dataexpertsgroup.com";

            var httpRequest = new HttpClient();
            await httpRequest.GetAsync($"{url}/api/Remote/AddTxtRecord");
            
            var requestResolver = new RequestResolver(null, new DefaultHttpClientFactory(),RootAddress, new [] {"20.20.20.20"}, "dexih.com", "gholland@dataexpertsgroup.com", 123, 60, $"{url}/api/Remote/GetTxtRecords");
            
            var domain = new Domain("dexih.com");
            
            var question = new Question(domain, RecordType.TXT, RecordClass.IN);
            var request = new Request(new Header(), new List<Question>() {question}, new List<IResourceRecord>());
            var resolve = await requestResolver.Resolve(request);

            var txtRecord = resolve.AnswerRecords.Where(c => c.Type == RecordType.TXT);
            
            Assert.Equal(1, txtRecord.Count());

            var value = Encoding.Default.GetString(txtRecord.First().Data);
            
            Assert.Equal("sample value", value.Trim());
        }
    }
    
    public sealed class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
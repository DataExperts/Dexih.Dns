using System;
using System.Collections.Generic;
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
        // public void Test_Txt_Request()
        // {
        //     var requestResolver = new RequestResolver(null, null,RootAddress, new [] {"20.20.20.20"}, "dexih.com", "gholland@dataexpertsgroup.com", 123, 60, "http://dexih.dataexpertsgroup.com/api/Remote/GetTxtRecords");
        //     
        //     var domain = new Domain("dexih.com");
        //     
        //     var question = new Question(domain, RecordType.TXT, RecordClass.IN);
        //     var request = new Request(new Header(), new List<Question>() {question}, new List<IResourceRecord>() {});
        //     var resolve = requestResolver.Resolve(request).Result;
        //     
        //     Assert.Equal(1, resolve.AnswerRecords.Count);
        // }
    }
}
using System;

namespace Dexih.Dns
{
    public class DnsException: Exception
    {
        public DnsException(string message) : base(message)
        {
        }
        public DnsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

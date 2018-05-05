using System;
using System.Collections.Generic;
using System.Text;

namespace Dexih.Dns
{
    public class DnsException: Exception
    {
        public DnsException() : base()
        {
        }
        public DnsException(string message) : base(message)
        {
        }
        public DnsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

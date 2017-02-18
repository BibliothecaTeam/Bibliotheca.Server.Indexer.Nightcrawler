using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions
{
    public class GatewayServiceNotAvailableException : Exception
    {
        public GatewayServiceNotAvailableException(string message) : base(message)
        {
        }
    }
}

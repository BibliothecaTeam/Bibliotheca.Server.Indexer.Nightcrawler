using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions
{
    public class DowonloadDocumentContentException : Exception
    {
        public DowonloadDocumentContentException(string message) : base(message)
        {
        }
    }
}

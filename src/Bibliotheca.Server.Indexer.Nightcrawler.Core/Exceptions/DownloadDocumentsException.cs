using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions
{
    public class DownloadDocumentsException : Exception
    {
        public DownloadDocumentsException(string message) : base(message)
        {
        }
    }
}

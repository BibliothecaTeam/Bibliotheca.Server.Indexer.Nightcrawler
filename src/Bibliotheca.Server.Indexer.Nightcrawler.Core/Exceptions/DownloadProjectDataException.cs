using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions
{
    public class DownloadProjectDataException : Exception
    {
        public DownloadProjectDataException(string message) : base(message)
        {
        }
    }
}

using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions
{
    public class RemoveIndexException : Exception
    {
        public RemoveIndexException(string message) : base(message)
        {
        }
    }
}

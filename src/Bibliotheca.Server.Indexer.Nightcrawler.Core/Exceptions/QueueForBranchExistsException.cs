using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions
{
    public class QueueForBranchExistsException : Exception
    {
        public QueueForBranchExistsException(string message) : base(message)
        {
        }
    }
}

using System;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions
{
    public class UploadDocumentToIndexException : Exception
    {
        public UploadDocumentToIndexException(string message) : base(message)
        {
        }
    }
}
